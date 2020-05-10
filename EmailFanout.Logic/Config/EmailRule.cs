namespace EmailFanout.Logic.Config
{
    public class EmailRule
    {
        public EmailFilter[] Filters { get; set; } = new EmailFilter[0];

        public EmailAction[] Actions { get; set; } = new EmailAction[0];
    }
}
