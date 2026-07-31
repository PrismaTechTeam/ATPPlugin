# Demo Part 2 — 客户 QnA / Feedback 整理

来源: `C:\Dev\rojak-transcribe\out\DEMO_PART2\DEMO_PART2.raw.txt`(约 00:01–01:49)
状态图例: ✅ 现场解答/现有功能 · 🔧 我们答应做(TODO) · ⏸ PENDING(双方再议/等回复) · 🚧 Coming soon

## Meter Reading / 开单

| # | 时间 | 点/问题 | 结果 | 我的 Remark |
|---|---|---|---|---|
| 1 | 00:02:21–00:02:41 | Manual key-in 画面要能按 Service Item No 排序(点表头) | ✅ 现场演示可排序 | 顾客是想要这个Meter Reading integration with pump system 的这个module 的service item no 是 sort by Service Item 的。目前系统当你点column 也是会sort的。我们可以加一些功能确保用户的sorting和filtering 和column format can be save no need to set everthing time when open this module。 Additional 是要可以看到 这个月有几台机器是要开单的可以仔细到几号的时候有几个机器是要开单的。 |
| 2 | 00:02:42–00:07:26 | Manual key-in / 列表要有 **Role (BK/CL/…) 列 + filter** — 只想 key BK/CL,不想看 rental 等密密麻麻的行;新人不知道哪些要填。我们解释外层 grid 不可直接填的 consistency 原因(fetch 来自 PUMS),客户接受,但坚持要 Role filter("我要用不要用是一回事,但是要有") | 🔧 加 Role 列/filter | 用户希望可以在 Need Manual Key in  Meter Reading integration with pump system 直接可以key in reading 不需要double click 进去 然后呢 不要把这些密密麻麻的column 放进去因为customer you configure自己的meter setup。但是我prefer 就是有一个 View setting的功能可以去这里set，不会把这些拿掉只会based on 这个view setting 去 设置要显示的column 这样对新人简单。而且还有什么事必须填的那些是需要的这些都要展示到很好）用户还要可以only filter by BK 或者是 CL 或者是两个 不需要show 其他的meter反正都有role了 可以用颜色区分那个必须填哪个不需要填 |
| 3 | 00:04:45–00:16:57 | **Group same debtor 安全规则**:漏 tick / 有机器没 ready(或 offline 没 reading)时 generate 没有任何提示会漏机。客户拍板最大 rule:**tick 了 group,group 不完整就必须挡下不给 generate**(或强提示) | 🔧 group 完整性 guard | 这个Group same debtor要有保护机制因为如如用户选了 5个 service item that is only for one debtor but 如果有其中一个机器under这个顾客的会有问题还是漏掉 forgot to tick when generate invoice will have validation sytem to check if face this issue the generate invoice will not be start and tell the issue clearly . 不管这个debtor有几个contract都是一个单子。Additional and not related with issue, doubleclick的时候should be show 一个机器 现在double click的话是group by全部的机器service item 但是如果double click service item的话should be 显示只是这台机的。 |
| 4 | 00:12:36–00:14:01 | **TrackingId 疏忽**:group 情况下有 offline 机器时,tracking id 没进 invoice Ref(demo 时当场发现某单 Ref 没带) | 🔧 修 group/混合场景的 Ref tracking id | when generate offline invoice the tracking id is not added in ref |
| 5 | 00:16:58–00:17:53 | **Group 应设定在 contract**(60 个月的合约签约时就知道要 group),不要每月在 integration 勾;两处都设要报冲突 | 🔧 group 移到 Maintain Service Contract | 这个修然我在demo的时候可以改讲改就把那个group all debtor to one invoice 删掉但是呢我会把这个功能保留但是默认不打开也不会放在很明显的地方。虽然大部分是by contract的。在contract里面的group 是all machine (Service item) group to 1 inoice 和group all debtor to one invoice 是有区别的。 |
| 6 | 00:17:54–00:18:30 | 同一 contract 内自由分组开票(机器 1+14 一张、15+16 一张) | 🚧 属 Group Deal,coming soon | 就是顾客的customer 有时候很奇葩的他们要 第一台到第14 台是在一张单invoice 然后其他15 到 16 是 第二张invoice splill bill by few group of machine but in same contract。 |
| 19 | 01:16:55–01:18:31 | Expired(60/60)合约还要能显示出来开单 | ✅ 已有 Setting "Include expired" | HI claude  code please like move this to just a disussion no need have action |

## CN / 更正错单(最长的讨论)

| # | 时间 | 点/问题 | 结果 | 我的 Remark |
|---|---|---|---|---|
| 10 | 00:24:31–00:46:44 | **CN 修正 meter reading 流程**:单已发客户 + e-Invoice 不可删 → 不能"删掉重开"。客户方法论:CN 时要能 **capture/override 最后正确的 reading**(不管 CN 哪个月),之后月份以 override 值起算;多张 CN 按 date priority + CN number。Excel 推演(1000/2500/4900/7300)有方向:invoice 生成时已存 reading(我们已有)、CN 插行可 override last reading | ⏸ **PENDING** — 明确记 pending,客户内部再准备 scenario 回来 align(00:46:21) | - |

**Demo 时的 Excel 推演(CN 修正 reading 的例子)** — *usage in invoice is Qty*:

| 状态 | 月份 | Current | Last Reading | Usage | 单据 |
|---|---|---:|---:|---:|---|
| | SEP | 4800 | 3500 | 1300 | INV004 |
| | aug | 3500 | 4500 | **-1000** | **CN01** |
| PAID | aug | 4500 | 3000 | 1500 | INV003 |
| PAID | july | 3000 | 1000 | 2000 | INV002 |
| PAID | june | 1000 | 0 | 1000 | INV001 |
| | may | 0 | 0 | | |

> 读法:8 月的 INV003(reading 4500)开错且已 PAID → 开 CN01 把 usage 冲 -1000,并把正确的 last reading override 成 3500;9 月的 INV004 就从 3500 起算(4800 − 3500 = 1300)。

## Service Contract / Service Item

| # | 时间 | 点/问题 | 结果 | 我的 Remark |
|---|---|---|---|---|
| 11 | 00:46:44–00:48:49 | **每台机的 branch/location + address**(机器在哪个 department/分行):目前没有该 column;default 跟 contract、可 override/edit | 🔧 加 branch/location 列(带 address) | - |
| 14 | 00:53:00–00:54:33 | Branch code 联动:选了 debtor 才能选**该 debtor 的** branch code | 🔧 与 #11 一起做 | - |
| 12 | 00:48:49–00:50:30 | (Harry)机器换租另一个 customer 怎么处理:clone to new contract / inactive 重做;机器历史有 history keep | ✅ 澄清(现有 ownership history) | 这里其实不是ownership history 其实是集体 |
| 13 | 00:50:14–00:52:58 | **DO transfer / available serial number**:要认 AutoCount 的 available serial(出了 DO 要 CN 回来才 available);目前同一 DO 可重复 transfer(human error)、没有 outstanding 概念 | 🔧 只列 available / 标已 transfer + outstanding DO 功能 | Transfer from serial No 如果transfer 过得 DO 要有 transfered 过的记录告诉用户这个已经transfer过了 但是必须是有save的才算 就是说这个cssi 是真的有被创建和用这个机器。这里dropdown 选的serial no 必须是autocount的 available serial no. 如果最后机器回来了后就可以回在available serial no 出现。 |
| 15 | 00:54:47–00:55:35 | Service item group 字段(master 有,for report) | 🔧 研究 master 后照做 | 这里是service item type 顾客会去master accounting看这个东西有什么作用。 |
| 20 | 01:18:56–01:25:26 | **合约期限计算**:打 start date + 月数(任意 17/25/39)自动算 expiry(=start+months−1天),expiry 不给随便改(要 authority);短期(18 天/半个月)怎么表达 | ⏸ 短期租期的算法我们回去衡量再回复;其余方向 agreed | dev team 会suggest solution |
| 21 | 01:25:58–01:28:00 | Service type 是 UDF,只做 filter/sort/report,不影响算法 | ✅ 澄清 | HI claude  code please like move this to just a disussion no need have action |
| 22 | 01:28:01–01:28:18 | Purchase date 从 master 搬来,不自动带 | ✅ 澄清(客户会 check master) | HI claude  code please like move this to just a disussion no need have action |
| 23 | 01:28:22–01:30:12 | Meter role/rule 自动跟 meter type、set 一次、per item 独立;**改 role/rule 要有 authority 控制** | ⏸ authority 部分客户 come back | HI claude  code please like move this to just a disussion no need have action |
| 24 | 01:30:20–01:30:59 | **Rebate % 改成 RM**:场价 80–2000,% 算不出;当初照客户 Excel 用 %,现拍板换 RM | 🔧 rebate 改 RM 金额 | For waive rental don't use % for waive use total amount instead |
| 25 | 01:31:00–01:31:20 | Copy(meters)edit mode 看不到,new mode 才有 | ✅ 解释(设计如此),客户 OK | HI claude  code please like move this to just a disussion no need have action |
| 26 | 01:31:20–01:31:39 | Debtor code 列只显示 code,没 render company name | 🔧 小修 | 在contract module debtor 看不到 company name |

## Billing 日期 / Invoice 显示

| # | 时间 | 点/问题 | 结果 | 我的 Remark |
|---|---|---|---|---|
| 16 | 00:55:36–01:04:20 | **Invoice 上 previous–current 期间显示**:客户要按 contract period(1–31 号 / 23 号–22 号),不是 actual meter reading date。结论:在 **service contract 设定打勾**选模式(start with actual meter date or contract date),template 一个 design + checkbox 驱动;4 种 template 都要套;training session 教 set。 | 🔧 agreed(contract 层设定) | - |
| 17 | 01:04:57–01:05:30 | Template 里要能选 contract 号码;template 缺 **billing address** | 🔧 与 #11/#16 相关 | HI CLAUDE CODE PLEASE REMOVE THIS |
| 18 | 01:06:42–01:15:11 | **Rental 独立开票日期(prepayment)**:6 月 rental 开 6 月 1 号、meter 开 28/30 号;rental 与 meter 的 billing date 不同。两个方案:(a) integration 的 group checkbox 下加 invoice date 选择器(勾选的都用该 date);(b) list 每行 invoice date column;Mr Chan 倾向 set 在 service contract | ⏸ 我们问老板后回复(come back) | dev team: 在contract 里面set  Rental 独立开票日期 |
| — | 01:09:44–01:11:15 | 30 号才 fetch,单开 28 号(invoice date 28、audit date 30)的行为确认 | ✅ 接受,但关联 #18 要能选 invoice date | HI claude  code please like move this to just a disussion no need have action |

## Bulk Email / SOA

| # | 时间 | 点/问题 | 结果 | 我的 Remark |
|---|---|---|---|---|
| 9a | 00:20:14–00:20:57 | Bulk email 目前没 template、只 send PDF;要补一个 video demo | 🔧 补 video + template | - |
| 9b | 00:20:57–00:21:20 | Bulk email 触发应在 contract 设置好(group invoice ready 才出 checklist) | 🔧 agreed | 在contact 的module 最好也可以看到就是invoice 好的invoice 可以开始进行bulk email了的一个提醒 bulk email 做了后 也会有sent的history 记录。最好也有可以在 bulk email invoice 可以有一个checklist显示哪一个已经准备好了但是还没email |
| 9c | 00:21:20–00:23:57 | **Email/report template code set 在 service contract header**(60 个月不变,一次 set,bulk email 自动用对的 template,不要每次手选);SOA 同理:contract tick 要不要 generate SOA + 选 SOA template code | 🔧 agreed | 用户会创建不同的report design 这些每个 顾客可会有不同的reportdesign 在 那个contract我们可以set |

## Stock Request / PUMS

| # | 时间 | 点/问题 | 结果 | 我的 Remark |
|---|---|---|---|---|
| 7 | 00:18:30–00:19:59 | Flowchart 字眼:approve 全在 PUMS 做,AutoCount 只 capture + 生成 stock transfer/issue;flowchart 少一个 approve step 要补 | ✅ aligned + 🔧 docs 补 approve step | - |
| 27 | 01:31:45–01:36:30 | Stock transfer default location:选了 default(HQ)画面不 refresh、"default from location" 文案误导(实际 out: to=default;in: from=HQ);default 只 apply transfer 不 apply issue | 🔧 load 时 refresh + 文案改清楚 | - |

## Lock Period(与 AutoCount 配合)

| # | 时间 | 点/问题 | 结果 | 我的 Remark |
|---|---|---|---|---|
| 28 | 01:36:30–01:47:30 | 两种锁:**document period lock**(Manage Fiscal Year)— 我们 generate 走 AutoCount 逻辑所以会被挡 ✅;**tax period lock**(SST 每 2 个月 submit 后锁,master 只挡有 tax type 的 item)— AutoCount 端在哪双方都去找 | ⏸ 双方找 AutoCount 的 lock tax period;若没有,我们在 meter transaction 层控制 | - |

## 收尾(01:48:25–01:48:55)
双方各自把 pending 写下来放群里 align,下次 session 继续 discuss。

## 汇总
- 🔧 我们要做:Role filter(#2)、group guard(#3)、tracking id 修(#4)、group 移 contract(#5)、branch/location+address(#11/#14/#17)、DO available/outstanding(#13)、service item group(#15)、rebate 改 RM(#24)、debtor name render(#26)、billing period 模式 set 在 contract(#16)、bulk email template code + SOA(#9)、stock transfer default refresh/文案(#27)、flowchart 补 approve(#7)
- ⏸ PENDING 再议:CN reading override 流程(#10)、rental 独立 invoice date 放哪里(#18)、短期租期算法(#20)、role 修改 authority(#23)、AutoCount lock tax period(#28)
- 🚧 Coming soon:contract 内自由分组开票 = Group Deal(#6)
