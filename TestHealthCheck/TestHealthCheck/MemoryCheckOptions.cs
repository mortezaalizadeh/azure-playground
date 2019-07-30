namespace TestHealthCheck
{
    public class MemoryCheckOptions
    {
        public long Threshold { get; set; } = 1024L * 1024L * 1024L;
    }
}
