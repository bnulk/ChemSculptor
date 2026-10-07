# 单点计算 API 参考

> 适用版本：`v0.27.0` 之后
> API 前缀：`/calculations`
> 设计原则：Api 只做 HTTP 适配，业务统一交给 `ISinglePointCalculationService`

---

## 1. 端点总览

| 方法 | 路径 | 作用 |
|---|---|---|
| `POST` | `/calculations/single-point` | 提交单点计算 |
| `GET` | `/calculations/{jobId}` | 查询作业状态 |
| `GET` | `/calculations/{jobId}/status` | 查询作业状态 |
| `GET` | `/calculations/{jobId}/result` | 查询规范化结果 |
| `GET` | `/calculations/{jobId}/validation` | 查询验证报告 |
| `POST` | `/calculations/{jobId}/cancel` | 取消运行中的作业 |

`GET /calculations/{jobId}` 和 `/status` 返回同一类状态数据。前者是简写，
后者语义更明确。

---

## 2. 提交单点计算

请求：

```http
POST /calculations/single-point
Content-Type: application/json
```

请求体：

```json
{
  "coordinateText": "O 0.000000 0.000000 0.117300\nH 0.000000 0.757200 -0.469200\nH 0.000000 -0.757200 -0.469200",
  "text": "计算水分子的单点能"
}
```

字段：

```text
coordinateText
  必需的分子坐标文本

text
  必需的客户原始自然语言，一字不改地交给服务器端理解
```

客户端不发送：

```text
charge
multiplicity
method
basis
program
overrides
```

这些计算参数由服务器端的会话、规划和审批流程决定。

当前默认电荷为 `0`。默认自旋多重度在坐标解析完成后计算：

```text
总电子数 = 全部原子序数之和 - 总电荷

偶数电子 → multiplicity = 1
奇数电子 → multiplicity = 2
```

用户或流程已经显式指定多重度时，不使用该默认规则。

成功时返回 HTTP 202：

```json
{
  "succeeded": true,
  "jobId": "job-...",
  "status": "Running",
  "inputFilePath": "C:\\...\\input\\job-....gjf",
  "outputFilePath": "C:\\...\\run\\output.log",
  "message": "Gaussian 16 单点计算已提交。",
  "diagnostics": []
}
```

失败时返回 HTTP 400：

```json
{
  "error": "坐标文本不能为空。",
  "diagnostics": []
}
```

---

## 3. 查询状态

```http
GET /calculations/{jobId}
```

或：

```http
GET /calculations/{jobId}/status
```

响应：

```json
{
  "jobId": "job-...",
  "state": "Running",
  "startedAt": "2026-09-27T05:00:00+00:00",
  "completedAt": null,
  "inputFilePath": "C:\\...\\input\\job-....gjf",
  "outputFilePath": "C:\\...\\run\\output.log",
  "diagnostics": []
}
```

主要状态：

```text
Created
InputGenerated
Queued
Running
Completed
Parsed
Validated
Failed
Canceled
```

如果作业不存在，返回 HTTP 404。

---

## 4. 查询结果

```http
GET /calculations/{jobId}/result
```

结果尚未就绪时返回 HTTP 409。

就绪后返回：

```json
{
  "jobId": "job-...",
  "energy": -76.3801013836,
  "energyUnit": "Hartree",
  "normalTermination": true,
  "failureKind": "None",
  "program": "Gaussian 16",
  "method": "CAM-B3LYP",
  "basis": "6-31G*",
  "charge": 0,
  "multiplicity": 1,
  "outputFilePath": "C:\\...\\run\\output.log",
  "diagnostics": []
}
```

---

## 5. 查询和下载科学点文件

查询清单：

```http
GET /calculations/{jobId}/artifact-manifest
```

响应：

```json
{
  "resultId": "scientific-result-job-...",
  "rootJobId": "job-...",
  "points": [
    {
      "pointId": "point-job-...",
      "directoryName": "01-original-m1",
      "files": []
    }
  ]
}
```

`kind` 是通用用途，不包含具体程序名称：

```text
Input
PrimaryOutput
SupportingOutput
RestartState
Other
```

逐个下载：

```http
GET /scientific-results/{resultId}/artifacts/{pointId}/{artifactId}
```

服务器只允许访问对应科学点的 `run` 目录，并验证相对路径、文件存在性和 SHA-256。
客户端只需要按清单下载，不需要判断 `.log`、`.gjf` 或 `.fchk` 的含义。

如果科学点文件清单尚未生成，返回 HTTP 404。

---

## 6. 查询验证报告

```http
GET /calculations/{jobId}/validation
```

响应：

```json
{
  "jobId": "job-...",
  "passed": true,
  "status": "Passed",
  "summary": "全部必要验证通过。正常终结是必要条件，但不是充分条件。必要检查 14/14 项通过。",
  "validatorName": "single-point-result-validator, gaussian-single-point-output-validator",
  "validatedAt": "2026-09-27T05:00:10+00:00",
  "checks": [
    {
      "code": "calculation.output_normal_termination",
      "description": "输出文件中包含正常终结",
      "passed": true,
      "severity": "Error",
      "requirement": "Required",
      "scope": "ProgramOutput",
      "expectedValue": "true",
      "actualValue": "True",
      "message": "通过。"
    }
  ],
  "issues": []
}
```

验证项目分级：

```text
Required
  必须通过

Recommended
  失败时产生警告

Informational
  只作为信息记录
```

验证范围：

```text
Structure
ProgramOutput
Numerical
ScientificPlausibility
CrossResultConsistency
Other
```

正常终结只属于：

```text
Required + ProgramOutput
```

它不是科学结果正确的充分条件。

验证报告尚未就绪时返回 HTTP 409。

---

## 7. 取消作业

```http
POST /calculations/{jobId}/cancel
```

可以取消：

```text
Queued
Running
```

成功：

```json
{
  "jobId": "job-...",
  "canceled": true,
  "message": "计算作业已取消。"
}
```

不能取消时返回 HTTP 409：

```json
{
  "error": "当前作业状态不能取消：Validated"
}
```

---

## 8. HTTP 状态码约定

```text
202
  作业已接受并开始后台处理

200
  查询成功或取消成功

400
  请求内容错误或无法提交

404
  作业不存在

409
  结果或验证尚未就绪
  当前作业没有可下载文件
  或者当前作业状态不能取消
```

---

## 9. 服务调用关系

Api 端点不直接操作 Gaussian、文件或进程。

调用关系：

```text
CalculationEndpoints
  → ISinglePointCalculationService
  → ISinglePointWorkflowEngine
  → WorkflowEngine
  → CalculationInputPreparationSkill
  → CalculationSubmissionSkill
  → CalculationWaitSkill
  → CalculationResultExtractionWorkflowSkill
  → CalculationWorkflowValidationSkill
  → CalculationWorkflowProcessingPlanSkill
```

因此 Api 的职责仅是：

```text
读取 HTTP 请求
转换为通用服务请求
调用服务
把结果转换为 HTTP 响应
```
