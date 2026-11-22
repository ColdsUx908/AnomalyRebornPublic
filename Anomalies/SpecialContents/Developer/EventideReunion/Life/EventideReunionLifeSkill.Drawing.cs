namespace Anomalies.SpecialContents.Developer.EventideReunion;

public sealed partial class EventideReunionLifeSkill
{
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D weapon = EventideReunionAssets.Weapon;
        Vector2 grip = WeaponGrip;
        Vector2 axis = WeaponAngle.ToRotationVector2();
        if (Age >= EventideReunionLifeRules.BurstTime && Age < EventideReunionLifeRules.LaunchTime)
            _TipTrail.Draw(WeaponTip, 24f);
        Main.spriteBatch.Draw(weapon, grip - Main.screenPosition, null, Color.White,
            WeaponAngle + MathHelper.PiOver4, new Vector2(144f, 156f), 1f, SpriteEffects.None, 0f);
        float growth = MathHelper.Clamp(Age / (float)EventideReunionLifeRules.BurstTime, 0f, 1f);
        if (Age < EventideReunionLifeRules.LaunchTime)
            EventideReunionLifeOrb.DrawBall(WeaponTip, 25f * MathHelper.SmoothStep(0f, 1f, growth));
        if (EventideReunionRuneAssets.Life?.Value is Texture2D rune && Age < EventideReunionLifeRules.BurstTime)
            for (int i = 0; i < 5; i++)
            {
                float t = (Age / 60f + i / 5f) % 1f;
                Vector2 position = grip + axis * MathHelper.Lerp(200f, -130f, t);
                Main.spriteBatch.Draw(rune, position - Main.screenPosition, null, Color.White * (MathF.Sin(t * MathHelper.Pi) * growth * 0.6f),
                    0f, rune.Size() * 0.5f, 16f / rune.Width, SpriteEffects.None, 0f);
            }
        return false;
    }

}

// 仅提交 DrawData，不调用玩家渲染器；树影始终位于人物后方。
public sealed class EventideReunionLifeTreeLayer : PlayerDrawLayer
{
    // 原图像素中心，按王冠、智慧、理解、慈爱、严厉、美、胜利、荣耀、基础、王国排序。
    private static readonly Vector2[] _Sefirot =
    [
        new(128, 37), new(181, 70), new(74, 70), new(181, 130), new(74, 130),
        new(128, 162), new(181, 191), new(74, 191), new(128, 227), new(128, 279)
    ];

    public override bool IsLoadingEnabled(Mod mod) => AnomalySpecialContentToggler.EventideReunion;
    public override Position GetDefaultPosition() => PlayerDrawLayers.BeforeFirstVanillaLayer;
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.shadow == 0f
        && drawInfo.drawPlayer.ownedProjectileCounts[ModContent.ProjectileType<EventideReunionLifeSkill>()] > 0;

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        if (EventideReunionLifeAssets.TreeOfLife?.Value is not Texture2D tree)
            return;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.owner != drawInfo.drawPlayer.whoAmI || projectile.ModProjectile is not EventideReunionLifeSkill skill)
                continue;
            const float Scale = 1.5f;
            float age = skill.Age;
            float descent = MathHelper.Clamp((age - EventideReunionLifeRules.PlantDuration)
                / (EventideReunionLifeRules.BurstTime - EventideReunionLifeRules.PlantDuration), 0f, 1f);
            float fade = MathHelper.SmoothStep(0f, 1f, (EventideReunionLifeRules.CastDuration - age)
                / EventideReunionLifeRules.RecoveryDuration);
            Vector2 origin = new(tree.Width * 0.5f, tree.Height - 10f);
            Vector2 position = drawInfo.drawPlayer.MountedCenter + new Vector2(0f, drawInfo.drawPlayer.height * 0.5f * drawInfo.drawPlayer.gravDir) - Main.screenPosition;
            float rotation = drawInfo.drawPlayer.gravDir < 0f ? MathHelper.Pi : 0f;
            float silhouette = MathHelper.SmoothStep(0f, 0.24f, age / EventideReunionLifeRules.PlantDuration);
            drawInfo.DrawDataCache.Add(new DrawData(tree, position, null, Color.White * (silhouette * fade),
                rotation, origin, Scale, SpriteEffects.None, 0));

            float step = descent * (_Sefirot.Length - 1);
            int current = (int)step;
            Vector2 next = _Sefirot[Math.Min(current + 1, _Sefirot.Length - 1)];
            float front = MathHelper.Lerp(_Sefirot[current].Y, next.Y, step - current);
            // 用原图逐层显露枝脉，保留树冠轮廓、路径和透明边缘。
            float release = MathHelper.Clamp((age - EventideReunionLifeRules.BurstTime) / EventideReunionLifeRules.RiseDuration, 0f, 1f);
            int rows = (int)MathHelper.Lerp(front, tree.Height, release);
            float arrival = MathHelper.SmoothStep(0f, 1f, (age - EventideReunionLifeRules.PlantDuration) / 4f);
            drawInfo.DrawDataCache.Add(new DrawData(tree, position, new Rectangle(0, 0, tree.Width, rows),
                Color.White * (0.78f * arrival * fade), rotation, origin, Scale, SpriteEffects.None, 0));

            if (EventideReunionLifeAssets.GlowDot?.Value is not Texture2D glow)
                continue;
            for (int i = 0; i <= current; i++)
            {
                float pulse = Math.Max(0f, 1f - (step - i));
                if (i == _Sefirot.Length - 1)
                    pulse = 1f - release;
                Vector2 center = position + ((_Sefirot[i] - origin) * Scale).RotatedBy(rotation);
                float diameter = 32f + pulse * (i == _Sefirot.Length - 1 ? 72f : 36f);
                drawInfo.DrawDataCache.Add(new DrawData(glow, center, null,
                    new Color(255, 225, 136, 0) * ((0.28f + 0.72f * pulse) * arrival * fade),
                    rotation, glow.Size() * 0.5f, diameter / glow.Width, SpriteEffects.None, 0));
            }
        }
    }
}
