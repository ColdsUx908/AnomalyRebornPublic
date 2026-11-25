using Anomalies.BaseContents;

namespace Anomalies.GameContents.MusicBoxes;

public sealed class PathwaytoReverieMusicBox : BaseMusicBox<PathwaytoReverieMusicBoxTile>
{
    public override string MusicFileName => "Rainpath — Pathway to Reverie";

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe()
            .AddIngredient(ItemID.MusicBox)
            .AddCondition(Condition.NearShimmer)
            .Register();
        if (!Main.dedServ && AnomaliesMusicHandler.PathwaytoReverie <= 0)
            recipe.DisableRecipe();
    }
}

public sealed class PathwaytoReverieMusicBoxTile : BaseMusicBoxTile;
