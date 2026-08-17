# Billing Format 分析 — 2026 年 7 月单据实际长什么样

资料来源：这个资料夹里的 **22 份 PDF**，涵盖 **20 个客户**，两家开单公司
（ATP SALES & SERVICES 和 ASN SETIA CETAK），invoice 日期 21/7/2026 – 31/7/2026。

下面每一条都标了出处 PDF 和 invoice no.，任何一条有疑问都可以翻原档验证。

---

## Source index — 档案对照表

| # | PDF | 客户 | Debtor | 机器 | 档案里的 invoice |
|---|---|---|---|---|---|
| 1 | `3000-F0019 INV 2607.pdf` + `3000-F0019 METER 2607.pdf` | FASTROCOM (M) / Pejabat Daerah dan Tanah Alor Gajah | 3000-F0019 | 10 | `MR2607.0303` 租金 · `MR2607.1133` 抄表（7 页） |
| 2 | `3000-J0056 INV 2607.pdf` + `3000-J0056 METER 2607.pdf` | Jabatan Perlindungan Hidupan Liar dan Taman Negara | 3000-J0056 | 4 | `MR2607.1134` |
| 3 | `HOSPITAL PASIR GUDANG.pdf` | Arena Stabil / Hospital Pasir Gudang | 3000-A0200 | 6 | `MR2607.1106` 抄表 · `MR2607.1107` 租金（RM 0.00） |
| 4 | `HOSPITAL PERMAI LAMA.pdf` | Arena Stabil / Jabatan Kesihatan Negeri Johor | 3000-A0074 | 7 | `MR2607.1338` |
| 5 | `HOSPITAL PONTIAN.pdf` | Arena Stabil / Hospital Pontian | 3000-A0090 | 6 | `MR2607.0357` |
| 6 | `HOSPITAL SULTAN ISMAIL.pdf` | Arena Stabil / Hospital Sultan Ismail | 3000-A0179 | 14 | `MR2607.1347` |
| 7 | `HOSPITAL SULTANAH AMINAH.pdf` | Arena Stabil / HSA Johor Bahru | 3000-A0102 | 6 | `MR2607.1216` 租金 · `MR2607.1340` 抄表 |
| 8 | `HOSPITAL TANGKAK.pdf` | Arena Stabil / Hospital Tangkak | 3000-A0213 | 5 | **9 张**：`MR2607.1219–1222` 租金 · `MR2607.1413–1417` 抄表 |
| 9 | `INSTITUT KEMAHIRAN TINGGI BELIA NEGARA (IKTBN) CHEMBONG.pdf` | IKTBN Chembong | 3000-I0001 | 4 | `AMR2607.0108` |
| 10 | `INSTITUT PENDIDIKAN GURU KAMPUS TENGKU AMPUAN AFZAN.pdf` | Arena Stabil / IPG Pahang | 3000-A0199 | 6 | `MR2607.1439` |
| 11 | `JABATAN KASTAM DIRAJA MALAYSIA TANJUNG KUPANG.pdf` | Jabatan Kastam Tg Kupang | 3000-J0026 | 24 | `AMR2607.0074` 租金（5 页）· `AMR2607.0124` 抄表（17 页） |
| 12 | `JPJ MELAKA.pdf` | Sky Active / JPJ Melaka | 3000-S0136 | 12 | `MR2607.0227` |
| 13 | `KOLEJ KOMUNITI ROMPIN.pdf` | Kolej Komuniti Rompin | 3000-K0002 | 5 | `AMR2607.0001` 租金 · `AMR2607.0087` + `AMR2607.0088` 抄表 |
| 14 | `LEMBAGA KEMAJUAN JOHOR TENGGARA (KEJORA).pdf` | KEJORA | 3000-L0003 | 18 | `AMR2607.0031` 租金 · `AMR2607.0153` 抄表（13 页） |
| 15 | `MAJLIS AMANAH RAKYAT (MARA).pdf` | MARA Johor | 3000-M0001 | 5 | `AMR2607.0020` 租金 · `AMR2607.0089` 抄表（4 页） |
| 16 | `MBJB.pdf` | Majlis Bandaraya Johor Bahru | *(单上没印)* | 51 | `AMR2607.0147`（3 页）+ 4 张工作表 |
| 17 | `PUSAT PEMULIHAN PENAGIHAN NARKOTIK (PUSPEN MUAR).pdf` | PUSPEN Muar | 3000-P0013 | 4 | `AMR2607.0138` |
| 18 | `SINGLE.pdf` | Single Advertising & Trading | 3000-S0027 | 2 | `MR2607.1250` |
| 19 | `SYNTURN.pdf` | Synturn (M) | 3000-S0033 | 2 | `MR2607.0192` |
| 20 | `KENSINGTON.pdf` | Kensington Green Specialist Centre | 3000-K0011 | 18 | `MR2607.0186` |

⚠️ `SINGLE.pdf` 的「SINGLE」是**客户名字**（Single Advertising），不是「一台机」的意思。

---

## 结论：不是 2 种，20 个客户 20 种

目前讲的两个 billing mode —

1. Rental + Reading 合并成 1 张 IV
2. Rental 一张 IV，Reading 另一张 IV

—— 这两条是对的，客户也几乎对半分（11 : 9）。**但它只是六个维度里的第一个**。
另外五个维度各自在变，组合起来就变成每个客户都长得不一样。

所以「有几种 format」这个问题本身要换个问法。真正要决定的是：**contract 上要开哪几个选项**。

---

## 六个维度

### ① Rental 和 Meter 分不分单

| 合并 1 张 | 分开 2 张（或更多） |
|---|---|
| `HOSPITAL PERMAI LAMA.pdf` · `HOSPITAL PONTIAN.pdf` · `HOSPITAL SULTAN ISMAIL.pdf` · `INSTITUT KEMAHIRAN…CHEMBONG.pdf` · `INSTITUT PENDIDIKAN GURU….pdf` · `JPJ MELAKA.pdf` · `SINGLE.pdf` · `SYNTURN.pdf` · `PUSAT PEMULIHAN…(PUSPEN MUAR).pdf` · `MBJB.pdf` · `3000-J0056 INV 2607.pdf` | `HOSPITAL PASIR GUDANG.pdf` · `HOSPITAL SULTANAH AMINAH.pdf` · `HOSPITAL TANGKAK.pdf` · `JABATAN KASTAM…TANJUNG KUPANG.pdf` · `KOLEJ KOMUNITI ROMPIN.pdf` · `LEMBAGA KEMAJUAN…(KEJORA).pdf` · `MAJLIS AMANAH RAKYAT (MARA).pdf` · `3000-F0019 INV 2607.pdf` |

### ② Meter 收费怎么拆行 — 5 种

| 做法 | 出处 | 证据 |
|---|---|---|
| 每台机 **× 每个颜色** 一行 | `3000-F0019 INV 2607.pdf` · `JABATAN KASTAM….pdf` · `LEMBAGA KEMAJUAN…(KEJORA).pdf` · `MAJLIS AMANAH RAKYAT (MARA).pdf` | Kastam `AMR2607.0124`：**37 行、17 页**，serial 直接写进 item code（`MR.BK.4WE04767`） |
| **一台机一张 invoice** | `HOSPITAL TANGKAK.pdf` | 5 台机 → **9 张单**：`MR2607.1219`、`.1220`、`.1221`、`.1222`、`.1413`–`.1417` |
| 按 **model 型号** 分组 | `MBJB.pdf` · `PUSAT PEMULIHAN…(PUSPEN MUAR).pdf` · `INSTITUT KEMAHIRAN…CHEMBONG.pdf` | MBJB `AMR2607.0147`：51 台机 → 7 行（`MR.BK.imageFORCEC5160` 一行盖 36 台） |
| 按 **duty class / 费率** 分组 | `JPJ MELAKA.pdf` · `HOSPITAL PASIR GUDANG.pdf` · `HOSPITAL SULTAN ISMAIL.pdf` | Sultan Ismail `MR2607.1347` 第 3、4 行**描述一模一样**都是 `BK COPY + PRINT A4&A3`，只差费率 0.0285 vs 0.019 |
| 整个车队 **BK 一行 + CL 一行** | `3000-J0056 INV 2607.pdf` · `HOSPITAL PERMAI LAMA.pdf` · `HOSPITAL PONTIAN.pdf` · `INSTITUT PENDIDIKAN GURU….pdf` · `KENSINGTON.pdf` · `SINGLE.pdf` · `SYNTURN.pdf` · `KOLEJ KOMUNITI ROMPIN.pdf` | Kensington `MR2607.0186`：18 台机 → 2 行 |

### ③ Rental 那一行怎么写 — 5 种写法

| 写法 | Qty 是什么意思 | 出处和实际那一行 |
|---|---|---|
| 每台一行 | 1 MTH | `JABATAN KASTAM….pdf` `AMR2607.0074` — 23 行，`Total Quantity : 23` · `HOSPITAL TANGKAK.pdf` `MR2607.1219` `MONTHLY RENTAL (11/36) 1 MTH 769.50` |
| 分组，Qty = **台数** × 每台单价 | 台数 | `MBJB.pdf` `RA -36 UNIT imageFORCEC5160 · 36 unit × 540.00 = 19,440.00` · `MARA` `RA-5 UNIT · 5 × 250.00` · `JPJ MELAKA.pdf` `MEDIUM DUTY 5 × 655.50` |
| 分组，Qty = **1**，单价 = 整组总额 | 1 | `INSTITUT KEMAHIRAN…CHEMBONG.pdf` `IRADVDX6855I 2 UNIT · 1 × 984.00` · `KEJORA` `IRADVC5850I 7 UNIT · 1 × 4,067.00` · `3000-F0019 INV 2607.pdf` `RA-10 UNIT · 1 UNIT × 5,097.00` |
| Qty = 1 MTH，**不管几台机** | 1 个月 | `SINGLE.pdf` `RA-60MTH MONTHLY RENTAL (16/60) 1 MTH 250.00`（2 台机）· `SYNTURN.pdf` `RA-60MTH (49/60) 1 MTH 1,399.00`（2 台机） |
| **租金 FOC 零元** | — | `HOSPITAL PASIR GUDANG.pdf` `MR2607.1107` — 三行 `RA-n UNIT … RENTAL`，Amount 写 `FOC`，`Net Total (MYR) : 0.00` |
| **完全没有租金** | — | `KENSINGTON.pdf` `MR2607.0186` |

### ④ Invoice 上印不印抄表数

| 印真实单机读数 | 印**加总后的虚拟读数** | 完全不印 |
|---|---|---|
| `3000-F0019 INV 2607.pdf` · `JABATAN KASTAM….pdf` · `LEMBAGA KEMAJUAN…(KEJORA).pdf` · `HOSPITAL TANGKAK.pdf` · `MAJLIS AMANAH RAKYAT (MARA).pdf` | `3000-J0056 INV 2607.pdf` · `HOSPITAL PERMAI LAMA.pdf` · `HOSPITAL PONTIAN.pdf` · `HOSPITAL SULTAN ISMAIL.pdf` · `INSTITUT KEMAHIRAN…CHEMBONG.pdf` · `INSTITUT PENDIDIKAN GURU….pdf` · `HOSPITAL PASIR GUDANG.pdf` · `KOLEJ KOMUNITI ROMPIN.pdf` · `SINGLE.pdf` · `SYNTURN.pdf` · `KENSINGTON.pdf` · `MBJB.pdf` | `JPJ MELAKA.pdf` · `HOSPITAL SULTANAH AMINAH.pdf` · `PUSAT PEMULIHAN…(PUSPEN MUAR).pdf` |

中间那栏最需要注意 —— **发票上印的那个读数，没有任何一台机器真的读到过**：

- `HOSPITAL PERMAI LAMA.pdf` `MR2607.1338` — `Current Meter Reading (31/07/2026) : 1037684`
  = `183517 + 208667 + 84857 + 206975 + 261593 + 66376 + 25699`（listing 那页七台机相加）
- `MBJB.pdf` `AMR2607.0147` 第 8 行 — `Current Meter Reading - (31/07/2026) : 1223453`，36 台机相加
- `3000-J0056 INV 2607.pdf` — `116283` = `14211 + 18558 + 22082 + 61432`（对照 `3000-J0056 METER 2607.pdf` 四行）

### ⑤ FOC / Rebate / 最低消费

| 机制 | 出处 | 数值 |
|---|---|---|
| FOC copies | `HOSPITAL SULTANAH AMINAH.pdf` | 每台 6,000 BK（listing `.1 .2 .3 .5` 四行） |
| | `HOSPITAL TANGKAK.pdf` | 5,000 BK + 500 CL —— 发票上明印 `Meter FOC Qty : 5000` |
| | `JABATAN KASTAM….pdf` | 500 BK（UNIT KREATIF 是 1,000） |
| | `SINGLE.pdf` | 1,000（两台机拆 600 + 400） |
| | `SYNTURN.pdf` | 20,000 |
| | `PUSAT PEMULIHAN…(PUSPEN MUAR).pdf` | 四台机中三台各 1,000 |
| Rebate % | `HOSPITAL PONTIAN.pdf` | `Meter Rebate Qty (2%) : 1522` |
| | `HOSPITAL TANGKAK.pdf` · `JABATAN KASTAM….pdf` | `Meter Rebate Qty (3%)` |
| | `PUSAT PEMULIHAN…(PUSPEN MUAR).pdf` | 固定数量 181 / 174 / 111 / 105 |
| 最低消费 committed minimum | `KENSINGTON.pdf` | `MIN 1764-12` |
| 两者都没有 | 其余大部分 | |

**印不印出来也分两派**：

- **明印** —— `JABATAN KASTAM….pdf` 每一行下面都印 `Meter FOC Qty : 500` 和
  `Meter Rebate Qty (3%) : 141`，算式看得见：
  `7862 − 2630 = 5232`，`− 500 FOC = 4732`，`− 141 rebate = **4591**`
- **默默扣掉** —— `HOSPITAL SULTANAH AMINAH.pdf` `MR2607.1340` 只印 `8692 PCS`，
  6,000 张免费额在发票上一个字都没提。`PUSPEN` 一样（只印 `18518 PCS`）

### ⑥ 税 — 这条之前完全没人提

| 做法 | 出处 | 证据 |
|---|---|---|
| **SST 6% 只加在 rental 上**，meter 不课税 | `SINGLE.pdf` · `SYNTURN.pdf` · `MBJB.pdf` · `MAJLIS AMANAH RAKYAT (MARA).pdf` | MBJB：`SST @ 6% : 1,685.10` = 租金 28,085.00 × 6%；meter 38,186.02 一分税都没有。SYNTURN：`83.94` = 1,399.00 × 6% |
| 完全免税 —— `PEFPOT` 豁免 + 备注 `SV-R6, net 2% service tax exemption` | 六间医院 · `JABATAN KASTAM….pdf` · `KEJORA` · `JPJ MELAKA.pdf` · 两间 `INSTITUT…` · `KOLEJ KOMUNITI ROMPIN.pdf` · 两对 `3000-…` | Tax 栏有，但是空的 |
| 连税那一行都没有 | `KENSINGTON.pdf` · `PUSAT PEMULIHAN…(PUSPEN MUAR).pdf` | 连 `Total :` 小计都没有，直接 `Net Total (MYR)` |

---

## 三个「只此一家」的特例

### SYNTURN — 彩色併进黑白算
`SYNTURN.pdf` `MR2607.0192`

`Current Meter Reading (20/07/2026) : 413579` 是两台机的 **BK 加 CL 全部加起来**
（`26528 + 325671 + 2454 + 58926`）。39,131 张彩色印量**没有单独的 CL 行**，
全部按一个费率 `RM 0.05` 算。

这不是排版问题，是**计价模型不同**。

### KENSINGTON — 最低消费，没有租金
`KENSINGTON.pdf` `MR2607.0186`

18 台机、**没有 rental 行**、发票上没有任何 serial、费率是四位小数
（`0.0245` / `0.3234`），然后多一行：

```
MIN 1764-12 | MINIMUM RM 1764 OF COMMITTED PRINT CHARGES (10/12) | — | MTH | — | —
```

实际用量 RM 2,236.56 > 最低消费 RM 1,764，所以这行**只是告知，金额留空**。
用量不足时才会变成真的收费行。

### MBJB — 51 台机，自制模板
`MBJB.pdf` `AMR2607.0147`

不是 AutoCount 的版面 —— 是 Excel 自己排的 3 页，有 `Authorised Signature` /
`Received by` 签收栏，租金和抄表各有小计（`TOTAL RENTAL` / `Total Meter Reading`）。
4 行租金 + 7 行抄表盖掉 51 台机、约 50 个部门，每台机的明细放在后面 4 张工作表
（PDF 第 4–7 页）。

---

## 资料夹里最有价值的一条线索

`MAJLIS AMANAH RAKYAT (MARA).pdf`（第 1 页）和
`PUSAT PEMULIHAN PENAGIHAN NARKOTIK (PUSPEN MUAR).pdf`（第 1 页）的工作表里，
有一个 **`Billing Type`** 栏位：**MARA = `2`，PUSPEN = `3`**。

也就是说 ATP 内部**早就在给格式编号了**。

如果 `Billing Type` 的完整代码表和定义拿得到，整件事就从「从 20 个客户逆向推测」
变成「实作一套已经讲好的类型」。

**这是第一件要问的事。**

---

## 顺便发现的资料问题

这些不是这份文件的主题，但每一条都是**已经寄给客户的单据**上的：

| 问题 | 出处 |
|---|---|
| 租 52 台，但 listing 只有 51 台（`ASNI 00000292.4` 整台不见） | `MBJB.pdf` 第 4 行 `RA -11 UNIT` vs 工作表第 7 页 |
| 6 台机抄表，只租 4 台 | `HOSPITAL SULTANAH AMINAH.pdf` `MR2607.1216` |
| 6 台抄表，租 5 台 | `HOSPITAL PONTIAN.pdf` `MR2607.0357` |
| 24 台抄表，租 23 台（`ASNI 00000440.24` 没有租金行） | `JABATAN KASTAM….pdf` `AMR2607.0074` |
| 4 台抄表，租 3 台 | `PUSAT PEMULIHAN…(PUSPEN MUAR).pdf` |
| 写 **`GST @ 6%`** —— GST 2018 年就废除了 | `MAJLIS AMANAH RAKYAT (MARA).pdf` `AMR2607.0020` |
| SST no. 写 `J21-2508-32100009`，同一家公司别张是 `J31-…`；公司注册号也少了位数 | `MBJB.pdf` vs `MAJLIS AMANAH RAKYAT (MARA).pdf` |
| `Inv No.` 栏整栏是 `XXX` | `LEMBAGA KEMAJUAN…(KEJORA).pdf` 第 1 页 · `MBJB.pdf` 第 4–7 页 |
| Serial 前缀 listing 和 invoice 不一样（`2YN19825` vs `2YB19825`） | `LEMBAGA KEMAJUAN…(KEJORA).pdf` |
| CL 那两行描述写成 `BK COPY + PRINT`，但按 CL 费率 0.30 收钱 | `LEMBAGA KEMAJUAN…(KEJORA).pdf` `AMR2607.0153` 第 4、22 项 |
| 彩色表倒退（`5834 < 5920`），那行 Qty 和金额留空 | `KOLEJ KOMUNITI ROMPIN.pdf` `AMR2607.0087` |
| 合并行标着**其中一台**的 serial（`38F11138`），但数量是四台加起来 | `KOLEJ KOMUNITI ROMPIN.pdf` `AMR2607.0087` |
| Service date 是 `5/8/2026`，比 `31/7/2026` 的发票还晚 | `MBJB.pdf` 工作表 `ASNI 00000292.10` |
| 发票上的抄表日期和 listing 的 service date 对不上 | `MARA`（25/7 vs 23/7）· `HOSPITAL PERMAI LAMA.pdf` · `HOSPITAL SULTAN ISMAIL.pdf`（02/07 vs 30/6）· `KEJORA` · `SINGLE.pdf`（20/7 vs 27/7）· `MBJB.pdf` |
| 发票写型号 `IRADVDX4935I`，listing 全部是 `IRADV DX 4945I` | `HOSPITAL SULTAN ISMAIL.pdf` |
| 发票 unit price 印 `0.029`，listing 是 `Rate of BK 0.0285` | `HOSPITAL TANGKAK.pdf` `MR2607.1413`–`.1415` |
| Listing 和 invoice 有几分钱的差 | `INSTITUT PENDIDIKAN GURU….pdf` 差 1 分 · `JPJ MELAKA.pdf` 差 3 分 · `HOSPITAL SULTAN ISMAIL.pdf` 差 1 分 · `HOSPITAL PERMAI LAMA.pdf` BK 和 CL 各差 1 分（总数刚好对） |

### 关于 rounding —— 这条是引擎级的

`3000-F0019 INV 2607.pdf` 直接 show 出规则：**逐行四舍五入，再相加**。

```
56,381 × 0.026 = 1,465.906   ← 照总量算会变 1,465.91
但 invoice 和 3000-F0019 METER 2607.pdf 都是 1,465.90
（因为是 227.006→227.01、133.614→133.61、20.098→20.10… 各自 round 完才加）
```

任何系统要重现这些单据，**round 的顺序必须一样**，否则每张单都会差几分钱。

---

## 建议做法

20 个客户 20 种格式，第 21 个一定又是新的。与其一个客户一个客户做，
不如把选项放到 contract 上：

| 设定 | 选项 |
|---|---|
| `RentalSeparateInvoice` | 分单 / 合并 |
| `MeterGrouping` | 每台×颜色 / 每台 / 按 model / 按费率组 / 全车队合并 |
| `RentalLineStyle` | 每台一行 / Qty=台数 / Qty=1 总价 / Qty=1 固定 / FOC / 无 |
| `ShowReadings` | 真实单机 / 加总 / 不印 |
| `ColourMode` | BK 和 CL 分开 / 全部併入单一费率 |
| `TaxMode` | SST 只课租金 / 免税 / 无 |
| `ZeroUsageLine` | 不印那行 / 印出来但 Qty 留空 |
| `ZeroInvoice` | 跳过 / 照开 RM 0.00 |

这资料夹里的 20 种，全部都是这 8 个开关的组合。
第 21 个客户就变成**设定问题**，不是**开发问题**。

---

## 开工前要先拍板的三件事

### 1. `Billing Type` 的完整清单
代码有几个？每个代表什么？目前只看到 `2`（`MAJLIS AMANAH RAKYAT (MARA).pdf`）
和 `3`（`PUSAT PEMULIHAN PENAGIHAN NARKOTIK (PUSPEN MUAR).pdf`）。
这张表如果存在，下面所有事情都会简单很多。

### 2. 那些特例是合约要求，还是历史上就这样开？
- `HOSPITAL TANGKAK.pdf` 5 台机一个月开 **9 张单**，其中三张是真的 RM 0.00
  e-invoice，而且都送去 LHDN 验证过了（`MR2607.1413`、`.1414`、`.1415`）
- `HOSPITAL PASIR GUDANG.pdf` 开了一张 **RM 0.00 的租金单**（`MR2607.1107`）

如果是合约要求，我们就照做。如果只是当初这样 key，现在应该收掉，而不是把它做进系统。

### 3. Rental 那 5 种写法，客户真的在意吗？
`MBJB.pdf` 的 `36 unit × 540.00` 和 `KEJORA` 的 `1 × 4,067.00`，
算出来是同一笔钱，只是呈现方式不同。

如果客户不在意看到哪一种，**统一成一种可以省掉一大半工作量**。
如果某个客户就是坚持，那就要知道是哪一个客户。
