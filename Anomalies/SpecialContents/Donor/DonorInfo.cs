using System.Text.Json;

namespace Anomalies.SpecialContents.Donor;

public record struct DonationInfo(DateOnly Date, decimal Price, string Message);

public record struct DonorInfo(string Name, List<DonationInfo> Donations)
{
    /// <summary>
    /// 存储规则：
    /// <para/><list type="bullet">
    /// <item/><description/>次序：第一次捐赠时间顺序排列，最早的在前。
    /// <item/><description/>存储内容：捐赠时间、捐赠金额、留言。
    /// </list>
    /// </summary>
    public static Dictionary<int, DonorInfo> DonorsByID = JsonSerializer.Deserialize<Dictionary<int, DonorInfo>>(
        AnomalyMain.Instance.GetFileBytes("SpecialContents/Donor/Donors.json"))
        ?? throw new InvalidDataException("Donors.json must contain a donor dictionary.");

    /// <summary>
    /// 单个捐赠者的总捐赠金额。
    /// </summary>
    public readonly decimal TotalDonation => Donations.Sum(d => d.Price);

    /// <summary>
    /// 所有捐赠者的总捐赠金额。
    /// </summary>
    public static decimal TotalDonations => DonorsByID.Values.Sum(d => d.TotalDonation);
}
