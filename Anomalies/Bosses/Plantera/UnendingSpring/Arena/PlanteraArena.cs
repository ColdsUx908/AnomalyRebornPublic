namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed class PlanteraArena : ModProjectile
{
    internal const float Radius = 1280f;
    private const int OpeningTicks = 90;
    private Vector2 _PanStart;
    private int _PanFrame;
    private int _PanDuration;
    private int _OutsideTicks;
    private ref float Age => ref Projectile.ai[1];
    private float Progress => Math.Min(Age / OpeningTicks, 1f);
    internal float VisibleRadius => Radius * (1f - 0.8f * MathF.Pow(1f - Progress, 3f));
    internal float Opacity => MathHelper.SmoothStep(0f, 1f, Progress);
    internal float FlowTime => Age / 60f;
    public override string Texture => "Terraria/Images/MagicPixel";

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.hide = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 2;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(_PanStart);
        writer.Write((byte)_PanFrame);
        writer.Write((byte)_PanDuration);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        _PanStart = reader.ReadVector2();
        _PanFrame = reader.ReadByte();
        _PanDuration = reader.ReadByte();
    }

    public override void AI()
    {
        if ((uint)(int)Projectile.ai[0] >= Main.maxNPCs)
        {
            Projectile.Kill();
            return;
        }
        NPC boss = Main.npc[(int)Projectile.ai[0]];
        if (!boss.active || boss.type != NPCID.Plantera || !PlanteraUnending.GetInstance(boss).ShouldProcess)
        {
            Projectile.Kill();
            return;
        }

        Projectile.timeLeft = 2;
        Age++;
        int duration = PlanteraUnending.GetInstance(boss).ArenaRecenterTicks;
        if (duration == 0)
            _PanFrame = 0;
        else
        {
            if (_PanFrame == 0)
            {
                _PanStart = Projectile.Center;
                _PanDuration = duration;
            }
            _PanDuration = Math.Min(_PanDuration, duration);
            _PanFrame = Math.Min(_PanFrame + 1, _PanDuration);
            Projectile.Center = Vector2.SmoothStep(_PanStart, boss.Center, _PanFrame / (float)_PanDuration);
        }

        if (Main.dedServ || Age < OpeningTicks || boss.target != Main.myPlayer || !boss.HasValidTarget)
        {
            _OutsideTicks = 0;
            return;
        }
        Player player = Main.LocalPlayer;
        Vector2 offset = Projectile.Center - player.Center;
        float distance = offset.Length();
        if (distance <= Radius || distance >= Radius * 4f + 4800f || player.ghost)
        {
            _OutsideTicks = 0;
            return;
        }
        float step = Math.Min(25f + 0.05f * ++_OutsideTicks, 60f);
        player.position += offset * (Math.Min(distance - Radius, step) / distance);
    }
}
