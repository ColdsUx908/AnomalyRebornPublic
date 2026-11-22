using static Anomalies.GameContents.LegendaryNight.StarfallKnockerGeometry;

namespace Anomalies.GameContents.LegendaryNight;

/// <summary>
/// 所属客户端控制输入与爆炸生成；其他端按 ai 中的阶段和时钟预测姿态。
/// </summary>
public sealed partial class StarfallKnockerHoldout : AnomalyModProjectile
{
    private enum Phase : byte { Charging, Swinging, Recovering }

    // ai[0]：阶段；ai[1]：阶段内游戏帧；ai[2]：挥击方向（1 下砸，-1 上挑）；velocity：锁定瞄准方向。
    private Phase CurrentPhase { get => (Phase)Projectile.ai[0]; set => Projectile.ai[0] = (float)value; }
    private ref float Timer => ref Projectile.ai[1];
    private bool IsOwner => Projectile.owner == Main.myPlayer;
    private Vector2 _ChargeVector;
    private readonly List<Vector2> _HeadTrail = [];
    private const int TrailSamplesPerFrame = 64;
    private const int TrailCapacity = TrailSamplesPerFrame * 7;
    private int _Target = -1;
    private float _StopTime;
    private float _FlameTime;
    private Vector2 _PreviousHeadCenter;
    private Vector2 _HeadMotion;
    private bool _HasPreviousHeadCenter;

    private int Facing => Projectile.velocity.X >= 0f ? 1 : -1;
    private static int AttackDuration => SwingDuration;
    private Vector2 WeaponVector => CurrentPhase == Phase.Charging ? _ChargeVector
        : SwingVector(Facing, Owner.gravDir, Projectile.ai[2],
            CurrentPhase == Phase.Swinging ? Timer : _StopTime);
    private Vector2 Grip => Owner.RotatedRelativePoint(Owner.MountedCenter, true)
        + WeaponVector.SafeNormalize(Projectile.velocity) * 6f;
    private Matrix WeaponTransform => TextureTransform(Grip, WeaponVector,
        Projectile.scale, Facing * Owner.gravDir * (CurrentPhase == Phase.Charging ? 1f
            : HeadHandedness(Projectile.ai[2], CurrentPhase == Phase.Swinging ? Timer : _StopTime)));
    private Vector2 CurrentHeadCenter => Vector2.Transform(TextureHead, WeaponTransform);

    private Vector2 SwingHeadCenter(float time)
    {
        Vector2 vector = SwingVector(Facing, Owner.gravDir, Projectile.ai[2], time);
        Vector2 grip = Owner.RotatedRelativePoint(Owner.MountedCenter, true)
            + vector.SafeNormalize(Projectile.velocity) * 6f;
        return HeadCenter(grip, vector, Projectile.scale,
            Facing * Owner.gravDir * HeadHandedness(Projectile.ai[2], time));
    }
    private bool _ReadySoundPlayed;
    private bool _SwingSoundPlayed;
    // 仅所有者的发送节流缓存，不是战斗状态；满蓄力时也持续同步转向。
    private Vector2 _LastSentAim;
    private int _InputSyncTimer;

    public override string Texture => "Anomalies/GameContents/LegendaryNight/StarfallKnocker";
    public override string LocalizationCategory => "GameContents";
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => IsOwner && CurrentPhase == Phase.Swinging && _Target >= 0 ? null : false;
    public override bool CanHitPvp(Player target) => false;

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 500;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 76;
        // 仅由玩家持有物图层绘制，避免世界弹幕层再次盖住前手。
        Projectile.hide = true;
        DrawHeldProjInFrontOfHeldItemAndArms = false;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.netImportant = true;
        Projectile.timeLeft = 120;
    }

    public override void AI()
    {
        bool becameReady = false;
        Player player = Owner;
        if (!player.active || player.dead || player.CCed || player.noItems
            || player.HeldItem.type != ModContent.ItemType<StarfallKnocker>())
        {
            Projectile.Kill();
            return;
        }
        Projectile.timeLeft = 120;
        if (CurrentPhase == Phase.Charging)
        {
            if (IsOwner)
            {
                Vector2 offset = Main.MouseWorld - player.MountedCenter;
                if (player.gravDir == -1f)
                    offset.Y = Main.screenPosition.Y + Main.screenHeight - Main.mouseY - player.MountedCenter.Y;
                Vector2 aim = offset.SafeNormalize(Vector2.UnitX * player.direction);
                if (Vector2.DistanceSquared(aim, _LastSentAim) > 0.0025f || ++_InputSyncTimer >= 15)
                {
                    Projectile.netUpdate = true;
                    _LastSentAim = aim;
                    _InputSyncTimer = 0;
                }
                Projectile.velocity = aim;
            }
            _ChargeVector = ChargeStep(_ChargeVector, ChargeTarget(Facing, player.gravDir));
            if (IsOwner)
            {
                if (!player.channel)
                {
                    if (Timer < ChargeDuration)
                    {
                        Projectile.Kill();
                        return;
                    }
                    CurrentPhase = Phase.Swinging;
                    Projectile.ai[2] = SelectSwingSide(Projectile.velocity, player.gravDir);
                    Timer = 0f;
                    Projectile.netUpdate = true;
                }
            }
            if (CurrentPhase == Phase.Charging)
            {
                Timer = Math.Min(ChargeDuration, Timer + 1f);
                // 满蓄力后继续保持；周期快照支持中途加入与输入停留。
                if (IsOwner && (Timer == ChargeDuration && !_ReadySoundPlayed || Timer % 30f == 0f && Timer < ChargeDuration))
                    Projectile.netUpdate = true;
                if (Timer >= ChargeDuration && !_ReadySoundPlayed)
                {
                    _ReadySoundPlayed = true;
                    becameReady = true;
                }
            }
        }
        else if (CurrentPhase == Phase.Swinging)
        {
            Timer++;
            if (Timer > AttackDuration)
            {
                _StopTime = AttackDuration;
                CurrentPhase = Phase.Recovering;
                Timer = 0f;
                Projectile.Center = CurrentHeadCenter;
                if (IsOwner)
                {
                    Projectile.netUpdate = true;
                    SpawnBurst(Projectile.Center);
                }
            }
        }
        else if (++Timer >= ImpactPause + RecoveryDuration)
        {
            Projectile.Kill();
            return;
        }

        if (IsOwner && CurrentPhase == Phase.Swinging)
        {
            // 按扫掠时间顺序检查，避免按 NPC 索引选择更晚接触的目标。
            for (int sample = 0; sample <= 128 && _Target < 0; sample++)
            {
                float time = Math.Max(0f, Timer - 1f) + sample / 128f;
                Vector2 center = SwingHeadCenter(time);
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy(Projectile) || !new Circle(center, HeadRadius * Projectile.scale).Collides(npc.Hitbox))
                        continue;
                    _Target = npc.whoAmI;
                    Projectile.Center = center;
                    _StopTime = time;
                    Projectile.Damage();
                    if (CurrentPhase == Phase.Recovering)
                        break;
                    _Target = -1;
                }
            }
        }
        Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;
        player.ChangeDir(Projectile.direction);
        player.heldProj = Projectile.whoAmI;
        player.itemTime = player.itemAnimation = 2;
        Projectile.Center = CurrentHeadCenter;
        Vector2 arm = WeaponVector.SafeNormalize(Projectile.velocity);
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, arm.ToRotation() - MathHelper.PiOver2);
        if (Main.dedServ)
            return;
        _FlameTime++;
        if (_HasPreviousHeadCenter)
            _HeadMotion = Vector2.Lerp(_HeadMotion, Projectile.Center - _PreviousHeadCenter, 0.65f);
        _PreviousHeadCenter = Projectile.Center;
        _HasPreviousHeadCenter = true;
        if (CurrentPhase == Phase.Swinging || CurrentPhase == Phase.Recovering && Timer == 0f)
        {
            float end = CurrentPhase == Phase.Swinging ? Timer : _StopTime;
            for (int i = 1; i <= TrailSamplesPerFrame; i++)
            {
                float sample = MathHelper.Lerp(Math.Max(0f, end - 1f), end, i / (float)TrailSamplesPerFrame);
                _HeadTrail.Add(SwingHeadCenter(sample));
            }
            if (_HeadTrail.Count > TrailCapacity)
                _HeadTrail.RemoveRange(0, _HeadTrail.Count - TrailCapacity);
        }
        if (CurrentPhase == Phase.Swinging && !_SwingSoundPlayed)
        {
            _SwingSoundPlayed = true;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.65f }, player.Center);
        }
        float charge = CurrentPhase == Phase.Charging ? Timer / ChargeDuration : 1f;
        Lighting.AddLight(Projectile.Center, new Vector3(0.65f, 0.42f, 0.08f) * charge);
        if (CurrentPhase == Phase.Charging)
        {
            Vector2 center = Projectile.Center;
            // MEAC 蓄力：每游戏帧两次汇聚，半径随蓄力收缩，初速度向内为 4。
            float radius = MathHelper.Lerp(150f, 12f, charge);
            for (int i = 0; i < 2; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                Dust.NewDustPerfectAction(center + direction * radius + Main.rand.NextVector2Circular(5f, 5f), DustID.GoldFlame, dust =>
                {
                    dust.noGravity = true;
                    dust.velocity = -direction * 4f;
                    dust.scale = 0.8f;
                });
            }
            if (becameReady)
            {
                // 与 MEAC 波涌之刃完成蓄力时相同的 Item4、0.4 音量和默认音高。
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.4f }, center);
                const int ReadyParticleCount = 48;
                for (int i = 0; i < ReadyParticleCount; i++)
                {
                    Vector2 direction = (MathHelper.TwoPi * i / ReadyParticleCount).ToRotationVector2();
                    Dust.NewDustPerfectAction(center + direction * 8f, DustID.GoldFlame, dust =>
                    {
                        dust.noGravity = true;
                        dust.velocity = direction * Main.rand.NextFloat(3f, 7f);
                        dust.scale = Main.rand.NextFloat(1.1f, 1.6f);
                    });
                }
            }
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        new Circle(Projectile.Center, HeadRadius * Projectile.scale).Collides(targetHitbox);

    protected override void SendAnomalyExtraAI(BinaryWriter writer)
    {
        writer.Write(_ChargeVector.X);
        writer.Write(_ChargeVector.Y);
        writer.Write(_StopTime);
    }

    protected override void ReceiveAnomalyExtraAI(BinaryReader reader)
    {
        _ChargeVector = new(reader.ReadSingle(), reader.ReadSingle());
        _StopTime = reader.ReadSingle();
    }

    public override bool? CanHitNPC(NPC target) => target.whoAmI == _Target ? null : false;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!IsOwner || CurrentPhase != Phase.Swinging)
            return;
        CurrentPhase = Phase.Recovering;
        Timer = 0f;
        Projectile.netUpdate = true;
        // Center 是本次扫掠采样的黄色锤面中心，不使用装饰星星或敌人包围盒中心。
        SpawnBurst(Projectile.Center);
    }

    // 命中与挥空共用伤害参数；只从所有者的挥击结束路径调用。
    private void SpawnBurst(Vector2 center)
    {
        Projectile.NewProjectileAction<StarfallKnockerBurst>(Projectile.GetSource_FromThis(), center,
            Vector2.Zero, Projectile.damage, Projectile.knockBack, Projectile.owner, burst =>
            {
                burst.CritChance = Projectile.CritChance;
                burst.ArmorPenetration = Projectile.ArmorPenetration;
            });
    }
}
