using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using System.Drawing;

namespace Redie;

public class Redie : BasePlugin
{
    public override string ModuleName => "redie";
    public override string ModuleVersion => "1.1.5";
    public override string ModuleAuthor => "exkludera";

    HashSet<ulong?> RediePlayers = new HashSet<ulong?>();

    public override void Load(bool hotReload)
    {
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

    #region commands
    [ConsoleCommand("css_redie", "redie command")]
    [ConsoleCommand("css_ghost", "redie command")]
    public void OnCmdRedie(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (blockCheck(player))
            return;

        var playerValue = player!.PlayerPawn.Value!;

        if (!RediePlayers.Contains(player!.SteamID))
        {
            RediePlayers.Add(player.SteamID);
            player.Respawn();
            player.RemoveWeapons();
            playerValue.HideHUD = 1;
            //playerValue.HideTargetID = true; no longer working in v215
            playerValue.Health = 420; // sets hp to match block weapons and some triggers
            playerValue.Render = Color.FromArgb(0, 255, 255, 255); // hides player
            playerValue.SetModel("characters\\models\\ctm_heavy\\ctm_heavy.vmdl"); // floating gloves fix
            playerValue.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING; //noblock fix
            playerValue.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING; //noblock fix
 
            //fix for custom player models
            AddTimer(0f, () => {
                playerValue.Render = Color.FromArgb(0, 255, 255, 255);
                playerValue.SetModel("characters\\models\\ctm_heavy\\ctm_heavy.vmdl");
                AddTimer(0f, () => {
                    playerValue.LifeState = (byte)LifeState_t.LIFE_DYING;
                });
            });
        }
    }

    [ConsoleCommand("css_unredie", "unredie command")]
    [ConsoleCommand("css_unghost", "unredie command")]
    public void OnCmdUnRedie(CCSPlayerController? player, CommandInfo command)
    {
        if (blockCheck(player))
            return;

        if (RediePlayers.Contains(player!.SteamID))
        {
            RediePlayers.Remove(player.SteamID);
            player.PlayerPawn.Value!.LifeState = (byte)LifeState_t.LIFE_ALIVE;
            player.CommitSuicide(false, true);
        }
    }
    #endregion

    #region events
    [GameEventHandler]
    public HookResult PlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid!;

        if (RediePlayers.Contains(player.SteamID))
            player.PlayerPawn.Value!.Render = Color.FromArgb(255, 255, 255, 255); // unhides player

        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult RoundStart(EventRoundStart @event, GameEventInfo info)
    {
        RediePlayers.Clear();

        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult TeamChange(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid!;

        if (RediePlayers.Contains(player.SteamID))
            RediePlayers.Remove(player.SteamID);

        return HookResult.Continue;
    }
    #endregion

    private bool blockCheck(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.PawnIsAlive || player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
            return true;

        return false;
    }

}
