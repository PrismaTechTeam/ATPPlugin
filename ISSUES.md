# ATP — 未完成事项 / 不合理点 / Bugs 清单

> 更新日期:2026-07-11 · 范围:Meter Reading / Service Contract / Stock Request / ATPApi
> 状态图例:🔴 高风险 Bug · 🟠 中风险 Bug · 🟡 待拍板 · 🔵 数据缺口 · ⚪ 待办 · ✅ 已修复

## ✅ 2026-07-11 已修复:B-1 ~ B-6 全部完成
- **B-1** 账期防重:`zSCP2_MeterEntry` 加 `InvoicedDocKey/DocNo/At` 章(迁移 `02_Update_zSCP2_MeterEntry_v2.sql`)。开票即盖章;Generate 跳过已开票行并回报「N 台已开票」;Fetch 不覆盖已开票行;重开画面显示 `INVOICED 单号 日期`。
- **B-2** 0 用量但 `TotalCharges > 0`(最低收费)的行不再被 "Include 0 Meter Usage" 隐藏。
- **B-3** 新增 `SelectedYear()`(所选月份晚于当前月 = 去年):LoadData 基准/到期过滤/暂存期间/offline `YYYY-MM` 全部年份正确;Filter 标题显示「billing period: MMM yyyy」。
- **B-4** `QualifiesByDate` 加年份比对(`la.Year == year`)。
- **B-5** MeterTrans 日期 + 发票「Current Meter Reading (日期)」改用 API `LastAuditDate`(缺值 fallback 当下)。
- **B-6** Fetch 的 day 按月长 clamp;列表侧原本已 clamp,两侧一致(31 → 短月 = 月底)。
- 附带:SQL Server TCP 被 20ms reset 循环打挂 → 已重启恢复;reset 循环改为 **5 秒**安全间隔重开(勿再用 20ms)。

---

## 一、真 Bug(建议尽快修)

### 🔴 B-1 同一账期可重复开票,无防呆
开完 5 月发票后,再 Fetch → Select All → Generate,会**再开一次一模一样的账**。
- 原因:开票写入 `zSCP_MeterTrans` 的日期是「开票当下」(如今天 7 月),不在 5 月账期内 → 下次算 5 月时基准读数不变 → 同样用量再算一次。
- 修法:开票前检查该 meter + 账期是否已有发票(MeterTrans 或暂存表加账期标记),已开的行标示「Invoiced」并挡掉 / 警告。

### 🔴 B-2 「0 用量但有最低收费」的机器会漏开票
- "Include 0 Meter Usage" 默认**不勾** = 隐藏 0 用量行;而 Generate **只看当前分页可见行** → 这些机器该收的 **Minimum Charges 收不到**。
- 修法(选一):① 0 用量但 `MinCharges > 0` 的行豁免隐藏;② Generate 时检测被隐藏的最低收费行并警告。

### 🔴 B-3 跨年月份全链路错误
代码多处写死 `year = DateTime.Today.Year`:
- `LoadData` 的 last-reading 边界(`< 该月1号`)
- Offline API 的 `?month=YYYY-MM`
- 暂存表 `zSCP2_MeterEntry` 的 PeriodYear
- 到期过滤 `DATEFROMPARTS(year, month, 1)`

→ **明年 1 月补 2026 年 12 月的账,会全部查成 2027-12。**
- 修法:月份下拉旁边加年份(或选月自动推断:所选月份 > 当前月 → 用去年)。

### 🟠 B-4 `QualifiesByDate` 不比对年份
只比 `月份 + 日`(`la.Month == month && la.Day <= day`)→ 一笔 **2025**-06 的旧读数会被当成 **2026** 年 6 月的匹配进来。
- 修法:加 `la.Year == 所选年份`(与 B-3 一起修)。

### 🟠 B-5 MeterTrans 写入日期用「开票当下」而非 API 的 LastAuditDate
下期的 "Last Read Date" 显示的是**开票日**,不是真实抄表日。
- 修法:`MeterInvoiceGenerator.WriteMeterTrans` 改用该行的 LastAuditDate(无值才 fallback 到当下)。
- ⚠ 需拍板:与 B-1 的账期判断方式有关联,建议一起定。

### ⚪ B-6(潜在)Billing Day 31 在短月的 clamp 不一致(SPEC-MR-002)
选 31 在 30 天的月份可能匹配不到(有效日 clamp 成 30 ≠ 31)。目前**没有任何机器用 31 号**,先记录不修。

---

## 二、不合理点 / 需要拍板

### 🟡 D-1 "Include expired Service Item" 默认值
目前默认 = **显示**过期机器(账单行为不变)。你说过「过期不显示不错」→ 要不要改成**默认隐藏**?(会让 ~1,199 台已过期的默认从列表消失)

### 🟡 D-2 开票方式有两处设置,互相冲突
- 合约编辑器有 **BillingMode G/S**;但 Generate 的 "**Separate invoice per CSSI**" 勾选框会**完全无视**合约设定(勾=全部 S,不勾=全部 G)。
- 新的多机合约设了 G/S 等于没用。
- 建议:默认**跟合约 BillingMode 走**,勾选框只作为一次性 override(或干脆三选:Follow contract / Force per CSSI / Force per contract)。

### 🟡 D-3 8 张「平票」合约的结账日规则
合约的 BillingDay 取 item override 众数;8 张平票(如 7、30 各一台)我取了**较大日**,未经确认。名单可随时列出。

### 🟡 D-4 发票日期 = 今天
补 5 月的账,发票 DocDate 是**今天**(影响账务归属月份)。要不要改成「账期月底」或「所选月份的 BillingDay」?

---

## 三、数据缺口(master 带来的)

### 🔵 G-1 86 台 CSSI 没有到期日
master(`v8_atp_main`)本身就没有 → 无法导入。有资料再补(例:CSSI 00000013 / 15 / 17 / 18 / 19…)。

### 🔵 G-2 4 台 CSSI 的 billing day 空白
Excel 里 day 为空:**CSSI 00003018 / 00003019 / 00003020 / 00003022** → 目前继承合约日。

### 🔵 G-3 121 个 `.N` 子项编号对不上
Excel 有 `CSSI 00002471.1` 这类子项,书里没有对应 item → billing day 没套到。需确认书里是用什么编号存的。

### 🔵 G-4 zSCP_MeterTrans 有脏日期
错误年份(8024-08、5022-05/06、3023-02/03、2222-01)+ 未来日期(2026-12)。查询已排除(取「账期前」最新一笔),但脏数据还躺着 → 建议清掉。

### 🔵 G-5 Demo 账套没有 2026-06 的读数
真实读数停在 2026-03 → 7 月账单的基准是 3 月(跨月累计,逻辑正确但看起来奇怪)。可塞 6 月测试读数。

---

## 四、未完成的待办

### ⚪ T-1 最后一批 code 未 commit / 未 merge main
未提交:单一 CSSI 勾选栏、Select All ↔ Unselect All、Generate 只看当前分页、勾选修复(禁排序 + 单击生效)、恢复的 per-CSSI 勾选框、SPEC 更新、ISSUES.md(本档)。
`main` 停在 `66164de`,`feat/stock-request-module` 已到 `9790f9d` + 未提交改动。

### ⚪ T-2 AED_ATPDEMO0003 的 Stock Request 测试数据未清
测试推的 webhook 行(53163、73xxxx、75xxxx、64xxxx、66xxxx…)还在。

### ⚪ T-3 ATPApi 发布默认值
`appsettings.sample.json` 的 BaseUrl 默认还是 `http://localhost:5007`;`http://+:5007`(反代必需)只改了本机安装。要不要更新 sample + installer → 发 **1.0.3**。

### ⚪ T-4 Online endpoint 服务器端优化(PUMS 后端)
`meter-reading-online.php` 又慢(~15s)又会闪断。插件已做并行 + 重试 + 30s timeout,但根治要后端加缓存(可提供参考 PHP)。

### ⚪ T-5 SPEC.md 待补
B-1 ~ B-5 修复后应记入 SPEC(账期防重、0 用量最低收费、年份处理)。

---

## 五、已确认没问题(供参考)
- 过期机器:到期当月照常结账,次月起隐藏(SPEC-MR-001,已实测 32 台 5 月到期 ✔)
- 旧数据已拆分:3,067 台 CSSI = 3,067 张合约(SPEC-SC-004,有备份表 ✔)
- Billing day 批量导入:1,579 台 override + 688 张合约众数日 ✔
- Fetch:异步 + 并行 + 重试 + 存库(重开免 Fetch)✔
- Generate Invoice:免逐张点击,进度框 + error log ✔
