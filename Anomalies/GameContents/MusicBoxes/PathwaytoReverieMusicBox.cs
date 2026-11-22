using Anomalies.BaseContents;

namespace Anomalies.GameContents.MusicBoxes;

public sealed class PathwaytoReverieMusicBox : BaseMusicBox<PathwaytoReverieMusicBoxTile>
{
    public override string MusicFileName => "Rainpath — Pathway to Reverie";

    public override void AddRecipes() => CreateRecipe()
        .AddIngredient(ItemID.MusicBox)
        .AddCondition(Condition.NearShimmer)
        .Register();
}

public sealed class PathwaytoReverieMusicBoxTile : BaseMusicBoxTile;