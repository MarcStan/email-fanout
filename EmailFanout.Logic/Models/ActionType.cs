namespace EmailFanout.Logic.Models
{
    public enum ActionType
    {
        Unknown = 0,

        /// <summary>
        /// Forwards the request as is to another webhook.
        /// </summary>
        Forward,
        /// <summary>
        /// Archives the email in storage in json format.
        /// </summary>
        Archive,

        /// <summary>
        /// Sends the message to a webhook in a simpler format (sender, subject, body & attachments).
        /// </summary>
        Webhook,

        /// <summary>
        /// Sends an email to a target (impersonating the sender to allow easy response).
        /// </summary>
        Email
    }
}
