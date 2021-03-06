using EmailFanout.Logic.Models;
using Newtonsoft.Json.Linq;

namespace EmailFanout.Logic.Config
{
    public class EmailAction
    {
        public bool Enabled { get; set; } = true;

        public string Id { get; set; }

        public ActionType Type { get; set; }

        public JObject Properties { get; set; }
    }
}
