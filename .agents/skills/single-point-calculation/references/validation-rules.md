# Single Point Validation Rules

## Required Checks

```text
1. 作业标识一致
2. 程序一致
3. 方法一致
4. 基组一致
5. 电荷一致
6. 多重度一致
7. 程序正常结束
8. SCF 已真正收敛
9. 能量存在
10. 能量有限
11. 能量单位为 Hartree
12. 输出文件存在
```

## Failure Handling

任一必需检查失败：

```text
不要报告为成功
不要继续生成科学结论
转交完整单点工作流
```

## Clarification Rules

```text
KeyOnly 参数 -> 提问
同一参数多个值 -> 提问
不支持的参数 -> 拒绝
未知 raw keyword -> 拒绝
无审批时自动提交，但必须返回完整参数摘要
```
