# Bulk Email Invoice — 设置与操作教程（录影用脚本）

> 对象：ATP 客户 · 模块：**Service & Contract → Bulk Email Invoice**
> 内容全部照实际画面写，没有编造的按钮。

---

## ⚠️ 录影前先修这一个

**寄件人的 email 地址现在是空的。**

寄件人不是 employee，是**公司档案**（`Profile` 表的 `CompanyName` + `EmailAddress`）。
我查过这个 book：`CompanyName = ATPDEMO0001`，**`EmailAddress` 是空的**。

不填的话，寄出去的信**没有寄件地址**，多数 SMTP 会直接退信。
先去 **Tools → Options → Company Profile**（或 General Maintenance 的公司资料）把 Email 填好，再开始录。

---

## Part 1 — 初次设置（一次过，之后不用再碰）

### 1️⃣ SMTP 邮件服务器

画面上方 **Email Setting** 按钮 → 开的是 **AutoCount 自己的邮件设定对话框**（跟 A/R 的 Batch Mail 用的是同一个，设定共用）。

填：SMTP Server、Port、帐号、密码、SSL。

> 讲解词：「这里不是我们另外做的设定，是 AutoCount 原本的邮件设定 —— 你之前寄 Statement 用的那一个。设一次，两边都能用。」

### 2️⃣ 寄件人（From）

来自**公司档案**，不是每个用户各自设定：

| 显示 | 来源 |
|---|---|
| 寄件人名称 | `Profile.CompanyName` |
| 寄件人 Email | `Profile.EmailAddress` |

### 3️⃣ 收件人（To）

来自**客户主档（Debtor）**，画面上的 **Email to:** 下拉决定用哪一个：

| 选项 | 取哪个栏位 |
|---|---|
| **Debtor Email Address** | Debtor 的 Email Address |
| **Statement Email (same as SOA Batch Mail)** | Debtor 的 Statement Email |

> 讲解词：「有些客户收发票和收月结单是不同的人、不同 email，所以这里可以选。」

### 4️⃣ Email 内容 Template（主旨 + 内文）

**Template…** 按钮 → 可以新增多个版本：

- **Subject**（主旨）
- **Body**（内文）
- **Style**：`PLAIN` 纯文字 · `STYLED` 套上有公司抬头的 HTML 外框 · `HTML` 自己写的 HTML
- **★ Set as Default** —— 设为默认

可用的 token（寄出时自动替换成该客户的资料）：

```
{AccNo}         客户代号
{CompanyName}   客户公司名称
{DocNos}        这次寄给他的发票号码
```

> **重点讲解：设一次默认就好。** 之后每个月寄信不用再选 template，系统自动用默认那一个。

### 5️⃣ 个别客户要不同措辞（选做）

**Maintain Service Contract → Email Template** 栏位：

- **留空** = 用上面设的默认 template（绝大多数客户）
- **选一个** = 这个客户用他自己的措辞（例如要中文、要加特别指示）

一次寄信里可以混不同 template，**不用分批寄**。

> ⚠️ 一条规矩要讲：寄信前的确认画面是**只读**的 —— 主旨和内文一律来自各自 contract 的 template，寄信当下改不了。要改措辞就去改 template 或改这个 contract 选的 template。这样才不会手滑把甲客户的话寄给乙客户。

### 6️⃣ 报表版面（选做）

**Maintain Service Contract** 里三个：

| 栏位 | 印什么 |
|---|---|
| **Invoice Template** | 这个客户的发票版面 |
| **SOA Template** | 这个客户的月结单版面 |
| **Listing Template** | Summary Sales Invoice Meter Listing 的版面 |

**三个都可以留空** = 用 AutoCount 的默认版面。要自己设计版面才需要选。

---

## Part 2 — 每个月怎么用（录影主线）

### 步骤 1 — 开画面
**Service & Contract → Bulk Email Invoice**

### 步骤 2 — 筛选要寄的发票

| 栏位 | 说明 |
|---|---|
| **Date From / To** | 发票日期范围 |
| **Customer** | 只寄某一个客户（留空 = 全部） |
| ☑ **Meter-billing invoices only** | 只列 meter 计费产生的发票，不会混到一般买卖单 |

按 **Filter**。下方会显示 `N invoice(s)`。

> 还没寄过的才显示：画面上有「只看还没寄」的勾选，用来核对这个月有没有漏寄。

### 步骤 3 — 勾要寄的

逐张勾，或按 **Select All** 全勾。

栏位标题上有筛选列，可以再按客户、金额等等细筛。

### 步骤 4 — 按 **Email Selected**

会开 **AutoCount 的 Batch Mail 对话框**：

- **一个客户一行**（不是一张发票一行）—— 同一个客户这个月有 3 张发票，就是**一封信、3 个 PDF 附件**
- 每行显示：Customer · Company Name · Invoices · Email
- Email 栏位**当场可以改**，改了就用改的寄

### 步骤 5 — 检查主旨内文 → **Send**

主旨和内文已经带上默认 template。确认没问题就按 Send。

### 步骤 6 — 寄出后

- 每张发票写一笔**寄送记录**（谁、寄去哪个 email、什么时候）
- 同时进 **AutoCount 的 Server Mailing List** —— 那里可以看到每一封信的状态，**可以 Resend**
- 回到画面，「已寄」的状态会更新

> 讲解词：「寄出去之后不是就没了 —— AutoCount 的 Server Mailing List 里每一封都有记录，客户说没收到，直接在那边 Resend。」

---

## 录影前 5 分钟检查清单

- [ ] **公司档案的 Email 填好了**（现在是空的，必修）
- [ ] Email Setting 里的 SMTP 测试过、寄得出去
- [ ] 至少 2～3 个客户的 Debtor Email 有填
- [ ] Template… 里有一个 template 而且**打了 ★ 默认**
- [ ] 该月的 meter 发票**已经 Generate 出来了**（没有发票就没东西可寄）
- [ ] 建议先寄给自己的 email 试一次，确认附件 PDF 打得开

---

## 一句话总结（给客户听的）

> 「每个月 generate 完发票，来这里选日期、勾单、按 Email Selected —— 一个客户一封信，他自己的发票 PDF 全部附上，寄出记录 AutoCount 都留着，可以随时补寄。」
