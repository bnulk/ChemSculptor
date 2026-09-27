# ChemSculptor WinForms 交互说明

> 适用版本：`v0.24.0` 之后
> 客户端定位：只负责用户输入、上传、轮询和展示

---

## 1. 客户端职责

WinForms 客户端只做四件事：

```text
选择坐标 txt
接收用户原始自然语言
调用服务器 HTTP API
展示服务器返回的状态、结果和验证报告
```

客户端不负责：

```text
判断任务类型
决定计算方法
决定电荷
决定自旋多重度
解析 Gaussian 输入或输出
执行纠错
```

客户端也不引用：

```text
ChemSculptor.Domain
ChemSculptor.Core
ChemSculptor.Agent
ChemSculptor.Compute
ChemSculptor.Compute.Gaussian
```

它只使用自己的 HTTP DTO。

---

## 2. 主界面

主界面分为：

```text
左侧
  会话列表

顶部
  服务地址、选择坐标 txt、保存结果

中间
  对话和计算状态

底部
  原始自然语言输入、发送按钮、取消计算按钮
```

---

## 3. 一次完整交互

### 第一步：选择坐标 txt

文件内容示例：

```text
O 0.000000 0.000000 0.117300
H 0.000000 0.757200 -0.469200
H 0.000000 -0.757200 -0.469200
```

客户端只读取文本，不解释化学含义。

### 第二步：输入自然语言

例如：

```text
单点计算
```

客户端不把这句话转换成方法、基组、电荷或多重度。

### 第三步：发送

客户端调用：

```text
POST /agent/messages
```

请求：

```json
{
  "sessionId": "session-1",
  "text": "单点计算",
  "coordinateText": "O ...\nH ...\nH ..."
}
```

### 第四步：接收作业编号

服务器返回：

```json
{
  "taskType": "SinglePoint",
  "jobId": "job-...",
  "status": "Running"
}
```

客户端将该作业加入本地轮询列表。

### 第五步：轮询状态

定时器每隔约 1.5 秒调用：

```text
GET /calculations/{jobId}/status
```

状态变化时显示：

```text
job-... 状态：Running
job-... 状态：Validated
```

### 第六步：读取结果

当状态进入：

```text
Validated
Failed
Canceled
```

客户端停止轮询该作业。

对于 `Validated` 或 `Failed`，继续读取：

```text
GET /calculations/{jobId}/result
GET /calculations/{jobId}/validation
```

### 第七步：显示结果

客户端显示：

```text
能量
能量单位
程序
方法
基组
正常终结
失败类别
验证状态
验证摘要
未通过的验证项
输出文件路径
```

### 第八步：保存

“保存结果”按钮把当前格式化结果写入 txt。

---

## 4. 取消计算

当作业仍在运行：

```text
POST /calculations/{jobId}/cancel
```

成功：

```text
作业进入 Canceled
客户端停止轮询
```

如果作业已完成：

```text
服务器返回 HTTP 409
```

---

## 5. 为什么保留一个发送按钮

旧界面有三个动作：

```text
发送
发送坐标
提交任务
```

它们容易让用户误以为客户端需要决定任务类型。

现在只保留一个动作：

```text
坐标 txt + 原始自然语言 → 发送
```

任务解释全部发生在服务器端。

---

## 6. 状态与结果的关系

```text
Running
  计算仍在进行

Validated
  结果存在且必要验证通过

Failed
  计算或验证失败

Canceled
  用户或后端已取消
```

`Validated` 仍然不等于科学结论一定正确。

正常终结只是必要条件，后续还会继续增加科学合理性验证。

---

## 7. 调试建议

先启动 Api：

```powershell
dotnet run --project src/ChemSculptor.Api
```

再启动 WinForms：

```powershell
dotnet run --project src/ChemSculptor.WinForms
```

确认：

```text
服务地址正确
坐标 txt 已选择
输入框不是空
Api 控制台没有异常
```

---

## 8. 当前边界

当前客户端已经支持：

```text
坐标上传
原始文本上传
作业轮询
结果展示
验证报告展示
取消
保存结果
```

尚未支持：

```text
多作业统一管理
服务器推送
完整历史持久化
参数确认对话
异常修正审批界面
```
