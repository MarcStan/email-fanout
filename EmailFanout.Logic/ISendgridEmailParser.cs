using EmailFanout.Logic.Models;
using System.IO;

namespace EmailFanout.Logic
{
    public interface ISendgridEmailParser
    {
        Email Parse(MemoryStream body);
    }
}
