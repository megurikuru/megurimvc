namespace Meguri.Services;

public class SMTPServerConf {
    public string HostName { get; set; }
    public int Port { get; set; }
    public string LocalDomain { get; set; }
    public string Account { get; set; }
    public string Password { get; set; }
    public string FromName { get; set; }
}
