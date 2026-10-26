using System.Reflection;
using MKS.DeepWater.Web.Controllers;
using MKS.DeepWater.Web.Models.Domain;

namespace MKS.DeepWater.Web.Tests;

/// <summary>
/// Anulowanie żądania musi dojść aż do zapytania w bazie. Sam parametr
/// <see cref="CancellationToken"/> w sygnaturze nic nie daje, jeśli nie zostanie przekazany
/// do wywołania EF - te testy sprawdzają przekazanie, nie deklarację.
/// </summary>
public class MembersControllerCancellationTests
{
    [Fact]
    public async Task Index_przerywa_sie_na_anulowanym_tokenie()
    {
        using var db = TestDb.Create();
        var controller = new MembersController(db.Context);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => controller.Index(search: null, status: null, feeYear: null,
                                   showDeleted: false, ct: Cancelled()));
    }

    [Fact]
    public async Task Edit_przerywa_sie_na_anulowanym_tokenie()
    {
        using var db = TestDb.Create();
        var controller = new MembersController(db.Context);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => controller.Edit(id: 1, ct: Cancelled()));
    }

    [Fact]
    public async Task Delete_przerywa_sie_na_anulowanym_tokenie()
    {
        using var db = TestDb.Create();
        var controller = new MembersController(db.Context);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => controller.Delete(id: 1, ct: Cancelled()));
    }

    /// <summary>
    /// Nieanulowany token nie może niczego psuć - bez tego testy wyżej przechodziłyby również
    /// wtedy, gdyby akcja rzucała wyjątek z całkiem innego powodu.
    /// </summary>
    [Fact]
    public async Task Index_z_aktywnym_tokenem_zwraca_liste()
    {
        using var db = TestDb.Create();
        db.Context.Members.Add(new Member
        {
            MembershipNumber = "001",
            FirstName = "Jan",
            LastName = "Testowy"
        });
        await db.Context.SaveChangesAsync(CancellationToken.None);

        var controller = new MembersController(db.Context);

        var result = await controller.Index(search: null, status: null, feeYear: null,
                                            showDeleted: false, ct: CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Mvc.ViewResult>(result);
    }

    /// <summary>
    /// Straż na przyszłość: nowa akcja asynchroniczna bez <see cref="CancellationToken"/>
    /// zapali ten test, zamiast przejść niezauważona w review.
    /// </summary>
    [Fact]
    public void Kazda_akcja_async_ma_parametr_CancellationToken()
    {
        var bezTokenu = typeof(MembersController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => typeof(Task).IsAssignableFrom(m.ReturnType))
            .Where(m => m.GetParameters().All(p => p.ParameterType != typeof(CancellationToken)))
            .Select(m => m.Name)
            .ToList();

        Assert.Empty(bezTokenu);
    }

    private static CancellationToken Cancelled()
    {
        var source = new CancellationTokenSource();
        source.Cancel();
        return source.Token;
    }
}
