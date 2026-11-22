namespace Anomalies.SpecialContents.Donor.SnowHareValkyrie;

public sealed partial class CarrotMissile
{
    private const int AttachedLifetime = 10 * 60 * 2;
    private Vector2 _TargetOffset;
    private float _RelativeRotation;
    private NPC _AttachedNpc;
    private static Asset<Texture2D> _Ring;

    // ai[2] 为 0 时飞行，-1 时固定，正数为黏附的 NPC 索引加一。
    private bool IsAttached => Projectile.ai[2] != 0f;

    public override void Load()
    {
        if (!Main.dedServ)
            _Ring = ModContent.Request<Texture2D>(Texture[..Texture.LastIndexOf('/')] + "/MarchHareMark");
    }

    public override void Unload() => _Ring = null;

    public override bool? CanDamage() => IsAttached ? false : null;

    private void Attach(NPC target, Vector2 center)
    {
        if (IsAttached)
            return;
        foreach (Projectile other in Main.ActiveProjectiles)
        {
            if (other.owner == Projectile.owner && other.ModProjectile is CarrotMissile { IsAttached: true })
            {
                CarrotBlast.Spawn(other);
                other.Kill();
            }
        }
        _AttachedNpc = target;
        _RelativeRotation = Projectile.rotation - (target?.rotation ?? 0f);
        _TargetOffset = target is null ? Vector2.Zero : (center - target.Center).RotatedBy(-target.rotation);
        Projectile.Center = center;
        Projectile.velocity = Vector2.Zero;
        Projectile.ai[1] = 0f;
        Projectile.ai[2] = target is null ? -1 : target.whoAmI + 1;
        Projectile.frame = 1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.extraUpdates = 1;
        Projectile.timeLeft = AttachedLifetime;
        Projectile.Opacity = 1f;
        Projectile.netUpdate = true;
    }

    protected override void SendAnomalyExtraAI(BinaryWriter writer)
    {
        base.SendAnomalyExtraAI(writer);
        writer.WriteVector2(_TargetOffset);
        writer.Write(_RelativeRotation);
    }

    protected override void ReceiveAnomalyExtraAI(BinaryReader reader)
    {
        base.ReceiveAnomalyExtraAI(reader);
        _TargetOffset = reader.ReadVector2();
        _RelativeRotation = reader.ReadSingle();
    }

    private void UpdateAttached()
    {
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        if (Projectile.ai[2] > 0f)
        {
            NPC npc = NPC.GetNPCFromIndex((int)Projectile.ai[2] - 1);
            if (npc.active && (!Projectile.IsOnOwnerClient || npc == _AttachedNpc))
            {
                Projectile.rotation = npc.rotation + _RelativeRotation;
                Projectile.Center = npc.Center + _TargetOffset.RotatedBy(npc.rotation);
            }
            else if (Projectile.IsOnOwnerClient)
            {
                CarrotBlast.Spawn(Projectile);
                Projectile.Kill();
                return;
            }
        }
        if (!Projectile.IsOnOwnerClient)
            return;
        int step = (int)Projectile.ai[1]++;
        for (int frame = MarchHare.SourceFrame(step); frame < MarchHare.SourceFrame(step + 1); frame++)
        {
            if (frame % 15 == 0)
                Emit(false);
            if (frame % 45 == 0)
                Emit(true);
        }

        void Emit(bool red)
        {
            float spread = red ? 160f : 800f;
            float height = red ? 1620f : 1440f;
            Vector2 center = _AttachedNpc?.Center ?? Projectile.Center;
            Vector2 spawn = center + new Vector2(Main.rand.NextFloat(-spread, spread), -height) * MarchHare.WorldScale;
            const float WorldMargin = 256f;
            spawn = Vector2.Clamp(spawn, new Vector2(WorldMargin),
                new Vector2(Main.maxTilesX * 16f - WorldMargin, Main.maxTilesY * 16f - WorldMargin));
            Projectile.NewProjectileAction<MarchHare>(Projectile.GetSource_FromThis(), spawn, Vector2.Zero,
                Projectile.damage, Projectile.knockBack, Projectile.owner, hare =>
                {
                    hare.ai[0] = red ? 1f : 0f;
                    hare.ai[1] = Main.rand.Next(1 << 23);
                    hare.ai[2] = center.Y + 2500f * MarchHare.WorldScale;
                });
        }
    }

    private void DrawRing()
    {
        Texture2D ring = _Ring.Value;
        float rotation = (AttachedLifetime - Projectile.timeLeft) * MathHelper.TwoPi / 720f;
        Main.EntitySpriteDraw(ring, Projectile.Center - Main.screenPosition, null, Color.White, rotation,
            ring.Size() * 0.5f, MarchHare.WorldScale, SpriteEffects.None);
    }
}
