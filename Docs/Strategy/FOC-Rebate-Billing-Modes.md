# FOC / 回扣计费口径讨论 · FOC / Rebate Billing Modes — Decision Needed

> 目的:决定 FOC(免费张数)和 Rebate(回扣 %)到底**扣不扣钱**。
> Purpose: decide whether FOC (free copies) and Rebate (%) actually REDUCE the billed amount.

---

## 现状(系统里同时存在两种算法 — 这是个 bug)
## Today (two different maths coexist — this is a bug)

| 位置 Where | 算法 Math | 结果 Result |
|---|---|---|
| **发票 Invoice**(实际开单) | **RAW 原始**:`usage × rate`,FOC/回扣只印在发票文字里,不扣钱 | 客户被收全额 |
| **Meter Reading 表格预览** | **NET 净额**:`(usage − FOC) × (1 − rebate%) × rate` | 屏幕显示的金额比发票少 |

- RAW 是 **master(V8)的实际惯例** — 已核对旧发票 MR2604.0006:usage 5645、FOC 1000,照样开 5645 × 0.03 = **RM169.35**(FOC 只是印出来给客户看)。
- NET 是你 **Marketing Strategy Excel** 里的算法(NET BK/NET CL 栏)。
- 只要某台机器 FOC 或回扣 > 0,**屏幕金额 ≠ 发票金额**。这次一定会修好(统一成同一套算法),问题只是:**统一成哪种?**

---

## 两种口径的具体例子 · Worked examples

**例 1(你们的真实旧单 MR2604.0006):usage 5645 张,FOC 1,000 张,回扣 0%,单价 0.03**

| 口径 Mode | 计算 Calculation | 开单金额 Billed |
|---|---|---|
| RAW 原始 | 5645 × 0.03 | **RM 169.35**(= master 旧单实际金额) |
| NET 净额 | (5645 − 1000) × 0.03 = 4645 × 0.03 | **RM 139.35** |

**例 2(Marketing Strategy Excel 第 4 区 CSSI 00002643):usage BK 3053 张,FOC 1,000,回扣 3%,单价 0.0291**

| 口径 Mode | 计算 Calculation | 开单金额 Billed |
|---|---|---|
| RAW 原始 | 3053 × 0.0291 | **RM 88.84** |
| NET 净额 | (3053 − 1000) = 2053;再扣 3% 回扣 = 2053 − 61.59 ≈ 1992 张;1992 × 0.0291 | **RM 57.97**(= Excel 里 BK CHARGES 的数) |

**例 3:usage 800 张,FOC 1,000(免费额度没用完)**

| 口径 Mode | 结果 Result |
|---|---|
| RAW 原始 | 照收 800 × rate |
| NET 净额 | 0 张 → **RM 0**,这台这个月不收抄表费(若无最低消费) |

---

## 我们建议的落地方式 · Proposed implementation(两边都要)

**口径跟着"策略"走,不是全局一刀切:**

1. 每条 **FOC-REBATE 策略**上有一个开关 **NetBilling(Y/N)**:
   - **N(默认)= RAW**:跟现在/跟 master 一样,FOC/回扣只显示。
   - **Y = NET**:按 Excel 算法扣减后开单。
2. **没挂策略的合同一律 RAW** — 所有现有客户的开单金额**零变化**。
3. 想给某客户 NET 扣减 → 建一条 NetBilling=Y 的策略挂到该合同,并可一键把 FOC/回扣数字套到它的 BK/CL meter。
4. 屏幕预览、发票、审计日志三处**永远同一套数字**(本次修复)。

**唯一会变的现状:** FOC/回扣 > 0 的机器,Meter Reading 屏幕金额会从 NET 改为显示 RAW(和发票一致)。发票金额不变。

---

## 请回答 · Questions for you

1. 上面的落地方式(策略开关,默认 RAW)可以吗? / OK with the per-strategy switch, default RAW?
2. 有没有哪些现有客户其实**应该**按 NET 扣减(即之前一直收多了/合约本来就该扣)?有的话给客户名单,我们建策略挂上去。
3. 例 3 那种"FOC 没用完"的月份,NET 口径下抄表费是 0 —— 这是你们要的行为吗?(租金/最低消费另计,不受影响)
