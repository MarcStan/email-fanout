namespace EmailFanout.Logic.Models
{
    public enum EmailFanoutStatus
    {
        Unknown = 0,
        /// <summary>
        /// Target has previously accepted the email.
        /// </summary>
        Completed,
        /// <summary>
        /// Target has not accepted the email on last try.
        /// </summary>
        DeferredOrFailed
    }
}
