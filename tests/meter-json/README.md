# 抄表 JSON — 二十三张对照合约

给 **Meter Reading Integration** 的 **TEST Fetch (JSON)** 用（`Ctrl+Shift+T` 才看得到那个按钮）。

一个合约一个档，`ALL.json` 是全部 **252 台机**放在一起。

```
tests\meter-json\DEMO-PON.json      六台机
tests\meter-json\ALL.json           二十三张合约，252 台机
```

## 里面是什么

**2026 年 10 月**的读数 —— 九月的数字，加上那台机一个月的用量。

```json
{
  "Code": "DEMO-PON-001",     // 合约里的 Service Item No
  "SerialNumber": "3MN10250", // 机身号，抓的时候用它对机器
  "TotalBK": 838986,          // 黑白的累计读数
  "TotalCL": 42               // 彩色的累计读数
}
```

**是累计数，不是这个月印了多少。** 用量是它减掉上一次的读数 —— 所以同一个档抓两次不会算两次。

没有 `LastAuditDate`：抓的时候会自动填成你选的那个月的 1 号，所以同一个档任何月份都能用。

彩色表没动过的机器（Pontian 那台 42 → 42）就照原样写回去，用量是 0 —— 单上不会印彩色那行。

## 为什么是十月

七、八、九月在书里**已经有资料**了（`seed-three-months.sql` 灌的），抓进去会撞。十月是空的，抓下去走的是真正的那条路：对机器、查冲突、staging。

## 用法

1. Meter Reading Integration → 选 **10 / 2026**
2. `Ctrl+Shift+T` → **TEST Fetch (JSON)** → 贴一个档进去
3. 读数进 staging，对好机器
4. Generate

## ⚠️ 开单还是要照顺序

基准是「**上一张单开到哪里**」，不是「上个月的读数」。七、八、九月都还没开单，所以现在直接开十月，会算成**四个月**的量。

要看跟 PDF 一样的金额，先开七月（`reports\billing-format-check.md` 里每张的金额都列了）。要看的是抓表这条路走不走得通，那就直接抓十月，数字不用理。

## 怎么重做

书里的读数改了就重做一次 —— 这些档是从 `zSCP2_MeterEntry` 的九月资料加一个月用量生出来的。
