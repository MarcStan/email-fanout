using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EmailFanout.Logic
{
    public static class Checksum
    {
        public static string Calculate(MemoryStream ms)
            => Calculate(ms.ToArray());
        public static string Calculate(string text)
            => Calculate(Encoding.UTF8.GetBytes(text));

        public static string Calculate(byte[] bytes)
        {
            using (var hashstring = SHA256.Create())
            {
                var sb = new StringBuilder();
                foreach (byte b in hashstring.ComputeHash(bytes))
                {
                    sb.AppendFormat("{0:x2}", b);
                }
                return sb.ToString();
            }
        }
    }
}
