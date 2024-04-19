using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using System.Drawing;

namespace RediePlugin;

[MinimumApiVersion(80)]
public class RediePlugin : BasePlugin
{
    public override string ModuleName => "redie";
    public override string ModuleVersion => "1.1.3";
    public override string ModuleAuthor => "exkludera";
    public override string ModuleDescription => "";

    HashSet<int?> Redie = new HashSet<int?>();

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
    public void OnCmdRedie(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (blockCheck(player))
            return;

        if (!Redie.Contains(player.UserId))
        {
            Redie.Add(player.UserId);
            player.Respawn();
            player.RemoveWeapons();
            player.PlayerPawn.Value.HideHUD = 1;
            player.PlayerPawn.Value.HideTargetID = true;
            player.PlayerPawn.Value.Health = 420; // sets hp to match block weapons and some triggers
            player.PlayerPawn.Value.Render = Color.FromArgb(0, 255, 255, 255); // hides player
            player.PlayerPawn.Value.SetModel("characters\\models\\ctm_heavy\\ctm_heavy.vmdl"); // floating gloves fix
            player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING; //noblock fix
            player.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING; //noblock fix
 
            //fix for custom player models
            AddTimer(1.0f, () => {
                player.PlayerPawn.Value.Render = Color.FromArgb(0, 255, 255, 255);
                player.PlayerPawn.Value.SetModel("characters\\models\\ctm_heavy\\ctm_heavy.vmdl");
                AddTimer(1.0f, () => {
                    player.PlayerPawn.Value.LifeState = (byte)LifeState_t.LIFE_DYING;
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

        if (Redie.Contains(player.UserId))
        {
            Redie.Remove(player.UserId);
            player.PlayerPawn.Value.LifeState = (byte)LifeState_t.LIFE_ALIVE;
            player.CommitSuicide(false, true);
        }
    }
    #endregion

    #region events
    [GameEventHandler]
    public HookResult PlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (Redie.Contains(@event.Userid.UserId))
            @event.Userid.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255); // unhides player
        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult RoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Redie.Clear();
        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult TeamChange(EventPlayerTeam @event, GameEventInfo info)
    {
        if (Redie.Contains(@event.Userid.UserId))
            Redie.Remove(@event.Userid.UserId);
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
