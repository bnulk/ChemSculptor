# 计算成果包教程 00：先冻结四个契约

## 1. 本步骤要解决什么

成果包最终包含 Markdown 报告、数据表、图片、计算原始文件和校验清单。最容易出现的问题不是文件写不出来，而是：

```text
报告生成时重新读取计算目录
不同 Workflow 各自拼 Markdown
文件清单描述不存在的文件
生成报告时又读取当前时间或最新作业
```

因此第一步先固定四个对象：

```text
ReportPackageRequest
  为什么生成、生成哪一个科学成果、采用哪种成果包剖面。

ReportEvidenceSnapshot
  报告可以读取的全部冻结证据，以及证据缺失情况。

ReportDocument
  与 Markdown 无关的章节和内容块。

ReportPackageManifest
  最终成果包内真实存在、已经校验过的文件。
```

本步骤只定义契约，不读取作业、不解析 Gaussian、不渲染、不打包。

## 2. 新增项目

代码位于：

```text
src/ChemSculptor.Reporting/
```

项目依赖只有：

```text
ChemSculptor.Compute
  CalculationJob、CalculationResult、验证报告、参数审计。

ChemSculptor.ScientificData
  ScientificResult 和科学点集合。
```

它不依赖：

```text
ChemSculptor.Agent
ChemSculptor.Api
ChemSculptor.WinForms
具体 Workflow
Gaussian
```

这条依赖规则保证报告层不会反向调用计算流程，也不会知道吸收光谱或 NTO 这些业务名称。

## 3. Request 是意图，不是证据

打开：

```text
src/ChemSculptor.Reporting/ReportPackageRequest.cs
```

关键字段：

| 字段 | 作用 |
|---|---|
| `RequestId` | 贯穿请求、快照、文档和成果包 |
| `ResultId` | 指定要报告的已归档科学成果 |
| `Profile` | Standard、ClientSummary 或 Archive |
| `RequestedSectionIds` | 可选章节，空表示按剖面默认 |
| `IncludeArtifacts` | 是否复制明确引用的原始计算文件 |
| `IncludeGeneratedData` | 是否包含报告层生成的 CSV、JSON、图 |
| `IncludeEvidenceSnapshot` | 是否把冻结证据写入成果包 |

`RequestedAt` 在请求创建时写定。后续渲染同一份证据时，不能重新取当前时间。

不要把计算坐标、输出文件内容或 Workflow 内部对象塞进 Request。这些属于证据。

## 4. Snapshot 是唯一事实来源

打开：

```text
src/ChemSculptor.Reporting/ReportEvidenceSnapshot.cs
```

它保存：

```text
ScientificResult
关联计算作业
计算规范化结果
结果验证报告
参数审计
来源指纹
证据问题
证据完整性
快照哈希
```

`ScientificResult` 使用可空类型，并在快照里保留缺失这一事实：

```csharp
public ScientificResult? ScientificResult { get; set; }
```

这比伪造一个空结果更安全。报告层可以据此输出：

```text
没有找到科学成果
```

而不是把没有数据误写成计算失败或没有计算。

`Completeness` 区分三类：

| 值 | 含义 |
|---|---|
| `Complete` | 生成该报告所需证据完整 |
| `Partial` | 仍有缺失，但可以生成并明确声明限制 |
| `Insufficient` | 缺少关键证据，不能生成有意义报告 |

`ReportEvidenceIssue` 使用稳定问题代码，例如：

```text
evidence.scientific-result.missing
evidence.validation.missing
evidence.job.missing
artifact.unavailable
```

问题代码不能使用面向界面的完整句子。这样后续 API、测试和统计可以使用同一个稳定标识。

### 关于不可变

本层使用普通 `class + 属性`，因为需要 JSON 往返和版本兼容。不可变性由第 1 步生成流程保证：

```text
一次性生成快照
序列化为规范 JSON
计算 SnapshotSha256
报告只读取已冻结快照
不允许边渲染边补读新证据
```

不要为了形式上的不可变，把所有描述类改成难以兼容和阅读的构造函数类型。

## 5. Document 是中间表示，不是 Markdown

打开：

```text
src/ChemSculptor.Reporting/ReportDocument.cs
```

结构是：

```text
ReportDocument
  List<ReportSection>
    List<ReportContentBlock>
```

内容块目前支持：

```text
Heading
Paragraph
KeyValueGrid
Table
Figure
Formula
Callout
MissingData
ArtifactTable
RawAppendix
```

一个表格由列和行组成：

```text
Columns
Rows
  Cells
```

领域模块只能提供这些通用块，不能直接返回完整 Markdown。

否则 Markdown 排版规则会散落到吸收光谱、NTO、频率和后续所有 Workflow 中。

`Figure.Files` 指向成果包内相对路径，不能保存服务器绝对路径。图片的原始数据和生成参数放在块 `Metadata` 中。

## 6. Manifest 只描述真实文件

打开：

```text
src/ChemSculptor.Reporting/ReportPackageManifest.cs
```

每个文件包含：

```text
Path
Role
MediaType
Length
Sha256
SourceReferenceId
IsRequired
```

`Path` 必须是成果包内相对路径。

`SourceReferenceId` 可用于关联原始 `PointArtifactReference`。报告生成的 `report.md` 没有原始证据标识，可以为空。

`Manifest` 只能在第 5 步成果包真正组装完成后使用。

第 0 步定义它，是为了让前面生成器知道最终要提交什么，而不是允许提前写假清单。

## 7. 统一 JSON 入口

打开：

```text
src/ChemSculptor.Reporting/ReportContractSerializer.cs
```

统一使用：

```csharp
string json = ReportContractSerializer.Serialize(value);
T? value = ReportContractSerializer.Deserialize<T>(json);
```

固定规则：

```text
camelCase
缩进 JSON
枚举使用字符串
同一契约版本使用同一选项
```

不要在各个端点或 Workflow 里自行创建新的 `JsonSerializerOptions`，否则枚举可能一会儿是 `1`，一会儿是 `"Table"`。

## 8. 测试如何证明本步骤

打开：

```text
tests/ChemSculptor.Core.Tests/ReportContractTests.cs
```

测试分别证明：

```text
ReportPackageContractsRoundTripThroughJson
  四个主契约和嵌套科学证据可以往返。

ReportDocumentRepresentsGenericWorkflowWithoutDomainSpecificTypes
  通用文档无需吸收光谱或 NTO 专用类型。

EvidenceSnapshotCanDescribeMissingEvidence
  科学成果缺失可以显式保存。

ManifestDescribesFinalPackageFilesAndChecksums
  清单保存文件角色、大小、哈希和必要性。

SerializerUsesFixedSchemaVersionAndStringEnums
  JSON 模式版本和枚举形式固定。
```

测试中的 `FindIssue` 按稳定问题代码查找，而不是假设问题列表顺序。

未来即使添加更多证据警告，测试也不会因为顺序改变而失效。

## 9. 本步骤故意没有做什么

```text
没有实现 ReportEvidenceSnapshotBuilder
没有读取 ScientificData 仓储
没有遍历 CalculationJob
没有读取原始文件
没有计算真实 SHA-256
没有注册章节提供者
没有渲染 Markdown
没有生成目录或压缩包
没有修改 API
没有修改客户端保存按钮
没有增加吸收光谱或 NTO 专用报告类型
```

这些都是后续步骤，不能在第 0 步顺手提前实现。

## 10. 下一步如何接

第 1 步实现快照生成器时，输入应是：

```text
ReportPackageRequest
IScientificDataRepository
ICalculationRepository
```

处理顺序：

```text
验证 ResultId
读取 ScientificResult
收集明确引用的作业 ID
读取作业、结果、验证和参数审计
记录缺失证据
确定完整性
生成来源指纹
序列化规范快照
计算 SnapshotSha256
```

只有以上工作完成后，报告提供者才允许读取快照并生成 `ReportDocument`。

## 11. 学习检查

如果能准确回答下面五个问题，本步骤就掌握了：

1. 为什么 Request 不能保存计算输出内容？
2. 为什么 Snapshot 必须能表示证据缺失？
3. 为什么 Document 不能直接保存 Markdown？
4. 为什么 Manifest 只能描述最终真实文件？
5. 为什么报告层不能引用具体 Workflow？

参考源码阅读顺序：

```text
ReportPackageRequest.cs
ReportEvidenceSnapshot.cs
ReportDocument.cs
ReportPackageManifest.cs
ReportContractSerializer.cs
ReportContractTests.cs
```
