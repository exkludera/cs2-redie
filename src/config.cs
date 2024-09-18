using CounterStrikeSharp.API.Core;

public class Config : BasePluginConfig
{
    public string Prefix { get; set; } = "{purple}[Redie]{grey}";
    public string Commands { get; set; } = "css_redie,css_ghost";
    public bool SlayOnDisrupting { get; set; } = true;
    public bool Messages { get; set; } = true;
    public string Message_Redie { get; set; } = "You are now a ghost";
    public string Message_UnRedie { get; set; } = "You are no longer a ghost";
    public string Message_UnRedieDisrupting { get; set; } = "You are no longer a ghost, due to disrupting gameplay";
}