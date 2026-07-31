# Demo Part 2 — 客户 QnA / Feedback 整理(v2:问题 + 讨论 + Remark 补充)

来源: `DEMO_PART2.raw.txt`(约 00:01–01:49) · 原始版(remark 原文): `demo-feedback-2026-07-30-PART2.md`
状态图例: 🔧 要做(TODO) · ⏸ PENDING(等回复/我们提方案) · 🚧 Coming soon · 💬 仅讨论,无 action

---

## Meter Reading / 开单

| # | 时间 | 问题/点 | 讨论 & 我们的想法 | 状态 |
|---|---|---|---|---|
| 1 | 00:02:21–00:02:41 | 客户要 Meter Reading Integration 的列表能按 Service Item No 排序(一点表头就顺) | 现场演示:点 column header 就能 sort ✅。**Remark 补充的 idea**:再进一步 — 用户的 sorting / filtering / column 布局要能**保存**,不用每次开 module 都重新 set;另外加一个概览:**这个月有几台机器要开单、细到几号有几台** | 🔧 |
| 2 | 00:02:42–00:07:26 | 列表密密麻麻(rental/waive/commit 都混在一起),客户只想 key BK/CL;新人分不清哪些要填。客户要求 **Role 列 + filter**("我要用不要用是一回事,但是要有")。我们解释了外层 grid 不可直接填的 consistency 原因(fetch 来自 PUMS),客户接受 | Demo 时讲到用 filter 只显示 BK。**Remark 补充的 idea**:(a) 让用户在 **Need Manual Key-In 直接 key reading**,不用 double-click 进 dialog;(b) 不硬删 column — 做 **View Setting** 功能,用户自己设置要显示哪些 column(对新人简单);(c) **必填/不必填用颜色区分**;(d) filter 支持 BK / CL / 两个,其他 meter 不 show(反正都有 role 了) | 🔧 |
| 3 | 00:04:45–00:16:57 | **Group same debtor 安全性**:新人漏 tick / debtor 底下有机器没 ready(或 offline 没 reading)时,generate 没有任何提示,会漏机 → 客户被投诉"讲好一个 debtor 一张单,你开两张给我"。客户拍板最大 rule:**tick 了 group,group 不完整就必须挡下不给 generate** | 中途厘清了几个行为:不 select 不会被 generate;offline 但有 meter 进来也会在 Ready 列表(offline/online 是 PUMS 的状态,不代表没 reading)。**Remark 补充**:validation 要**清楚讲明问题**才不给开始;不管这个 debtor 有几个 contract 都是一张单。**Additional(remark,不相关但要做)**:double-click 一个 service item 应该只显示**这台机**,现在 double-click 是 group by 全部机器 | 🔧 |
| 4 | 00:12:36–00:14:01 | Demo 当场发现:generate **offline** invoice 时 tracking id 没进 invoice Ref(我们自己承认疏忽) | 设计本意:offline 有 tracking id 就放进 Ref;group/混合情况下漏了 | 🔧 修 bug |
| 5 | 00:16:58–00:17:53 | 客户:这种 60 个月的合约,**签约时就知道要 group**,为什么每个月都要在 integration 勾 "group same debtor"?应该 set 在 contract;两处都设要报冲突 | Agree。**Remark 补充**:demo 时已答应改 — "group all debtor to one invoice" checkbox **功能保留但默认不开、不放显眼位置**(大部分场景是 by contract)。注意区分两个概念:contract 内 all machines → 1 张单(现有 Billing Mode)vs **同一个 debtor 的多个 contract → 1 张单**(这才是那个 checkbox) | 🔧 |
| 6 | 00:17:54–00:18:30 | 客户的 customer 有时很奇葩:同一个 contract,机器 1–14 要一张 invoice、15–16 另一张(**split bill by few groups of machines in same contract**) | 我们答:这属于 **Group Deal**,还没做好 | 🚧 |
| 19 | 01:16:55–01:18:31 | Expired(60/60 期满)的合约还要能显示出来开单 | 已有 Setting "Include expired",untick/tick 自选,客户 OK("这个我相信你的") | 💬 |

## CN / 更正错单(最长的讨论)

| # | 时间 | 问题/点 | 讨论 & 我们的想法 | 状态 |
|---|---|---|---|---|
| 10 | 00:24:31–00:46:44 | 开错单(reading 错 / scan 错机器 code),但 invoice 已发客户 + **e-Invoice 不可删** → 不能"删掉重开"(我们原本的做法)。客户问:CN 之后,之后月份的 reading 怎么接得回来?连错两三个月怎么办? | 客户方法论:CN 时要能 **capture/override "最后正确的 reading"**(不管 CN 的是哪个月),之后月份以 override 值起算;多张 CN 按 **date priority + CN number**。我们:invoice 生成时已存当下 reading(已有);方向是 **CN 插一行、可 override last reading**。Excel 推演了 1000/2500/4900/7300 的例子(见下表)但**没达成共同点** → 明确记 pending,客户内部准备更多 scenario 回来 align(00:46:21) | ⏸ |

**Demo 时的 Excel 推演(usage in invoice is Qty):**

| 状态 | 月份 | Current | Last Reading | Usage | 单据 |
|---|---|---:|---:|---:|---|
| | SEP | 4800 | 3500 | 1300 | INV004 |
| | aug | 3500 | 4500 | **-1000** | **CN01** |
| PAID | aug | 4500 | 3000 | 1500 | INV003 |
| PAID | july | 3000 | 1000 | 2000 | INV002 |
| PAID | june | 1000 | 0 | 1000 | INV001 |
| | may | 0 | 0 | | |

> 读法:8 月的 INV003(reading 4500)开错且已 PAID → 开 CN01 冲 usage -1000,并把正确的 last reading override 成 3500;9 月 INV004 从 3500 起算(4800 − 3500 = 1300)。

## Service Contract / Service Item

| # | 时间 | 问题/点 | 讨论 & 我们的想法 | 状态 |
|---|---|---|---|---|
| 11+14 | 00:46:44–00:48:49, 00:53:00–00:54:33 | 客户要知道每台机在哪个 department/分行:**每台机的 branch/location + address** 要能填/显示;master 的做法是机器 register 一个 branch code | 做 branch/location column(带 address),default 跟 contract、可 override/edit("给完我 address,要不要 print 我自己勾,at least 它先有")。**联动规则**:选了 debtor 才能选**该 debtor 的** branch code,没选 debtor 就没得选 | 🔧 |
| 12 | 00:48:49–00:50:30 | (Harry)机器换租给另一个 customer 怎么处理?会不会一直改 delivery address? | 客户自答:clone to a new contract / inactive 掉重做新的;我们:机器有 history 记录(去过哪些 customer)。*Remark 原文:"其实不是 ownership history 其实是集体" — 字眼待确认* | 💬 |
| 13 | 00:50:14–00:52:58 | **Transfer from Serial No(DO)控制**:目前同一 DO 可以重复 transfer(human error),没有 outstanding 概念;serial 应该认 AutoCount 的 available serial(卖出去要 CN 回来才 available) | **Remark 细化**:transfer 过的 DO 要有 **transferred 记录**告诉用户已经 transfer 过 — **必须是有 SAVE 的才算**(CSSI 真的创建并用了这台机);dropdown 只列 **AutoCount available serial no**;机器回来后 serial 重新出现在 available。或者干脆只显示 available 的、transferred 的不出现 | 🔧 |
| 15 | 00:54:47–00:55:35 | **Service Item Type** 字段(master 有)拿来做什么? | 客户会去 master accounting 看这个东西的作用,之后跟进;方向是 for report | 🔧 等客户查 |
| 20 | 01:18:56–01:25:26 | **合约期限计算**:客户要打 start date + 月数(任意 17/25/39),系统自动算 expiry(= start + months − 1 天);expiry 不给随便改(要 authority);现在手打 year/expiry 会人为错(打 2511 实际变 42 个月) | 方向 agreed(去掉 year,只打 month)。**卡点:短期租(18 天 / 半个月)怎么表达**(0.5 个月?天数?)→ **dev team 会 suggest solution** | ⏸ |
| 21 | 01:25:58–01:28:00 | Service type 会不会影响 generate / rental separate 的算法? | 不会 — 它是 UDF,只做 filter/sort/report | 💬 |
| 22 | 01:28:01–01:28:18 | Purchase date 是什么的 purchase date?会自动带吗? | 从 master 搬来的字段,不自动带,可填可不填;客户会 check master | 💬 |
| 23 | 01:28:22–01:30:12 | Meter 的 role/rule 每次都要选吗?set 了会被 master 影响吗?改 rule 要不要 authority? | 跟 meter type 自动带、set 一次、per service item 独立不被 master 影响。authority 的部分客户说 come back | 💬 |
| 24 | 01:30:20–01:30:59 | **Waive rental 的 % 不能用**:场价 80 块到 2000 块,% 算不出("这个是场价钱的这种能算 percentage 咩?") | 当初照客户给的 Excel 用 %;现拍板:**waive 用 total amount (RM),不用 %** | 🔧 |
| 25 | 01:31:00–01:31:20 | 为什么 edit mode 看不到 Copy(meters)? | 设计如此:只在 new mode 有;客户 OK | 💬 |
| 26 | 01:31:20–01:31:39 | Contract module 的 debtor 只显示 code,看不到 **company name** | 我们承认疏忽,补 render company name | 🔧 |

## Billing 日期 / Invoice 显示

| # | 时间 | 问题/点 | 讨论 & 我们的想法 | 状态 |
|---|---|---|---|---|
| 16 | 00:55:36–01:04:20 | 客户的 customer 要 invoice 显示的 previous–current 期间按 **contract period**(1–31 号 / 23 号–22 号),不是 actual meter reading date("你不要跟我讲 28 到 27 跨月") | 两种模式:**start with actual meter date 或 contract date**。结论:在 **service contract 打勾设定**(不要靠 template 猜),invoice template 一个 design + checkbox 驱动;有 4 种 template 都要套 solution;training session 时教怎么 set。顺带发现:billing period display 之前被 hide 了(fix 在一个月) | 🔧 |
| 18 | 01:06:42–01:15:11 | **Rental 独立开票日期(prepayment)**:rental 要开在月头 1 号(6 月的 rental 开 6/1),meter 开 28/30 号 — rental 和 meter 的 billing date 不同 | Demo 时给了两个方案:(a) integration 的 group checkbox 下面加 **invoice date 选择器**,勾选的都用该 date;(b) list 每行一个 invoice date column。Mr Chan 倾向 set 在 service contract。**Remark 定案(dev team):在 contract 里面 set rental 独立开票日期** | 🔧 |
| — | 01:09:44–01:11:15 | 30 号才 fetch 的 reading,单还是开 28 号? | 是:invoice date 28、last audit date 30,两个日期都在;客户接受(关联 #18 的可选 invoice date) | 💬 |

## Bulk Email / SOA

| # | 时间 | 问题/点 | 讨论 & 我们的想法 | 状态 |
|---|---|---|---|---|
| 9a | 00:20:14–00:20:57 | Bulk email 的 template 在哪里 set? | 目前没有 template,只是 send PDF;加 template 不难,补一个 video demo | 🔧 |
| 9b | 00:20:57–00:21:20 | 客户 idea:bulk send email 应该在 service contract 那边就做好 — group invoice ready 了才出 checklist 告诉你要 send email | Agree。**Remark 补充**:(a) contract module 里最好也能看到"invoice 好了、可以开始 bulk email"的**提醒**;(b) bulk email 发过要有 **sent history** 记录;(c) Bulk Email Invoice 画面加 **checklist:哪些已 ready 但还没 email** | 🔧 |
| 9c | 00:21:20–00:23:57 | Invoice print 有 template/report code — 客户要求 **在 service contract header set template code**,bulk email 自动用对的 template,不要每次手选("60 个月不会变,set 一次");SOA 同理:contract tick 要不要 generate SOA + 选 SOA template code | Agree。**Remark 补充**:用户会创建不同的 report design,**每个顾客可能不同的 report design,在 contract set** | 🔧 |

## Stock Request / PUMS

| # | 时间 | 问题/点 | 讨论 & 我们的想法 | 状态 |
|---|---|---|---|---|
| 7 | 00:18:30–00:19:59 | Flowchart 字眼确认:是不是 PUMS approve 了才 flow 来 AutoCount?AutoCount 不做 request/approve? | 对 — approve 全在 PUMS,AutoCount 只 capture + 生成 stock transfer/issue;任何 cancel/改 quantity 的 update 也在 PUMS。flowchart 少一个 approve step 要补 | ✅ aligned + 🔧 docs |
| 27 | 01:31:45–01:36:30 | Stock transfer 的 default location:选了 default(HQ)画面没反应,"default from location" 这名字误导 | 实际逻辑:out → to = default location;in → from = HQ。要做:load 时 **refresh** 出来、文案改清楚;default 只 apply stock transfer,不 apply stock issue | 🔧 |

## Lock Period(与 AutoCount 配合)

| # | 时间 | 问题/点 | 讨论 & 我们的想法 | 状态 |
|---|---|---|---|---|
| 28 | 01:36:30–01:47:30 | 客户每 2 个月 submit SST 后会 lock period,我们的 generate 要配合两种锁:**document period lock** 和 **tax period lock**(master 只挡有 tax type 的 item,没 tax type 照过 — 这是 tax period 和 accounting period 的差别) | Document lock:我们 generate 走 AutoCount 逻辑,会被自然挡下 ✅。Tax lock:AutoCount 端在哪双方都去找(SST module 都不熟);找到了 generate 自然被挡,找不到我们就在 meter transaction 层控制 | ⏸ |

---

## 收尾(01:48:25–01:48:55)
双方各自把 pending 写下来放群里 align,下次 session 继续 discuss。

## 汇总(v2)

- **🔧 TODO**:#1 grid 布局持久化 + 月度开单概览 · #2 inline key-in + View Setting + 颜色必填 + BK/CL filter · #3 group guard + double-click 单机 · #4 offline tracking id 修 · #5 group 移 contract(all-debtor checkbox 藏起来) · #11+14 branch/location+address 联动 · #13 DO transferred 记录 + available serial · #15 Service Item Type 跟进 · #24 waive 用 RM · #26 debtor company name · #16 billing period 模式 set 在 contract · #18 rental 开票日期 set 在 contract · #9a template + video · #9b ready 提醒/sent history/checklist · #9c per-contract report design + SOA · #7 flowchart 补 approve · #27 default location refresh/文案
- **⏸ PENDING**:#10 CN reading override(客户回来 align) · #20 短期租期算法(dev team 提方案) · #28 lock tax period(双方找)
- **🚧 Coming soon**:#6 Group Deal 分组开票
- **💬 无 action**:#19 · #12(字眼待确认) · #21 · #22 · #23 · #25 · 28 号开单行为
- **已删除**:原 #17(template billing address / contract 号码)— 按 remark 移除
