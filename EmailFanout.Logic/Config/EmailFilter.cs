namespace EmailFanout.Logic.Config
{
    public class EmailFilter
    {
        public bool Enabled { get; set; } = true;

        public string Type { get; set; } = "";

        public string[] OneOf { get; set; } = new string[0];

        public string[] AllOf { get; set; } = new string[0];
    }
}
