using System;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// "Professional" bulk-email styling. The clerk writes PLAIN TEXT (+ tokens) — this wraps it
    /// in an email-client-safe HTML frame (table layout, inline styles): a coloured header bar
    /// with the sender company, comfortably spaced body text, and a small footer. AutoCount's
    /// MailHelper auto-detects the &lt;html&gt; wrapper and sends the mail as HTML.
    /// </summary>
    public static class ScpMailHtml
    {
        /// <summary>HTML-escape then convert line breaks — the user's text stays text.</summary>
        public static string EscapeText(string plain)
        {
            plain = plain ?? "";
            plain = plain.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
            plain = plain.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br>\r\n");
            return plain;
        }

        /// <summary>Wraps an ALREADY-ESCAPED body (tokens may still be inside — they are plain
        /// words, substitution after wrapping is safe) into the styled frame.</summary>
        public static string Wrap(string escapedBody, string senderCompany)
        {
            string company = EscapeText(string.IsNullOrEmpty(senderCompany) ? "" : senderCompany)
                .Replace("<br>\r\n", " ");
            return
                "<html>\r\n<body style=\"margin:0;padding:0;background:#f2f4f6;\">\r\n" +
                "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#f2f4f6;\">\r\n" +
                "<tr><td align=\"center\" style=\"padding:24px 12px;\">\r\n" +
                "<table width=\"640\" cellpadding=\"0\" cellspacing=\"0\" " +
                "style=\"background:#ffffff;border:1px solid #e3e7ea;border-radius:6px;" +
                "font-family:'Segoe UI',Arial,sans-serif;\">\r\n" +
                "<tr><td style=\"background:#76b82a;padding:18px 28px;color:#ffffff;" +
                "font-size:20px;font-weight:bold;\">" + company + "</td></tr>\r\n" +
                "<tr><td style=\"padding:26px 28px;font-size:14px;color:#333333;line-height:1.7;\">" +
                escapedBody + "</td></tr>\r\n" +
                "<tr><td style=\"padding:14px 28px;background:#f7f9fa;border-top:1px solid #e3e7ea;" +
                "font-size:11px;color:#8a949c;\">This email and its attached invoice(s) were sent by " +
                company + ". Please contact us if you have any questions.</td></tr>\r\n" +
                "</table>\r\n</td></tr></table>\r\n</body>\r\n</html>";
        }

        /// <summary>Plain template text (tokens intact) → the full styled HTML mail body.</summary>
        public static string BuildStyled(string plainBody, string senderCompany)
        {
            return Wrap(EscapeText(plainBody), senderCompany);
        }

        /// <summary>User-authored Custom HTML: AutoCount only treats a body as HTML when it has an
        /// &lt;html&gt;…&lt;/html&gt; wrapper — add a minimal one if the user's snippet lacks it.</summary>
        public static string EnsureHtml(string html)
        {
            html = html ?? "";
            bool wrapped = System.Text.RegularExpressions.Regex.IsMatch(html, "<html[\\s>]",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                && System.Text.RegularExpressions.Regex.IsMatch(html, "</html>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return wrapped ? html : "<html>\r\n<body>\r\n" + html + "\r\n</body>\r\n</html>";
        }
    }
}
