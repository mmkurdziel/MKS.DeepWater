using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MKS.DeepWater.Web.Data;

/// <summary>
/// Kwota w złotych (<see cref="decimal"/>) zapisywana w bazie jako całkowita liczba groszy.
/// <para>
/// SQLite nie ma typu dziesiętnego. Zapis jako <c>REAL</c> (double) przekłamuje kwoty
/// (0,1 + 0,2 ≠ 0,3) i błąd rośnie przy sumowaniu wielu wpłat. Liczba całkowita groszy
/// sumuje się w SQL dokładnie i dalej pozwala sortować i porównywać po stronie bazy.
/// </para>
/// </summary>
public sealed class GroszeConverter : ValueConverter<decimal, long>
{
    public GroszeConverter()
        : base(zlote => ToGrosze(zlote), grosze => FromGrosze(grosze))
    {
    }

    /// <summary>
    /// Kwota z dokładnością do grosza - inaczej wyjątek. Ciche zaokrąglenie zgubiłoby
    /// różnicę bez śladu, a kwoty z wyciągów i formularzy zawsze mają najwyżej dwa miejsca.
    /// </summary>
    public static long ToGrosze(decimal zlote)
    {
        var grosze = zlote * 100m;
        if (grosze != decimal.Truncate(grosze))
        {
            throw new ArgumentException(
                $"Kwota {zlote} ma więcej niż dwa miejsca po przecinku.", nameof(zlote));
        }

        return (long)grosze;
    }

    public static decimal FromGrosze(long grosze) => grosze / 100m;
}
