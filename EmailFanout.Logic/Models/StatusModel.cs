using EmailFanout.Logic.Config;
using Microsoft.Azure.Cosmos.Table;
using System;

namespace EmailFanout.Logic.Models
{
    public class StatusModel : TableEntity
    {
        public StatusModel()
        {
        }

        public StatusModel(EmailRequest email, EmailAction action, EmailFanoutStatus status)
        {
            Status = status.ToString();
            ActionId = action.Id;
            ReceivedAt = email.Timestamp;

            PartitionKey = GetPartitionKey(email);
            RowKey = GetRowKey(email, action);
        }

        public static string GetPartitionKey(EmailRequest email)
            => email.Checksum;

        public static string GetRowKey(EmailRequest email, EmailAction action)
            // user provided content. will contain non-allowed chars
            => Checksum.Calculate($"{email.Email.From.Email}_{email.Email.Date}_{action.Id}");

        public string Status { get; set; }

        public EmailFanoutStatus GetStatus()
            => string.IsNullOrEmpty(Status) ? EmailFanoutStatus.Unknown : (EmailFanoutStatus)Enum.Parse(typeof(EmailFanoutStatus), Status, true);

        public string ActionId { get; set; }

        public DateTimeOffset ReceivedAt { get; set; }
    }
}
