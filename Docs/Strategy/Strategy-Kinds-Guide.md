# Strategy Maintenance — 6 种策略说明 (Rule Kinds Guide)

> 策略 = 一个 code + 描述 + **多条规则(rule)**。一个策略可以叠好几条不同 kind 的规则,引擎按顺序全套。
> 母版在 **General Setup → Strategy Maintenance**(模板库);挂到合同时,在合同的 **Strategy tab** 里
> 从模板 Copy 进来,变成**这个合同自己的规则副本**,之后随便改都不影响母版。

---

## 一览表

| # | Rule Kind (画面名) | 内部代码 | 干什么 | 参数 |
|---|---|---|---|---|
| 1 | **Waive Rental - by Target (RM)** | `WAIVE-TARGET` | 印够目标就**免租金**(可部分免) | Target Amount、Partial waive %、Count usage from |
| 2 | **Rental Free - first N months** | `RENTAL-FREE-N` | **前 N 个月免租**(签约促销) | Free Months (N) |
| 3 | **Committed Print Charges** | `COMMIT-MIN` | **承诺最低消费**(保底) | Committed Amount |
| 4 | **FOC + Rebate** | `FOC-REBATE` | **免费张数 + 回扣** | FOC Copies、Rebate %、Apply FOC/Rebate to |
| 5 | **Initial Meter** | `INITIAL-METER` | **估读登记**(纯标记) | 无 |
| 6 | **FOC Limit (single/group)** | `LIMIT` | **免费印量上限**(单机 / 群组) | Limit Scope、FOC Limit Qty |

每条规则还有:
- **Applied for all Service Items**(勾选框,默认勾)+ **Service Item(s)** 多选打勾列表 —— 决定这条规则
  管**全部机器**还是**只管勾选的那几台**。
- **Scope**(只 ① 和 ④ 有)—— 见下。

---

## 逐个解释

### ① Waive Rental - by Target (RM) — 印够就免租
客户这个月的**印量费(usage)**达到 **Target** 金额,就把这台机的**租金免掉**;没达到但达到
**Partial waive %** 的门槛,就按比例部分免。

- **Count usage from**:算目标额时数哪些 usage meter —— **BK / CL / BK+CL**。
- **Partial waive %**:达到 target 的 X% → 免 X% 租金;`0` = 全有全无(必须打满 target 才免)。

**例子**:Target `RM500`、Partial `50%`、月租 `RM350`
| 这个月印量费 | 达 target 的 | 结果 |
|---|---|---|
| ≥ RM500 | 100% | 租金 **RM0** |
| RM250 | 50% | 租金 **RM175**(免一半) |
| RM200 | 40% | 租金 **RM350**(不免) |

> 用途:鼓励客户多印 —— 印够量就送租金。
> 生效方式:**生成发票时当场算**(要等这个月印量出来才知道够不够)。

---

### ② Rental Free - first N months — 前 N 月免租
新签约头 **N** 个月不收租金,第 N+1 个月起才开始收。

- **Free Months (N)**:免几个月。

**例子**:`6` → 头 6 个月租金 = 0,第 7 个月起收全额。

> 用途:签约优惠。
> 生效方式:点 **Apply Strategy to Meters** → 把 N 写进租金 meter 的**免租月数**,每期开单自动递减,
> 减到 0 才收租。

---

### ③ Committed Print Charges — 承诺最低消费
客户承诺每月至少付 **Committed Amount**,不管实际印多少 —— 作为最低消费保底。

- **Committed Amount**:最低金额。

**例子**:`RM200` → 就算只印了 RM50 的量,也收 **RM200**。

> 用途:保底收入。
> 生效方式:作为该 meter 的 **minimum charge**(最低消费下限)。Apply 时会**报告**哪些 meter 的最低
> 消费跟承诺不符,供你核对。

---

### ④ FOC + Rebate — 免费张数 + 回扣
给客户一定**免费印量(FOC copies)**,超出的部分再打**回扣(rebate %)**。

- **FOC Copies**:免费张数。
- **Rebate %**:超出部分的折扣。
- **Apply FOC/Rebate to**:给哪些 meter —— **BK / CL / BK+CL**。

**例子**:FOC `1000` 张、Rebate `3%`、BK+CL
→ 头 1000 张免费,之后每张打 97 折。

> 用途:大客户优惠。
> 生效方式:点 **Apply Strategy to Meters** → 把免费张数/回扣% 写到对应 BK/CL meter 上,开单引擎自动扣。
> ⚠️ **NET vs RAW 口径待定**:超量部分是按原价还是净额(扣掉 FOC+回扣后)开票,还在等决定 —— 见
> `FOC-Rebate-Billing-Modes.md`。定了才统一。

---

### ⑤ Initial Meter — 估读登记
纯**标记**:新装的机器还没有真实读数,标记这个合同/机器走"人手估读"开单方式。**不自动改 meter**。

- 无参数。

> 用途:新机开单期还没实际抄表,先用估读(读数在 Meter Reading 里手动 key-in)。
> 生效方式:只给合同打标签,不推任何数字。

---

### ⑥ FOC Limit (single/group) — 免费印量上限
给一个**免费印量上限**。

- **Limit Scope**:**single** = 每台机器各自算上限;**group** = 一组机器**合并**算上限。
- **FOC Limit Qty**:上限张数。

**例子**:single `5000` → 每台机头 5000 张免费;group `5000` → 一组机合起来 5000 张。

> group 用途:一个客户多台机共享一个免费额度。
> 生效方式:**single 单机自动**;**group 走 master 的 `.C` 合并机惯例**(一个 `.C` 结尾的合并 service
> item 挂群组 meter)—— 缺 `.C` 时会**告警**提醒你建一个。

---

## 关键区别：三种"生效方式"

| 生效方式 | 哪些 kind | 怎么做 |
|---|---|---|
| **A. 推数字到 meter**(点 Apply Strategy to Meters) | ② Rental Free、④ FOC+Rebate、③ Committed(报告) | Apply 把参数写到 meter,之后开单引擎读 meter 自动生效 |
| **B. 生成发票时当场算** | ① Waive Rental | 开单时看这个月印量够不够 target |
| **C. 只登记 / 校验** | ⑤ Initial Meter、⑥ FOC Limit | 打标签 / 缺 `.C` 告警,不推 meter |

---

## 策略 vs Meter Grid —— 谁说了算(重要)

②③④ 写的 `Min Charges / Rebate % / Free Qty` 正是 **meter 配置 grid** 里已有的栏位。这**不是两份值在打架**:

- **每台 meter 只有一份值**(存在 `zSCP2_ItemMeter` 那一行)。meter grid、Rental Maintenance、strategy
  的 Apply —— 大家改的是**同一行、同一栏**。没有重复存储。
- **meter grid 才是开单依据(single source of truth)。**
- **strategy 的 "Apply Strategy to Meters" = 一次性把值填进 grid**(带预览 old→new + 确认),不会自动
  反复套。填完你还能在 meter grid 里**逐台手改**(单台特殊处理)。
- 唯一"非静态"的是 **① Waive Rental** —— 它不写 grid,生成发票时才动态算(要等这个月印量出来)。

所以流程是:**strategy 一键批量填 → grid 是准 → 需要就单台微调**。会"覆盖"只发生在你**主动 re-apply**
时,而 Apply 的预览会先显示 old→new,你不确认就不会盖。

> strategy 对 ②③④ 的价值 = **可复用模板 + 批量套用**(一个策略填 50 台机、跨 10 个合同),不是"另一份值"。

## 一句话记忆

- 想**免租金** → ①(按印量)或 ②(按月数)
- 想**保底收入** → ③
- 想**给优惠印量** → ④(免费+折扣)或 ⑥(上限)
- 新机**没读数** → ⑤

**一个策略可叠多条**,例如:
```
GOV-2026「政府配套 2026」
  1. Rental Free - first N months     免租 6 个月
  2. FOC + Rebate  (BK)               免 1000 张 + 回扣 3%
  3. FOC + Rebate  (CL)               免 500 张 + 回扣 3%
  4. Waive Rental - by Target (RM)    印够 RM500 → 免租
```
换个客户就换一套规则组合。

---

## 怎么用(合同操作)

1. **General Setup → Strategy Maintenance**:建好模板(code + 规则)。
2. 打开合同 → **Strategy tab**(第二个)→ 顶部选一个 **Strategy Template** → 点 **Copy Rules from Template**
   → 规则拷进这个合同。
3. 在这里**继续改**(加/删规则、改参数、绑 service item)—— 不影响母版模板。
4. **保存合同**。
5. 需要"推到 meter"的策略(②③④)→ ribbon 点 **Apply Strategy to Meters**(会先给预览再确认)。
6. Meter Reading 生成发票 → ①(Waive)当场算,② ④ 按 meter 上的值开。

> **Rental separate invoice**(Strategy tab 顶部勾选框):开时租金**独立开一张票**(和印量费分开);
> 不勾则租金并进同一张发票。
