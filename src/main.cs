using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using System.Runtime.InteropServices;
using System.Drawing;

public class Plugin : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "Redie";
    public override string ModuleVersion => "1.0.1";
    public override string ModuleAuthor => "exkludera";

    HashSet<int> RediePlayers = new HashSet<int>();

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerSpawn>(EventPlayerSpawn);
        RegisterEventHandler<EventRoundStart>(EventRoundStart);
        RegisterEventHandler<EventPlayerTeam>(EventPlayerTeam);

        HookEntityOutput("trigger_teleport", "OnStartTouch", Disrupting, HookMode.Pre);
        HookEntityOutput("trigger_hurt", "OnHurtPlayer", Disrupting, HookMode.Pre);
        HookEntityOutput("func_door", "OnBlockedClosing", Disrupting, HookMode.Pre);
        HookEntityOutput("func_door", "OnBlockedOpening", Disrupting, HookMode.Pre);

        foreach (var cmd in Config.Commands.Split(','))
            AddCommand(cmd, "Redie Command", (player, command) => Command_Redie(player));

        VirtualFunctions.CCSPlayer_WeaponServices_CanUseFunc.Hook(OnCanUse, HookMode.Pre);
    }

    public override void Unload(bool hotReload)
    {
        DeregisterEventHandler<EventPlayerSpawn>(EventPlayerSpawn);
        DeregisterEventHandler<EventRoundStart>(EventRoundStart);
        DeregisterEventHandler<EventPlayerTeam>(EventPlayerTeam);

        UnhookEntityOutput("trigger_teleport", "OnStartTouch", Disrupting, HookMode.Pre);
        UnhookEntityOutput("trigger_hurt", "OnHurtPlayer", Disrupting, HookMode.Pre);
        UnhookEntityOutput("func_door", "OnBlockedClosing", Disrupting, HookMode.Pre);
        UnhookEntityOutput("func_door", "OnBlockedOpening", Disrupting, HookMode.Pre);

        foreach (var cmd in Config.Commands.Split(','))
            RemoveCommand(cmd, (player, command) => Command_Redie(player));

        VirtualFunctions.CCSPlayer_WeaponServices_CanUseFunc.Unhook(OnCanUse, HookMode.Pre);
    }

    public Config Config { get; set; } = new Config();
    public void OnConfigParsed(Config config)
    {
        Config = config;
        Config.Prefix = StringExtensions.ReplaceColorTags(config.Prefix);
    }

    public void Command_Redie(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.PawnIsAlive || player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
            return;

        var playerPawn = player.PlayerPawn.Value!;

        if (!RediePlayers.Contains(player.Slot))
        {
            RediePlayers.Add(player.Slot);

            player.Respawn();
            player.RemoveWeapons();

            HidePlayer(player, true);

            playerPawn.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING; //noblock fix
            playerPawn.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING; //noblock fix
 
            //fix for custom player models
            AddTimer(0.0f, () => {

                player.RemoveWeapons();
                HidePlayer(player, true);

                AddTimer(0.0f, () => { playerPawn.LifeState = (byte)LifeState_t.LIFE_DYING; });
            });

            if (Config.Messages)
                player.PrintToChat($"{Config.Prefix} {Config.Message_Redie}");
        }

        else if (RediePlayers.Contains(player.Slot))
        {
            RediePlayers.Remove(player.Slot);
            player.PlayerPawn.Value!.LifeState = (byte)LifeState_t.LIFE_ALIVE;
            player.CommitSuicide(false, true);

            if (Config.Messages)
                player.PrintToChat($"{Config.Prefix} {Config.Message_UnRedie}");
        }
    }


    private static readonly MemoryFunctionWithReturn<nint, string, int, int> SetBodygroupFunc = new(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "55 48 89 E5 41 56 49 89 F6 41 55 41 89 D5 41 54 49 89 FC 48 83 EC 08" : "40 53 41 56 41 57 48 81 EC 90 00 00 00 0F 29 74 24 70");
    private static readonly Func<nint, string, int, int> SetBodygroup = SetBodygroupFunc.Invoke;
    public void HidePlayer(CCSPlayerController player, bool status)
    {
        var pawn = player.PlayerPawn.Value;

        pawn!.Render = status ? Color.FromArgb(0, 0, 0, 0) : Color.FromArgb(255, 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

        var gloves = pawn.EconGloves;
        SetBodygroup(pawn.Handle, "default_gloves", status ? 0 : 1);
    }

    HookResult EventPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (RediePlayers.Contains(player.Slot))
            HidePlayer(player, false);

        return HookResult.Continue;
    }

    HookResult EventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        RediePlayers.Clear();

        return HookResult.Continue;
    }

    HookResult EventPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (RediePlayers.Contains(player.Slot))
        {
            HidePlayer(player, false);
            RediePlayers.Remove(player.Slot);
        }

        return HookResult.Continue;
    }

    HookResult OnCanUse(DynamicHook hook)
    {
        var weaponservices = hook.GetParam<CCSPlayer_WeaponServices>(0);

        var player = new CCSPlayerController(weaponservices.Pawn.Value.Controller.Value!.Handle);

        if (RediePlayers.Contains(player.Slot))
        {
            hook.SetReturn(false);
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    HookResult Disrupting(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        if (Config.SlayOnDisrupting)
        {
            if (activator == null || caller == null)
                return HookResult.Continue;

            if (activator.DesignerName != "player")
                return HookResult.Continue;

            var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value!.Handle);

            if (player == null)
                return HookResult.Continue;

            if (RediePlayers.Contains(player.Slot))
            {
                RediePlayers.Remove(player.Slot);
                player.PlayerPawn.Value!.LifeState = (byte)LifeState_t.LIFE_ALIVE;
                player.CommitSuicide(false, true);

                if (Config.Messages)
                    player.PrintToChat($"{Config.Prefix} {Config.Message_UnRedieDisrupting}");

                return HookResult.Handled;
            }
        }

        return HookResult.Continue;
    }
}