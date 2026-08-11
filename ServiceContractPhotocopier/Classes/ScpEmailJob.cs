using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Bulk invoice email as a background JOB (customer 10/08).
    ///
    /// Why a job and not a dialog: sending 200 customers takes minutes, and the operator should not
    /// have to babysit a modal window. Closing the form must not abandon a half-finished run — so
    /// the worker thread is owned by this class, not by any form, and every step is written to the
    /// database as it happens. The progress screen is a VIEW of those rows, which means it also
    /// shows a run someone else started on another PC.
    ///
    /// Double-send protection is a real distributed lock: one row per invoice in zSCP2_EmailLock,
    /// taken by INSERT so the primary key does the arbitration. Two operators clicking Send at the
    /// same moment cannot both win. A run that dies mid-way leaves stale rows, so they are taken
    /// over after LOCK_STALE_MINUTES rather than blocking that invoice forever.
    ///
    /// Sending the same invoice twice is ALLOWED (the customer asked for it) — it is only ever
    /// blocked while another send is actually in flight. Every send is recorded in zSCP2_EmailLog,
    /// so a second one shows up as a second row, not as a silent overwrite.
    /// </summary>
    public static class ScpEmailJob
    {
        /// <summary>A lock older than this is presumed abandoned and may be taken over.</summary>
        private const int LOCK_STALE_MINUTES = 15;

        /// <summary>
        /// Development safety net: while the whitelist is on, only these addresses may actually
        /// receive mail. Everything else is skipped and says why. The point is that running a test
        /// against a live book cannot reach real customers — so this is checked in the JOB, the last
        /// place before the send, not merely in the UI where it could be bypassed.
        /// </summary>
        public static bool IsBlockedByWhitelist(DBSetting db, string email, out string allowedList)
        {
            allowedList = "";
            bool on;
            try
            {
                on = ServiceContractPhotocopier.Data.PumsConfig.GetBool(db,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_EMAIL_WHITELIST_ON,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_EMAIL_WHITELIST_ON);
            }
            catch { on = ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_EMAIL_WHITELIST_ON; }
            if (!on) return false;

            string raw;
            try
            {
                raw = ServiceContractPhotocopier.Data.PumsConfig.Get(db,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_EMAIL_WHITELIST,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_EMAIL_WHITELIST);
            }
            catch { raw = ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_EMAIL_WHITELIST; }

            List<string> allowed = new List<string>();
            foreach (string part in (raw ?? "").Split(new char[] { '\r', '\n', ',', ';' }))
            {
                string a = part.Trim();
                if (a.Length > 0) allowed.Add(a);
            }
            allowedList = string.Join(", ", allowed.ToArray());

            // An EMPTY list while the switch is on blocks everything on purpose: "protection turned
            // on but nothing listed" must fail closed, never silently mail the whole customer base.
            string test = (email ?? "").Trim();
            foreach (string a in allowed)
                if (string.Equals(a, test, StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        /// <summary>True when the safety net is active — the screens shout about it.</summary>
        public static bool WhitelistOn(DBSetting db)
        {
            try
            {
                return ServiceContractPhotocopier.Data.PumsConfig.GetBool(db,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_EMAIL_WHITELIST_ON,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_EMAIL_WHITELIST_ON);
            }
            catch { return ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_EMAIL_WHITELIST_ON; }
        }

        public class Recipient
        {
            public string DebtorCode = "";
            public string DebtorName = "";
            public string Email = "";
            public List<long> DocKeys = new List<long>();
            public List<string> DocNos = new List<string>();
            /// <summary>Resolved per-customer wording; null = use the default template.</summary>
            public ScpEmailTemplates.Template Template;
            /// <summary>Filled by the job as it goes. Rendering used to happen up front on the UI
            /// thread, which froze the form for the whole batch before the operator got control
            /// back; it now runs here, spread through the send it feeds.</summary>
            public List<AutoCount.Mail.AttachmentData> Attachments = new List<AutoCount.Mail.AttachmentData>();
        }

        private static readonly object _gate = new object();
        private static Thread _worker;
        private static long _currentJobKey;

        /// <summary>True while this process is running a job. Another PC's job is not visible here —
        /// look at the job table for that.</summary>
        public static bool IsRunning
        {
            get { lock (_gate) { return _worker != null && _worker.IsAlive; } }
        }

        public static long CurrentJobKey
        {
            get { lock (_gate) { return _currentJobKey; } }
        }

        /// <summary>
        /// Queue the run and return immediately with the new JobKey. The caller can close its form;
        /// the thread is a foreground thread on purpose so an in-flight send is not torn down when
        /// the UI goes away.
        /// </summary>
        public static long Start(DBSetting db, List<Recipient> recipients, string fromName, string fromEmail,
            ScpEmailTemplates.Template defaultTemplate, ScpInvoicePdfRenderer renderer, ScpExtraDocs extras)
        {
            if (db == null || recipients == null || recipients.Count == 0) return 0;
            lock (_gate)
            {
                if (_worker != null && _worker.IsAlive)
                    throw new InvalidOperationException(
                        "A bulk email run is already in progress on this PC. Open Send Progress to watch it.");

                long jobKey = CreateJob(db, recipients, defaultTemplate, renderer, extras);
                _currentJobKey = jobKey;
                _worker = new Thread(delegate ()
                {
                    try { Run(db, jobKey, recipients, fromName, fromEmail, defaultTemplate, renderer, extras); }
                    catch (Exception ex) { AbortJob(db, jobKey, ex.Message); }
                    finally { lock (_gate) { _worker = null; } }
                });
                _worker.IsBackground = false;   // survive the form; do NOT die with the UI
                _worker.Name = "SCP bulk email job";
                _worker.SetApartmentState(ApartmentState.STA);
                _worker.Start();
                return jobKey;
            }
        }

        // ───────────────────────── job rows ─────────────────────────

        private static long CreateJob(DBSetting db, List<Recipient> recipients,
            ScpEmailTemplates.Template defaultTemplate, ScpInvoicePdfRenderer renderer,
            ScpExtraDocs extras)
        {
            string who = "", machine = "";
            try { who = AutoCount.Authentication.UserSession.CurrentUserSession.LoginUserID; } catch { }
            try { machine = Environment.MachineName; } catch { }

            long jobKey;
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "INSERT INTO dbo.zSCP2_EmailJob (CreatedBy, MachineName, Status, TotalCount, LastHeartbeat) " +
                    "VALUES (@u,@m,'RUNNING',@t,GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS bigint);", cn))
                {
                    cmd.Parameters.AddWithValue("@u", (object)who ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@m", (object)machine ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@t", recipients.Count);
                    jobKey = Convert.ToInt64(cmd.ExecuteScalar());
                }
                foreach (Recipient r in recipients)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "INSERT INTO dbo.zSCP2_EmailJobItem (JobKey, DebtorCode, DebtorName, Email, DocNos, DocKeys, " +
                        "EmailTemplateName, InvoiceLayoutName, ExtraDocs, Status) " +
                        "VALUES (@j,@c,@n,@e,@dn,@dk,@tpl,@lay,@extra,'PENDING')", cn))
                    {
                        cmd.Parameters.AddWithValue("@j", jobKey);
                        cmd.Parameters.AddWithValue("@c", Cut(r.DebtorCode, 30));
                        cmd.Parameters.AddWithValue("@n", Cut(r.DebtorName, 200));
                        cmd.Parameters.AddWithValue("@e", Cut(r.Email, 200));
                        cmd.Parameters.AddWithValue("@dn", Cut(string.Join(", ", r.DocNos.ToArray()), 900));
                        cmd.Parameters.AddWithValue("@dk", Cut(JoinKeys(r.DocKeys), 900));
                        // Stamped now, while the answer is knowable — the history has to survive a
                        // template being renamed or a contract being pointed somewhere else later.
                        cmd.Parameters.AddWithValue("@tpl", Cut(TemplateNameOf(r, defaultTemplate), 200));
                        cmd.Parameters.AddWithValue("@lay", Cut(LayoutNameOf(r, renderer), 200));
                        cmd.Parameters.AddWithValue("@extra",
                            Cut(extras == null ? "" : extras.DescribeFor(r.DebtorCode), 200));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            return jobKey;
        }

        /// <summary>
        /// The email wording this customer gets: their contract's template, or the book default.
        /// Public so the confirmation screen and the history show the SAME name — the operator
        /// should never see one word before sending and a different one after.
        /// </summary>
        public static string TemplateNameOf(Recipient r, ScpEmailTemplates.Template defaultTemplate)
        {
            if (r != null && r.Template != null && !string.IsNullOrEmpty(r.Template.Name))
                return r.Template.Name;
            if (defaultTemplate != null && !string.IsNullOrEmpty(defaultTemplate.Name))
                return defaultTemplate.Name + "  (default)";
            return "(default)";
        }

        /// <summary>
        /// The report design this customer's invoices are rendered with. One customer can hold
        /// invoices from several contracts, so the layouts can genuinely differ — say so rather
        /// than picking one and implying it covers them all.
        /// </summary>
        public static string LayoutNameOf(Recipient r, ScpInvoicePdfRenderer renderer)
        {
            if (r == null || renderer == null || r.DocKeys == null) return "";
            List<string> seen = new List<string>();
            foreach (long dk in r.DocKeys)
            {
                string n = renderer.LayoutFor(dk);
                if (!seen.Contains(n)) seen.Add(n);
            }
            if (seen.Count == 0) return "";
            if (seen.Count == 1) return seen[0];
            return string.Join(" / ", seen.ToArray());
        }

        // ───────────────────────── the run ─────────────────────────

        private static void Run(DBSetting db, long jobKey, List<Recipient> recipients,
            string fromName, string fromEmail, ScpEmailTemplates.Template defaultTemplate,
            ScpInvoicePdfRenderer renderer, ScpExtraDocs extras)
        {
            int ok = 0, failed = 0, skipped = 0;

            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                DataTable items = GetItems(cn, jobKey);
                int idx = 0;

                foreach (Recipient r in recipients)
                {
                    long itemKey = idx < items.Rows.Count ? Convert.ToInt64(items.Rows[idx]["ItemKey"]) : 0;
                    idx++;
                    Heartbeat(cn, jobKey);

                    if (string.IsNullOrEmpty((r.Email ?? "").Trim()))
                    {
                        SetItem(cn, itemKey, "SKIPPED", "No email address on the customer.");
                        skipped++; Counts(cn, jobKey, ok, failed, skipped); continue;
                    }

                    // Checked HERE, immediately before the send, rather than only in the UI — the
                    // whole value of the safety net is that no code path can get around it.
                    string allowed;
                    if (IsBlockedByWhitelist(db, r.Email, out allowed))
                    {
                        SetItem(cn, itemKey, "SKIPPED",
                            "BLOCKED by the email whitelist (development safety). Allowed: " +
                            (allowed.Length == 0 ? "(nothing listed)" : allowed));
                        skipped++; Counts(cn, jobKey, ok, failed, skipped); continue;
                    }

                    // Take the lock on every invoice in this email, all or nothing. A partial grab
                    // would leave the customer's set split across two senders.
                    List<long> held = new List<long>();
                    string blockedBy;
                    if (!TryLockAll(cn, jobKey, r.DocKeys, held, out blockedBy))
                    {
                        ReleaseAll(cn, held);
                        SetItem(cn, itemKey, "SKIPPED",
                            "Another send is already in progress for these invoices" +
                            (string.IsNullOrEmpty(blockedBy) ? "." : " (" + blockedBy + ")."));
                        skipped++; Counts(cn, jobKey, ok, failed, skipped); continue;
                    }

                    try
                    {
                        SendOne(db, r, fromName, fromEmail, defaultTemplate, renderer, extras);
                        SetItem(cn, itemKey, "SENT", null);
                        WriteLog(cn, r);
                        ok++;
                    }
                    catch (Exception ex)
                    {
                        // One bad address must never stop the run — that is the whole reason this is
                        // a job. Record it and carry on to the next customer.
                        SetItem(cn, itemKey, "FAILED", Cut(ShortError(ex), 500));
                        failed++;
                    }
                    finally
                    {
                        ReleaseAll(cn, held);
                        Counts(cn, jobKey, ok, failed, skipped);
                    }
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE dbo.zSCP2_EmailJob SET Status='DONE', FinishedAt=GETDATE(), LastHeartbeat=GETDATE(), " +
                    "SuccessCount=@s, FailedCount=@f, SkippedCount=@k WHERE JobKey=@j", cn))
                {
                    cmd.Parameters.AddWithValue("@s", ok);
                    cmd.Parameters.AddWithValue("@f", failed);
                    cmd.Parameters.AddWithValue("@k", skipped);
                    cmd.Parameters.AddWithValue("@j", jobKey);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void SendOne(DBSetting db, Recipient r, string fromName, string fromEmail,
            ScpEmailTemplates.Template defaultTemplate, ScpInvoicePdfRenderer renderer, ScpExtraDocs extras)
        {
            ScpEmailTemplates.Template tpl = r.Template ?? defaultTemplate;
            string subject = tpl == null ? "" : (tpl.Subject ?? "");
            string body = tpl == null ? "" : (tpl.Body ?? "");
            string style = tpl == null ? "PLAIN" : (tpl.Style ?? "PLAIN");
            string docNos = string.Join(", ", r.DocNos.ToArray());

            subject = Tokens(subject, r, docNos);
            body = Tokens(body, r, docNos);
            if (style == "STYLED") body = ScpMailHtml.BuildStyled(body, fromName);
            else if (style == "HTML") body = ScpMailHtml.EnsureHtml(body);

            // Render this customer's documents now, not at the start of the batch — the operator is
            // already back in the UI and the work is spread across the run.
            if (r.Attachments == null) r.Attachments = new List<AutoCount.Mail.AttachmentData>();
            if (r.Attachments.Count == 0 && renderer != null)
            {
                List<string> failed = new List<string>();
                for (int i = 0; i < r.DocKeys.Count; i++)
                {
                    string fileName;
                    byte[] pdf = renderer.Render(r.DocKeys[i], r.DocNos[i], out fileName);
                    if (pdf == null || pdf.Length == 0) { failed.Add(r.DocNos[i]); continue; }
                    AutoCount.Mail.AttachmentData a = new AutoCount.Mail.AttachmentData();
                    a.FileName = fileName;
                    a.Binary = pdf;
                    r.Attachments.Add(a);
                }
                // A customer with five invoices who receives one PDF used to be recorded as fully
                // sent. Any missing invoice fails the whole recipient: sending someone a partial
                // bill and calling it done is worse than not sending at all.
                if (failed.Count > 0)
                    throw new Exception("Could not produce the invoice PDF for " +
                        string.Join(", ", failed.ToArray()) + " — nothing was sent to this customer.");

                // Extras the contract asks for: the statement, the meter listing, or both.
                if (extras != null) extras.Attach(db, r);
            }
            if (r.Attachments.Count == 0)
                throw new Exception("No invoice PDF could be produced for " + docNos + ".");

            AutoCount.Mail.EmailData data = new AutoCount.Mail.EmailData();
            data.Email = r.Email.Trim();
            data.RecipientName = r.DebtorName;
            data.Subject = subject;
            data.EmailBody = body;
            data.FromName = fromName;
            data.FromEmail = fromEmail;
            data.Attachments = r.Attachments.ToArray();

            // Uses the SMTP configured in Email Setting — the same transport AutoCount's own
            // Batch Mail uses, so there is one place to configure and one place to fix.
            //
            // The RESULT IS NOT OPTIONAL. AutoCount returns false without throwing when the mail
            // server has no host name, so ignoring it marked every recipient SENT, wrote a log row,
            // greened the Emailed column and reported "Completed" for a run that sent nothing.
            bool sent = AutoCount.Mail.MailHelper.SendMailWithDefaultMailServerSetting(db, data);
            if (!sent)
                throw new Exception(
                    "The mail server rejected the message or is not configured. " +
                    "Check Email Setting — SMTP Server, port, user and password — then send again.");
        }

        private static string Tokens(string text, Recipient r, string docNos)
        {
            if (text == null) return "";
            text = text.Replace("{AccNo}", r.DebtorCode ?? "");
            text = text.Replace("{CompanyName}", r.DebtorName ?? "");
            text = text.Replace("{DocNos}", docNos ?? "");
            return text;
        }

        // ───────────────────────── distributed lock ─────────────────────────

        /// <summary>Take the lock on every doc, or none. Returns false and names the holder when
        /// any of them is already being sent elsewhere.</summary>
        private static bool TryLockAll(SqlConnection cn, long jobKey, List<long> docKeys,
            List<long> held, out string blockedBy)
        {
            blockedBy = "";
            string who = "", machine = "";
            try { who = AutoCount.Authentication.UserSession.CurrentUserSession.LoginUserID; } catch { }
            try { machine = Environment.MachineName; } catch { }

            foreach (long dk in docKeys)
            {
                // Clear an abandoned lock first, then INSERT. The PK is the arbiter: if a second PC
                // inserts the same DocKey a microsecond earlier, ours throws and we back off.
                using (SqlCommand del = new SqlCommand(
                    "DELETE FROM dbo.zSCP2_EmailLock WHERE DocKey=@d AND LockedAt < DATEADD(MINUTE,-" +
                    LOCK_STALE_MINUTES + ",GETDATE())", cn))
                {
                    del.Parameters.AddWithValue("@d", dk);
                    del.ExecuteNonQuery();
                }
                try
                {
                    using (SqlCommand ins = new SqlCommand(
                        "INSERT INTO dbo.zSCP2_EmailLock (DocKey, JobKey, LockedBy, MachineName) VALUES (@d,@j,@u,@m)", cn))
                    {
                        ins.Parameters.AddWithValue("@d", dk);
                        ins.Parameters.AddWithValue("@j", jobKey);
                        ins.Parameters.AddWithValue("@u", (object)who ?? DBNull.Value);
                        ins.Parameters.AddWithValue("@m", (object)machine ?? DBNull.Value);
                        ins.ExecuteNonQuery();
                    }
                    held.Add(dk);
                }
                catch (SqlException)
                {
                    using (SqlCommand q = new SqlCommand(
                        "SELECT TOP 1 ISNULL(LockedBy,'') + ' on ' + ISNULL(MachineName,'?') " +
                        "FROM dbo.zSCP2_EmailLock WHERE DocKey=@d", cn))
                    {
                        q.Parameters.AddWithValue("@d", dk);
                        object o = q.ExecuteScalar();
                        blockedBy = o == null || o == DBNull.Value ? "" : Convert.ToString(o);
                    }
                    return false;
                }
            }
            return true;
        }

        private static void ReleaseAll(SqlConnection cn, List<long> held)
        {
            foreach (long dk in held)
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM dbo.zSCP2_EmailLock WHERE DocKey=@d", cn))
                    {
                        cmd.Parameters.AddWithValue("@d", dk);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { }   // a lock we cannot release will time out on its own
            }
        }

        // ───────────────────────── small helpers ─────────────────────────

        private static DataTable GetItems(SqlConnection cn, long jobKey)
        {
            DataTable t = new DataTable();
            using (SqlCommand cmd = new SqlCommand(
                "SELECT ItemKey FROM dbo.zSCP2_EmailJobItem WHERE JobKey=@j ORDER BY ItemKey", cn))
            {
                cmd.Parameters.AddWithValue("@j", jobKey);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(t);
            }
            return t;
        }

        private static void SetItem(SqlConnection cn, long itemKey, string status, string error)
        {
            if (itemKey <= 0) return;
            try
            {
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE dbo.zSCP2_EmailJobItem SET Status=@s, ErrorMsg=@e, " +
                    "SentAt=CASE WHEN @s='SENT' THEN GETDATE() ELSE SentAt END WHERE ItemKey=@k", cn))
                {
                    cmd.Parameters.AddWithValue("@s", status);
                    cmd.Parameters.AddWithValue("@e", (object)error ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@k", itemKey);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private static void Counts(SqlConnection cn, long jobKey, int ok, int failed, int skipped)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE dbo.zSCP2_EmailJob SET SuccessCount=@s, FailedCount=@f, SkippedCount=@k, " +
                    "LastHeartbeat=GETDATE() WHERE JobKey=@j", cn))
                {
                    cmd.Parameters.AddWithValue("@s", ok);
                    cmd.Parameters.AddWithValue("@f", failed);
                    cmd.Parameters.AddWithValue("@k", skipped);
                    cmd.Parameters.AddWithValue("@j", jobKey);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private static void Heartbeat(SqlConnection cn, long jobKey)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE dbo.zSCP2_EmailJob SET LastHeartbeat=GETDATE() WHERE JobKey=@j", cn))
                {
                    cmd.Parameters.AddWithValue("@j", jobKey);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        /// <summary>Permanent per-invoice history. A resend adds a row; it never overwrites one.</summary>
        private static void WriteLog(SqlConnection cn, Recipient r)
        {
            string by = "";
            try { by = AutoCount.Authentication.UserSession.CurrentUserSession.LoginUserID; } catch { }
            for (int i = 0; i < r.DocKeys.Count; i++)
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "INSERT INTO dbo.zSCP2_EmailLog (DocKey, DocNo, DebtorCode, Email, SentAt, SentBy) " +
                        "VALUES (@dk,@dn,@acc,@em,GETDATE(),@by)", cn))
                    {
                        cmd.Parameters.AddWithValue("@dk", r.DocKeys[i]);
                        cmd.Parameters.AddWithValue("@dn", r.DocNos[i]);
                        cmd.Parameters.AddWithValue("@acc", r.DebtorCode ?? "");
                        cmd.Parameters.AddWithValue("@em", r.Email ?? "");
                        cmd.Parameters.AddWithValue("@by", by);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { }   // the email already went out; history must never undo that
            }
        }

        private static void AbortJob(DBSetting db, long jobKey, string message)
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(db.ConnectionString))
                {
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE dbo.zSCP2_EmailJob SET Status='ABORTED', FinishedAt=GETDATE() WHERE JobKey=@j", cn))
                    {
                        cmd.Parameters.AddWithValue("@j", jobKey);
                        cmd.ExecuteNonQuery();
                    }
                    using (SqlCommand cmd = new SqlCommand(
                        "DELETE FROM dbo.zSCP2_EmailLock WHERE JobKey=@j", cn))
                    {
                        cmd.Parameters.AddWithValue("@j", jobKey);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        private static string JoinKeys(List<long> keys)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (long k in keys) { if (sb.Length > 0) sb.Append(","); sb.Append(k); }
            return sb.ToString();
        }

        private static string Cut(string s, int max)
        {
            if (s == null) return "";
            return s.Length <= max ? s : s.Substring(0, max);
        }

        private static string ShortError(Exception ex)
        {
            Exception e = ex;
            while (e.InnerException != null) e = e.InnerException;
            return e.Message;
        }
    }
}
