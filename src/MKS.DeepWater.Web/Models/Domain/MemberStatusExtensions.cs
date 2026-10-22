namespace MKS.DeepWater.Web.Models.Domain;

/// <summary>Opisy statusu członkostwa używane w widokach.</summary>
public static class MemberStatusExtensions
{
    public static string Label(this MemberStatus status) => status switch
    {
        MemberStatus.Active => "Aktywny",
        MemberStatus.Suspended => "Zawieszony",
        MemberStatus.Former => "Były członek",
        _ => status.ToString()
    };

    public static string BadgeCss(this MemberStatus status) => status switch
    {
        MemberStatus.Active => "text-bg-success",
        MemberStatus.Suspended => "text-bg-warning",
        MemberStatus.Former => "text-bg-secondary",
        _ => "text-bg-light"
    };
}
