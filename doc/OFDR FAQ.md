# OFDR FAQ

### 1. 实际是 `real` 模式（最常见）

典型场景：

- 发布目录里没有 `ServerConfig.json`，程序会生成默认配置，默认 `RunMode` 是 `real`（见 `ConfigLoader.cs`）。
- 工厂把 `RunMode` 改成了 `real`，但 `FlaHost` 仍是占位 `192.168.1.1`，仪器 unreachable。
- `SwitchCom: "COM3"` 在工控机上不存在或串口被占用。

处理：

- 无硬件联调：保持 `"RunMode": "mock"`。
- 上产线：改为 `"RunMode": "real"`，并把 `FlaHost` 改成 FLA 仪器的局域网 IP（向设备厂商或网络同事确认），`FlaPort` 一般为 `4300`。



**首先请确认 FLA和光开关是可达的**

**FLA通过网口工具确认**

**光开关通过串口工具确认**



### 2. 防火墙 / 端口（影响客户端，）

- 入站：工控机/服务器需放行 TCP `ListenPort`（5600），客户端才能连中央服务。
- 出站：`real` 模式下还需能访问 `FlaHost:FlaPort`（如 4300）；这是出站规则，

`mock` 模式下通常只需放行 5600 入站；若仍秒退，优先查日志而不是只改防火墙。



### 3. 看日志确认（最重要）

程序目录下会有：`logs/server-*.log`。

重点搜：

- `Failed to connect to FLA instrument`
- `Failed to connect to OSW instrument`
- `Server crashed`







# OFDR FAQ  -- English

## Make it Actually running in **real mode** 

Typical scenarios:

\- There is no `ServerConfig.json` in the release directory. The program generates a default config, and the default `RunMode` is `real` (see ConfigLoader.cs).

\- The factory changed `RunMode` to `real`, but `FlaHost` is still the placeholder `192.168.1.1`, causing the instrument to be unreachable.

\- `SwitchCom: "COM3"` does not exist on the IPC, or the serial port is already occupied. 

Resolution:

\- Without hardware integration: keep `"RunMode": "mock"`.

\- For production line: set `"RunMode": "real"` and update `FlaHost` to the actual LAN IP of the FLA instrument (confirm with the equipment vendor or network team). `FlaPort` is usually `4300`.

\> **First, make sure that both the FLA and the optical switch are reachable**

\>

\> - Use a network tool to verify the FLA connection 

\> - Use a serial port tool to verify the optical switch connection 

\---

##  Firewall / Port (affects client connections)

\- **Inbound**: The IPC/server must allow TCP `ListenPort` (5600), otherwise the client cannot connect to the central service.

\- **Outbound**: In `real` mode, access to `FlaHost:FlaPort` (e.g., 4300) must be allowed. This is an outbound rule.

Notes:

\- In `mock` mode, usually only inbound port 5600 needs to be open.

\- If the program still exits immediately, check logs first instead of only modifying firewall settings.

\---

## Check logs (most important)

Log files are located in the program directory:

Focus on searching for: - `Failed to connect to FLA instrument` - `Failed to connect to OSW instrument` - `Server crashed`