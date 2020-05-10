namespace EmailFanout.Logic
{
    public interface IEmailTarget
    {
        string Type { get; }
    }
}
