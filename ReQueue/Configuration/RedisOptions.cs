namespace ReQueue;

public class RedisOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public int DB { get; set; } = 0;
    public string Password { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = false;
}