using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using System.Drawing;

namespace Redie;

[MinimumApiVersion(80)]
public class Redie : BasePlugin
{
    public override string ModuleName => "redie";
    public override string ModuleVersion => "1.1.1";
    public override string ModuleAuthor => "exkludera";
    public override string ModuleDescription => "";

    HashSet<ulong> RedieCheck = new HashSet<ulong>(); //tried using this for weapon blocking but weaponservices cant check steamid :skull:

    #region load
    public override void Load(bool hotReload)
    {
        VirtualFunctions.CBaseTrigger_StartTouchFunc.Hook((DynamicHook hook) =>
        {
            CBaseTrigger player = hook.GetParam<CBaseTrigger>(1);
            if (player.Health == 420) {
                hook.SetReturn(false);
                return HookResult.Handled;
            }
            return HookResult.Continue;
        },HookMode.Pre);

        VirtualFunctions.CCSPlayer_WeaponServices_CanUseFunc.Hook((DynamicHook hook) =>
        {
            CCSPlayer_WeaponServices player = hook.GetParam<CCSPlayer_WeaponServices>(0);
            if (player.Pawn.Value.Health == 420)
            {
                hook.SetReturn(false);
                return HookResult.Handled;
            }
            return HookResult.Continue;
        },HookMode.Pre);
    }
    #endregion

    #region commands
    [ConsoleCommand("css_redie", "redie command")]
    [ConsoleCommand("css_ghost", "redie command")]
    public void OnCmdRedie(CCSPlayerController? player, CommandInfo command)
    {
        if (blockCheck(player))
            return;

        RedieCheck.Remove(player.SteamID);

        if (!RedieCheck.Contains(player.SteamID))
        {
            RedieCheck.Add(player.SteamID);
            player.Respawn();
            player.RemoveWeapons();
            player.PlayerPawn.Value.Health = 420; // sets hp to match block weapons and some triggers
            player.PlayerPawn.Value.Render = Color.FromArgb(0, 255, 255, 255); // hides player 
            player.PlayerPawn.Value.SetModel("characters\\models\\ctm_heavy\\ctm_heavy.vmdl"); // floating gloves fix
            player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING; //noblock fix
            player.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING; //noblock fix
            AddTimer(1.0f, () => {
                player.PlayerPawn.Value.Render = Color.FromArgb(0, 255, 255, 255);
                player.PlayerPawn.Value.SetModel("characters\\models\\ctm_heavy\\ctm_heavy.vmdl");
                player.PlayerPawn.Value.LifeState = (byte)LifeState_t.LIFE_DYING; //timer on this to fix black screen
            });
        }
    }

    [ConsoleCommand("css_unredie", "unredie command")]
    [ConsoleCommand("css_unghost", "unredie command")]
    public void OnCmdUnRedie(CCSPlayerController? player, CommandInfo command)
    {
        if (blockCheck(player))
            return;

        if (RedieCheck.Contains(player.SteamID))
        {
            RedieCheck.Remove(player.SteamID);
            player.PlayerPawn.Value.LifeState = (byte)LifeState_t.LIFE_ALIVE;
            player.CommitSuicide(false, true);
        }
    }
    #endregion

    #region events
    [GameEventHandler]
    public HookResult RoundStart(EventRoundStart @event, GameEventInfo info)
    {
        RedieCheck.Clear();
        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult PlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        @event.Userid.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255); // unhides player
        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult PlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        if (RedieCheck.Contains(@event.Userid.SteamID))
            RedieCheck.Remove(@event.Userid.SteamID);
        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult PlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (RedieCheck.Contains(@event.Userid.SteamID))
            RedieCheck.Remove(@event.Userid.SteamID);
        return HookResult.Continue;
    }
    #endregion

    private bool blockCheck(CCSPlayerController? player)
    {
        if (player == null || player.PawnIsAlive || player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
            return true;
        return false;
    }

}
