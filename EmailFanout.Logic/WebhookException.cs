using System;
using System.Runtime.Serialization;

namespace EmailFanout.Logic
{
    [Serializable]
    internal class WebhookException : Exception
    {
        public WebhookException()
        {
        }

        public WebhookException(string message) : base(message)
        {
        }

        public WebhookException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WebhookException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
