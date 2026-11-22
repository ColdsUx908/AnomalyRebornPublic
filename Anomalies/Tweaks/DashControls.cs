using Terraria.GameInput;

namespace Anomalies.Tweaks;

public sealed class DashControls : ModSystem
{
    public override void Load()
    {
        if (!Main.dedServ)
            On_Player.DoCommonDashHandle += HandleDash;
    }

    // TODO: tModLoader 升级到 Terraria 1.4.5 后，移除独立冲刺热键及其本地化，改为优化原版单键冲刺的方向判定。
    // 若原版已支持禁用双击冲刺，移除本模组的对应配置与拦截逻辑；否则保留。
    private static void HandleDash(On_Player.orig_DoCommonDashHandle orig, Player player, out int direction,
        out bool dashing, Player.DashStartAction dashStartAction)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            if (AnomalyKeybinds.SinglePressDash.JustPressed && !player.CCed)
            {
                AnomalyPlayer input = player.GetModPlayer<AnomalyPlayer>();
                direction = 0;
                if (player.controlLeft && player.controlRight)
                    direction = input.DashInput_LastDirection;
                else if (player.controlLeft)
                    direction = -1;
                else if (player.controlRight)
                    direction = 1;

                if (direction != 0)
                {
                    player.direction = direction;
                    player.dashTime = 0;
                    player.timeSinceLastDashStarted = 0;
                    dashing = true;
                    dashStartAction?.Invoke(direction);
                    return;
                }
            }

            if (AnomalyClientConfig.Instance.DisableDoubleTapDash)
                player.dashTime = 0;
        }

        orig(player, out direction, out dashing, dashStartAction);
    }
}

public sealed class DashInputBehavior : AnomalyPlayerBehavior
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        AnomalyPlayer input = AnomalyPlayer;
        // 同一帧检测到两个按下边沿时，沿用 Fargo 的右键优先规则。
        if (Player.controlRight && !input.DashInput_RightWasDown)
            input.DashInput_LastDirection = 1;
        else if (Player.controlLeft && !input.DashInput_LeftWasDown)
            input.DashInput_LastDirection = -1;

        input.DashInput_LeftWasDown = Player.controlLeft;
        input.DashInput_RightWasDown = Player.controlRight;
    }
}
