public interface IResumeRepository
{
    void Save(string type, string content);
    string? Get(string type);
}
