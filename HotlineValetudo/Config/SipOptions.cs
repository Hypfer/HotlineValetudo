namespace HotlineValetudo.Config;

public class SipOptions
{
    public string Mode { get; set; } = "server";
    public ServerModeOptions ServerMode { get; set; } = new();
    public ClientModeOptions ClientMode { get; set; } = new();
}

public class ServerModeOptions
{
    public int ListenPort { get; set; } = 5060;
}

public class ClientModeOptions
{
    public string LocalSipUri { get; set; } = "";
    public string RegistrarUri { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}