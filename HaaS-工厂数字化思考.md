# HaaS（硬件即服务）与工厂数字化：从 OFDR 共享测长到智能调度平台

> 基于当前 `OFDRCentralControlServer` 设备共享实践的延伸思考  
> 文档性质：战略/架构调研备忘，非实施规格  
> 更新日期：2026-08-12

---

## 1. 一句话结论

当前 OFDR 中控服务器已经是 **HaaS 的最小可行单元（MVP）**：把昂贵、独占、协议异构的硬件，封装成「可排队、可调用、可观测」的共享能力。工厂数字化要做的，不是再买一套 MES 贴标签，而是把这种模式 **从单仪器扩展到全厂设备池**，用统一的任务、状态、计费与调度语义，让产线「按需用设备、按结果付价值」。

---

## 2. 从本项目看见的「种子」

### 2.1 业务本质

| 维度 | OFDR 中控现状 | HaaS 抽象 |
|------|---------------|-----------|
| 资产 | 1 台 FLA + 光开关矩阵 | 稀缺/高价值设备池 |
| 消费者 | 16 个工位 PC | 多产线 / 多租户 / 多工艺 |
| 接入 | TCP JSON-Lines | 统一 Device API / 任务总线 |
| 互斥 | `SemaphoreSlim` 串行独占 | 资源锁 + 能力声明 + SLA |
| 编排 | FIFO `Channel<MeasureTask>` | 优先级 / 截止时间 / 亲和性调度 |
| 结果 | MemoryCache 1h | 可追溯结果仓 + 质量闭环 |
| 运维 | Mock/Real、重试、日志 | 远程运维、预测维护、计费结算 |

### 2.2 已经做对的事

1. **能力抽象**：客户端不直接碰仪器协议，只提交 `scan` / `zero` / `auto_peak`。
2. **资源互斥**：硬件天然不可并行，用锁保证正确性，用队列提高利用率。
3. **边侧轻量**：无 Web 框架、单文件部署，贴近工厂内网与工控机现实。
4. **可替换驱动**：`IFlaDeviceCommunicator` / `IOpticalSwitchController` + Mock，为多设备适配留下缝。

### 2.3 与「宏大 HaaS」之间的鸿沟

| 缺口 | 说明 |
|------|------|
| 单设备类型 | 仅 OFDR 测长链路，未覆盖光谱仪、功率计、温箱、PLC 工站等 |
| 调度过简 | FIFO，无优先级、截止时间、多资源联合预约 |
| 无计费/SLA | 共享了设备，但尚未「服务化」到商务与考核层 |
| 无统一数据面 | 结果在内存，难对接 MES/QMS/追溯 |
| 无多站点 | 一台中控 = 一个岛，缺跨车间/跨厂联邦 |
| 安全模型 | 内网无鉴权，规模化后不可接受 |

**判断**：技术上应从「单资源队列服务器」演进为「设备能力平台」；商业上应从「省一台仪器」演进为「按测量次数 / 按可用时间 / 按合格产出」售卖能力。

---

## 3. HaaS 到底是什么（避免概念混淆）

市场上至少有三层含义，必须分清：

```
┌─────────────────────────────────────────────────────────────┐
│  L3 商业模式层：EaaS / Pay-per-Use / Outcome-based           │
│      所有权在服务方，客户为「开机小时 / 零件数 / 可用率」付费   │
├─────────────────────────────────────────────────────────────┤
│  L2 平台能力层：设备即能力（Device-as-Capability）             │
│      统一接入、排队调度、数字孪生、OEE、预测维护、开放 API     │
├─────────────────────────────────────────────────────────────┤
│  L1 工程实现层：本项目所在位置                                 │
│      协议适配、独占锁、任务队列、结果回推                      │
└─────────────────────────────────────────────────────────────┘
```

- **IT 语境的 HaaS**：租 PC、服务器、网络设备（IBM 等强调的 MSP 订阅）。
- **工业语境的 EaaS / Machine-as-a-Service**：机床、激光器、机器人按产出或可用时间计费（TRUMPF、Formic）。
- **工厂数字化语境的「设备共享服务」**（本项目路径）：产线内把测试/加工资源池化，软件完成调度与计量——**不一定立刻改变资产所有权**，但为未来 EaaS 商务模型打地基。

对本工厂最务实的路径是：

> **先做 L1→L2（能力平台 + 智能调度），再视资产与财务条件切入 L3（真正按次/按结果收费）。**

---

## 4. 市场典型案例调研

### 4.1 按产出付费的机床/激光：TRUMPF Pay-per-Part

- **模式**：客户不买断机床，按加工零件数付费；OEM 保留所有权并保障可用。
- **使能技术**：传感器、远程监控、预测分析（工业 4.0）。
- **启示**：HaaS 的商业闭环依赖 **可计量的产出事件**（本项目对应「一次成功测量」），以及 **可用率承诺**。
- **参考**：[California Management Review — EaaS in Machine Tool Manufacturing (2025)](https://cmr.berkeley.edu/2025/11/equipment-as-a-service-in-machine-tool-manufacturing-unlocking-greater-value-for-all/)

### 4.2 按飞行小时：Rolls-Royce Power-by-the-Hour

- **模式**：发动机可用小时计费，维护责任在服务方。
- **启示**：Outcome / Uptime 合同要求远程遥测与预测维护；工厂版对应 **设备健康度 + SLA 计费**。

### 4.3 机器人即服务：Formic RaaS

- **模式**：无大额 CapEx，按系统可用小时收费；涵盖选型、部署、备件、24/7 运维。
- **技术栈亮点**：设备无关 OS（Core）、机队智能（Cortex）、OTA、OEE 看板。
- **启示**：真正可规模化的 RaaS/HaaS 往往是 **「硬件无关的控制与运维层」**，而不是绑定单一 OEM。
- **参考**：[Formic Full Service Automation](https://formic.co/full-service-automation)

### 4.4 工业物联网平台：Siemens MindSphere（河钢案例）

- **做法**：MindConnect 采集电机/PLC/仪表/MES；云端 APP 做辊温、质量偏差、轧辊档案、乳化液分析。
- **启示**：价值从「连上设备」快速转向 **场景 APP**；连接本身不够，要有领域模型。
- **参考**：西门子河钢数字化案例

### 4.5 IIoT + MES-lite：PTC ThingWorx / Kepware（NEI 轴承）

- **做法**：Kepware 统一连接 500+ 台异构机台 → ThingWorx 实时看板与瓶颈分析 → 多工厂扩展至 1000+ 设备。
- **启示**：**异构协议归一**是数字化第一硬仗；平台可先当「轻量 MES」，再深耦合正式 MES。
- **参考**：[PTC NEI Case Study](https://www.ptc.com/cn/case-studies/transforming-factory-operations-at-nei-with-iot)

### 4.6 车间调度大脑：东方汽轮机叶片黑灯产线 + MOM

- **做法**：柔性加工单元 + 立库 + AGV；MOM 下发生产/物流/检验指令并闭环状态，实现无人化衔接。
- **启示**：智能调度不是「排产算法炫技」，而是 **多资源协同状态机**（机台、物流、检验、仓储同一套指令语义）。
- **参考**：佰思杰 MOM / 东方汽轮机案例

### 4.7 国内产线改造常见模式：边缘无线 + 多品牌 PLC → MES

- **做法**：新旧产线混杂、西门子/三菱/汇川并存，用工业无线网桥透明传输，数据进 MES，做 OEE 与调度。
- **启示**：工厂现实是 **棕地**；方案必须兼容私有串口、TCP 文本帧（如本项目 FLA）、Modbus、OPC 等，而不能假设全厂 OPC UA。

### 4.8 3D 打印 / 医疗影像等「设备订阅」

- Stratasys 等常走「卖机 + 材料/服务订阅」混合模型。
- 医疗影像 MRI/CT 的设备订阅（PwC/SAP 讨论较多）：高 CapEx 资产 OpEx 化。
- **启示**：光纤测试仪、光谱仪等高单价仪器与 MRI 同构——**共享 + 订阅** 比「每工位买一台」更合理。

### 4.9 市场体量（量级参考，口径不一）

| 来源口径 | 信号 |
|----------|------|
| IBM 对广义 HaaS | 2022 约 700 亿美元量级，高增速预期 |
| Carlsquare EaaS 2025 | 至 2032 约 6% CAGR；合同形态转向 outcome / IoT 监控 |
| ABI 对 RaaS | 预测至 2026 安装相关收入可达数百亿美元量级 |

对工厂内部项目更重要的不是全球 GMV，而是：**单台高价值设备利用率从 20% 提到 70%+，以及跨工位 CAPEX 节省**。

---

## 5. 工厂数字化要集成「所有设备」——目标架构

### 5.1 分层参考（ISA-95 + 现代 UNS）

```
L4 ERP / 财务结算 / 多工厂 KPI
        ↑↓
L3 MOM/MES：工单、质量、物料、排产
        ↑↓
L2.5 本构想核心：Device Capability Fabric（设备能力织物）
     - 任务编排 / 资源锁 / SLA / 计费计量
     - 统一 Device API（能力、状态、命令、结果）
        ↑↓
L2 控制与适配：OPC UA / SCADA / 各中控（OFDR Server 等）
        ↑↓
L1/L0 现场：PLC、仪表、光开关、FLA、机器人、AGV
```

**统一命名空间（UNS）** 已成为 2025–2027 主流工业数据架构：

- 传输：MQTT（常用 Sparkplug B）或 OPC UA Pub/Sub over MQTT  
- 主题树：`Enterprise/Site/Area/Line/Cell/Device/...`（ISA-95 拓扑）  
- 边缘网关：把 OPC UA / Modbus / 私有 TCP / 串口 规范化后发布到 Broker  
- 消费者：MES、数字孪生、调度器、看板、AI Agent **订阅同一真相源**，避免点对点意大利面集成  

参考方向：HiveMQ / EMQX + Sparkplug、OPC UA Companion Spec、Digital Twin ↔ MES 事件同步。

### 5.2 设备能力模型（建议的核心抽象）

每个物理设备注册为 **Capability**：

```yaml
deviceId: fla-01
capabilities:
  - name: fiber.length.scan
    exclusive: true          # 独占资源
    maxConcurrency: 1
    avgDurationMs: 8000
    paramsSchema: {...}
    resultSchema: {...}
routing:
  requires: [optical.switch.matrix-A]
  channelMap: clientId -> switchPort
sla:
  availabilityTarget: 0.985
metering:
  unit: successful_measurement
```

OFDR 现状可直接映射：

- `fiber.length.scan` / `fiber.length.zero` / `fiber.auto_peak`
- `requires: optical.switch`
- `exclusive: true` ↔ `_deviceLock`
- 计量单位 ↔ 成功 `result`

### 5.3 智能调度（从 FIFO 到可生产级）

调度问题本质是 **带约束的资源分配**：

| 策略层级 | 适用场景 | 算法/机制 |
|----------|----------|-----------|
| L0 FIFO | 当前 OFDR | Channel 队列 |
| L1 优先级 | 急单插队、复测优先 | 优先队列 + 老化防饥饿 |
| L2 截止时间 | 节拍线、出货窗口 | EDF / 加权最短作业 |
| L3 多资源 | 测长+温箱+光谱仪串联 | 预约日历 / 工作流 DAG |
| L4 全局优化 | 多线体、换型成本 | 启发式 + 仿真 / OR-Tools |
| L5 学习型 | 历史节拍、故障率 | 强化学习或预测到达率调参 |

**联合预约示例**（光纤测试工位）：

```
工单 W → [切通道] → [zero] → [scan] → [写 QMS]
         └─ switch 锁 ─┘ └─ fla 锁 ──────┘
```

需要：事务性资源租约（lease）、超时回收、失败补偿（重试/换备用通道）。

### 5.4 推荐技术栈（务实、可演进）

| 层次 | 建议 | 说明 |
|------|------|------|
| 现场适配 | 保留 .NET 边侧中控（本项目） | 串口/私有帧最稳；每类设备一个 Adapter |
| 边侧总线 | MQTT（EMQX）+ 可选 Sparkplug B | 解耦 MES/看板/调度 |
| 语义 | 内部 Capability Schema + 对外 OPC UA 门面 | 棕地协议先进 Adapter，再归一 |
| 任务编排 | 持久化队列（Redis Streams / NATS / Kafka） | 替代进程内 Channel，支持重启恢复 |
| 调度服务 | 独立 Scheduler（可先规则引擎） | 与 Worker 分离 |
| 状态与结果 | PostgreSQL + 时序库（可选） | 替代 MemoryCache；支撑计费与追溯 |
| API | gRPC / HTTP JSON（对内）+ TCP JSON-Lines（工位兼容） | 渐进迁移，不打断现有客户端 |
| 可观测 | OpenTelemetry + 现有 Serilog | TaskId 贯穿 |
| 安全 | mTLS / Token、设备证书、主题 ACL | 规模化前置条件 |
| AI | 先做预测维护与排队 ETA，再谈「自主排产 Agent」 | 数据质量不够时 Agent 无意义 |

**不建议一上来**：全厂上巨型商业 IIoT 套件替代边侧中控；边侧确定性控制必须留在本地。

### 5.5 与现有 OFDR 服务的演进关系

```
阶段 A（已完成）
  TcpServer + Channel + MeasurementWorker + Switch/FLA Adapter

阶段 B（设备平台雏形）
  + Device Registry（多设备实例）
  + 持久化任务/结果
  + 优先级调度
  + MQTT 状态发布（给 MES/看板）
  + 鉴权

阶段 C（多设备编排）
  + 多 Adapter（功率计、光谱、温箱、视觉…）
  + Workflow（多步测量配方）
  + 资源日历与联合锁
  + OEE / 利用率看板

阶段 D（HaaS 商业化）
  + 计量与账单（按次/按时/按可用率）
  + SLA 合同与告警
  + 多租户 / 跨产线配额
  + 远程运维与备件闭环

阶段 E（智能工厂）
  + UNS 全厂接入
  + 数字孪生与仿真调度
  + 与 MOM/AGV/立库协同
  + 可选 outcome-based 合同
```

---

## 6. 「集成所有设备」的现实策略

「所有设备」在工程上应拆成三类，分类施策：

| 类型 | 例子 | 接入策略 |
|------|------|----------|
| A. 独占高价值仪器 | FLA、光谱仪、高端 AOI | **中控共享 + 队列**（本项目模式） |
| B. 产线控制设备 | PLC、变频器、IO | **OPC UA / 驱动采集**，偏遥测与指令下发，一般不「共享」 |
| C. 物流与柔性单元 | AGV、立库、机器人 | **任务级编排**（与 MOM 协同），设备由专有控制器掌握 |

HaaS 的锋利点在 **类型 A**：CAPEX 高、利用率低、协议怪、天然互斥——正是 OFDR 场景。  
智能化调度的锋利点在 **A+C 组合**：测试资源与物流/工位节拍对齐。

棕地原则：

1. **不改设备固件优先**：Adapter 吞协议差异。  
2. **先观测后控制**：能采状态再逐步接管命令。  
3. **一设备一类契约**：Capability Schema 版本化。  
4. **失败可降级**：调度器挂了，单机中控仍能本地 FIFO（与本项目一致）。

---

## 7. 商业与组织层面（常被技术忽略）

| 议题 | 问题 | 建议 |
|------|------|------|
| 谁拥有设备 | 产线资产 vs 测试中心资产 | 先内部「虚拟计费」，再谈外部 EaaS |
| 如何计量 | 开机时长 vs 成功测量次数 | 测试类设备优先 **成功任务数 + 排队等待** |
| 如何考核 | OEE 还是一次通过率 | 同时看利用率与质量，避免「空转刷时长」 |
| 谁运维 | 设备厂家 / 厂内仪表班 / 软件团队 | Formic 启示：软件+备件+现场必须打包 |
| 数据归属 | 测量曲线、配方参数 | 明确进 QMS/追溯，避免只落在工位 PC |

---

## 8. 风险与反模式

1. **先买平台再找场景**：应对齐「共享测长」这类可算 ROI 的场景横向复制。  
2. **用云取代边侧实时控制**：光开关/测量时序必须本地确定性。  
3. **只做大屏不做闭环**：没有工单/质量回写，数字化停在「看得见」。  
4. **FIFO 硬扛所有业务**：急单与维护窗口会逼出影子流程。  
5. **无安全地扩大暴露面**：从单机 TCP 扩到全厂 API 前必须上鉴权。  
6. **过早 All-in AI 排产**：先把状态真、延迟低、计量准。

---

## 9. 可量化的成功标准（建议）

| KPI | 当前可测（OFDR） | 平台期目标 |
|-----|------------------|------------|
| 设备利用率 | 忙闲比（锁占用时间） | ≥ 60–80%（视产品结构） |
| 平均排队等待 | Task 入队→开跑 | 按节拍设 SLO |
| 任务成功率 | success / (success+failed) | ≥ 99%（含重试） |
| 单位 CAPEX 产出 | 工位数 / 仪器数 | 持续提高共享比 |
| 接入新设备周期 | — | 新 Adapter &lt; 2–4 周 |
| 数据入追溯率 | 低（内存） | 100% 关键结果落库 |

---

## 10. 对「下一步」的具体建议（贴合本仓库）

不必立刻重写。建议用「垫脚石」演进：

1. **抽象 DeviceAgent 接口**：把 FLA+Switch 收成一个 `IDeviceAgent`，为第二台仪器留位。  
2. **任务持久化**：队列与结果落库，进程重启不丢单。  
3. **优先级字段**：协议增加 `priority` / `deadline`，调度仍可单进程。  
4. **MQTT 旁路发布**：`task.queued|running|done` 事件给看板/MES，客户端 TCP 协议可保留。  
5. **计量日志**：每次成功测量写计量事件（未来账单的原材料）。  
6. **第二类设备试点**：选一台同产线高价值仪器，复制「中控共享」模式，验证平台抽象。  

当出现 **≥3 类设备、≥2 条产线共享同一调度语义** 时，再拆独立 Scheduler 与 UNS，性价比最高。

---

## 11. 总览图：从本服务到工厂 HaaS

```
  工位客户端 × N          其他系统(MES/QMS/看板)
        │                         │
        │ TCP JSON-L              │ MQTT / API
        ▼                         ▼
┌──────────────────────────────────────────┐
│         Device Capability Fabric         │
│  Registry · Scheduler · Metering · SLA   │
└──────────────────────────────────────────┘
        │            │            │
        ▼            ▼            ▼
   OFDR Agent   Spec Agent   ... Agent
   (本项目)      (未来)
        │
   FLA + 光开关
```

**本项目的战略价值**：证明了在真实工厂约束下（私有协议、串口、Windows 工控机、单设备互斥），「硬件能力服务化」是可落地的。HaaS 宏大叙事的正确打开方式，是把这一模式 **产品化、多设备化、可计量化**，再与 MOM/UNS 对接，而不是从空中楼阁的「AI 调度中枢」起步。

---

## 12. 参考与延伸阅读

1. TRUMPF / EaaS：CMR *Equipment as a Service in Machine Tool Manufacturing* (2025)  
2. IBM：*What is Hardware as a Service (HaaS)?*  
3. Carlsquare：*Equipment-as-a-Service Market Report 2025*  
4. Formic：RaaS / Full Service Automation  
5. PTC：NEI ThingWorx + Kepware 工厂数字化案例  
6. Siemens MindSphere：河钢集团质量与设备透明化案例  
7. Unified Namespace + MQTT Sparkplug / OPC UA PubSub 架构综述（2026）  
8. 东方汽轮机叶片产线 + MOM 黑灯调度案例  
9. 本仓库：`claude.md`、`OFDRCentralControlServer/` 实现

---

## 附录 A：术语速查

| 术语 | 含义 |
|------|------|
| HaaS | Hardware as a Service，硬件订阅/托管 |
| EaaS | Equipment as a Service，工业设备按使用/产出付费 |
| RaaS | Robotics as a Service |
| UNS | Unified Namespace，工厂统一数据命名空间 |
| MOM | Manufacturing Operations Management |
| OEE | Overall Equipment Effectiveness |
| Capability | 设备对外暴露的可调用能力（本构想核心对象） |

## 附录 B：与本项目命令的映射示例（未来 API 草图）

```jsonc
// 今天
{"command":"submit","clientId":3,"mode":"scan","params":{"zero_length":"5.0"}}

// 明天（能力平台）
{
  "command": "submit",
  "capability": "fiber.length.scan",
  "resourceHints": { "channel": 3 },
  "priority": 10,
  "deadline": "2026-08-12T10:05:00+08:00",
  "params": { "zero_length": "5.0" },
  "meteringAccount": "line-A-station-03"
}
```

兼容路径：旧 `mode` 继续工作；新字段渐进启用。
