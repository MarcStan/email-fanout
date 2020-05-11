using System;
using System.IO;

namespace EmailFanout.Logic.Models
{
    public class EmailRequest
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
        /// The date of the email
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Checksum of the email
        /// </summary>
        public string Checksum { get; set; }
    }
}
