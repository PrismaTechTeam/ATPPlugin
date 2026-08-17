# Billing Mode Matrix — 21 customers

> 取代 `BILLING-FORMAT-ANALYSIS.md`。
> 用 SC 定的 3 个 axis。不同的描述写法已经合并成同一个 mode。

---

## 答案：**12 种组合**，来自 **2 + 3 + 3 个设定**

| Axis | 值 | 客户数 |
|---|---|---|
| **A. Rental 是否分开一张单** | 否（rental + meter 同一张） | 12 |
| | 是（Rental 1 INV + Meter 1 INV） | 9 |
| **B. Rental 分组** | 全并一行 | 7 |
| | 按 **rate / duty class** 一行 | 12 |
| | 每台一行 | 2 |
| **C. BK & CL 分组** | 全并一组 BK + 一组 CL | 12 |
| | 按 **rate / duty class** 一组 | 3 |
| | 每台一组 | 6 |

2 × 3 × 3 = **18 种可能**，实际用到 **12 种**。

再加 3 个 flag（不是 mode，是开关）：

```
SplitByLocation   合约层  → 按 branch/location 拆单        （TANGKAK）
SeparateInvoice   机器层  → 这台自己开一张单               （ROMPIN, KENSINGTON）
NoRental          机器层  → 这台不算 rental                （PONTIAN, PASIR GUDANG?）
```

---

## 哪些描述被合并了（请确认）

**BK & CL「全并一组」—— 这 3 种写法我当成同一个：**

| 你的原文 | 客户 |
|---|---|
| "no sperate by model just group all the usage from all machine become one bk and one color" | Sultan Ismail, Sultanah Aminah, IKTBN, IPG, JPJ, PUSPEN, SINGLE, SYNTURN |
| "Group by one meter BK and on meter for CL only" | TANGKAK |
| "one bk one cl" | Normal_J0056 |

**BK & CL「每台一组」—— 这 2 种写法我当成同一个：**

| 你的原文 | 客户 |
|---|---|
| "Sperate each e.g. if have 10 machine mean will have 10 bk and 10 cl (Description will have meter Model and SN)" | F0019, J0056 METER |
| "Seprate by each single machine no group... So will be 10 bk 10 cl decription" | KASTAM, KEJORA, MARA, MBJB |

**Rental「全并一行」—— 这 4 种写法我当成同一个：**

| 你的原文 | 客户 |
|---|---|
| "Group by one meter (Even in one contract have difference type of machine model)" | F0019 |
| "Group by one meter" / "Group by one meter only" | J0056 METER, TANGKAK |
| "Group by one meter only even have difference machine" | Sultanah Aminah |
| "even have two machine but lumpsum the rental into one not group my model" | SINGLE, SYNTURN |

**Rental「按 rate」—— 括号里的变体我全当成同一个：**
`"By Model Name Group by one meter"` ＋ `(if have 3 model will have 3 rental item)` / `(if 3 difference model mean 3 rental item will be charge)` → 同一个 mode。

**两个我原本算错的：**

| 原本 | 实际 |
|---|---|
| "3 INV" 当成第 4 种 invoice split | ❌ 它是 **Rental 分开 + 一台机器 `SeparateInvoice` flag**（ROMPIN, KENSINGTON） |
| "by location" 当成第 4 种 invoice split | ❌ 它是 **1 INV for all + `SplitByLocation` flag**（TANGKAK） |

---

## 完整矩阵（12 种）

| # | A. Rental 分单 | B. Rental 分组 | C. BK & CL 分组 | 客户 |
|---|---|---|---|---|
| 1 | 是 | 全并一行 | 每台一组 | F0019 |
| 2 | 否 | 全并一行 | 每台一组 | J0056 METER |
| 3 | 是 | 按 rate | 按 rate | PASIR GUDANG |
| 4 | 否 | 按 rate | 按 rate | PERMAI LAMA, PONTIAN |
| 5 | 否 | 按 rate | 全并一组 | SULTAN ISMAIL, JPJ MELAKA, PUSPEN MUAR |
| 6 | 是 | 全并一行 | 全并一组 | SULTANAH AMINAH |
| 7 | 否 | 全并一行 | 全并一组 | SINGLE, SYNTURN, Normal_J0056, **TANGKAK**＋`SplitByLocation` |
| 8 | 是 | 按 rate | 全并一组 | IKTBN CHEMBONG, **ROMPIN**＋`SeparateInvoice`, IPG AFZAN ⚠️ |
| 9 | 是 | 每台一行 | 每台一组 | JABATAN KASTAM |
| 10 | 是 | 每台一行 | 全并一组 | **KENSINGTON**＋`SeparateInvoice` |
| 11 | 是 | 按 rate | 每台一组 | KEJORA, MARA |
| 12 | 否 | 按 rate | 每台一组 | MBJB |

⚠️ IPG AFZAN 的 invoice split 你清单上漏了，先暂放 #8。

`Normal_J0056` 只有一台机器，三个 mode 结果都一样，所以放哪一格都可以 —— 放 #7。

---

## 三个从 PDF 挖出来的更正

### 1. `KOLEJ KOMUNITI ROMPIN.pdf` 你没列到

3 张单：`AMR2607.0001` rental only（`RA - 1 UNIT` 500 + `RA - 3 UNIT` 900 = 1,400）、
`AMR2607.0087` 4 台合并 meter（BK 26,509 × 0.03 = 795.27）、
`AMR2607.0088` 单独一台 PERPUSTAKAAN（BK 174 × 0.03 = 5.22）。

跟 KENSINGTON 完全一样的形状 → 所以 KENSINGTON 不是特例，那个 `SeparateInvoice` flag 至少 2 个客户在用。

---

### 2. 不是 "By Model Name"，是 **By Rate / Duty Class**

`HOSPITAL PASIR GUDANG.pdf` 的 `MR2607.1106` 只有 3 行，客户有 6 台机器：

| 发票行 | Qty | Rate | 实际是哪几台 |
|---|---|---|---|
| `01.MR.BK...HEAVY DUTY` | 60,249 | 0.019 | `.6` = iR-ADV 8505（1 台） |
| `02.MR.BK...MEDIUM DUTY` | 52,860 | 0.0285 | `.1`+`.2`+`.3`+`.4`+`.5`（5 台） |
| `03.MR.CL...MEDIUM DUTY` | 5,693 | 0.285 | `.1` 的 colour |

第 2 行那 5 台的 model 是 **C5550i、4545i、4545i、4545i、C4535i —— 四种不同 model**。
并在一起的唯一原因是 **rate 都 0.0285**。

```
28,643 + 10,040 + 4,049 + 7,171 + 2,957 = 52,860 ✓
```

大部分客户看起来像 by model，是因为同 model 刚好同 rate。JPJ 的 `HEAVY / MEDIUM / LIGHT` 也是同一件事。
**照 model 分组的话 PASIR GUDANG 会开成 4 行，就错了。**

---

### 3. 分组机制已经在资料里 —— 也是 meter 重复的真正原因

每个 worksheet 都有同一套 CSSI 编码：

```
CSSI 00002357.1 ~ .6   ← 真实机器（只提供读数，自己不产生发票行）
CSSI 00002357.C        ← "0 COMBINE"  → 挂 INV MR2607.1106
CSSI 00002357.R        ← "RENTAL"     → 挂 INV MR2607.1107
```

ROMPIN 一模一样（`ASNI 00000112.1~.4` + `.C` + `.R`）。
**发票号挂在 `.C` / `.R` 这两个虚拟 row 上，不是挂在机器上。**

所以 `RA-3 UNIT` 不是一个 meter，**它是一个分组桶的名字**。每个客户要自己的桶（不同价、不同 duty class），只能 clone item code、用空格制造"不同"的 code —— 这就是 `01. RA-3 UNIT` / `01. RA - 3 UNIT` 的来源。

对照：BK/CL 的 item code（`109-BK C+ P`、`124-COLOR C+ P`）ROMPIN 跟别人是**共用**的。只有 RENTAL 类在被复制。因为 BK/CL 不用分桶，rental 要。

---

## 印出来的读数是加总的假读数

| 客户 | 发票印的 | 实际 |
|---|---|---|
| PASIR GUDANG `02.MR.BK` | Current 570,351 / Previous 517,491 | 5 台相加 |
| ROMPIN `AMR2607.0087` | Current 349,707 / Previous 323,198 | 79,255+11,890+63,876+194,686 ✓ |

系统要能产生这个 —— 不能只印某一台的读数。

---

## 两个边缘 case

| 情况 | 出处 | 要怎样 |
|---|---|---|
| 负数用量（换机 / 归零） | ROMPIN `.1` CL = **−86** | 夹到 0，但**那一行还是要印**（qty 空白、金额空白、rate 0.30 照印） |
| Rental 金额 0 | PASIR GUDANG `MR2607.1107` | 印 `FOC`，不是 `0.00`；Net Total 才印 `0.00` |

---

## 建议的设定

**合约层：**
```
RentalSeparateInvoice : bool
RentalGrouping        : SingleLine | ByRateClass | PerMachine
MeterGrouping         : AllInOne   | ByRateClass | PerMachine
SplitByLocation       : bool
```

**机器层（CSSI）：**
```
BillGroupCode    分组 key，就用现在的 HEAVY / MEDIUM / LIGHT DUTY
SeparateInvoice  这台自己开一张单
NoRental         这台不算 rental
```

---

## 还要确认的 3 件事

1. **IPG TENGKU AMPUAN AFZAN 是 1 张还是 2 张单？**
2. **PASIR GUDANG 的 rental 只有 5 台**（`01.RA-1 UNIT` + `02.RA-1 UNIT` + `03.RA-3 UNIT`），机器有 6 台。少的那台是 `NoRental` 还是漏开？
3. **`BillGroupCode` 就用 `HEAVY / MEDIUM / LIGHT DUTY` 当标准值可以吗？**
