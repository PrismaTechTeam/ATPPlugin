using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Keeps a stored password out of plain sight. Encrypt on the way in, decrypt on the way out.
    ///
    /// <para>That is the entire requirement and it was set deliberately. The two books this serves
    /// belong to the same owner and the same finance department; the password is stored so that a
    /// monthly billing run does not stop to ask for it, not to hold a line against somebody who
    /// already has the database. Treating it as a trust boundary would mean a key store, a rotation
    /// story and a recovery story for a secret whose owner can read both books anyway.</para>
    ///
    /// <para>Deliberately NOT DPAPI. DPAPI ties the ciphertext to one Windows account on one
    /// machine, so the clerk who takes over next year, or the same book opened from the second PC in
    /// the office, would find the saved password unreadable with no way to tell why. A key carried
    /// in the assembly travels with the book -- restore the database anywhere and it still
    /// opens.</para>
    ///
    /// <para>Everything is tagged with a version marker, so a stored value can always be recognised
    /// and a later scheme can be added beside this one without guessing at what is already
    /// stored.</para>
    /// </summary>
    public static class ScpSecret
    {
        private const string Marker = "enc1:";

        // Fixed passphrase + fixed salt: the key must be derivable from the assembly alone, on any
        // machine, for ever. See the class note -- this is obfuscation with a version number, and
        // saying so plainly here is better than a comment implying more.
        private const string Passphrase = "ATP.ServiceContract.InterBilling.2026";
        private static readonly byte[] Salt = new byte[]
        {
            0x41, 0x54, 0x50, 0x2D, 0x53, 0x43, 0x50, 0x32,
            0x2D, 0x49, 0x42, 0x2D, 0x76, 0x31, 0x2E, 0x30
        };

        /// <summary>Is this a value Protect produced? Anything else is treated as plain text, so a
        /// password typed straight into the table by hand still works.</summary>
        public static bool IsProtected(string stored)
        {
            return stored != null && stored.StartsWith(Marker, StringComparison.Ordinal);
        }

        /// <summary>Encrypts. An empty password stays empty -- there is nothing to hide and an
        /// encrypted empty string would only look like a password that exists.</summary>
        public static string Protect(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            if (IsProtected(plain)) return plain;          // already done; do not double-wrap
            try
            {
                byte[] key = DeriveKey();
                using (AesManaged aes = new AesManaged())
                {
                    aes.KeySize = 256;
                    aes.Key = key;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.GenerateIV();
                    byte[] iv = aes.IV;
                    byte[] body = Encoding.UTF8.GetBytes(plain);
                    using (ICryptoTransform enc = aes.CreateEncryptor())
                    using (MemoryStream ms = new MemoryStream())
                    {
                        // The IV goes in front of the ciphertext, in the clear. That is what an IV is
                        // for: it must be different every time and it does not need to be secret.
                        ms.Write(iv, 0, iv.Length);
                        using (CryptoStream cs = new CryptoStream(ms, enc, CryptoStreamMode.Write))
                        {
                            cs.Write(body, 0, body.Length);
                            cs.FlushFinalBlock();
                        }
                        return Marker + Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch
            {
                // Storing the password in the clear would be a silent downgrade nobody could see.
                // Storing nothing makes the connection fail loudly, with a screen that can retype it.
                return "";
            }
        }

        /// <summary>Decrypts. Returns "" for anything unreadable, and returns a value that was never
        /// encrypted unchanged -- a password typed into the table by hand is honoured.</summary>
        public static string Unprotect(string stored)
        {
            if (string.IsNullOrEmpty(stored)) return "";
            if (!IsProtected(stored)) return stored;
            try
            {
                byte[] all = Convert.FromBase64String(stored.Substring(Marker.Length));
                if (all.Length <= 16) return "";
                byte[] iv = new byte[16];
                Buffer.BlockCopy(all, 0, iv, 0, 16);
                byte[] key = DeriveKey();
                using (AesManaged aes = new AesManaged())
                {
                    aes.KeySize = 256;
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform dec = aes.CreateDecryptor())
                    using (MemoryStream ms = new MemoryStream(all, 16, all.Length - 16))
                    using (CryptoStream cs = new CryptoStream(ms, dec, CryptoStreamMode.Read))
                    using (StreamReader r = new StreamReader(cs, Encoding.UTF8))
                    {
                        return r.ReadToEnd();
                    }
                }
            }
            catch
            {
                return "";
            }
        }

        /// <summary>What to show a user in place of a stored password: that one exists, not what it
        /// is. The length is fixed on purpose -- a mask that matched the real length would give the
        /// length away.</summary>
        public static string Mask(string stored)
        {
            return string.IsNullOrEmpty(stored) ? "" : "********";
        }

        private static byte[] DeriveKey()
        {
            using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(Passphrase, Salt, 1000))
            {
                return kdf.GetBytes(32);
            }
        }
    }
}
