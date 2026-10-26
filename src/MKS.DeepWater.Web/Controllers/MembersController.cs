using MKS.DeepWater.Web.Data;
using MKS.DeepWater.Web.Models.Domain;
using MKS.DeepWater.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MKS.DeepWater.Web.Controllers;

[Authorize]
public class MembersController : Controller
{
    private readonly ApplicationDbContext _db;

    public MembersController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, MemberStatus? status, int? feeYear,
        bool showDeleted = false, CancellationToken ct = default)
    {
        var year = feeYear ?? DateTime.Today.Year;

        // Usunięte rekordy są odfiltrowane globalnie - archiwum wymaga ich jawnego włączenia.
        var query = showDeleted
            ? _db.Members.IgnoreQueryFilters().AsNoTracking()
            : _db.Members.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m =>
                m.LastName.Contains(term) ||
                m.FirstName.Contains(term) ||
                m.MembershipNumber.Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

        var items = await query
            .OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
            .Select(m => new MemberListItem
            {
                Id = m.Id,
                MembershipNumber = m.MembershipNumber,
                FullName = m.FirstName + " " + m.LastName,
                PhoneNumber = m.PhoneNumber,
                Street = m.Street,
                HouseNumber = m.HouseNumber,
                City = m.City,
                Status = m.Status,
                SharesCount = m.SharesCount,
                PaidInSelectedYear = m.Payments
                    .Where(p => p.FeeYear == year)
                    .Sum(p => p.Amount),
                DeletedAt = m.DeletedAt
            })
            .ToListAsync(ct);

        return View(new MemberListViewModel
        {
            Search = search,
            Status = status,
            FeeYear = year,
            ShowDeleted = showDeleted,
            Items = items
        });
    }

    [HttpGet]
    public IActionResult Create() => View(new MemberFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MemberFormViewModel form, CancellationToken ct)
    {
        ValidateRules(form);
        // Nowy rekord nie może wykluczać z kontroli unikalności żadnego Id - Id z formularza
        // przychodzi od klienta i wskazanie nim istniejącego członka obeszłoby walidację.
        await ValidateMembershipNumberAsync(form.MembershipNumber, excludeId: 0, ct);
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var member = new Member();
        Apply(form, member);
        _db.Members.Add(member);

        if (!await TrySaveAsync(ct))
        {
            return View(form);
        }

        TempData["Message"] = $"Dodano członka {member.FullName} (nr {member.MembershipNumber}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == id, ct);

        if (member is null)
        {
            return NotFound();
        }

        return View(ToForm(member));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MemberFormViewModel form, CancellationToken ct)
    {
        if (id != form.Id)
        {
            return BadRequest();
        }

        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == id, ct);

        if (member is null)
        {
            return NotFound();
        }

        ValidateRules(form);
        await ValidateMembershipNumberAsync(form.MembershipNumber, excludeId: id, ct);
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        Apply(form, member);
        member.UpdatedAt = DateTimeOffset.UtcNow;

        if (!await TrySaveAsync(ct))
        {
            return View(form);
        }

        TempData["Message"] = $"Zapisano zmiany dla {member.FullName}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var member = await _db.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        return member is null ? NotFound() : View(member);
    }

    [HttpPost, ActionName(nameof(Delete))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (member is null)
        {
            return NotFound();
        }

        member.DeletedAt = DateTimeOffset.UtcNow;
        member.DeletedBy = User.Identity?.Name;
        await _db.SaveChangesAsync(ct);

        TempData["Message"] =
            $"Usunięto członka {member.FullName}. Dane zostały zachowane - rekord można przywrócić " +
            "po zaznaczeniu „pokaż usunięte”.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var member = await _db.Members
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (member is null || member.DeletedAt is null)
        {
            return NotFound();
        }

        member.DeletedAt = null;
        member.DeletedBy = null;
        member.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        TempData["Message"] = $"Przywrócono członka {member.FullName}.";
        return RedirectToAction(nameof(Index), new { showDeleted = true });
    }

    /// <summary>
    /// Numer członkowski jest unikalny także wśród usuniętych rekordów.
    /// <paramref name="excludeId"/> pochodzi z serwera (0 dla nowego rekordu, Id z trasy przy edycji).
    /// </summary>
    private async Task ValidateMembershipNumberAsync(string? membershipNumber, int excludeId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(membershipNumber))
        {
            return;
        }

        var number = membershipNumber.Trim();
        var taken = await _db.Members
            .IgnoreQueryFilters()
            .AnyAsync(m => m.MembershipNumber == number && m.Id != excludeId, ct);

        if (taken)
        {
            AddMembershipNumberTakenError();
        }
    }

    /// <summary>Reguły obejmujące kilka pól - uruchamiane zawsze, niezależnie od błędów pojedynczych pól.</summary>
    private void ValidateRules(MemberFormViewModel form)
    {
        foreach (var violation in form.GetRuleViolations())
        {
            foreach (var field in violation.MemberNames.DefaultIfEmpty(string.Empty))
            {
                ModelState.AddModelError(field, violation.ErrorMessage!);
            }
        }
    }

    /// <summary>
    /// Zapis z obsługą kolizji unikalnego numeru - wyścig dwóch osób wpisujących ten sam numer
    /// kończy się błędem walidacji, a nie wyjątkiem z indeksu bazy.
    /// </summary>
    private async Task<bool> TrySaveAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsMembershipNumberConflict(ex))
        {
            AddMembershipNumberTakenError();
            return false;
        }
    }

    private static bool IsMembershipNumberConflict(DbUpdateException ex) =>
        ex.InnerException is SqliteException { SqliteErrorCode: 19 } sqlite &&
        sqlite.Message.Contains("Members.MembershipNumber", StringComparison.OrdinalIgnoreCase);

    private void AddMembershipNumberTakenError() =>
        ModelState.AddModelError(nameof(MemberFormViewModel.MembershipNumber),
            "Ten numer członkowski jest już zajęty (sprawdź także usunięte rekordy).");

    private static MemberFormViewModel ToForm(Member member) => new()
    {
        Id = member.Id,
        MembershipNumber = member.MembershipNumber,
        FirstName = member.FirstName,
        LastName = member.LastName,
        PhoneNumber = member.PhoneNumber,
        Status = member.Status,
        SharesCount = member.SharesCount,
        Notes = member.Notes,
        Street = member.Street,
        HouseNumber = member.HouseNumber,
        City = member.City
    };

    private static void Apply(MemberFormViewModel form, Member member)
    {
        member.MembershipNumber = form.MembershipNumber.Trim();
        member.FirstName = form.FirstName.Trim();
        member.LastName = form.LastName.Trim();
        member.PhoneNumber = Trimmed(form.PhoneNumber);
        member.Status = form.Status;
        member.SharesCount = form.SharesCount;
        member.Notes = Trimmed(form.Notes);
        member.Street = Trimmed(form.Street);
        member.HouseNumber = Trimmed(form.HouseNumber);
        member.City = Trimmed(form.City);
    }

    private static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
