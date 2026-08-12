# HaaS（硬件即服务）与工厂数字化：从 OFDR 共享测长到智能调度平台

> 基于当前 `OFDRCentralControlServer` 设备共享实践的延伸思考  
> 文档性质：战略/架构调研备忘，非实施规格  
> 更新日期：2026-08-12（补充：国内案例、落地路径、开源项目对照）

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

### 4.0 怎么读案例（四问法）

每个案例只抓四件事，避免淹没在宣传稿里：

1. **卖的是什么服务？**（按件 / 按小时 / 按可用率 / 按共享机时）
2. **稀缺资源是什么？**（机床、机器人、检测仪、产线产能）
3. **谁决定排队/调度规则？**（OEM 远程中心、测试中心、MOM、产线计划员）
4. **钱和责任怎么分？**（坏了谁修、失败谁买单、数据归谁）

对照本项目：稀缺资源 = FLA + 光开关；服务 = `scan` / `zero` / `auto_peak`；责任 = 中控串行执行。案例多出来的部分，就是下一阶段要补的（计量、优先级、多设备、质量闭环）。

---

### 4.1 按产出付费的机床/激光：TRUMPF Pay-per-Part（强烈推荐精读）

- **模式**：EaaS 合同提供 TruLaser Center + 料库；客户**只为实际加工出的零件付费**；TRUMPF 在 Neukirch 远程控制中心排程、编程、运维。
- **业务动机**：解决「订单满 + 缺熟练工」；试点客户宣称生产率可提升约 50%。
- **使能条件**：机台摄像头/传感器/远程控制；产出事件可计量；故障由服务方主动感知（利益一致）。
- **对本项目启示**：HaaS 闭环 = **可计量产出事件**（成功测量）+ **可用率承诺**；远程中心 ≈ 你们未来的「设备能力调度台」。
- **参考**：[CMR EaaS 文章 (2025)](https://cmr.berkeley.edu/2025/11/equipment-as-a-service-in-machine-tool-manufacturing-unlocking-greater-value-for-all/) · [TRUMPF Pay-per-Part 新闻](https://www.trumpf.com/en_INT/newsroom/global-press-releases/press-release-detail-page/release/pay-per-part-trumpf-offers-new-business-model-to-utilize-spare-machine-capacity/)

### 4.2 按飞行小时：Rolls-Royce Power-by-the-Hour

- **模式**：发动机可用小时计费，维护责任在服务方。
- **启示**：Outcome / Uptime 合同要求远程遥测与预测维护；工厂版对应 **设备健康度 + SLA 计费**（测长仪「可测小时」或「可用班次」）。

### 4.3 机器人即服务：Formic RaaS（含 ICON 注塑厂客户故事）

- **模式**：无大额 CapEx；按系统**运行小时**收费（公开叙述约 $10/小时量级）；厂商包选型、部署、备件、编程、24/7。
- **客户侧结果（ICON）**：产能约 +20%、运营费用约 −40%、节拍效率约 +30%（厂商/媒体披露，作量级参考）。
- **技术栈亮点**：设备无关 OS（Core）、机队智能（Cortex）、OTA、OEE。
- **启示**：可规模化的 RaaS 是 **硬件无关的控制与运维层**；中小厂用 OpEx 换自动化，与「少买 FLA、共享测长」同构。
- **参考**：[Formic](https://formic.co/full-service-automation) · [ICON × Formic 报道](https://www.machinedesign.com/automation-iiot/article/21273986/an-injection-molder-embraces-emergent-technology-with-robotics-as-a-service)

### 4.4 工业物联网平台：Siemens MindSphere（河钢案例）

- **做法**：MindConnect 采集电机/PLC/仪表/MES；云端 APP 做辊温、质量偏差、轧辊档案、乳化液分析。
- **启示**：价值从「连上设备」快速转向 **场景 APP**；连接本身不够，要有领域模型（对本项目 = 光纤测量领域能力，而非通用 IoT）。
- **参考**：西门子河钢数字化案例

### 4.5 IIoT + MES-lite：PTC ThingWorx / Kepware（NEI 轴承）

- **做法**：Kepware 统一连接 500+ 台异构机台 → ThingWorx 实时看板与瓶颈分析 → 多工厂扩展至 1000+ 设备。
- **启示**：**异构协议归一**是数字化第一硬仗；平台可先当「轻量 MES」，再深耦合正式 MES。
- **参考**：[PTC NEI Case Study](https://www.ptc.com/cn/case-studies/transforming-factory-operations-at-nei-with-iot)

### 4.6 车间调度大脑：东方汽轮机叶片黑灯产线 + MOM

- **做法**：柔性加工单元 + 立库 + AGV；MOM 下发生产/物流/检验指令并闭环状态，实现无人化衔接。
- **启示**：智能调度不是「排产算法炫技」，而是 **多资源协同状态机**（机台、物流、检验、仓储同一套指令语义）。
- **参考**：佰思杰 MOM / 东方汽轮机案例

### 4.7 国内智能排产：华为南方工厂 / 长安渝北新工厂

| 案例 | 做法摘要 | 可学点 |
|------|----------|--------|
| **华为南方工厂**（《工业数字化2030》） | 排产模型细到线体/设备/夹具/人员/场地；宣称排产人员 −50%、产出 +20% | 多资源约束下的计划；调度粒度比「单机 FIFO」高一层 |
| **长安渝北工厂 × 华为** | 1.2 万+ 设备入数字底座；月→周→日→顺序计划；AI 辅助排产与全息质量 | **计划分层**；IT/OT 数据同源后再谈智能应用 |
| **某电气 APS 叙述** | 规则 + 有限产能 + 优先级 | 第一版智能调度 = 规则引擎，不是深度学习 |

- **参考**：华为《工业数字化2030》PDF · [长安渝北案例](https://e.huawei.com/cn/case-studies/industries/manufacturing/202412-changan-yubei-new-factory)

### 4.8 国内资源共享 / 「产能银行」：海尔卡奥斯、产业集群检测共享

| 案例 | 做法摘要 | 可学点 |
|------|----------|--------|
| **海尔上海洗衣机互联工厂 + COSMOPlat** | 数字孪生仿真排产；一物一码全周期追溯；虚拟验证后再实产 | 排产先仿真；追溯与服务化绑定 |
| **卡奥斯「产能银行」叙事** | 闲置设备/产能跨企业调剂 | 把产能当可交易资源；厂内可先做「虚拟计费」简化版 |
| **珠三角等检测设备共享叙述** | 多家企业共享精密检测仪，利用率拉高 | 与「一台 FLA 服务多工位」**同一逻辑**，组织边界更大 |

- **注意**：跨企业共享涉及信任、计量纠纷、数据权属，**远难于厂内共享**；建议厂内 HaaS 跑通后再外扩。
- **参考**：[海尔松江洗衣机工厂报道](https://www.haier.com/press-events/news/20230728_212611.shtml)

### 4.9 国内产线改造常见模式：边缘无线 + 多品牌 PLC → MES

- **做法**：新旧产线混杂、西门子/三菱/汇川并存，用工业无线网桥透明传输，数据进 MES，做 OEE 与调度。
- **启示**：工厂现实是 **棕地**；方案必须兼容私有串口、TCP 文本帧（如本项目 FLA）、Modbus、OPC 等，而不能假设全厂 OPC UA。

### 4.10 3D 打印 / 医疗影像等「设备订阅」

- Stratasys 等常走「卖机 + 材料/服务订阅」混合模型。
- 医疗影像 MRI/CT 的设备订阅（PwC/SAP 讨论较多）：高 CapEx 资产 OpEx 化。
- **启示**：光纤测试仪、光谱仪等高单价仪器与 MRI 同构——**共享 + 订阅** 比「每工位买一台」更合理。

### 4.11 灯塔级（开阔眼界，不宜第一年对标）

| 案例 | 一句话 | 为何暂不照抄 |
|------|--------|--------------|
| 富士康 × NVIDIA Omniverse 数字孪生 | 产线虚拟组装、跨厂复制、视频 AI Agent | 需庞大标准化资产库与 IT 团队 |
| 完整「黑灯工厂」宣传案例 | 全无人产线 | 前置条件是工艺/物流/质量全部数字化闭环 |

### 4.12 案例对照本项目（一张表）

| 你要补的能力 | 优先对标案例 | 开源可动手处（见 §6） |
|--------------|--------------|----------------------|
| 按次/按产出计量 | TRUMPF、Formic | 自研计量事件 + 时序库 |
| 异构设备接入 | NEI/Kepware、河钢 | ThingsGateway / Neuron / UMH |
| 厂级数据总线 UNS | 长安数字底座、UMH 用户故事 | UMH、EMQX、UNS Framework |
| 多资源调度 | 东方汽轮机 MOM、华为南方工厂 | Timefold（排程引擎） |
| 看板 / OEE | NEI、海尔孪生大屏 | FUXA、Grafana |
| 检测资源共享 | 产业集群检测共享、本项目 | **就是 OFDR 中控本身** |

### 4.13 市场体量（量级参考，口径不一）

| 来源口径 | 信号 |
|----------|------|
| IBM 对广义 HaaS | 2022 约 700 亿美元量级，高增速预期 |
| Carlsquare EaaS 2025 | 至 2032 约 6% CAGR；合同形态转向 outcome / IoT 监控 |
| ABI 对 RaaS | 预测至 2026 安装相关收入可达数百亿美元量级 |

对工厂内部项目更重要的不是全球 GMV，而是：**单台高价值设备利用率从 20% 提到 70%+，以及跨工位 CAPEX 节省**。

---

## 5. 落地路径：没做过这类项目怎么起步

> 原则：**选痛点，不选平台；先服务目录与规则，再智能算法；先厂内虚拟计费，再对外 HaaS。**

### 5.1 六步法（业务为主）

| 步骤 | 做什么 | 完成标志 |
|------|--------|----------|
| 1. 选场景 | 设备贵、闲着多、多人抢、协议怪 | 写下 ROI：少买几台 / 等待降多少 |
| 2. 服务目录 | 列出可调用能力（如归零/测长/复测）、SLA、失败策略 | 一页「产品说明书」 |
| 3. 人工规则调度 | FIFO + 急单插队 + 维护禁入 + 防饥饿 | 两周无无「影子插队」 |
| 4. 内部虚拟计费 | 按成功次数记到工位/部门 | 利用率报表能拿出来开会 |
| 5. 能力 Owner | 明确谁修设备、谁定规则、谁认质量结果 | RACI 表 |
| 6. 横向复制 | 第二台高价值仪器套同一套「提交—排队—结果—计量」 | 才叫平台雏形 |

### 5.2 和本仓库的映射

```
今天：工位 → submit → FIFO → 回 result     （服务化 MVP）
↓ §5.1 步骤 3–4
近场：+ priority/deadline + 计量日志 + 结果落库
↓ 第二类设备
中场：多 Agent + MQTT 事件给 MES/看板
↓ 多产线
远场：UNS + 独立 Scheduler + 可选按次对外收费
```

---

## 6. 开源项目参考（可学、可 fork、可对照架构）

> **定位**：商业案例告诉你「为什么」与「做成什么样」；开源项目告诉你「第一块砖怎么砌」。  
> **原则**：边侧确定性控制（光开关时序、FLA 私有帧）**继续自研**；开源优先用于 **采集网关、消息总线、看板、排程引擎、UNS 参考实现**。  
> 下列项目生态变化快，采用前请核对当前 License 与社区活跃度。

### 6.1 按架构层次对照

```
工位 / MES / 看板 / 排程
        │
        ▼
┌─────────────────── 开源可借鉴层 ───────────────────┐
│  UNS / 消息：UMH · EMQX · Mosquitto · Sparkplug   │
│  看板/HMI：FUXA · Grafana                         │
│  排程优化：Timefold Solver                        │
│  采集网关：ThingsGateway · Neuron · IoTGateway    │
│  OPC UA：Eclipse Milo · automation-gateway       │
└───────────────────────────────────────────────────┘
        │
        ▼
┌─────────────────── 建议继续自研 ───────────────────┐
│  OFDRCentralControlServer（任务队列、设备锁、     │
│  FLA/光开关 Adapter、测量领域逻辑、工位 TCP 协议） │
└───────────────────────────────────────────────────┘
```

### 6.2 统一命名空间 / 工厂数据底座

| 项目 | 地址 | 学什么 | 与 OFDR 关系 |
|------|------|--------|--------------|
| **United Manufacturing Hub (UMH)** | [github.com/united-manufacturing-hub/united-manufacturing-hub](https://github.com/united-manufacturing-hub/united-manufacturing-hub) · [umh.app](https://www.umh.app/) | 开源 UNS 全栈思路：边缘采集 → 标准化 → 存储/交换 → 可视化；UMH Core 单容器边缘网关 | 阶段 B/E：中控把 `task.*` 事件打进 UNS，而不是点对点改 MES |
| **UNS Framework** | [unsframework.com](https://unsframework.com/) | ISA-95 主题树 YAML 约定、payload 规范、参考 serverless 实现 | 设计 MQTT 主题时直接抄层级：`Enterprise/Site/Area/Line/Device` |
| **EMQX**（社区版） | [github.com/emqx/emqx](https://github.com/emqx/emqx) | 高并发 MQTT Broker；工业 UNS 常用底座之一 | 旁路发布测量事件；文档区有大量 UNS/OPC-MQTT 文章 |
| **Eclipse Tahu / Sparkplug** | Eclipse Tahu 等 | 工业 MQTT 载荷与 birth/death 会话语义 | 若要对齐生态，任务状态可用 Sparkplug 风格规范化 |
| **idp-os** | [github.com/cftservices/idp-os](https://github.com/cftservices/idp-os) | 「Connect→Condition→Model→Store→Orchestrate」参考流水线（NiFi/RabbitMQ/Neo4j 等） | 学分层，不必整包搬进工厂 |

### 6.3 工业采集网关 / 协议归一（棕地刚需）

| 项目 | 地址 | 学什么 | 与 OFDR 关系 |
|------|------|--------|--------------|
| **ThingsGateway** | [github.com/ThingsGateway/ThingsGateway](https://github.com/ThingsGateway/ThingsGateway)（亦有 Gitee） | **.NET 8** 工业网关：多 PLC 协议、MQTT/Kafka/OPC UA 转发、插件化——与本仓库技术栈同族 | 第二类「标准协议设备」可走网关；FLA 私有帧仍用自研 Adapter |
| **Neuron / NeuronEX**（EMQ） | [github.com/emqx/neuron](https://github.com/emqx/neuron) | 轻量工业协议采集，南向 Modbus/OPC/S7 等，北向 MQTT | 边缘「协议翻译官」参考 |
| **IoTGateway** | [github.com/iioter/iotgateway](https://github.com/iioter/iotgateway) | .NET B/S 网关、可视化配置、多驱动 | 快速搭采集 POC |
| **EdgeX（anviod/edgex 等工业边缘实现）** | GitHub 检索 industrial edge gateway | 南向多协议 + 北向 MQTT/Sparkplug + 边缘规则 | 学「影子真源 + 北向解耦」 |
| **Apache PLC4X** | [plc4x.apache.org](https://plc4x.apache.org/) | 统一 API 读多种 PLC | Java/多语言生态；作协议知识库 |
| **automation-gateway** | [github.com/vogler75/automation-gateway](https://github.com/vogler75/automation-gateway) | OPC UA ↔ MQTT/GraphQL，可写时序库 | 当需要「OPC 门面」给 MES 时参考 |
| **Eclipse Milo** | [github.com/eclipse-milo/milo](https://github.com/eclipse-milo/milo) | Java OPC UA 协议栈 | 自建 OPC UA Server/Client 时的标准库 |
| **ThingsBoard + Gateway** | [thingsboard.io](https://thingsboard.io/) · [thingsboard-gateway](https://github.com/thingsboard/thingsboard-gateway) | 设备管理、规则链、仪表盘；网关接 Modbus/OPC UA | 快速做「设备台账 + 遥测看板」MVP |

### 6.4 看板 / 轻量 SCADA / HMI

| 项目 | 地址 | 学什么 | 与 OFDR 关系 |
|------|------|--------|--------------|
| **FUXA** | [github.com/frangoteam/FUXA](https://github.com/frangoteam/FUXA) | Web SCADA/HMI：Modbus/OPC UA/MQTT/S7，拖拽画面 | 订阅中控 MQTT 事件，做通道忙闲/队列长度大屏 |
| **Grafana** + 时序库 | grafana.com | OEE、等待时长、成功率仪表盘 | 计量与 KPI 可视化（§11） |

### 6.5 智能排程 / 约束优化（调度从规则到求解器）

| 项目 | 地址 | 学什么 | 与 OFDR 关系 |
|------|------|--------|--------------|
| **Timefold Solver**（OptaPlanner 继任社区版） | [github.com/TimefoldAI/timefold-solver](https://github.com/TimefoldAI/timefold-solver) | Job Shop、任务分配、维护窗口等约束求解（Apache-2.0） | 阶段 C/D：多仪器 + 截止时间 + 换型成本时再引入；**日常 FIFO/优先级不必上求解器** |
| **Google OR-Tools** | [developers.google.com/optimization](https://developers.google.com/optimization) | CP-SAT/车辆路径等经典运筹库 | 离线仿真排程、产能 What-if |

### 6.6 工作流 / 编排（多步测量配方）

| 项目 | 地址 | 学什么 | 与 OFDR 关系 |
|------|------|--------|--------------|
| **Temporal** / **Cadence** | temporal.io 等 | 长事务工作流、重试、补偿 | `切通道→zero→scan→写QMS` 可建模为 Workflow |
| **Node-RED** | nodered.org | 低代码连设备与 HTTP/MQTT | 实验室/试点连线；生产关键路径仍建议代码化 |
| **n8n** | n8n.io | 自动化编排（idp-os 等方案里常见） | 告警、工单、钉钉/邮件通知 |

### 6.7 建议的「开源学习顺序」（贴合本仓库）

| 顺序 | 动作 | 对应演进阶段 |
|------|------|--------------|
| ① | 读 UMH / UNS Framework 文档，画一张本厂主题树草图 | 概念对齐 §7 |
| ② | 本机起 EMQX，中控旁路发布 `task.queued/running/done` | 阶段 B |
| ③ | FUXA 或 Grafana 订阅读队列深度、通道占用 | 可观测 |
| ④ | 用 ThingsGateway 接一台标准协议设备（如 Modbus 仪表） | 验证「网关 + 自研 Agent」双轨 |
| ⑤ | 需要多约束排程时，用 Timefold 做**离线**仿真，不要先替换在线 FIFO | 阶段 C+ |
| ⑥ | 始终保留 OFDR 中控本地锁与队列，作为调度器故障时的降级路径 | 反模式 §10 |

### 6.8 开源选型注意

1. **License**：Apache-2.0 / MIT 利于商用闭源集成；AGPL/部分 SSPL 需法务评估。  
2. **OT 安全**：开源组件默认配置常过宽，上线前强制 TLS、ACL、只读账号。  
3. **不要用开源 MES「替换」测长中控**：领域协议与互斥语义仍在边侧；开源吃「数据与调度」，自研吃「设备真相」。  
4. **中文社区友好**：ThingsGateway、IoTGateway、EMQX/Neuron 文档与 Issue 对国内产线更友好。

---

## 7. 工厂数字化要集成「所有设备」——目标架构

### 7.1 分层参考（ISA-95 + 现代 UNS）

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

### 7.2 设备能力模型（建议的核心抽象）

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

### 7.3 智能调度（从 FIFO 到可生产级）

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

### 7.4 推荐技术栈（务实、可演进）

| 层次 | 建议 | 说明 |
|------|------|------|
| 现场适配 | 保留 .NET 边侧中控（本项目）；标准协议可旁路 ThingsGateway（§6.3） | 串口/私有帧最稳；每类设备一个 Adapter |
| 边侧总线 | MQTT（EMQX 开源）+ 可选 Sparkplug B；参考 UMH / UNS Framework（§6） | 解耦 MES/看板/调度 |
| 语义 | 内部 Capability Schema + 对外 OPC UA 门面（Milo / automation-gateway） | 棕地协议先进 Adapter，再归一 |
| 任务编排 | 持久化队列（Redis Streams / NATS / Kafka） | 替代进程内 Channel，支持重启恢复 |
| 调度服务 | 先规则引擎；多约束时参考 Timefold 离线求解（§6.5） | 与 Worker 分离 |
| 状态与结果 | PostgreSQL + 时序库（可选） | 替代 MemoryCache；支撑计费与追溯 |
| API | gRPC / HTTP JSON（对内）+ TCP JSON-Lines（工位兼容） | 渐进迁移，不打断现有客户端 |
| 可观测 | OpenTelemetry + Serilog；看板可用 FUXA / Grafana（§6.4） | TaskId 贯穿 |
| 安全 | mTLS / Token、设备证书、主题 ACL | 规模化前置条件 |
| AI | 先做预测维护与排队 ETA，再谈「自主排产 Agent」 | 数据质量不够时 Agent 无意义 |

**不建议一上来**：全厂上巨型商业 IIoT 套件替代边侧中控；边侧确定性控制必须留在本地。

### 7.5 与现有 OFDR 服务的演进关系

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

## 8. 「集成所有设备」的现实策略

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

## 9. 商业与组织层面（常被技术忽略）

| 议题 | 问题 | 建议 |
|------|------|------|
| 谁拥有设备 | 产线资产 vs 测试中心资产 | 先内部「虚拟计费」，再谈外部 EaaS |
| 如何计量 | 开机时长 vs 成功测量次数 | 测试类设备优先 **成功任务数 + 排队等待** |
| 如何考核 | OEE 还是一次通过率 | 同时看利用率与质量，避免「空转刷时长」 |
| 谁运维 | 设备厂家 / 厂内仪表班 / 软件团队 | Formic 启示：软件+备件+现场必须打包 |
| 数据归属 | 测量曲线、配方参数 | 明确进 QMS/追溯，避免只落在工位 PC |

---

## 10. 风险与反模式

1. **先买平台再找场景**：应对齐「共享测长」这类可算 ROI 的场景横向复制。  
2. **用云取代边侧实时控制**：光开关/测量时序必须本地确定性。  
3. **只做大屏不做闭环**：没有工单/质量回写，数字化停在「看得见」。  
4. **FIFO 硬扛所有业务**：急单与维护窗口会逼出影子流程。  
5. **无安全地扩大暴露面**：从单机 TCP 扩到全厂 API 前必须上鉴权。  
6. **过早 All-in AI 排产**：先把状态真、延迟低、计量准。

---

## 11. 可量化的成功标准（建议）

| KPI | 当前可测（OFDR） | 平台期目标 |
|-----|------------------|------------|
| 设备利用率 | 忙闲比（锁占用时间） | ≥ 60–80%（视产品结构） |
| 平均排队等待 | Task 入队→开跑 | 按节拍设 SLO |
| 任务成功率 | success / (success+failed) | ≥ 99%（含重试） |
| 单位 CAPEX 产出 | 工位数 / 仪器数 | 持续提高共享比 |
| 接入新设备周期 | — | 新 Adapter &lt; 2–4 周 |
| 数据入追溯率 | 低（内存） | 100% 关键结果落库 |

---

## 12. 对「下一步」的具体建议（贴合本仓库）

不必立刻重写。建议用「垫脚石」演进（与 §5、§6.7 一致）：

1. **抽象 DeviceAgent 接口**：把 FLA+Switch 收成一个 `IDeviceAgent`，为第二台仪器留位。  
2. **任务持久化**：队列与结果落库，进程重启不丢单。  
3. **优先级字段**：协议增加 `priority` / `deadline`，调度仍可单进程。  
4. **MQTT 旁路发布**：`task.queued|running|done` 事件给看板/MES（EMQX + FUXA/Grafana），客户端 TCP 协议可保留。  
5. **计量日志**：每次成功测量写计量事件（未来账单的原材料）。  
6. **第二类设备试点**：标准协议走 ThingsGateway；私有协议复制「中控共享」模式，验证双轨抽象。  

当出现 **≥3 类设备、≥2 条产线共享同一调度语义** 时，再拆独立 Scheduler 与 UNS（UMH / 自建主题树），性价比最高。

---

## 13. 总览图：从本服务到工厂 HaaS

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

## 14. 参考与延伸阅读

### 14.1 商业与行业案例

1. TRUMPF / EaaS：CMR *Equipment as a Service in Machine Tool Manufacturing* (2025)；TRUMPF Pay-per-Part 官方新闻  
2. Formic RaaS / ICON 注塑客户故事（Machine Design 等报道）  
3. IBM：*What is Hardware as a Service (HaaS)?*  
4. Carlsquare：*Equipment-as-a-Service Market Report 2025*  
5. PTC：NEI ThingWorx + Kepware 工厂数字化案例  
6. Siemens MindSphere：河钢集团质量与设备透明化案例  
7. 华为《工业数字化2030》；长安渝北新工厂案例  
8. 海尔卡奥斯 / 松江洗衣机互联工厂报道  
9. 东方汽轮机叶片产线 + MOM 黑灯调度案例  
10. Rolls-Royce Power-by-the-Hour（经典 outcome 合同）

### 14.2 架构与 UNS

11. Unified Namespace + MQTT Sparkplug / OPC UA PubSub 架构综述（HiveMQ / EMQX 博客等，2026）  
12. UNS Framework：https://unsframework.com/  
13. United Manufacturing Hub：https://umh.app/ · GitHub `united-manufacturing-hub`

### 14.3 开源项目（详见 §6）

14. ThingsGateway（.NET 工业网关）  
15. Neuron / EMQX  
16. FUXA（Web SCADA/HMI）  
17. Timefold Solver（约束排程）  
18. Eclipse Milo、Apache PLC4X、automation-gateway  
19. ThingsBoard + IoT Gateway  
20. Temporal / Node-RED（工作流试点）

### 14.4 本仓库

21. `claude.md`、`OFDRCentralControlServer/` 实现  
22. 本文：从单仪器共享到 HaaS 能力平台的演进备忘

---

## 附录 A：术语速查

| 术语 | 含义 |
|------|------|
| HaaS | Hardware as a Service，硬件订阅/托管 |
| EaaS | Equipment as a Service，工业设备按使用/产出付费 |
| RaaS | Robotics as a Service |
| UNS | Unified Namespace，工厂统一数据命名空间；开源参考 UMH、UNS Framework（§6.2） |
| Sparkplug B | 工业 MQTT 载荷与会话约定，常与 UNS 搭配 |
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
