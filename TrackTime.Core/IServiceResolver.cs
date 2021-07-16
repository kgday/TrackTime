namespace TrackTime
{
    public interface IServiceResolver
    {
        T GetService<T>(string? contract = null);
        void RegisterDependencies();
    }
}