namespace EmailFanout.Logic.Config
{
    public class EmailRule
    {
        public bool Enabled { get; set; } = true;

        public EmailFilter[] Filters { get; set; } = new EmailFilter[0];

        public EmailAction[] Actions { get; set; } = new EmailAction[0];
    }
}
