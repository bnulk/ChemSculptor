# 单点计算 API 参考

> 适用版本：`v0.23.0` 之后
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
  "sessionId": "session-001",
  "goal": "计算水分子的单点能",
  "coordinateText": "O 0.000000 0.000000 0.117300\nH 0.000000 0.757200 -0.469200\nH 0.000000 -0.757200 -0.469200",
  "overrides": [
    {
      "name": "charge",
      "value": "0"
    },
    {
      "name": "multiplicity",
      "value": "1"
    }
  ]
}
```

字段：

```text
sessionId
  会话标识，可以为空

goal
  用户目标，可以为空

coordinateText
  必需的分子坐标文本

overrides
  可选的计算参数覆盖
```

当前支持的覆盖：

```text
program
method
basis
charge
multiplicity
```

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

## 5. 查询验证报告

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

## 6. 取消作业

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

## 7. HTTP 状态码约定

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
  或者当前作业状态不能取消
```

---

## 8. 服务调用关系

Api 端点不直接操作 Gaussian、文件或进程。

调用关系：

```text
CalculationEndpoints
  → ISinglePointCalculationService
  → ISkillInvoker
  → GaussianInputGenerationSkill
  → IComputeBackend
  → CalculationJobMonitor
  → GaussianSinglePointResultExtractionSkill
  → CalculationResultValidationSkill
```

因此 Api 的职责仅是：

```text
读取 HTTP 请求
转换为通用服务请求
调用服务
把结果转换为 HTTP 响应
```
