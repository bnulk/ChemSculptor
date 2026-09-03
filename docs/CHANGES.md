# ChemSculptor 项目改动说明

> 维护约定：**每次代码改动后更新本文档**，记录版本、改动目的、改动内容和教程式说明。
> 版本规则：`MAJOR.MINOR.PATCH`。新增功能 +1 MINOR；修复问题 +1 PATCH；架构性大改动 +1 MAJOR。

---

## v0.4.0（2026-09-06）：服务器端接收分子坐标（阶段 A 第一切片）

### 版本

- 当前版本：`0.4.0`
- 日期：2026-09-06
- 版本类型：新增功能（MINOR）

### 改动目的

按照阶段 A 推进“坐标进、结论出”的最小闭环，先打通第一环：客户端把分子坐标文本发送到服务器，服务器完成接收、解析并返回结构化确认。

### 改动内容

`ChemSculptor.InputProcessor` 新增几何文本解析能力：

- `GeometryModels.cs`：`GeometryAtom`、`MolecularGeometry`。
- `GeometryTextParser.cs`：`IGeometryTextParser` 接口和 XYZ 文本解析实现，支持标准 XYZ 文本（原子数行、注释行、`元素 x y z` 坐标行），输出分子式和原子列表，并对非坐标行/数量不一致给出诊断。

`ChemSculptor.Api` 新增坐标接收端点：

- 新增 `Endpoints/GeometryEndpoints.cs`：`POST /geometries`，接收 `text/plain` 坐标文本，解析后返回分子式、原子数、原子坐标和诊断。
- `Program.cs` 注册 `IGeometryTextParser`，挂载端点，并把 `POST /geometries` 加入根端点列表。

`ChemSculptor.WinForms` 增加坐标发送入口：

- 新增“发送坐标”按钮，复用当前选择的 txt。
- 新增 `GeometryAtomDto`、`GeometrySubmitResult` 本地模型。
- 发送成功后显示服务器返回的分子式、原子数和原子列表。

新增示例文件：

```text
samples/water.xyz.txt
```

### 教程式说明

#### 数据流

```text
WinForms 选择 water.xyz.txt
  → 点击“发送坐标”
  → POST /geometries（text/plain 原始坐标文本）
  → GeometryTextParser 解析 XYZ
  → 返回 JSON：{ formula, atomCount, atoms }
```

#### XYZ 文本格式

```text
3
water molecule
O 0.000000 0.000000 0.117300
H 0.000000 0.757200 -0.469200
H 0.000000 -0.757200 -0.469200
```

第 1 行是原子数，第 2 行是名称，之后每行是 `元素 x y z`。

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：5/5 通过
- 端到端实测：向 `POST /geometries` 发送 `samples/water.xyz.txt`，服务器返回 `H2O`、3 个原子及完整坐标，诊断为空。

---

## v0.3.0（2026-09-03）：WinForms 与 ChemSculptor 服务器端独立

### 版本

- 当前版本：`0.3.0`
- 日期：2026-09-03
- 版本类型：架构调整 + 新增功能（MINOR）

### 改动目的

按用户的界面约束调整架构：

1. WinForms 中禁止出现任何 `using ChemSculptor`，界面不再引用内核或领域模型。
2. WinForms 只负责：接收客户的 txt 输入 → 发送给 ChemSculptor → 被动轮询反馈 → 接收结束信息和结果 txt。
3. 单机版和以后的服务器版保持同构：单机时 ChemSculptor 本体运行在本地 Api；部署到服务器后，WinForms 只改服务地址即可。

### 改动内容

`ChemSculptor.WinForms` 重写为纯 HTTP 客户端：

- 移除对 `ChemSculptor.Core`、`ChemSculptor.Domain` 的项目引用。
- 删除 `Services/IChemSculptorService.cs` 和 `Services/LocalChemSculptorService.cs`。
- 新增本地 DTO：`Models/ClientJobSummary.cs`。
- `MainForm` 通过 `HttpClient` 完成：选择 txt → 上传到 `/client/jobs` → 定时轮询 `/client/jobs/{id}/status` → 结果就绪后读取 `/client/jobs/{id}/result`，并可保存为 txt。
- 界面层不再使用任何 ChemSculptor 命名空间。

新增 `ChemSculptor.InputProcessor`（客户输入解析工程）：

- `IClientInputParser`：输入解析器接口，未来可扩展 JSON/二进制解析器。
- `TextClientInputParser`：当前文本解析实现。
- `ProcessedClientRequest`：解析结果，包含工作流 Id、目标描述和原始文本。
- 文本请求格式 v1：支持 `workflow:` 和 `goal:` 行；未指定工作流时默认 `tadf_mechanism_diagnosis`。

`ChemSculptor.Api` 增加客户端作业能力：

- 新增 `Client/ClientJob.cs`、`Client/ClientJobService.cs`。
- 新增 `Endpoints/ClientJobEndpoints.cs`。
- `Program.cs` 注册 `TextClientInputParser` 和 `ClientJobService`，并挂载客户端作业端点。
- `ChemSculptor.Api.csproj` 引用 `ChemSculptor.InputProcessor`。
- 作业在 Api 进程内后台执行（单用户阶段不建队列，按用户决定预留位置）。

### 教程式说明

#### 现在的整体形态

```text
WinForms 客户端（纯界面，零 ChemSculptor 依赖）
   │ 选择用户 txt → HTTP 上传
   │ 定时轮询状态
   │ 下载结果 txt
   ▼
ChemSculptor.Api（本地运行 = 单机版；以后部署到服务器 = 服务器版）
   ├── InputProcessor：把客户输入文件解析成内核可用请求
   └── WorkflowEngine + 技能容器
```

#### 一次完整数据流

```text
用户选择 txt
  → WinForms POST /client/jobs（multipart 上传原始文本）
  → Api 保存任务，后台执行
  → InputProcessor 解析 workflow:/goal: 行
  → Api 选择工作流模板并调用 WorkflowEngine
  → WinForms 定时轮询 GET /client/jobs/{id}/status
  → 状态变为 Passed/Failed 且 HasResult=true
  → WinForms GET /client/jobs/{id}/result，得到结果 txt
```

#### 文本请求格式示例（v1，占位）

```text
workflow: tadf_mechanism_diagnosis
goal: 判断超分子体系是否为 TADF 并定位主要发光通道
```

#### 新增端点

| 方法 | 路径 | 作用 |
|---|---|---|
| `POST` | `/client/jobs` | 接收客户 txt，创建客户端任务 |
| `GET` | `/client/jobs/{id}/status` | 轮询任务状态 |
| `GET` | `/client/jobs/{id}/result` | 下载结果 txt |

#### 如何运行

先启动 Api：

```powershell
dotnet run --project src/ChemSculptor.Api --urls http://127.0.0.1:5080
```

再运行 WinForms：

```powershell
dotnet run --project src/ChemSculptor.WinForms
```

WinForms 顶部的服务地址默认是 `http://127.0.0.1:5080`；以后服务器版只需改成远程地址。

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：5/5 通过
- 客户端/服务器独立模式代码已按当前代码跑通（用户环境确认）；本文档更新仅涉及记录同步，不改动代码。

---

## v0.2.0（2026-09-02）：WinForms 单机版（历史过渡，已从当前代码中移除）

> 本节是历史记录。v0.2.0 描述的 WinForms 直连内核形态在当前仓库已不存在，相关文件已删除；当前客户端形态以 v0.3.0 为准。

### 版本

- 当前版本：`0.2.0`（历史）
- 日期：2026-09-02
- 版本类型：新增功能（MINOR）

### 当时做了什么

- 把 WinForms 加入解决方案，作为“单机版操作界面”。
- WinForms 曾直接引用 `ChemSculptor.Core` 与 `ChemSculptor.Domain`。
- 提供过 `IChemSculptorService` / `LocalChemSculptorService` 服务层，以及“载入示例 / 刷新 / 执行所选”按钮。
- `MainForm` 曾订阅事件总线，把 `task.started`、`task.completed` 等事件实时显示到日志框。

### 与当前代码的差异

当前 `ChemSculptor.WinForms` 已不含上述服务和按钮：

- csproj 不再引用 `ChemSculptor.Core` 与 `ChemSculptor.Domain`，没有任何项目引用。
- 不存在 `Services/` 文件夹，不存在 `IChemSculptorService`、`LocalChemSculptorService`。
- 不存在“载入示例 / 刷新 / 执行所选”等直连内核按钮。
- 界面只保留纯 HTTP 客户端功能：服务地址、选择 txt、提交任务、轮询状态、显示并保存结果。

v0.2.0 的代码形态已被 v0.3.0 取代，仅作为过程记录保留。

### 验证（当时）

- v0.2.0 时代构建与测试通过；其代码现已删除，不再作为运行基线。

---

## v0.1.0（历史）：框架骨架

首次建立的 C# 框架骨架：Domain 契约、Core 极简内核、Api 宿主、Tests 测试，以及示例工作流和教程文档。本次改动从 v0.1.0 开始登记版本号。

---

## 未来改动记录模板

以后每次改动后，在文档顶部追加以下格式：

```markdown
## vX.Y.Z（日期）：改动标题

### 版本
- 当前版本：X.Y.Z
- 日期：YYYY-MM-DD
- 版本类型：新增功能 / 修复 / 架构调整

### 改动目的
为什么做这次改动。

### 改动内容
改了哪些项目/文件，新增、修改、删除了什么。

### 教程式说明
这次改动在系统里扮演什么角色，和已有部分的关系，如何运行验证。

### 验证
- 构建结果
- 测试结果
- 其他验证方式
```
