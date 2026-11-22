using Anomalies.Assets.Effects;

namespace Anomalies.SpecialContents.Donor.SnowHareValkyrie;

public sealed partial class SnowHareValkyrie
{
    private string DonorName => DonorInfo.DonorsByID[DonorID].Name;

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        AnomalyItemTooltipModifier modifier = new(Item, tooltips);
        int index = modifier.Valid ? modifier._LastTooltipIndex + 1 : tooltips.Count;
        IDonorItem.AddDonorItemIdentifier(Mod, tooltips, index);
        if (Main.keyState.PressingShift())
            IDonorItem.AddDonorItemOwnerIdentifier(Mod, tooltips, index + 1, DonorName);
        else
        {
            modifier.Update();
            modifier.AddExpendedDisplayLine();
        }
    }

    public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
    {
        if (!AnomalyClientConfig.Instance.EnableShaders)
            return true;

        if (SnowHareValkyrieEffects.SnowHareName is MiscShaderContainer nameShader
            && TODrawUtils.TryDrawItemNameWithCustomShader(line, text => nameShader.Data
                .SetCustomParameter("uOrigin", new Vector2(text.X, text.Y) * Main.UIScale)
                .SetCustomParameter("uScale", text.BaseScale * Main.UIScale)
                .Apply()))
            return false;

        if (line.Mod != Mod.Name || line.Name != "Tooltip_DonorItemOwnerIdentifier"
            || SnowHareValkyrieEffects.DonorName is not MiscShaderContainer shader)
            return true;

        string name = DonorName;
        int index = line.Text.IndexOf(name, StringComparison.Ordinal);
        if (index < 0)
            return true;

        SpriteBatch batch = Main.spriteBatch;
        Vector2 start = new Vector2(line.X, line.Y) - (line.Origin * line.BaseScale).RotatedBy(line.Rotation);
        Color pink = line.OverrideColor ?? line.Color;
        string prefix = line.Text[..index];
        int end = index + name.Length;
        Vector2 namePosition = PositionAfter(prefix);
        TODrawUtils.DrawBorderString(batch, line.Font, line.Text, start, Color.Black, 4, 1f, line.BaseScale, line.Rotation);
        batch.DrawString(line.Font, prefix, start, pink, line.Rotation, Vector2.Zero, line.BaseScale, SpriteEffects.None, 0f);
        if (end < line.Text.Length)
            batch.DrawString(line.Font, line.Text[end..], PositionAfter(line.Text[..end]), pink, line.Rotation,
                Vector2.Zero, line.BaseScale, SpriteEffects.None, 0f);

        using (batch.Scope(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp))
        {
            shader.Data
                .SetCustomParameter("uOrigin", namePosition * Main.UIScale)
                .SetCustomParameter("uScale", line.BaseScale * Main.UIScale)
                .SetCustomParameter("uRotation", line.Rotation)
                .SetCustomParameter("uWidth", line.Font.MeasureString(name).X)
                .Apply();
            batch.DrawString(line.Font, name, namePosition, Color.White, line.Rotation, Vector2.Zero,
                line.BaseScale, SpriteEffects.None, 0f);
        }
        return false;

        Vector2 PositionAfter(string text) => start + (new Vector2(line.Font.MeasureString(text).X, 0f) * line.BaseScale).RotatedBy(line.Rotation);
    }
}
