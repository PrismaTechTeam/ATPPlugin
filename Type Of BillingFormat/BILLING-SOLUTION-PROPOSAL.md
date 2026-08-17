# 开单格式解决方案 — Billing Format Solution Proposal

> 基于 [BILLING-MODE-LIST.md](BILLING-MODE-LIST.md)（11 种 mode ＋ 7 个 legacy hack）。
> 5 个角度（Settings / Engine / UX / 旧数据 / Risk）各自提案，再互相挑毛病一轮，下面是收敛后的结论。
> 所有 code line、DB 数字都是 agent 实际打开文件 / 跑 query 查过的（`AED_ATPTEST`）。

---

## 0. 一句话

**Master Accounting 是死的软件，客户只能把开单规则藏进 item code 里；我们有 full code，把这些规则变成合约上的 3 个设定 ＋ 机器上的 1 个标签，钱照旧每台机器算，只是印的时候折起来。**

Risk lens 要我先跟客户讲的一句话：

> *在改任何一张发票的样子之前，我们会在测试 book 里把 7 月的每一张单重现到 sen，并且每种格式各送一张过 LHDN sandbox —— 这两个不绿、Jean 没签 diff，医院不会看到任何一张 plugin 开的单。*

---

## 1. 方案总览

```
┌──────────────────────────────────────────────────────────────────────────┐
│  A. 设定       Billing Format = master preset（11 种）→ 合约选一个   机器：Line label / Own invoice │
│  B. 引擎       钱按机器算（现有引擎不动）→ 分单 → 折行 → 印              │
│  C. UX         合约上一个 "Invoice Format" 栏 + Generate 前的 Preview 树   │
│  D. 旧数据     alias 对照表 + .C/.R 家族合并 + 读数接续 gate + 清理工具     │
│  E. 上线       LHDN sandbox 先 → 21 张 golden 对账 → 平行 2 个月 → 分批切  │
└──────────────────────────────────────────────────────────────────────────┘
```

**新增的 schema：1 张 master 表 ＋ 4 个栏位**（其他全部用现有的）：

| 新东西 | 层 | 值 | 默认（= 今天的行为） |
|---|---|---|---|
| **`zSCP2_BillingFormat`**（master，preset） | General Setup | FormatCode, Name, InvoiceSplit, RentalLineMode, MeterLineMode, ReadingText, 模板 | seed 11 种有名字的 |
| `zSCP2_Contract.BillingFormatCode` | 合约 | FK → master，`SearchLookUpEdit` 选一个 | NULL = 今天 |
| `zSCP2_Contract.RentalLineMode` | 合约 | `A` 跨 model / `M` 同 model / `S` 每台（选 format 时写入的 snapshot） | 从现有全局 `GROUP_RENTAL_BY_METER` backfill |
| `zSCP2_Contract.MeterLineMode` | 合约 | `A` / `M` / `S`（同上 snapshot） | `S`（每台一组 = 今天） |
| `zSCP2_Item.LineGroupCode` | 机器 | nvarchar(20)，如 `HEAVY DUTY` | `''`（不起作用） |

**Meter 那边没有新 schema** —— meter type 从 446 个整理成 3 个（`RENTAL` / `BK` / `CL`，加少数 `MIN` / `WAIVE`），价钱本来就住在机器上。见 §4.3。

「开几张单」**不加栏位** —— 用现有的 `BillingMode` (G/S) × `RentalSeparateInvoice` (Y/N)：

| 你要的 | BillingMode | RentalSeparateInvoice |
|---|---|---|
| 1 张单 | G | N |
| Rental 一张 ＋ Meter 一张 | G | Y |
| 每台一套 | S | Y |
| 每台一张（rent＋meter 同一张） | S | N（今天的 "Separate per service item"，保留） |

- **某一台自己开一张**（ROMPIN / KENSINGTON）= 那台机器一个独有的 `BillGroupCode`（现有栏位）—— **不加 flag**
- **某一台不算 rental**（PONTIAN / PG）= 那台没有 RENTAL meter row —— **不加 flag**，Preview 每个月都印「6 machines / 5 rentals」防止漏

---

## 2. Part A — 设定（Settings）

### 2.1 最终设定表

| 设定 | 层 | 类型 / 值 | 默认 | 状态 |
|---|---|---|---|---|
| **`zSCP2_BillingFormat`** | master | FormatCode / Name / InvoiceSplit / RentalLineMode / MeterLineMode / ReadingText / 2 模板 / Inactive | seed 11 种 | **新** `02_CreateTable_zSCP2_BillingFormat.sql` ＋ `04_Seed_zSCP2_BillingFormat.sql` |
| `BillingFormatCode` | 合约 | FK → master（SearchLookUpEdit） | NULL | **新** `02_Update_zSCP2_Contract_v14_BillingFormat.sql` |
| `BillingMode` | 合约 | G / S（选 format 时写入） | G | 现有 |
| `RentalSeparateInvoice` | 合约 | Y / N（选 format 时写入） | N | 现有 |
| `RentalLineMode` | 合约 | A / M / S（选 format 时写入） | backfill | **新**（同 v14） |
| `MeterLineMode` | 合约 | A / M / S（选 format 时写入） | S | **新**（同 v14） |
| `BillGroupCode` | 机器 | nvarchar(20) | '' | 现有 —— **只管「哪几台同一张单」** |
| `LineGroupCode` | 机器 | nvarchar(20) | '' | **新** `02_Update_zSCP2_Item_v11_LineGroup.sql` —— **只管「哪几台同一行 ＋ 印什么标签」** |
| `METER_INVOICE_GROUPING` | 全局 | FOLLOW / DEBTOR / MACHINE | FOLLOW | 现有，保留为 run-time override |
| `MERGED_READING_TEXT` | 全局 | SUM / SUM+SN / ROWS | SUM+SN | **新** PumsConfig（MeterLineMode=S 时无效） |
| `ZERO_LINES` | 全局 | PRINT / DROP | DROP（= 今天） | **新** PumsConfig，seed 翻成 PRINT |
| `REBATE_MODE` | 全局 | PCT / QTY | PCT（= 今天） | **新** PumsConfig —— **动到钱，要签字** |
| `TMPL_RENTAL_DESC` / `TMPL_METER_DESC` | 全局 | text | 空 = 今天 | **新** PumsConfig（format 上的模板 NULL 时用） |
| `GROUP_RENTAL_BY_METER` | 全局 | — | — | **退役**：一次性 backfill 进 `RentalLineMode` 后引擎不再读 |

**为什么 format 是 master 不是每张合约自己配**：Jean 的思路是「这个客户跟 Hospital 那种一样」，不是「这个客户 rental 同 model、meter 跨 model」。11 种有名字的 preset 建一次，3,000 张合约选一个；改 preset 一次影响一批；lookup 的 popup 直接看到「Used by N contracts」。合约上的 4 个 mode 栏位是选 format 时写入的 **snapshot** —— 现有 engine code 一行不用改，没选 format 的合约 = 今天。

**Reject 掉的提议**（有人提了但不需要）：per-contract 3 组 radio（改成 preset）、`RentalFirst`（行的顺序固定 Rental→BK→CL）、`InvoiceSplit=PER_GROUP`（= BillingMode G ＋ 机器有 BillGroupCode，不要第二个真相来源）、`HideItemCode`（report design 的事）、per-contract 开单日（**已经存在** `BillingDay` ＋ `Item.BillingDayOverride`；那个 28 只是 NULL fallback）、rental 税码 / `SalesExemptionNo`（AutoCount 原生 Item / Debtor 栏位）、`LayoutVersion` 当 kill switch（见 §6）。

### 2.2 分组 key —— 最终规则（改了两次才对）

**Line key**（同 key 的折成一行）：

```
Rental : R | ACItemCode | UnitPrice | n/N | StrategyNote | LineGroupCode        +M: | Model   +S: | ItemKey
Meter  : Role | ACItemCode | EffUnitPrice | RebatePct | StrategyNote            +M: | Model | LineGroupCode   +S: | ItemKey
```

- **单价永远在 key 里** —— 不同价钱的东西永远不共用一行 Qty × Price
- **`LineGroupCode` 是 role-scoped**：Rental 永远看它；Meter 只在 `M` 模式看它，`A` 模式下它只当标签（成员一致才印）
  - 为什么：JPJ 机器标 HEAVY/MEDIUM/LIGHT，rental 要 3 行，但 BK 是 12 台**一行**（rate 全 0.0285）—— 如果 label 永远进 key，BK 会被切成 3 行 ❌
- **`M` 模式 bucket = Model ＋ LineGroupCode**（两个都看，不是 COALESCE）
  - 为什么：MBJB 的 C5160 / C5150 都印 "MEDIUM HEAVY DUTY" 但是分开两行；PG 的 RA-1 / RA-3 都印 "MEDIUM DUTY" 也分开
- ladder（multi-price）/ MIN / waive 行永远不折

**三个客户的推导：**

| 客户 | 设定 | 结果 |
|---|---|---|
| PASIR GUDANG | G / Y / Rental **M** / Meter **A** | Rental 3 行（8505×1, C5550i×1, 4545i×3 全 FOC）；Meter BK 0.019 = 60,249 ＋ BK 0.0285 = 52,860（5 台 3 个 model）＋ CL 5,693 ✓ |
| JPJ | G / N / Rental **M** / Meter **A** | Rental 3 行（3 个 model）；BK 1 行 80,720 ＋ CL 1 行 10,643 ✓ |
| MBJB | G / N / Rental **M** / Meter **M** | Rental 4 行 by model；Meter 7 行 role×model ✓ |

### 2.3 Backfill —— 21 个客户

`05_Backfill_zSCP2_Contract_BillingFormats.sql`（migration lens 拥有）：按 DebtorCode ＋ ContractNo 把 MODE-LIST 里的 21 个客户 seed 进去，**只碰还是默认值的合约**。
同一个文件把 2,058 个 role `NA` 的 meter 按 meter type 翻成 RENTAL / COMMIT / WAIVE（查证：1,728 / 81 / 65 行会翻，184 个「第二条 BK」duty 行留给工具处理）。

**升级零改变的保证**：`MeterLineMode` 默认 S = 今天；`RentalLineMode` 从全局 backfill = 今天；`LineGroupCode` 空 = 不起作用；`ZERO_LINES` / `REBATE_MODE` 默认 = 今天。只有 seed script 和 Jean 手动改的合约会有不同。

---

## 3. Part B — 引擎（Engine）

### 3.1 原则

**钱全部照现在每台机器算 —— strategy / multi-price / MIN / waive / FOC 一行 code 都不动。合并只是 render-time fold：决定印几行、落在哪张单。**

```
GENERATE(period, tickedRows)
 A. rows → MeterBillLine（每台每 meter，现有 :3861-3952）＋ ModelCode / LineGroupCode / DelBranch…
 B. strategy passes 不动（GroupLimit → RentalFreeN → Waive → CommittedMin）
 C. PARTITION 分单：现有 job key（:3834-3859）  C{contract} [_I{item}] [_G{BillGroupCode}] [_R]
 D. FOLD 折行（新 class ScpInvoiceLayout；现有 GroupRentalLines 变成它的 rental 分支）
      key 见 §2.2；RenderLine = {Qty=ΣBillCopies, UnitPrice, Units, ΣCurrent, ΣLast, ΣFOC, ΣRebateQty, Members[]}
 E. ORDER  Rental → BK → CL（固定）
 F. RENDER 一个 charge row；读数 / S/N / FOC / rebate 全部进 FurtherDescription（不再是 text rows，见 §3.3）
 G. IV header → Save → WriteMeterTrans over Members（每台机器，现有 :155-247 不动）
```

**`PlanInvoices()` 必须是 side-effect-free、Preview 和 Generate 用同一条路** —— 包括 zero split、strategy passes、guard。否则 Preview 说 9 张，Generate 出 6 张。

### 3.2 印出来的字（templates，token 可改）

```
Rental 合并行 :  {Debtor}
                 MODEL:{Model} {Units} UNIT              （A 模式：MODEL:{Models} 逗号列表）
                 MONTHLY RENTAL ({n}/{N}) {LineGroup}    金额 0 → 加 " - FOC"，report 印 FOC 不印 0.00
                 Qty = Units   UOM = MTH   UnitPrice = 单台价      → MARA 5 × 250.00 = 1,250.00 ✓

BK/CL 合并行  :  {MeterDesc} — {LineGroup} ({Units} units)     e.g. BK COPY + PRINT A4&A3 — MEDIUM DUTY (5 units)
                 Qty = Σ net copies   UnitPrice = 共用 rate
                 FurtherDescription（默认 SUM+SN）:
                   S/N 2JD01705, YAJ01479, UMV05259, UPB00820, UNV01325
                   Current (23/07–26/07/2026): 570351 · Previous (23/06–24/06/2026): 517491 · Usage 52860
                 （成员日期不同就印区间，不印假日期）
                 或（ROWS）: C5550i 2JD01705 326760→355403 28643 · 4545i YAJ01479 58218→68258 10040 · …

每台一组      :  {MeterDesc}
                 MODEL:{Model} S/N:{Serial} - {Location}   顺序 BK→CL 每台成对（MARA）
```

这正是你写给 Jean 的 `MESSAGE-METER-CODE-STANDARDISATION.md` —— IKTBN `AMR2607.0108` 已经是这个样子。

### 3.3 ⚠️ P0 —— 现在的 plugin 发票**送不进 LHDN**（跟 billing mode 无关）

Engine lens 打开 AutoCount source 验证了 Risk lens 的 claim：**TRUE**。

| 现在的 code | LHDN 那边 | 结果 |
|---|---|---|
| `ScpInvoiceBuilder.cs:469-471` 每个 meter 后面加一行空白分隔行（`Description=""`） | `JsonInvoiceHelper.cs:682` 选所有 `DtlType='N' AND AddToSubTotal='T'`；`:693-695` 空 description → throw `DetailDescIsEmpty` | **每一张 plugin 发票 submit 失败** |
| `:404-408, 423-428, 435-466` 每个 meter 4-6 行 text rows（Current / Previous / Usage…） | 没设 `DefaultClassificationCode` → throw `DetailMissingClassification`；设了 → **每行变一个 RM0 line item 送去 LHDN** | 要嘛失败，要嘛 payload 里一堆垃圾 |
| `AddDetail()` 一律 `AddToSubTotal='T'`, `DtlType='N'`（`InvoicingDocument.cs:2289-2291`），plugin 从没 override | | |

**修法：** 删掉空白行；读数 / S/N / FOC / rebate 全部进 charge row 的 **`FurtherDescription`**（`JsonInvoiceHelper` 从不序列化它，V8 就是这样做的 `:217-219`，report design 已经会印）；BK / CL / RENTAL 三个 item 设 Classification code。**这个先做、先 sandbox 送 PG 1106/1107，再碰任何 fold code。**

### 3.4 会动到钱的 5 件事 —— 要客户签字

| # | 改什么 | 证据 |
|---|---|---|
| 1 | **Rebate 改数量模式**（`REBATE_MODE=QTY`）：每台 `floor((usage−FOC)×pct)`，从 qty 扣，不用 `dtl.Discount` | TANGKAK `MR2607.1416`：我们现在算 53.63 / qty 1940，单上 **53.64 / qty 1882**；PONTIAN 印 "Rebate Qty (2%): 1522" = 每台 floor 相加（整体算是 1524） |
| 2 | Rounding 改 `AwayFromZero`（`ScpInvoiceBuilder.cs:204`） | PG CL 1,622.505 → 单上 1,622.51 |
| 3 | 合并行只 round 一次（Σqty × rate） | HSI 单上 3,048.36，逐台加是 3,048.37；JPJ 3,033.26 vs 3,033.29 —— **开出去的单是真相** |
| 4 | `ZERO_LINES=PRINT`：零元发票要保存（现在 `MeterInvoiceGenerator.cs:82-95` 会丢掉） | TANGKAK `MR2607.1413-1415` RM0.00 已经 e-Invoice；PG 1107 rental FOC = 0.00 也有 UUID |
| 5 | `LayoutVersion 1` 改变 LHDN payload（每个 charge 一个 item，不再有 text rows） | §3.3 |

Per-machine 的 `Charge`（每台各自 round）还是会 stamp 进 `ScpMeterReadingLog` → 和 IV 差 ≤ (台数−1) sen，Preview 底下注明，harness 签 ±0.05 waiver。

### 3.5 Edge case 决定

| 情况 | 决定 |
|---|---|
| 负数用量（ROMPIN CL −86） | 夹到 0，**那行照印**（qty 0、金额 0、真实读数）；但**上期 > 本期 → Preview 红色、整个合约 block**（防止 migration 后静默 RM0） |
| Rental 金额 0 | 印 `- FOC`，不丢；FOC-months 的扣减现在只在 `WriteNoCharge`（`MeterInvoiceGenerator.cs:295`）→ 要走 `WriteMeterTrans` 路径不然 PG RA-3 永远 FOC |
| FOC rental 要折（PG "RA-3 UNIT FOC"） | `IsGroupableRental` `:156` 的 `Charge > 0m` 要拿掉，**而且** StrategyNote 一样的也要能折（现在 `:153-157` 有 note 就不折）；key 用 `EffUnitPrice`（FOC = 0） |
| 一组里有台没读数 | 现有 `METER_GROUP_GUARD` 延伸到 merge group：ON → block ＋ 列出哪台；OFF → 折已勾的，描述加 "(k of N units)" |
| Multi-price ladder 在合并里 | **每台各自 tier，永远不对总数 tier**；ladder 行不折（HSA `MR2607.1340` 每台 FOC 6000 没 pool，证明客户逻辑是 per machine） |
| MIN / waive | 每台（或 IsGroupItem 全 fleet）先算好，自己一行，不折 |
| Rental 单 Ref | 字面 `RENTAL`（PG / HSA / TANGKAK 都这样）；meter 单 Ref = tracking id（一台一张时 = 那台的 voucher） |
| 一台一张时 Branch / Contact | `doc.BranchCode = item.DelBranchCode`（AutoCount 自动带地址），`Attention = DelContactPerson`（TANGKAK "ANSEL" / "Office"） |

### 3.6 验收测试（4 张 PDF 必须完全重现）

| PDF | 张数 | 行 / 金额 |
|---|---|---|
| **PASIR GUDANG** | 2 | Meter: BK 60,249×0.019=1,144.73 ＋ BK 52,860×0.0285=1,506.51 ＋ CL 5,693×0.285=1,622.51 = 4,273.75；Rental: 3 行 FOC，net 0.00 |
| **MARA** | 2 | Rental 5 × 250 = 1,250 "(15/36)"；Meter 10 行 BK→CL 每台成对 = 3,103.86 |
| **ROMPIN** | 3 | Rental 1×500 ＋ 3×300 = 1,400；Meter BK 26,509×0.03 = 795.27 ＋ CL qty 0 照印；单独 BK 174×0.03 = 5.22 |
| **TANGKAK** | 9 | 4 rental "(11/36)" 769.50×3 ＋ 1,406；5 meter：3 张 0.00（FOC 5000）、53.64＋7.98=61.62、320.34＋155.47=475.81 |

---

## 4. Part C — UX（Jean 每个月怎么用）

**原则：Jean 永远看不到 "Mode 7"、"BillingMode"、"LineGroupCode" 这些字。她看到的是「这个客户的单长什么样」。**

### 4.1 Billing Format 是 **preset（master data）**，合约只是选一个

不是每张合约自己配 3 组 radio。格式是一个 **master 记录**（`zSCP2_BillingFormat`），Jean 建一次（大概 11 个），每张合约用 `SearchLookUpEdit` **选一个**。跟 Contract Type / Meter Type 一样的做法。

**Master 表** `02_CreateTable_zSCP2_BillingFormat.sql`：

```
FormatCode      nvarchar(20)  PK        e.g. HOSP-2INV-MODEL
FormatName      nvarchar(100)           e.g. "Hospital · 2 invoices · rental by model"
InvoiceSplit    char(2)  ONE / RS / PM / PMS   → 写进合约的 BillingMode + RentalSeparateInvoice
RentalLineMode  char(1)  A / M / S
MeterLineMode   char(1)  A / M / S
ReadingText     char(1)  S (SUM+SN) / R (ROWS)   NULL = 全局
RentalDescTemplate / MeterDescTemplate  nvarchar(max) NULL   NULL = 全局
Inactive        char(1)
```

`zSCP2_Contract.BillingFormatCode` FK（`02_Update_zSCP2_Contract_v14`）。选了 format → 表单同时把 resolved 值写进合约现有的 `BillingMode` / `RentalSeparateInvoice` / `RentalLineMode` / `MeterLineMode`（**snapshot，现有 code 全部照跑，没选 format 的合约 = 今天**）。改 preset 时问「套用到用这个 format 的 N 张合约？」。

`04_Seed_zSCP2_BillingFormat.sql` 预载 MODE-LIST 的 11 种（有名字），backfill 时 21 个客户直接指过去。Contract Type 可以带一个默认 format（选了 type 自动填，可改）。

**合约表单** —— `GrpBilling` 里现在的三个 checkbox（`ChkBillGroup` / `ChkBillSeparate` / `ChkRentalSeparate`，`zSCP2_Contract_Form.cs:1129-1165, 1284`）换成一个 lookup ＋ 一行摘要 ＋ 用这张合约真实机器画的 preview：

```
┌ Billing ──────────────────────────────────────────────────────────────────────────────────┐
│ Billing Due Day [ 1]   Billing Format [ HOSP-2INV-MODEL ▾🔍]  Hospital · 2 invoices · rental by model
│                        → 6 machines: Rental invoice 3 lines · Meter invoice 3 lines · ⚠ UPB00820 no rental meter
│                        ┌ INVOICE 1 · RENTAL ──────┐ ┌ INVOICE 2 · METER ─────────┐
│                        │ 8505    1 UNIT   FOC     │ │ BK HEAVY DUTY    1 machine │
│                        │ C5550i  1 UNIT   FOC     │ │ BK MEDIUM DUTY   5 machines│
│                        │ 4545i   3 UNIT   FOC     │ │ CL MEDIUM DUTY   1 machine │
│                        └──────────────────────────┘ └────────────────────────────┘
└──────────────────────────────────────────────────────────────────────────────────────────┘
```

- Lookup 的 popup grid：FormatCode · Name · Invoices · Rental · Meters · **Used by N contracts**
- Preview 用**这张合约真实的机器**画，换 format 就重算；单机合约提示「所有格式印出来都一样」

**Master 编辑器** `BillingFormat_Form`（triple，General Setup 下，`BillingFormatLst_Form` ＋ `BillingFormat_Form` 照 SimpleLookup 的 base class）—— 3 组 radio 在这里，不在合约上：

```
┌ Billing Format — HOSP-2INV-MODEL ────────────────────────────────────────────────────────┐
│ Code [HOSP-2INV-MODEL]   Name [Hospital · 2 invoices · rental by model        ]
│
│ How many invoices?            Rental lines            Black / Colour lines     Sample: [CSSC 00002357 ▾]
│ ( ) One invoice               ( ) One line for all    (•) One BK + one CL      ┌ INVOICE 1 · RENTAL ───┐
│ (•) Rental + Meter separate   (•) One line per model  ( ) One per model        │ 8505    1 UNIT   FOC  │
│ ( ) Per machine, one invoice  ( ) One line per machine( ) BK + CL per machine  │ 4545i   3 UNIT   FOC  │
│ ( ) Per machine, rental+meter                                                  ├ INVOICE 2 · METER ────┤
│     separate                                                                   │ BK … 2 lines · CL 1   │
│                                                                                └───────────────────────┘
│ Merged-line text: (•) Sum + S/N list  ( ) One row per machine  ( ) Company default
│ Tab: Format | Description text                                        Used by 5 contracts   [ OK ] [ Cancel ]
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

- Preview 挑一张 sample 合约来画（默认第一张用这个 format 的）
- `Line label` / `Own invoice` / `Rental ●` 还是在**机器**上（那是资料，不是格式）—— 见 §4.2

### 4.2 机器 grid：两个动词，两个栏

`GridViewItems`（现有 `_inlineBillGroupRepo` 的做法）：

```
No│Service Item│Serial  │Model  │…│Own invoice│Line label ▾  │Rental│
1 │2357.1      │2JD01705│C5550i │ │    ☐      │MEDIUM DUTY   │  ●   │
5 │2357.5      │UNV01325│C4535i │ │    ☐      │MEDIUM DUTY   │  ○   │  ← 没有 rental meter
6 │2357.6      │SWD00508│8505   │ │    ☑      │HEAVY DUTY    │  ●   │
```

- **Own invoice ☐** = 勾了写一个独有的 `BillGroupCode`（`SOLO-<ItemKey>`），是「这台搬去自己一张单」
- **Line label ▾** = `LineGroupCode`，是「印在行上的字 / 同 model 时的分组」
- **Rental ●○** = 只读，有没有 RENTAL meter row；改的方法是加/删那条 meter（Jean 本来就会）
- `BillGroupAssign_Form` 改名 "Machine billing tags"，加 **Fill label from Model / from BK rate** 两个按钮 —— JPJ、PG 一键搞定；原始 "Invoice group" 栏藏在 column chooser（KENSINGTON 那种少数用）
- Picker 上一句话：*"A Line label prints on the invoice; Own invoice moves the machine to its own invoice."*

### 4.3 新合约的 meter 怎么建 —— **就选 RENTAL / BK / CL，没有 unit**

**核心：`3 UNIT` 不是任何人 key 的，是系统合并的时候数出来的。**

```
Jean 建的（每台机器）                  系统开单时自己产生的
─────────────────────                  ──────────────────────
机器 2357.2  iR-ADV 4545i               HOSPITAL PASIR GUDANG
  RENTAL   300.00                       MODEL:IRADVDX4545I 3 UNIT   ← 数出来的
  BK       0.0285                       MONTHLY RENTAL (13/36)      ← 算出来的
机器 2357.3  iR-ADV 4545i                          3 MTH × 300.00 = 900.00
  RENTAL   300.00
  BK       0.0285
机器 2357.4  iR-ADV 4545i
  RENTAL   300.00
  BK       0.0285
```

所以 meter 那边只有一件事：**这台机器有哪几个 meter、各多少钱。**

**Meter type 的 dropdown 从 446 个变成 3 个**（少数情况多两个）：

| Meter Type | 什么时候用 |
|---|---|
| `RENTAL` | 有月租的机器 |
| `BK` | 黑白 |
| `CL` | 彩色 |
| `MIN` | 少数：committed minimum（KENSINGTON） |
| `WAIVE` | 少数：rental waive 契约 |

Meter type 从此只是「种类」，不是「价目」—— 价钱住在机器上（`zSCP2_ItemMeter.ChargesRate`，schema 本来就这样）。
**Jean 建新客户不用再开任何 meter type。** 建 meter type 变成罕见的管理动作。

现在的 meter panel（`GridViewMeterCfg`）**结构不用改**，只是选项变干净：

```
机器 2357.1  ·  iR-ADV C5550i  ·  S/N 2JD01705
┌ Meters ─────────────────────────────────────────────────────────────┐
│ Meter Type ▾│ Role  │ Unit Price │ Free Qty │ Rebate % │ Min │ 初始读数 │
│ RENTAL      │RENTAL │     0.00   │        0 │        0 │   0 │        0 │  ← FOC
│ BK          │  BK   │   0.0285   │        0 │        0 │   0 │  326,760 │
│ CL          │  CL   │   0.2850   │        0 │        0 │   0 │   41,172 │
└─────────────────────────────────────────────────────────────────────┘
```

选了 meter type，Role / 默认 rate 自动带出来（现有 `ViewMeterCfg_CellValueChanged` 已经会做）。Jean 只改单价。

**这些全部不用 key，开单时自己产生：**

| 发票上的 | 从哪来 |
|---|---|
| `3 UNIT` | fold 的时候数机器 |
| `MODEL:IRADVDX4545I` | 机器的 `ItemCode` |
| `S/N: YAJ01479, UMV05259…` | 机器的 serial |
| `(13/36)` | 合约的 RentalStartDate / RentalMonths |
| `MEDIUM DUTY` | 机器的 `LineGroupCode` |
| 加总读数 / FOC / rebate 数量 | 每台算完相加 |

**量大的时候（MBJB 51 台）** —— 不建新表，就在机器 grid 上勾几台 → 右键 **「Copy meters to selected」**：把当前这台的 meter 组合（type ＋ 单价 ＋ FOC ＋ rebate ＋ Line label）套到勾选的机器。已有 meter 的先出 diff（「3 台的 BK 0.03 → 0.0285，套吗？」），不闷声改。51 台同 model 的 → 一次搞定。

**存档前的检查**（不用 rate card 也能做）：

| 情况 | 提示 |
|---|---|
| 机器一个 meter 都没有 | ⚠ 这台不会开到单 |
| 非 FOC 的 meter 单价 = 0 | ⚠ 确认是免费还是漏填 |
| 同 model 的机器单价不一样 | ⚠ 列出来确认（PG 的 `.5` 没有 rental 就会在这里跳出来） |
| `RentalLineMode = M` 但机器没有 model | 🔴 挡下（同 model 分组会全部挤成一行） |
| 机器没有 RENTAL meter | 机器 grid 的 `Rental ●○` 一直看得到，Preview 每月印「6 machines / 5 rentals」 |

---

### 4.4 每月：Generate → **Preview 树** → Create

`BtnGenerateInvoice_Click` 现在是 3 个 wall-of-text guard box ＋ 1 个 confirm（`:3679-3816`）。全部换成 **`MeterInvoicePreview_Form`**（`XtraTreeList`）：

```
┌ Preview — July 2026 · 2 invoices ready · 1 blocked ──────────────────────────────────────────────┐
│                                                          This month   Last month    Δ%
│ ▼ ✔ ARENA STABIL · HOSPITAL PASIR GUDANG — RENTAL · 3 lines       0.00       0.00     —
│     ├ 4545i 3 UNIT · MEDIUM DUTY - RENTAL (13/36)                    FOC
│     │   └ 2357.2 YAJ01479 · 2357.3 UMV05259 · 2357.4 UPB00820
│ ▼ ⚠ ARENA STABIL · HOSPITAL PASIR GUDANG — METER · 3 lines     4,273.75   4,105.20   +4%
│     ├ BK … MEDIUM DUTY (5 units)         52,860 × 0.0285 = 1,506.51                    [✎ edited]
│     │   ├ 2357.1 2JD01705   326,760 → 355,403 = 28,643
│     │   └ 2357.5 UNV01325   ⚠ no reading keyed
│ ▶ ✔ MARA — METER · 10 lines                                    3,103.86   2,980.10   +4%
│ ⚠ 1 customer blocked: a machine inside a merged line has no reading. Key it in, or untick that customer.
│                                              [ Create 2 invoices (not yet submitted to LHDN) ]  [ Close ]
└──────────────────────────────────────────────────────────────────────────────────────────────────┘
```

- **Preview 永远展开到每台机器**（Jean 按机器审），客户印出来的默认还是 SUM ＋ S/N（跟他们一直看到的一样）
- Δ > ±30% 琥珀色；**上期 > 本期 红色、整个合约 block**（同 incomplete-group guard 的机制）；⚠ 合约展开，✔ 折叠
- ⚠ 的合约**跳过**、✔ 的照开 —— 不再有整批 abort
- 一行的 Description 可以改一次（✎），只进**这张**单，模板不动，下个月又是自动；有 UUID 之后锁
- 按钮写明「not yet submitted to LHDN」—— Preview 就是 review gate，MyInvois 送出还是 AutoCount 那一步
- Meter Reading grid 加 "Will bill as" 计算栏（"Meter inv · BK MEDIUM (5 units)"）＋ Invoiced tab 加 "e-Inv" 状态栏；**不加新颜色**
- 月 1 的 "Last month" 是空的（Master Accounting 没有 plugin history）→ 用 alias 把 7 月 IVDTL import 进来当 baseline

### 4.5 Description 模板

`ServiceOption_Form` 新 tab "5. Invoice Description"：两个 memo（Rental / Meter）＋ token chip `{Customer} {Branch} {Model} {Models} {Units} {MonthNo} {TotalMonths} {Serials} {MeterName} {Group}` ＋ 用真实合约渲染的例子。合约层可以 override（`BillingFormat_Form` 第二个 tab，"Use company default ☑"）。

### 4.6 三个不让步的

1. **一条 `PlanInvoices()` 同时喂 Preview 和 Generate** —— 她看到的就是开出来的
2. **不再有 abort 整批的文字墙** —— blocked 的显示 ⚠ 跳过，ready 的照开
3. **没碰过 Invoice Format 的合约印出来跟今天一模一样**；BillingMode / LineGroupCode 这些字永远不出现在画面上

### 4.7 画面清单

| 画面 | 改什么 | 量 |
|---|---|---|
| `zSCP2_Contract_Form.*` | Billing Format `SearchLookUpEdit` ＋ 摘要 ＋ 真实机器 preview；机器 grid Own invoice / Line label / Rental ● | M |
| `BillingFormatLst_Form.*` ＋ `BillingFormat_Form.*`（新，triple ×2，General Setup） | master 列表 ＋ 编辑器：4 个 invoice radio、3 个 rental、3 个 meter、sample-contract preview、Merged-line text、Description tab、Used by N | L |
| `04_Seed_zSCP2_BillingFormat.sql` | 11 种有名字的 preset | S |
| 机器 grid 右键「Copy meters to selected」 | 把一台的 meter 组合套到勾选的机器 ＋ diff 确认 ＋ 存档前检查 | S |
| `BillGroupAssign_Form.*` | 写 LineGroupCode ＋ Own invoice；Fill from Model / BK rate | M |
| `MeterReadingIntegration_Form.*` | "Will bill as" 栏、e-Inv 栏、Generate → Preview、guard 搬家 | M |
| `MeterInvoicePreview_Form.*`（新，triple） | TreeList、Δ vs last month、上期>本期 block、✎ override | L |
| `MeterReadingSetting_Form.*` | override 措辞、退役 `ChkGroupRental` | S |
| `ServiceOption_Form.*` | Tab 5 模板 ＋ token ＋ 例子 | M |
| `zSCP2_Item_Form.*` | header 显示 label / own invoice / rental | S |

---

## 5. Part D — 旧数据（Migration ＋ 长期 maintain）

### 5.1 先讲清楚：真数据在哪

- `AED_ATPLUGIN001`（CLAUDE.md 说的目标 DB）**是空的**：4 个 Item、1 张 IV
- 客户 import 进来的 book 是 **`AED_ATPTEST`**：1,835 Item、68,718 IV、618,625 IVDTL（2018-05 → **2028-01**，有未来日期的单）、3,080 合约 / 3,082 机器、446 meter type、145,845 MeterTrans（只有 **14** 笔有 `SalesInvoiceDocKey`）
- MARA / ROMPIN / KASTAM / MBJB 是 **ASN SETIA CETAK** 开的（`AMR` / `ASNI` 前缀）—— **另一本 book，不在这台机器上**

### 5.2 盘点（真实数字）

| Hack | Pattern | Items | 发票行 |
|---|---|---|---|
| H4 serial 在 code 里 | `%[-. ]XXX#####%` | 258 | 1,547 |
| H5 `01.` `02.` 前缀 | `[0-9][0-9].%` | 251 | 1,260 |
| **H7 税码后缀**（新发现） | `%_E`, `%_EB2B` | 110 | 1,797 —— `RA-36MTH`=SV-6, `_E`=SV-E6, `_EB2B`=SV-EB2B6 |
| H2b 月数 | `%[0-9]MTH%` | 74 | **34,709**（`RA-60MTH` 一个 18,066 行） |
| H2a 台数 | `%[0-9] UNIT%`, `%UINT%` | 34 | 436 |
| H3 客户名 | HOSPITAL / HSA / IPG / … | 23 | 174 |
| H6 空格变体 | `REPLACE(code,' ','')` | 2 组 / 4 个 | ASN book 里更多 |

候选 **465 个 item / 158,606 行**。真正的 canonical 只要 **≈6 个**：BK、CL、RENTAL、MIN、WAIVE、plotter。干净的其实已经占大头：`201-BK C+ P` 63,350 行、`223-COLOR C+ P` 51,011 行。

**假机器**：`.C` 38 个、`.R` 69 个，分布 107 个合约。当初 import 是**一台一个合约**；`.1~.n` 兄弟 **0 个 meter、0 笔读数**，全挂在 `.C` 上（344 ItemMeter、2,033 MeterTrans）。`02.MR.BK.HPG` 是 role NA —— 因为 `UX_zSCP2_ItemMeter_BK` 一台只准一个 BK，「MEDIUM DUTY 第二条 BK」撞到了。

### 5.3 CSSI → 合约的规则（查过 KENSINGTON / TANGKAK 后定案）

**CSSI = 合约 1:1（保留 import 的结构），只有当初有 `.C`/`.R` bucket 的家族才合并成一张合约。**

- 判断「谁是 bucket 的成员」：dot 前缀 / member Description "COMBINE CSSI x.C" / bucket 的 meter code 带 member serial（`08.MR.BK.4LS02257`）/ Jean sheet 的 INV NO 栏 —— 工具里逐个确认
- 合并后：`BillingMode G`，`RentalSeparateInvoice` = 有没有 `.R`
- 查证：TANGKAK 现在的 5 台（`00002949-53`）**已经 1:1、没有 `.C/.R`** → 什么都不用做；ROMPIN 的 `00000116` 本来就是另一个合约；KENSINGTON = 29 个 CSSI，bucket `00001781.C` 有 7 个成员（不带 `.` 后缀）→ 合并成一张
- **零个 case 需要新 flag**

### 5.4 Alias 对照表（4 张表 ＋ 1 个 view）

| 表 | 用途 |
|---|---|
| `zSCP2_ItemCodeAlias` | `LegacyItemCode` (unique) → `CanonicalItemCode`, `MeterTypeCode`, `MeterRole`, `HackFlags`, 解析出的 `UnitCount` / `TenureMonths` / `SerialNo` / `SiteTag` / `LegacyTaxCode`, `Status` SUGGESTED→APPROVED→APPLIED→REVERTED, `Confidence`, `MappedBy/At`, `BatchKey` |
| `zSCP2_MeterTypeAlias` | 241/446 meter type code 跟 StockCode 不同，item alias 不够 |
| `zSCP2_LegacyFamily` | `.C/.R` 家族 → 目标合约 |
| `zSCP2_MigrationLog` | append-only，undo 的来源 |
| `zvSCP_MeterUsageHistory` | `IVDTL JOIN IV LEFT JOIN alias` → `COALESCE(canonical, ItemCode)`，"BK usage by customer by month" 跨新旧一条 SQL |

规则：
- **已过账的 IV / IVDTL 永远不改**；有 UUID 的单 item code / description / tax code 冻结
- **CN 必须 copy 原始 `IVDTL.ItemCode`**，不是 canonical
- 新发票由引擎从 canonical meter type 写 `ACItemCode` —— 天生就是 canonical
- Legacy `dbo.Item` 保留，家族切完 ＋ 没有 active meter type 引用才 `IsActive='F'`
- **HackFlags 要挂在 CSSI 上不是 code 上**（`201-BK C+ P` 在单机是干净的，在 HSA 的 `.C` bucket 是 hack）
- 税：H7 拆出来后 tax 不再住在 item code —— `Debtor.TaxCode` ＋ `SalesExemptionNo`(PEFPOT) 在 debtor，canonical RENTAL item `TaxCode='SV-6'`，BK/CL NULL；alias 留 `LegacyTaxCode`（76 SV-6 / 14 SV-E6 / 94 SV-EB2B6）

### 5.5 读数接续（最容易出事的地方）

Risk lens 的警告：如果「上期读数」从上一张发票取，每台真机器都会继承 `.C` 的**总和** → 8 月用量负数 → 夹到 0 → **RM0 发票静默开出去**。

做法：
1. 每台真机器插一笔 `zSCP_MeterTrans`，日期 = `.C` 的日期，`SalesInvoiceDocKey NULL`，`Remark='SEED CSSI 00002357.C MR2607.1106'`，mirror 进 `InitialReading`；log `Source='SEED-LEGACY'`
2. 来源：合并格式的从 Jean 的 sheet（"Current TOTAL BK/CL"）；**per-machine 格式（KASTAM / MARA / TANGKAK / MBJB）从 7 月发票行取**（KENSINGTON sheet 写 1,764 发票是 2,236.56 —— sheet 不是真相）；找不到 → PUMS history → 手动 key `Source='SEED-MANUAL'` `Confidence=LOW`；刚好只缺一台可以用 `.C − Σ已知` 反推（flag）；缺 >1 台 block
3. **HARD gate**：Σ seeds（每 meter type）≠ `.C` 最后读数 → 整个 transaction rollback，batch 停在 DRYRUN
   - PG：355,403+68,258+42,472+79,314+24,904 = 570,351 ✓、594,908 ✓、46,865 ✓
4. M0 的 `zSCP2_MeterEntry` 盖成已开票 —— **但用 DocNo ＋ `Source='MIGRATION'`、DocKey NULL**，因为 `ReconcileDeletedInvoices`（`:1751-1755`）看 `dbo.IV` 找不到 DocKey 就会解除印记，ASN 的单根本不在这本 book
5. 引擎：上期 > 本期 → Preview block（不夹 0）；migration 加一个 first-run audit：任何 migrated 机器 usage 0 但 current > 0 → alert
6. 2027-2028 未来日期的 IV：隔离，不参与 seed / baseline

### 5.6 清理工具 `LegacyCodeMigration_Form`（plugin 里，triple file）

`General Setup\Maintenance Forms\`，逻辑在 `Classes\ScpLegacyMigration.cs`，**白名单写入**（4 张新表、`zSCP2_Item.ContractKey/Inactive/LegacyRole`、`zSCP2_ItemMeter` insert、`zSCP_MeterTrans` seed insert、`zSCP2_MeterReadingLog`、`dbo.Item.IsActive`；**没有任何 code path 碰 IV / IVDTL / CN**）。

Tabs：Item Codes · Meter Types · Fake Machines · Log/Undo
流程：**Scan → Suggest → Approve（勾选） → Dry-run（报表 grid ＋ Excel 列出每一个要改的） → Apply（一个 batch 一个 transaction） → Undo batch**

- **Undo 拒绝**：受影响的 meter 只要有一笔 stamped row（开过单）就不准 undo —— 不做 soft-delete
- 每个 batch 前 `BACKUP DATABASE`
- Grid：`Sel | LegacyItemCode | Description | Family | HackFlags | Units | Months | Serial | Site | LegacyTaxCode | IVLines | LastUsed | ActiveMachines | SuggestedCanonical | SuggestedMeterType | Confidence | Status | MappedBy | MappedAt | Notes`
- 新 access right

### 5.7 两本 book（ATP / ASN）

不加栏位 —— 开票公司、SST no、TIN、IV 编号、item master 在 AutoCount 是 book-level 的。同一个工具、同一套 SQL，**每本 book 跑一次**（migration 在 load 时跑）。ASN 差异：item master（`RA - 1 UNIT`、`MR.BK.<serial>`）、meter type、DocNo（`AMR`/`ASNI`）、debtor、e-Invoice（ASN 没验证过 UUID → 冻结规则一样适用，限制更少）。
**要开始 ASN 需要客户给：** ASN book 的 `.bak`（或 V8 / Master export）、ASN 的月度 Excel、plugin Guid 注册在那本 book、谁负责 ASN 开单。

---

## 6. Part E — 上线（Rollout）

### 6.1 顺序（先做什么）

```
0. LHDN sandbox   —— 删空白行 + FurtherDescription，每种格式各送一张（含 RM0），PG 1106/1107 第一个
1. Money sign-off —— §3.4 五件事客户签字
2. Golden harness —— 21 个客户 7 月的 PDF 转 JSON，测试 book 里 Generate，diff
3. Settings + Engine + Preview（同一条 PlanInvoices）
4. Migration tool + alias + seed
5. 平行跑 ≥ 2 个月（copy book；Master Accounting 继续开真单）
6. 分批切
```

### 6.2 对账 harness（怎么证明新引擎 = 7 月）

- **Golden set**：`pdftotext` 21 份 PDF → `Type Of BillingFormat\golden\2607\<customer>.json`，每张单 {entity, docno, docdate, debtor, ref, lines[{code, desc, qty, uom, unitprice, amount, focQty, rebateQty, prev, curr}], netTotal, tax}。**Excel 第一页不是真相**（KENSINGTON）
- **测试 book**：从 production restore（每个 entity 一本），seed 6 月 previous ＋ 7 月 current，设 21 个合约，Generate 7 月，抽 IV/IVDTL 出同样的 JSON
- **Diff 层级**：L0 张数 ＋ 哪台在哪张 · L1 行数/顺序/alias · L2 qty 精确、unit price 4 位、金额 · L3 net · L4 text（FOC、rebate qty、读数 —— 日期不比，legacy 日期是假的）
- **容差 0.00**；只有 JPJ / MBJB / PONTIAN 的 rounding case 签 ±0.05 waiver
- **验收 6 个**（要过 L0-L4 ＋ 真的送 MyInvois pre-prod）：PG、TANGKAK、ROMPIN、MARA、JPJ、KENSINGTON；21 个全部过 L0-L3
- Diff 存 JSON ＋ diff MD 进 repo，每次跑一份

### 6.3 平行跑与切换顺序

| 波 | 客户 | 为什么 |
|---|---|---|
| 1 | SINGLE, SYNTURN, TANGKAK | 最简单 / 已经 1:1 |
| 2 | KASTAM, F0019, J0056 | per-machine 格式，无合并 |
| 3 | PASIR GUDANG | 第一个 `.C` 合并，Σ gate 最干净 |
| 4 | KENSINGTON | 29 CSSI、非 dot bucket、group MIN —— 不简单，放 PG 后 |
| 5 | PONTIAN, HSA, HSI, JPJ, PUSPEN, IKTBN, IPG, KEJORA | 合并格式 |
| 6 | ASN book（MARA, ROMPIN, KASTAM…） | 等 C7 拿到 book |
| 7 | MBJB | 最后；bespoke 3 页 template 先手工 |

- Go / no-go 每个客户：**连续两个月 zero-diff**、MyInvois pre-prod 验证过、医院/中间商 AP 看过 sample 没反对、Jean 签 diff sheet
- **Kill switch = 平行期间继续用 Master Accounting 开**（一张单只能开一次，不能双开）；`LayoutVersion 0` 不是真的 kill switch —— 那是 plugin 的旧样子（`BuildInvoice :213-221` copy V8 book），不是 Master Accounting，而且还有空白行 LHDN 会拒
- 切换前一个月：一页 letter ＋ sample 给每家医院 / 中间商（Sky Active、Arena Stabil —— 他们 AP 对的是行结构和 Item Code 栏）；Ref No / PO No 语义不变，编号序列不变

### 6.4 明确不做的

MBJB 的 bespoke template（第 7 波前手工）· alias 表的 UI（SQL seed ＋ report 够了）· pixel/PDF diff（比数据）· 重现假的读数日期 · FOC pooling 当默认 · 迁移历史发票的 code · 每客户 report design · ASN 任何东西直到拿到 book

---

## 7. 这轮讨论挖出来的 blocker（跟 mode 无关但必须先解决）

| # | 问题 | 严重度 | 证据 |
|---|---|---|---|
| 1 | **Plugin 发票送不进 LHDN**（空白分隔行 ＋ text rows） | 🔴 fatal | §3.3，`ScpInvoiceBuilder.cs:469-471`，`JsonInvoiceHelper.cs:682-712` |
| 2 | **两个法人 = 两本 book**，ASN 那本我们看不到 | 🔴 | MARA / ROMPIN / KASTAM / MBJB = `AMR` 前缀、Public Bank、没 UUID |
| 3 | 单价小数：dev book `SalesPriceDecimal=2` → 0.0285 印成 0.03 | 🟠 | 查 production，设 ≥4 |
| 4 | **合约开单日上限 28**（`v8_RetireMonthEnd.sql`，2026-07-27 决定退役月底）→ legacy 的 31/7 做不出来 | 🟠 | JPJ 22/7、KENSINGTON 21/7 可以；31/7 是 product 决定不是 engine |
| 5 | Doc date ≠ issue date：legacy 单日期 31/7 但 24-29/7 验证；plugin 程序化存档没挂 `CheckTodayDateAsEInvoiceIssueDate` 的 confirm | 🟠 | `InvoicingDocument.cs:10350-10400`；production 开 `AlwaysUseTodayDateAsEInvoiceIssueDate` |
| 6 | 税：SST 6% 只在 rental（MBJB、MARA —— MARA 印的是 "**GST** @ 6%" 是旧 label 不能照抄）；AutoCount 把 debtor 税码套到所有行 | 🟠 | RENTAL item 带税码 ＋ debtor `SalesExemptionNo`；6% vs 8% 让税务代理确认 |
| 7 | 合并错一张 = 整张 CN（72 小时内可改 `EnableEditValidatedEInvoiceWithin72Hours`） | 🟡 | 所以 Preview 是 review gate，永远不在 Generate 时自动送 |
| 8 | TANGKAK 印的 "Sales Tax No." 是 ASN 的号码 —— header 主档是脏的 | 🟡 | 不要盲目迁 header 栏位 |

---

## 8. 要客户回答的问题（合并、排序）

1. **ATP vs ASN**：哪些合约在哪本 book？会有两本 book？ASN 上 e-Invoice 了吗？→ 没有答案 ASN 那边完全不能开始
2. **钱的规则确认**（§3.4）：FOC 每台、rebate 每台 floor 再相加、合并行 round 一次 —— 要**完全重现**发票（yes/no）？
3. **合并行印什么**：加总读数 ＋ S/N（今天的样子）还是每台一行？有没有任何 AP 拒收过发票、为什么？
4. **发票日期政策**：DocDate 31/7 ＋ issue date = 今天？还是 date = 开单日？JPJ 22/7 是故意的？月底 31 要不要开回来？
5. **PASIR GUDANG rental 只有 5 台**（1+1+3），机器 6 台 —— `.5` C4535i 是故意没 rental 还是漏了？
6. **Line label**：印 HEAVY / MEDIUM / LIGHT DUTY 还是 model 名？系统可以从 model 自动填吗？
7. **Item Code 栏**：客户看的单上可以消失（或显示 canonical）吗？有没有 AP 系统靠它对账？
8. **`109-BK C+ P` vs `201-BK C+ P`**（和 `106` vs `223`）差在哪 —— 并掉还是保留两个？

---

## 9. Panel 争议 → 怎么定的

| 争议 | 各方 | 结论 |
|---|---|---|
| 分组 key | Settings：mode＋price＋model＋label；Engine：rate＋model 就够；UX：重用 BillGroupCode；Risk：MBJB 要 by model | `BillGroupCode` 只管分单；新 `LineGroupCode` role-scoped（rental 永远、meter 只在 M）；MBJB 证明 meter 要 M；JPJ 证明 A 模式下 label 不能进 key |
| SeparateInvoice flag | Settings：BillGroupCode of one；Engine/Risk：先定 CSSI 映射 | 查了 DB：零个 case 需要 flag |
| NoRental flag | Settings：derived；Engine/UX：以为要 flag | derived，Preview 每月印 "N machines / M rentals" |
| `InvoiceSplit=PER_GROUP` | UX 要；Settings 反对 | 不要（第二个真相来源） |
| 合并行读数 | Engine：SUM+SN；Risk：每台 rows；UX：问 Jean | 印 SUM+SN（客户习惯），Preview 永远每台；ROWS 是 per-contract option；两种都在 FurtherDescription（LHDN 只看到 1 个 item） |
| Rounding | Engine：round 一次；Risk：JPJ 单是真相 | 一致：round 一次；per-machine log 差 ≤ 1-2 sen 注明 |
| Kill switch | Settings：`LayoutVersion 0`；Risk：那不是 Master Accounting | 平行期 = Master Accounting 继续开；LayoutVersion 保留但不当 kill switch 卖 |
| 两个法人 | Migration/Risk 发现；其他三个没考虑 | 两本 book，不加栏位 |
| 开单日 28 hardcoded | Risk 说 hardcoded | Settings/Engine 查证：只是 NULL fallback，真正限制是合约上限 28（v8 退役月底）→ 变成 product 问题 |
| Backfill 来源 | Settings：从全局一次性 | Risk：3,073 全 A 不对，21 个有名的 10 个是 M/S → 按合约 seed |

---

## 10. 分阶段 / 工作量

| Phase | 内容 | 量 |
|---|---|---|
| **P0** | LHDN 修复（空白行、FurtherDescription、Classification）＋ sandbox 送 PG；`AwayFromZero`；单价小数 | S–M，**先做** |
| **P1** | `zSCP2_BillingFormat` master ＋ seed 11 种 ＋ 合约 FK ＋ 4 栏位 ＋ backfill 21 客户 ＋ NA role 翻转 | S–M |
| **P2** | Engine：`ScpInvoiceLayout` fold ＋ `PlanInvoices()` ＋ REBATE_MODE / ZERO_LINES ＋ FOC rental fold ＋ guard 延伸 | L |
| **P3** | UX：`BillingFormat_Form` ＋ 合约表单 ＋ 机器 grid ＋ `MeterInvoicePreview_Form` ＋ 模板 tab | L |
| **P4** | Golden harness（21 JSON ＋ diff）＋ 6 个验收 ＋ MyInvois pre-prod | M |
| **P5** | Migration：4 张表 ＋ view ＋ `LegacyCodeMigration_Form` ＋ seed/gate ＋ family 合并 | L |
| **P6** | 平行 2 个月 ＋ 分 7 波切换 ＋ ASN book | 时间，不是工 |

P0 和 P4 的 harness 骨架可以先动，不用等客户答问题；P2 里动到钱的 3 件事要等第 2 题签字。
