using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iPhoneRestrictionsPasscodeBFLib
{
    public static class PasscodeBreaker
    {
        public static string BreakPassCode(string backupFolder)
        {
            string hashFilename = String.Empty;
            using (SHA1 sha = new SHA1Managed())
            {
                hashFilename = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes("HomeDomain-Library/Preferences/com.apple.restrictionspassword.plist"))).Replace("-", "");
            }

            string filePath = Path.Combine(backupFolder, hashFilename);
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(backupFolder, hashFilename.Substring(0, 2), hashFilename);
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException();
                }
            }
            PList plist = new PList(new Uri(filePath));

            string key = (plist["RestrictionsPasswordKey"] as string).Trim();
            int keyLength = Convert.FromBase64String(key).Length;
            byte[] salt = Convert.FromBase64String((plist["RestrictionsPasswordSalt"] as string).Trim());
            string foundKey = String.Empty;
            CancellationTokenSource cancelSource = new CancellationTokenSource();
            try
            {
                Parallel.For(0, 10000, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancelSource.Token }, (n) =>
                {
                    string testCode = n.ToString("D4");
                    string b64Key = HashPassword(testCode, salt, keyLength);
                    if (b64Key.Equals(key))
                    {
                        foundKey = testCode;
                        cancelSource.Cancel();
                    }
                });
            }
            catch (OperationCanceledException)
            {
            }
            return foundKey;
        }

        private static string HashPassword(string password, byte[] salt, int outputByteCount)
        {
            return Convert.ToBase64String(GetPbkdf2Bytes(password, salt, 1000, outputByteCount));
        }

        private static byte[] GetPbkdf2Bytes(string password, byte[] salt, int iterations, int outputByteCount)
        {
            PBKDF2 pbkdf2 = new PBKDF2(password, salt)
            {
                IterationCount = iterations
            };
            return pbkdf2.GetBytes(outputByteCount);
        }
    }
}
