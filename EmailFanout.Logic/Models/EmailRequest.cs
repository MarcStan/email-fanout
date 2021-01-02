using System;
using System.IO;

namespace EmailFanout.Logic.Models
{
    public class EmailRequest : IDisposable
    {
        /// <summary>
        /// The original body received.
        /// </summary>
        public MemoryStream Body { get; set; }

        /// <summary>
        /// The parsed email.
        /// </summary>
        public Email Email { get; set; }

        /// <summary>
        /// The date of the email.
        /// May not be parsable by .Net parser depending on date format.
        /// </summary>
        public string Timestamp { get; set; }

        /// <summary>
        /// Checksum of the email
        /// </summary>
        public string Checksum { get; set; }

        public static EmailRequest Parse(Stream stream, ISendgridEmailParser sendgridEmailParser)
        {
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            var email = sendgridEmailParser.Parse(ms);
            ms.Position = 0;
            var checksum = Logic.Checksum.Calculate(ms);
            ms.Position = 0;
            var request = new EmailRequest
            {
                Body = ms,
                Email = email,
                Checksum = checksum,
                Timestamp = email.Date
            };
            return request;
        }

        public void Dispose()
        {
            Body.Dispose();
        }
    }
}
