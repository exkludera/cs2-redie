using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Drawing;

namespace Redie;

[MinimumApiVersion(80)]
public class Redie : BasePlugin
{
    public override string ModuleName => "redie";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "exkludera";
    public override string ModuleDescription => "";

    #region load
    public override void Load(bool hotReload)
    {
        VirtualFunctions.CBaseTrigger_StartTouchFunc.Hook((DynamicHook hook) =>
        {
            CBaseTrigger test = hook.GetParam<CBaseTrigger>(1);
            if (test.LifeState == (byte)LifeState_t.LIFE_DYING)
            {
                hook.SetReturn(false);
                return HookResult.Handled;
            }
            return HookResult.Continue;
        },HookMode.Pre);
        VirtualFunctions.CCSPlayer_WeaponServices_CanUseFunc.Hook((DynamicHook hook) =>
        {
            CCSPlayer_WeaponServices test = hook.GetParam<CCSPlayer_WeaponServices>(0);
            if (test.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_DYING)
            {
                hook.SetReturn(false);
                return HookResult.Handled;
            }
            return HookResult.Continue;
        },
        HookMode.Pre);
    }
    #endregion

    #region command
    [ConsoleCommand("css_redie", "redie command")]
    [ConsoleCommand("css_ghost", "redie command")]
    public void OnCmdRedie(CCSPlayerController? player, CommandInfo command)
    {
        if (player != null && !player.PawnIsAlive && player.Team != CsTeam.Spectator && player.Team != CsTeam.None)
        {
            player.Respawn();
            player.RemoveWeapons();
            player.SetListenOverride(player, ListenOverride.Mute);
            player.PlayerPawn.Value.Render = Color.FromArgb(0, 255, 255, 255);
            player.PlayerPawn.Value.SetModel("characters\\models\\ctm_heavy\\ctm_heavy.vmdl");
            AddTimer(1.0f, () => {
                player.RemoveWeapons();
                player.PlayerPawn.Value.LifeState = (byte)LifeState_t.LIFE_DYING;
            });
        }
    }
    #endregion

    #region events
    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        AddTimer(1.0f, () => {
            @event.Userid.Pawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
        });
        return HookResult.Continue;
    }
    #endregion

    #region lol
    /*
    player.PlayerPawn.Value.Collision.CollisionGroup = 1;
    player.PlayerPawn.Value.Collision.SolidType = 0;
    player.PlayerPawn.Value.Collision.SolidFlags = 4;
    player.PlayerPawn.Value.MyCollisionGroup = 1;
    player.PlayerPawn.Value.SetModel("characters\\models\\ctm_heavy\\ctm_heavy.vmdl");
    */
    #endregion
}