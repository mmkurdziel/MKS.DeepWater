using MKS.DeepWater.Web.Models.Domain;

namespace MKS.DeepWater.Web.Models.ViewModels;

public class MemberListViewModel
{
    public string? Search { get; set; }
    public MemberStatus? Status { get; set; }
    public int FeeYear { get; set; }
    public bool ShowDeleted { get; set; }
    public IReadOnlyList<MemberListItem> Items { get; init; } = Array.Empty<MemberListItem>();
    public int TotalCount => Items.Count;
}

public class MemberListItem
{
    public int Id { get; init; }
    public string MembershipNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Street { get; init; }
    public string? HouseNumber { get; init; }
    public string? City { get; init; }
    public MemberStatus Status { get; init; }
    public int SharesCount { get; init; }
    public decimal PaidInSelectedYear { get; init; }
    public DateTimeOffset? DeletedAt { get; init; }
    public bool IsDeleted => DeletedAt is not null;

    /// <summary>
    /// Adres składany po odczycie z bazy, nie w zapytaniu: konkatenacja w SQL-u z wartościami
    /// NULL dałaby pusty wynik dla adresu wpisanego tylko częściowo.
    /// </summary>
    public string AddressLine => Member.FormatAddress(Street, HouseNumber, City);
}
