namespace MKS.DeepWater.Web.Models.Domain;

/// <summary>Status członkostwa w spółce.</summary>
public enum MemberStatus
{
    Active = 0,      // Aktywny
    Suspended = 1,   // Zawieszony
    Former = 2       // Były członek
}

/// <summary>Forma wpłaty składki.</summary>
public enum PaymentMethod
{
    Transfer = 0,  // Przelew
    Cash = 1,      // Gotówka
    Card = 2,      // Karta
    Other = 3
}
