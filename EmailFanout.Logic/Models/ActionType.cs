namespace EmailFanout.Logic.Models
{
    public enum ActionType
    {
        Unknown = 0,
        Forward,
        Archive,
        Webhook
    }
}
