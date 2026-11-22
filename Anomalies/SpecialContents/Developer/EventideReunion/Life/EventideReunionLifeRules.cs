namespace Anomalies.SpecialContents.Developer.EventideReunion;

internal static class EventideReunionLifeRules
{
    public const int Cooldown = 1080;
    public const int PlantDuration = 18;
    public const int GrowDuration = 42;
    public const int RiseDuration = 18;
    public const int RecoveryDuration = 24;
    public const int BurstTime = PlantDuration + GrowDuration;
    public const int CastDuration = BurstTime + RiseDuration + RecoveryDuration;
    public const int BlessingDuration = 900;
    public const int MarkDuration = 480;
    public const int FieldDuration = 180;
    public const int FruitDuration = 600;
    public const int FruitCount = 3;
    public const int OrbCount = 5;
    public const int OrbHealing = 10;
    public const int LaunchTime = BurstTime + RiseDuration + 5;
    public const int FruitBonusDivisor = 10;
    public const int MaxBlessingStacks = 3;
    private const int HeartUnitDivisor = 20; // 每个生命条单元仍表示基础上限的 5%。
    public const float Radius = 320f;

    public static int BonusLife(int maximumLife, int stacks) => (int)((long)Math.Max(0, maximumLife) * Math.Clamp(stacks, 0, MaxBlessingStacks) / FruitBonusDivisor);
    // 先在整数域中扣除完整单元，避免浮点舍入在整颗心之后多染一个像素。
    public static float HeartCoverage(int baseLife, int bonusLife, int unit)
    {
        if (baseLife <= 0 || bonusLife <= 0 || unit < 0)
            return 0f;
        long remaining = (long)bonusLife * HeartUnitDivisor - (long)unit * baseLife;
        return Math.Clamp(remaining, 0L, baseLife) / (float)baseLife;
    }

    public static int FruitLifeGain(int previousMaximum, int newMaximum, int currentLife)
        => Math.Max(0, newMaximum - Math.Max(previousMaximum, currentLife));

    public static int AllyHealing(int maximumLife) => (15 + maximumLife / 20) / 2;
    public static int EffectiveHealing(int requested, int life, int maximumLife, bool moonLeech)
        => moonLeech ? 0 : (int)Math.Clamp((long)requested, 0L, Math.Max(0L, (long)maximumLife - life));
    public static bool Ally(int caster, int recipient, int casterTeam, int recipientTeam, bool casterHostile, bool recipientHostile)
        => caster == recipient || casterTeam != 0 && casterTeam == recipientTeam && !casterHostile && !recipientHostile;
}
