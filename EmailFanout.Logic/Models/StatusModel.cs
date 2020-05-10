using EmailFanout.Logic.Config;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace EmailFanout.Logic.Models
{
    public class StatusModel : TableEntity
    {
        public StatusModel()
        {
        }

        public StatusModel(Email email, EmailAction action)
        {
            Status = EmailFanoutStatus.Unknown;
            PartitionKey = GetPartitionKey(email);
            RowKey = GetRowKey(email, action);
        }

        public string GetPartitionKey(Email email)
            => Hash($"{email.From.Email}_{email.Subject}");

        public string GetRowKey(Email email, EmailAction action)
            => Hash($"{email.Headers.First(k => k.Key == "Date").Value}_{action.Id}");

        public EmailFanoutStatus Status { get; set; }

        public string Id => PartitionKey + RowKey;

        private string Hash(string text)
        {
            using (var hashstring = new SHA256Managed())
            {
                var sb = new StringBuilder();
                foreach (byte b in hashstring.ComputeHash(Encoding.Unicode.GetBytes(text)))
                {
                    sb.AppendFormat("{0:x2}", b);
                }
                return sb.ToString();
            }
        }
    }
}
