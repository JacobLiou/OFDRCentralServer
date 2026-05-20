# OFDRCentralControlServer — 项目上下文

## 项目概述

OFDR（Optical Frequency Domain Reflectometry）中控服务器是一个运行在工厂内网的 **设备共享服务**。  
它将一台 FLA（Fiber Length Analyzer）光纤测量仪器和一组光开关作为共享资源，向多个客户端（测试工位 PC/上位机）暴露统一的 TCP 接口。  
客户端通过提交测量任务进行排队，服务器 FIFO 串行驱动硬件，最大化设备利用率并避免并发冲突。

**典型业务场景**：  
工厂产线有 16 个光纤测试工位，每个工位的 PC 连接到该服务器，提交 `scan` / `zero` / `auto_peak` 任务。服务器通过光开关将 FLA 接到对应通道，完成测量后将结果推回给该工位的客户端。

---

## 技术栈

| 层次 | 技术 |
|------|------|
| 运行时 | .NET 8，C# 13，顶级语句入口 |
| TCP 服务 | `System.Net.Sockets.TcpListener`，每连接 fire-and-forget 协程 |
| 高性能备选 | `FastTcpServer`：`System.IO.Pipelines` + `Socket.AcceptAsync` |
| 任务队列 | `System.Threading.Channels.Channel<MeasureTask>`（Unbounded，多读多写） |
| 设备通信 | FLA 仪器：TCP 4300（私有文本帧协议）；光开关：`System.IO.Ports.SerialPort` RS232 |
| 并发保护 | `SemaphoreSlim(1,1)` 保护设备独占访问；写锁保护 TCP 会话 |
| 结果缓存 | `Microsoft.Extensions.Caching.Memory.MemoryCache`（1h TTL，上限 10000 条） |
| 配置 | `ServerConfig.json`（业务配置）+ `appsettings.json`（Serilog） |
| 日志 | Serilog，Console + File（按日滚动，保留 14 天），含线程 ID 富化 |
| 发布 | `PublishSingleFile=true`，`SelfContained=true`，目标 `win-x64` |
| 依赖包 | Serilog 3.x / Sinks.Console / Sinks.File / Settings.Configuration / Enrichers.Thread；Microsoft.Extensions.Caching.Memory 8.x；System.IO.Ports 8.x；System.IO.Pipelines 8.x |

---

## 目标

1. **设备共享**：一台 FLA 仪器服务多个工位，避免重复购买。
2. **公平排队**：FIFO 队列，先提交先服务，每个任务串行执行。
3. **可观测**：Serilog 结构化日志，全链路追踪 TaskId、ClientId。
4. **可维护**：Mock 模式与 Real 模式一键切换，便于离线调试。
5. **稳定性**：设备操作全部带重试（指数退避 + Jitter），断线重连，读超时后保持连接。
6. **轻量部署**：单文件自包含可执行文件，Windows 服务或后台进程启动。

---

## 约束

- **无 ASP.NET / HTTP**：纯 TCP，JSON-Lines 协议，避免引入 Web 框架依赖。
- **单设备串行**：`_deviceLock`（SemaphoreSlim）保证任意时刻只有一个任务在驱动硬件，不可并行化。
- **ASCII 协议**：FLA 仪器和光开关均使用 ASCII 文本协议，不使用二进制帧。
- **Windows 部署**：SerialPort（COM 口）依赖 Windows；目标运行时 `win-x64`。
- **无持久化**：测量结果仅缓存在内存（MemoryCache 1h TTL），重启后丢失；如需持久化需另行扩展。
- **无鉴权**：当前版本无 Token/TLS，仅限内网使用。
- **不引入 IoC 容器**：保持最小依赖，`new` 直接注入，不引入 DI 框架。

---

## 代码规范

### 命名
- 类、接口、方法、属性：`PascalCase`
- 私有字段：`_camelCase`（前缀下划线）
- 常量：`UPPER_SNAKE_CASE`（如 `RETRY_SWITCH_MAX`）
- 局部变量、参数：`camelCase`
- 接口以 `I` 开头：`IFlaDeviceCommunicator`, `IOpticalSwitchController`
- Mock 类以 `Mock` 结尾：`FlaInstrumentAdapterMock`, `OpticalSwitchControllerMock`

### 文件组织
```
OFDRCentralControlServer/
├── Program.cs              # 顶级语句入口：配置、DI（手动）、启动
├── ConfigLoader.cs         # ServerConfig 加载/保存
├── appsettings.json        # Serilog 配置
├── ServerConfig.json       # 业务运行时配置（不提交敏感值）
├── Core/                   # 基础设施：服务器、会话、队列、Worker、状态追踪
├── Devices/                # 设备驱动：FlaInstrumentAdapter, OpticalSwitchController
├── Interfaces/             # 抽象接口（用于 Mock 替换）
├── Models/                 # 数据传输对象（DTO）和枚举
└── Protocol/               # FLA 专有文本帧的编解码
```

### 日志规范
- 使用 `Serilog.Log.ForContext<T>()` 获取类型化 Logger，**不使用** 全局 `Log.XXX`（顶层除外）。
- 消息模板使用结构化占位符：`Log.Information("Task start {TaskId} ch={Channel}", task.TaskId, task.ClientId)`
- 级别约定：`Debug` = 内部帧/协议细节；`Information` = 业务里程碑；`Warning` = 可恢复的异常情况；`Error` = 需要关注的错误；`Fatal` = 无法恢复，触发退出。

### 异步规范
- 所有 I/O 操作必须 `async/await`，不阻塞线程池。
- `CancellationToken ct` 必须贯穿所有 async 调用链。
- `Task.Run` 仅用于 CPU 密集型，I/O 场景直接 `await`。
- fire-and-forget 协程（如 `HandleClientAsync`）用 `_ = Task.Run(...)` 形式，并在内部捕获所有异常。

### 重试模式
设备操作统一使用 `RetryAsync` 辅助方法（在 `MeasurementWorker` 中定义），指数退避 + Jitter：
```csharp
await RetryAsync(
    async () => await _switch.SwitchToOutputMSWAsync(task.ClientId, ct),
    "switch", RETRY_SWITCH_MAX, BASE_DELAY_SWITCH, ct, Log,
    failDetail: $"channel={task.ClientId}"
);
```
重试常量统一在类顶部以 `const int RETRY_XXX_MAX` 定义。

---

## 架构设计

### 整体数据流

```
Client (TCP) ──JSON-Line──► TcpServer.HandleClientAsync
                                │
                                ▼
                        Channel<MeasureTask>  (FIFO 队列)
                                │
                                ▼
                        MeasurementWorker.StartAsync
                                │
                    ┌───────────┴────────────┐
                    ▼                        ▼
          OpticalSwitchController    FlaInstrumentAdapter
          (RS232 / MSW 指令)         (TCP 4300 / SCAN+OP 帧)
                    │                        │
                    └───────────┬────────────┘
                                │
                                ▼
                        TcpServer.SendResultAsync
                                │
                                ▼
                        Client (TCP) ◄──JSON-Line── result
```

### 关键组件

#### TcpServer
- 监听 `0.0.0.0:{ListenPort}`，每个连接生成 `ClientSession`，fire-and-forget `HandleClientAsync`。
- 读超时（10s）时发送 `{"command":"ping"}` 保活，`null` 行才视为断开。
- `SendResultAsync(task, message, ct)`：通过 `session.SendAsync` 将结果推回给任务发起方。
- 会话集合用 `List<ClientSession>` + `lock(_lock)` 管理。

#### ClientSession
- 包装 `TcpClient`，内置 ASCII `StreamReader/Writer`。
- 写操作由 `SemaphoreSlim _writeLock` 序列化，防止并发写乱序。
- `NoDelay = true`，`ReceiveTimeout/SendTimeout = 10000`。

#### MeasurementWorker / MeasurementWorkerMock
- 单循环消费 `Channel<MeasureTask>`，`_deviceLock.WaitAsync` 保证串行。
- 任务流程：切光开关 → 调用 FLA 测量 → 构建结果 → `SendResultAsync`。
- 支持三种测量模式：`scan`（测长）、`zero`（归零）、`auto_peak`（自动寻峰）。

#### FlaInstrumentAdapter
- TCP 连接到 FLA 仪器（默认 `192.168.1.1:4300`）。
- 握手：等待设备回复 `OCI`。
- 发送 `SCAN_...._NACS` 帧（由 `SendFramer.Frame()` 生成，含校验和）。
- 接收 `OP_..._PO` 帧（由 `FrameParser.TryParseFrame()` 解析，含校验验证）。
- 归零（`ZeroLengthAsync`）：自动寻峰，取最后一个峰位为基准线长。
- 测长（`ScanLengthAsync`）：以归零线长为起点，再次自动寻峰，取最大峰位。

#### OpticalSwitchController
- RS232 串口（默认 `COM3`，`115200 8N1`），ASCII + `\r\n` 终止符。
- 使用级联光开关（SW1 + SW9/SW10），`_commandMap` 映射 clientId(1-16) → MSW 指令串。
- `SwitchToOutputMSWAsync(clientId, ct)`：查表发送对应 MSW 指令。
- 通道映射（SW9，端口 1-8 对应 L3-4 到 L2-1）。

#### RunningTaskTracker（单例）
- `ConcurrentDictionary<string, byte>` 追踪任务状态（0=queued, 1=running）。
- 提供 `MarkQueued / MarkRunning / MarkFinished / IsQueued / IsRunning`。

#### HourlyResultStore（单例）
- `MemoryCache` 保存 `ResultMessage`，TTL 1h，上限 10000 条。
- 提供 `AddOrUpdate / TryGet / Remove`，支持按需查询结果。

---

## 通信协议（JSON-Lines）

每条消息为一行 JSON，`\n` 分隔，ASCII 编码。

### 客户端 → 服务端

```jsonc
// 提交测量任务
{"command":"submit","clientId":3,"mode":"scan","params":{"zero_length":"5.0"}}
{"command":"submit","clientId":3,"mode":"zero","params":{}}
{"command":"submit","clientId":3,"mode":"auto_peak","params":{"start_m":"0.5","end_m":"25","id":"12","sn":"SN9II1"}}

// 查询任务状态
{"command":"status","taskId":"T2025..."}

// 查询任务结果
{"command":"result","taskId":"T2025..."}
```

### 服务端 → 客户端

```jsonc
// ACK（任务已入队）
{"command":"ack","taskId":"T2025...","clientId":3,"mode":"scan"}

// 状态推送
{"command":"status","taskId":"T2025...","status":"running"}

// 结果推送（scan）
{"command":"result","taskId":"T2025...","status":"success",
 "data":{"clientId":3,"mode":"scan","length_m":21.45,"peak_db":-48.3}}

// 结果推送（auto_peak）
{"command":"result","taskId":"T2025...","status":"success",
 "data":{"clientId":3,"mode":"auto_peak","peak_pos_m":13.226,"peak_db":-47.82,"id":"12","sn":"SN9II1"}}

// 失败
{"command":"result","taskId":"T2025...","status":"failed","error":"Switch timeout"}

// 心跳
{"command":"ping"}
```

### TaskId 格式
`T` + `yyyyMMddHHmmss` + `fff` + `_` + `clientId`，例如：`T20250520143022345_3`

### ResultStatus 枚举
`queued` → `running` → `success` | `failed` | `expired`

---

## FLA 仪器帧协议

### 发送帧（SCAN 指令）
```
SCAN_{start}_{end}_{count}_{algo}_{width}_{thr}_{id}_{sn}_{checksum}_NACS
```
示例：`SCAN_0.0_30.0_100_2_0.5_-80_12_SN9II1_123.456_NACS`

校验和 = Σ|数值参数| + Σ(SN 各数字位)，由 `SendFramer.Frame()` 计算。

### 接收帧（OP 结果）
```
OP_{pos1}_{pos2}..._{db1}_{db2}..._{id}_{sn}_{checksum}_PO
```
示例：`OP_21.275_-57.128_9_SN9II1_97.404_PO`

正值 → 峰位（米），负值 → 强度（dBm），由 `FrameParser.TryParseFrame()` 解析并验证校验和。

### 默认参数（`Protocol/Const.cs`）
| 参数 | 默认值 | 含义 |
|------|--------|------|
| `DEFAULT_START` | `"0.0"` | 扫描起点（m） |
| `DEFAULT_ALGO` | `"2"` | 算法 |
| `DEFAULT_WIDTH` | `"0.5"` | 峰宽（m） |
| `DEFAULT_THRESHOLD` | `"-80"` | 峰值门限（dB） |
| `DEFAULT_ID` | `"12"` | 仪器 ID |
| `DEFAULT_SN` | `"SN9II1"` | 仪器序列号 |
| `SCAN_END_MARGIN_M` | `5.0` | 扫描终点安全裕度（m） |

---

## 光开关通道映射

级联拓扑：BB光源 → SW1（1×2）→ SW9（1×8）或 SW10（1×8）→ 16 路光纤。

| ClientId | MSW 指令 | 标签 |
|---------|---------|------|
| 1 | `MSW 1,1,2;9,1,1;` | L3-4 |
| 2 | `MSW 1,1,2;9,1,2;` | L3-3 |
| 3-8 | SW9 端口 3-8 | L3-2 … L2-1 |
| 9-16 | `MSW 1,1,2;10,1,1~8;` | SW10 通道 1-8 |

---

## 配置说明

### ServerConfig.json
```json
{
  "RunMode": "mock",        // "real" | "mock"
  "ListenPort": 5600,
  "FlaHost": "192.168.1.1",
  "FlaPort": 4300,
  "SwitchCom": "COM3",
  "SwitchBaud": 115200,
  "SwitchIndex": 1,
  "SwitchInput": 1,
  "ZeroEnd": 100.0,         // 归零扫描最大范围（m）
  "ScanEnd": 300.0          // 测长扫描最大范围（m）
}
```

### appsettings.json
Serilog 配置，MinimumLevel Default=Debug，写入 Console 和 `logs/server-YYYYMMDD.log`，保留 14 天。

---

## 运行与构建

```bash
# 开发运行（mock 模式，需 .NET 8 SDK）
cd OFDRCentralControlServer
dotnet run

# 指定端口
dotnet run -- 5800

# 发布单文件（win-x64）
dotnet publish -c Release -r win-x64

# 后台无窗口启动（Windows）
start /b OFDRCentralControlServer.exe >nul 2>&1
```

---

## 扩展指引

- **新增测量模式**：在 `MeasurementWorker` 和 `MeasurementWorkerMock` 的 `if/else if` 链中添加新 mode 分支。
- **更换设备驱动**：实现 `IFlaDeviceCommunicator` 或 `IOpticalSwitchController`，在 `Program.cs` 替换构造参数。
- **持久化结果**：扩展 `HourlyResultStore`，在 `AddOrUpdate` 中异步写入 SQLite/文件。
- **鉴权**：在 `TcpServer.HandleClientAsync` 的协议处理入口处增加 Token 校验步骤。
- **指标监控**：在 `MeasurementWorker` 任务完成后上报 Prometheus/OpenTelemetry 计数器。
