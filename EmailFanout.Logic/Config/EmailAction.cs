using Newtonsoft.Json.Linq;

namespace EmailFanout.Logic.Config
{
    public class EmailAction
    {
        public string Type { get; set; } = "";

        public JObject Properties { get; set; }
    }
}
