# Type of Billing Mode — 11 种

> 👉 解决方案在 **[BILLING-SOLUTION-PROPOSAL.md](BILLING-SOLUTION-PROPOSAL.md)**（5 个角度两轮讨论的结果）

```
1. 开几张单？        1 张  /  Rental 一张 + Meter 一张  /  每台机器一张
2. Rental 怎么排？   跨 model 合并  /  同 model 合并  /  每台一行
3. BK & CL 怎么排？  跨 model 合并  /  同 model 合并  /  每台一组
```

**BK & CL「同 model 合并」只有一个客户在用：MBJB**（52 台 → 7 行 meter，按 model 分，BK 全部 0.020、CL 全部 0.280 —— key 是 model 不是 rate）。
其他全部不是跨 model 全并，就是每台一组。

---

## 一览（v3 — panel 查证 PDF 后更正）

| Mode | 开几张 | Rental | BK & CL | 客户 |
|---|---|---|---|---|
| **1** | 1 张 | 跨 model | 跨 model | SULTAN ISMAIL, SINGLE, SYNTURN, PERMAI LAMA, PONTIAN, Normal_J0056 |
| **2** | 1 张 | 同 model | 跨 model | JPJ MELAKA, PUSPEN, **IKTBN** ⬅ 查证是 1 张单，不是 2 张 |
| **3** | 1 张 | 跨 model | 每台 | J0056 METER |
| **4** | 1 张 | 同 model | **同 model** | **MBJB** ⬅ 查证 `AMR2607.0147`：4 行 rental by model ＋ 7 行 meter by model |
| **5** | 2 张 | 跨 model | 跨 model | SULTANAH AMINAH |
| **6** | 2 张 | 同 model | 跨 model | IPG AFZAN, ROMPIN, **PASIR GUDANG** ⬅ 查证 rental 是 3 行 by model |
| **7** | 2 张 | 跨 model | 每台 | F0019 |
| **8** | 2 张 | 同 model | 每台 | KEJORA, MARA |
| **9** | 2 张 | 每台 | 每台 | KASTAM |
| **10** | 2 张 | 每台 | 跨 model | KENSINGTON |
| **11** | 每台一张 | — | — | TANGKAK |

2 × 3 × 3 ＋ 1 = **19 种可能**，用到 **11 种**。

**Panel 另外查出的：**
- TANGKAK 现在的 5 台（`CSSI 00002949-53`）**已经是一台一个合约、没有 `.C/.R`** —— 每台一张单是 CSSI 结构自然产生的，不用特别做
- KENSINGTON 是 **29 个 CSSI**（不是 19），bucket `00001781.C` 的成员不带 `.` 后缀
- 「Rental 同 model」在 JPJ 是真的按 model：3 个 model 的 BK rate 都一样 0.0285，只有 rental 单价不同（1,287.25 / 655.50 / 476.90）

---

## 「合并」的两种 —— 5 台机器（2 台 model A ＋ 3 台 model B）

| | 出几行 |
|---|---|
| **同 model 合并** | model A 一行、model B 一行（价钱不同再分） |
| **跨 model 合并** | 不看 model，**按价钱**并行 |
| 每台一行 | 5 行 |

> 「跨 model 合并」不一定只出 1 行。PASIR GUDANG 出 2 行 BK，其中一行装了 4 种 model。
> 重点是**合并的时候不看 model**，出几行由资料决定。

## 只有价钱一样才合得起来

一行发票就是一条 `张数 × 单价 = 金额`。两个不同的单价挤进同一行，这条算式就不成立了。
所以**价钱不一样 → 一定分行**，任何 mode 都一样。

PASIR GUDANG 那 2 行 BK 不是 Jean 建 meter 建出来的怪现象，是**唯一诚实的印法**：

```
BK   60,249 × 0.019   = 1,144.73     ← 8505 自己一行
BK   52,860 × 0.0285  = 1,506.51     ← 其余五台（四种 model）一行
```

JPJ 是同一条规则的另一面：12 台机器 rental 分 **3 行**（按价钱），
但 12 支 BK 共用 0.0285，所以 BK 只出 **1 行** 80,720。

| | 合并行的金额 | 价钱不同时 |
|---|---|---|
| **RENTAL** | 台数 × 一个约定的价 | 在 contract 定 group 价 → 全组用它 → 就合得起来 |
| **BK / CL** | 各机器算出来的**总和** | 合不起来，分行 |

> **RENTAL 有一个额外动作**：contract 上定了 group 价，这个价会在算钱之前写进每一台，
> 所以那一组到了排版的时候本来就一致，自然合成一行。
> 「不同价的 rental 要变一行」＝ **去讲好这条线多少钱**，不是把它们平均掉。

## Multi-Price 阶梯

**同一个 scheme 的机器照样合并**：每台各自走自己的阶梯算出金额，那条线是它们的**总和**，
印出来的单价是倒推的混合价。不同 scheme 的机器分行。
单机专属的 tier override（`#<meterkey>`）永远自己一行 —— 设 override 的意思就是「这台跟别人不一样」。

---

# 甲、一张单

## Mode 1 — Rental 跨 model ＋ BK/CL 跨 model
```
┌─ INV ──────────────┐
│  Rental            │
│  BK                │
│  CL                │
└────────────────────┘
```
SULTAN ISMAIL、SINGLE、SYNTURN、PERMAI LAMA、PONTIAN、Normal_J0056

## Mode 2 — Rental 同 model ＋ BK/CL 跨 model
```
┌─ INV ──────────────┐
│  Rental   model A  │
│  Rental   model B  │
│  BK                │   meter 不看 model
│  CL                │
└────────────────────┘
```
JPJ MELAKA、PUSPEN

## Mode 3 — Rental 跨 model ＋ BK/CL 每台
```
┌─ INV ──────────────┐
│  Rental            │
│  BK   机器 1       │
│  CL   机器 1       │
│  BK   机器 2       │
│  CL   机器 2       │
│      ⋮             │
└────────────────────┘
```
3000-J0056（METER）

## Mode 4 — Rental 同 model ＋ BK/CL 每台
```
┌─ INV ──────────────┐
│  Rental   model A  │
│  Rental   model B  │
│  BK   机器 1       │
│  CL   机器 1       │
│  BK   机器 2       │
│  CL   机器 2       │
│      ⋮             │
└────────────────────┘
```
MBJB

---

# 乙、Rental 一张 ＋ Meter 一张

## Mode 5 — 都跨 model
```
┌─ INV 1 ────────┐   ┌─ INV 2 ────────┐
│  Rental        │   │  BK            │
└────────────────┘   │  CL            │
                     └────────────────┘
```
SULTANAH AMINAH、PASIR GUDANG

## Mode 6 — Rental 同 model ＋ BK/CL 跨 model
```
┌─ INV 1 ────────┐   ┌─ INV 2 ────────┐
│  Rental model A│   │  BK            │
│  Rental model B│   │  CL            │
└────────────────┘   └────────────────┘
```
IKTBN、IPG AFZAN、ROMPIN
> ROMPIN 另外抽一台机器出来自己开第 3 张 → `SeparateInvoice`

## Mode 7 — Rental 跨 model ＋ BK/CL 每台
```
┌─ INV 1 ────────┐   ┌─ INV 2 ────────┐
│  Rental        │   │  BK   机器 1   │
└────────────────┘   │  CL   机器 1   │
                     │  BK   机器 2   │
                     │  CL   机器 2   │
                     │      ⋮         │
                     └────────────────┘
```
3000-F0019

## Mode 8 — Rental 同 model ＋ BK/CL 每台
```
┌─ INV 1 ────────┐   ┌─ INV 2 ────────┐
│  Rental model A│   │  BK   机器 1   │
│  Rental model B│   │  CL   机器 1   │
└────────────────┘   │  BK   机器 2   │
                     │  CL   机器 2   │
                     │      ⋮         │
                     └────────────────┘
```
KEJORA、MARA

## Mode 9 — 都每台
```
┌─ INV 1 ────────┐   ┌─ INV 2 ────────┐
│  Rental 机器 1 │   │  BK   机器 1   │
│  Rental 机器 2 │   │  CL   机器 1   │
│      ⋮         │   │  BK   机器 2   │
└────────────────┘   │  CL   机器 2   │
                     │      ⋮         │
                     └────────────────┘
```
JABATAN KASTAM

## Mode 10 — Rental 每台 ＋ BK/CL 跨 model
```
┌─ INV 1 ────────┐   ┌─ INV 2 ────────┐
│  Rental 机器 1 │   │  BK            │
│  Rental 机器 2 │   │  CL            │
│      ⋮         │   └────────────────┘
└────────────────┘
```
KENSINGTON
> 另外抽一台机器出来自己开第 3 张 → `SeparateInvoice`

---

# 丙、每台机器自己一套

## Mode 11
```
机器 1  ┌─ INV ──┐  ┌─ INV ──┐
        │ Rental │  │ BK, CL │
        └────────┘  └────────┘
机器 2  ┌─ INV ──┐  ┌─ INV ──┐
        │ Rental │  │ BK, CL │
        └────────┘  └────────┘
   ⋮
```
TANGKAK — 真实：5 台机器 → **9 张单**（4 rental ＋ 5 meter）

> **拆单看机器，不是 location。** PENTADBIRAN 同一个地点 2 台机器，
> 开了 2 张不同的 meter 单（`MR2607.1416`、`MR2607.1417`）。

---

# 查证记录

| 客户 | 机器 | model 数 | Rental | BK / CL | 结论 |
|---|---|---|---|---|---|
| PERMAI LAMA | 7 | **1** | 1 行 | 1 BK ＋ 1 CL | 单 model，分不出同/跨 |
| PONTIAN | 6 | 2 | 1 行 | 1 BK **跨 2 model** | 跨 model |
| PASIR GUDANG | 6 | 4 | 3 行 | 2 BK **跨 4 model** | 跨 model |
| JPJ MELAKA | 12 | 3 | **3 行 ＝ 3 model** | 1 BK **跨 3 model** | rental 同 model |
| MARA | 5 | **1** | 1 行 | 每台 BK＋CL | 单 model，分不出 |
| ROMPIN | 4 | 2 | **2 行 ＝ 2 model** | 1 BK **跨 2 model** | rental 同 model |
| TANGKAK | 5 | 2 | 每台一张 | 每台一张 | 每台拆单 |

**JPJ 是「同 model」最干净的证据**：12 台 3 个 model，rental 3 行 —— HEAVY 1 台＝DX8986、
MEDIUM 5 台＝C3935I、LIGHT 6 台＝C3926I，台数完全对上。
而且三个 model 的 BK rate 都是 **0.0285 一样**，所以分组的 key 是 **model**（rental 单价不同 1,287.25 / 655.50 / 476.90），不是 rate。

**PASIR GUDANG 是反例**：`02.MR.BK ... MEDIUM DUTY` 一行 52,860，里面装了 C5550i、
4545i×3、C4535i —— **4 种 model 并成一行**。`28,643＋10,040＋4,049＋7,171＋2,957 = 52,860` ✓

所以两种都存在，要做成设定。

---

# 行的排列顺序 ≠ mode

发票上行的先后顺序不是结构，是 item code 的编号决定的，各客户不一样：

| 客户 | 实际顺序 |
|---|---|
| MARA `AMR2607.0089` | BK 机器1 → CL 机器1 → BK 机器2 → CL 机器2 …（每台成对） |
| PASIR GUDANG `MR2607.1106` | `01.MR.BK` → `02.MR.BK` → `03.MR.CL`（BK 先全部，CL 最后） |
| JPJ `MR2607.0227` | Rental ×3 → BK → CL |
| PONTIAN `MR2607.0357` | **BK → Rental**（rental 排最后） |

---

# ⚠️ 为什么会有这么多种 —— Master Accounting 撑不住，客户拿 meter type 去凑

**上面 11 种不是 11 条业务规则。** 它们是客户在旧系统 **Master Accounting** 里凑出来的结果。

Master Accounting 没有「一张单要怎么分组」这种设定 —— 它只会照 item code 出行。
客户要做到上面那些格式，唯一的办法就是**把分组规则写进 meter type 和 meter type 的 item code 里**。
去 Master Accounting 打开 meter type 和它的 item code 看，就看得到这些 hack。

结果是：**格式的差异，其实是 item code 的差异**，不是生意规则的差异。

---

## 看得到的 6 种 hack

### Hack 1 — 建假机器来装发票
每个客户的资料里都有不存在的机器：

```
CSSI 00002357.1 ~ .6   ← 真机器
CSSI 00002357.C        ← model 写「0 COMBINE」    → 挂 meter 那张单
CSSI 00002357.R        ← model 写「RENTAL」       → 挂 rental 那张单
```

发票号是挂在 `.C` / `.R` 上的，真机器一张单都没有。
`.C` 和 `.R` 就是**分组的桶** —— 因为系统没有「分组」这个概念，只好造一台假机器来当桶。

ROMPIN 一样（`ASNI 00000112.C` = `0 COMBINE METER`、`.R` = `RENTAL`）。

---

### Hack 2 — 台数塞进 item code
```
RA-1 UNIT      RA-3 UNIT      RA-5 UNIT      RA-6 UNIT      RA-7 UNIT
RA-5 UINT_EB2B（PONTIAN，打错字也变成永久的 code）
RA-36MTH_EB2B（TANGKAK）
```
客户从 5 台加到 6 台 → 要开一个新的 item code。
台数是**合约资料**，不该长在 item 上。

---

### Hack 3 — 客户名塞进 item code
```
01.MR.BK. HOSPITAL PG        01.RA-1 UNIT HOSPITAL PG_EB2B
02.MR.BK. HOSPITAL PG        02.RA-1 UNIT HOSPITAL PG_EB2B
03.MR.CL. HOSPITAL PG        03.RA-3 UNIT HOSPITAL PG_EB2B
```
PASIR GUDANG 一个客户吃掉 6 个 item code。每多一个客户就多一批。

---

### Hack 4 — Serial number 塞进 item code
MARA 最夸张，**一台机器一个 meter type 一个 code**：
```
MR.BK.4MU10545   MR.CL.4MU10545
MR.BK.4MU10548   MR.CL.4MU10548
MR.BK.4MU10549   MR.CL.4MU10549
MR.BK.4MU10551   MR.CL.4MU10551
MR.BK.4MU10558   MR.CL.4MU10558
```
**5 台机器 ＝ 10 个 item code。** 换一台机器，10 个里面有 2 个要作废重开。

---

### Hack 5 — 编号前缀控制行的顺序和分组
```
01.MR.BK ... HEAVY DUTY
02.MR.BK ... MEDIUM DUTY
03.MR.CL ... MEDIUM DUTY
```
`01.` `02.` `03.` 不是分类，是**排序用的**。发票上行的先后完全靠这个前缀。
`HEAVY / MEDIUM / LIGHT DUTY` 也不是系统栏位 —— 是写在 description 里的字，
拿来当「这几台是同一组」的标记。

---

### Hack 6 — 用空格制造「不同」的 code
```
01. RA-3 UNIT
01. RA - 3 UNIT
01. RA- 3 UNIT
```
三个 code 意思完全一样，只差空格。因为不同客户的价钱不同，
但 item code 又不能重复 —— 只好用空格造出「不同」的 code。

---

## 附带的后遗症

| 问题 | 例子 |
|---|---|
| **假读数** | 合并行印的 Current / Previous 是加总出来的，没有任何一台机器是那个数字。ROMPIN 印 `349,707`＝79,255＋11,890＋63,876＋194,686 |
| **Item master 爆炸** | MARA 5 台机器就要 10 个 meter item code |
| **同一个 meter type 各客户不同 code** | BK 有 `109-BK C+ P`、`201-BK C+ P`、`01.MR.BK.HOSPITAL PG`、`MR.BK.4MU10545`⋯ 全部都是「BK COPY + PRINT」 |
| **报表做不出来** | code 全部不一样，没办法按 meter type 汇总全公司的 BK 用量 |
| **打错字变永久** | PONTIAN 的 `RA-5 UINT_EB2B`（UNIT 打成 UINT），已经开出去了，改不了 |
| **规则藏在字串里** | 分组规则不在任何栏位上，在 item code 的字串和 description 的字里面。没有人接手得了 |

---

## 对 ATP 的意思

**这 11 种要变成设定，不是变成 item code。**

```
合约层    InvoiceSplit      1 张 / 2 张 / 每台一张
          RentalGrouping    跨 model / 同 model / 每台
          MeterGrouping     跨 model / 每台
机器层    SeparateInvoice   这台自己开一张
          NoRental          这台不算 rental
```

设定好了之后：

- `.C` / `.R` 假机器 → 不用了，分组由设定决定
- `RA-n UNIT` → 一个 `RENTAL` item 就够，台数从合约来，description 由系统产生
- `MR.BK.<serial>` → 一个 `BK` item 就够，机器资讯从合约来
- 空格变体 → 全部合并成一个 code
- `HEAVY / MEDIUM / LIGHT` → 变成机器上的 `BillGroupCode` 栏位，不是 description 里的字

**这也是 [MESSAGE-METER-CODE-STANDARDISATION.md](MESSAGE-METER-CODE-STANDARDISATION.md) 那个提案的真正理由** ——
不是 Ms Jean 建 item 的习惯问题，是旧系统逼出来的。

---

# 两个 flag（不是 mode）

| Flag | 意思 | 客户 |
|---|---|---|
| `SeparateInvoice` | 某一台抽出来自己开一张 | ROMPIN、KENSINGTON |
| `NoRental` | 某一台不算 rental | PONTIAN、TANGKAK、PASIR GUDANG |

---

# 还没查证的

下面这几个的「同 model」是照你的描述放的，还没开 PDF 核对：
**PUSPEN、MBJB、IKTBN、IPG AFZAN、KEJORA**

以及 **IPG AFZAN 开 1 张还是 2 张单** —— 你的清单上漏了这一行。
