using MKS.DeepWater.Web.Models.Domain;

namespace MKS.DeepWater.Web.Models.ViewModels;

public class MemberListViewModel
{
    public string? Search { get; set; }
    public MemberStatus? Status { get; set; }
    public int FeeYear { get; set; }
    public IReadOnlyList<MemberListItem> Items { get; init; } = Array.Empty<MemberListItem>();
    public int TotalCount => Items.Count;
}

public class MemberListItem
{
    public int Id { get; init; }
    public string MembershipNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public MemberStatus Status { get; init; }
    public int SharesCount { get; init; }
    public DateOnly JoinDate { get; init; }
    public decimal PaidInSelectedYear { get; init; }
}
