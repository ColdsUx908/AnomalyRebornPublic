using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Anomalies.Bosses.Plantera.UnendingSpring;

public sealed class HookLightning : ModProjectile
{
    internal const int Delay = 9;
    private const float Speed = 144f;
    private const int FadeTicks = 8;
    private const float SpriteScale = 3f;
    private const int Frames = 4;
    private static Texture2D _BeamStrip;
    private int _Age;
    private float _Length;
    private int _Fade;
    public override string Texture => "Anomalies/Bosses/Plantera/UnendingSpring/Assets/HookLightning";
    public override void SetStaticDefaults() =>
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)PlanteraUnending.RayLength + 64;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = Delay + (int)MathF.Ceiling(PlanteraUnending.RayLength / Speed) + FadeTicks + 2;
    }
    public override bool ShouldUpdatePosition() => false;
    private Vector2 Direction => Projectile.ai[1].ToRotationVector2();
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((byte)_Age);
        writer.Write(_Length);
        writer.Write((byte)_Fade);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        _Age = reader.ReadByte();
        _Length = reader.ReadSingle();
        _Fade = reader.ReadByte();
    }
    public override void AI()
    {
        NPC boss = Main.npc[(int)Projectile.ai[0]];
        int hook = (int)Projectile.ai[2];
        if (!boss.active || boss.type != NPCID.Plantera || !PlanteraUnending.GetInstance(boss).LightningActive ||
            !PlanteraUnending.GetInstance(boss).OwnsHook(hook))
        {
            Projectile.Kill();
            return;
        }
        Projectile.Center = Main.npc[hook].Center;
        _Age++;
        if (_Fade > 0 || _Length >= PlanteraUnending.RayLength)
        {
            if (++_Fade == 1 && Main.netMode == NetmodeID.Server) Projectile.netUpdate = true;
            if (_Fade >= FadeTicks || _Length == 0) Projectile.Kill();
            return;
        }
        if (_Age > Delay) _Length = Math.Min(PlanteraUnending.RayLength, _Length + Speed);
    }
    public override bool? CanDamage() => _Length > 0f && _Fade == 0 ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (_Length <= 0f || _Fade > 0) return false;
        return RotatedRectangle.FromLineSegment(Projectile.Center, Projectile.Center + Direction * _Length, 12f).Collides(targetHitbox);
    }

    private static Color[] BuildStrip(Color[] source, int width, int height, int stripWidth)
    {
        // The artwork tapers to transparent tips. Mirror its interior so the
        // contours on both sides of each join meet at exactly the same pixel.
        // 生成器已提取原图用于平铺的中央 96 像素。
        int tile = width;
        int frameHeight = height / Frames;
        Color core = Color.FromNonPremultiplied(140, 252, 175, 230);
        Color[] result = new Color[stripWidth * height];
        for (int y = 0; y < height; y++)
        {
            int localY = y % frameHeight;
            bool spine = localY == frameHeight / 2 - 1 || localY == frameHeight / 2;
            for (int x = 0; x < stripWidth; x++)
            {
                int offset = x % (2 * tile);
                int srcX = offset < tile ? offset : 2 * tile - 1 - offset;
                Color pixel = source[y * width + srcX];
                result[y * stripWidth + x] = spine && pixel.A < core.A ? core : pixel;
            }
        }
        return result;
    }

    private Texture2D BeamStrip()
    {
        if (_BeamStrip != null) return _BeamStrip;
        Texture2D source = Projectile.Texture;
        int width = (int)MathF.Ceiling(PlanteraUnending.RayLength / SpriteScale);
        Color[] pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        _BeamStrip = new Texture2D(source.GraphicsDevice, width, source.Height);
        _BeamStrip.SetData(BuildStrip(pixels, source.Width, source.Height, width));
        return _BeamStrip;
    }

    public override void Unload()
    {
        Texture2D texture = _BeamStrip;
        _BeamStrip = null;
        if (texture != null) Main.QueueMainThreadAction(texture.Dispose);
    }

    public override void OnKill(int timeLeft)
    {
        for (float d = 0f; d < _Length; d += 36f)
            ClearParticles.Burst(Projectile.Center + Direction * d, new Color(140, 252, 175), 2);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (_Length <= 0f) return false;
        Texture2D texture = BeamStrip();
        Rectangle frame = texture.Frame(1, Frames, 0, (_Age - Delay) / 2 % Frames);
        frame.Width = (int)MathF.Ceiling(_Length / SpriteScale);
        Vector2 origin = new(0f, frame.Height / 2f);
        float widthScale = 1f - _Fade / (float)FadeTicks;
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White,
            Projectile.ai[1], origin, new Vector2(_Length / frame.Width, SpriteScale * widthScale), SpriteEffects.None);
        return false;
    }
}