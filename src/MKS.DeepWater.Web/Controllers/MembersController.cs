using MKS.DeepWater.Web.Data;
using MKS.DeepWater.Web.Models.Domain;
using MKS.DeepWater.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MKS.DeepWater.Web.Controllers;

[Authorize]
public class MembersController : Controller
{
    private const AddressType PrimaryAddressType = AddressType.Registered;

    private readonly ApplicationDbContext _db;

    public MembersController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, MemberStatus? status, int? feeYear, bool showDeleted = false)
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
                Email = m.Email,
                PhoneNumber = m.PhoneNumber,
                Address = m.Addresses
                    .OrderBy(a => a.Type)
                    .Select(a => a.Street + " " + a.HouseNumber +
                                 (a.ApartmentNumber == null ? "" : "/" + a.ApartmentNumber) +
                                 ", " + a.PostalCode + " " + a.City)
                    .FirstOrDefault(),
                Status = m.Status,
                SharesCount = m.SharesCount,
                JoinDate = m.JoinDate,
                PaidInSelectedYear = m.Payments
                    .Where(p => p.FeeYear == year)
                    .Sum(p => p.Amount),
                DeletedAt = m.DeletedAt
            })
            .ToListAsync();

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
    public IActionResult Create() => View(new MemberFormViewModel
    {
        JoinDate = DateOnly.FromDateTime(DateTime.Today)
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MemberFormViewModel form)
    {
        await ValidateMembershipNumberAsync(form);
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var member = new Member();
        Apply(form, member);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"Dodano członka {member.FullName} (nr {member.MembershipNumber}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var member = await _db.Members
            .Include(m => m.Addresses)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (member is null)
        {
            return NotFound();
        }

        return View(ToForm(member));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MemberFormViewModel form)
    {
        if (id != form.Id)
        {
            return BadRequest();
        }

        var member = await _db.Members
            .Include(m => m.Addresses)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (member is null)
        {
            return NotFound();
        }

        await ValidateMembershipNumberAsync(form);
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        Apply(form, member);
        member.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Message"] = $"Zapisano zmiany dla {member.FullName}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var member = await _db.Members
            .Include(m => m.Addresses)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        return member is null ? NotFound() : View(member);
    }

    [HttpPost, ActionName(nameof(Delete))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == id);
        if (member is null)
        {
            return NotFound();
        }

        member.DeletedAt = DateTimeOffset.UtcNow;
        member.DeletedBy = User.Identity?.Name;
        await _db.SaveChangesAsync();

        TempData["Message"] =
            $"Usunięto członka {member.FullName}. Dane zostały zachowane - rekord można przywrócić " +
            "po zaznaczeniu „pokaż usunięte”.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var member = await _db.Members
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (member is null)
        {
            return NotFound();
        }

        member.DeletedAt = null;
        member.DeletedBy = null;
        member.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Message"] = $"Przywrócono członka {member.FullName}.";
        return RedirectToAction(nameof(Index), new { showDeleted = true });
    }

    /// <summary>Numer członkowski jest unikalny także wśród usuniętych rekordów.</summary>
    private async Task ValidateMembershipNumberAsync(MemberFormViewModel form)
    {
        if (string.IsNullOrWhiteSpace(form.MembershipNumber))
        {
            return;
        }

        var number = form.MembershipNumber.Trim();
        var taken = await _db.Members
            .IgnoreQueryFilters()
            .AnyAsync(m => m.MembershipNumber == number && m.Id != form.Id);

        if (taken)
        {
            ModelState.AddModelError(nameof(form.MembershipNumber),
                "Ten numer członkowski jest już zajęty (sprawdź także usunięte rekordy).");
        }
    }

    private static MemberFormViewModel ToForm(Member member)
    {
        var address = member.Addresses.FirstOrDefault(a => a.Type == PrimaryAddressType);

        return new MemberFormViewModel
        {
            Id = member.Id,
            MembershipNumber = member.MembershipNumber,
            FirstName = member.FirstName,
            LastName = member.LastName,
            NationalId = member.NationalId,
            Email = member.Email,
            PhoneNumber = member.PhoneNumber,
            BirthDate = member.BirthDate,
            JoinDate = member.JoinDate,
            LeaveDate = member.LeaveDate,
            Status = member.Status,
            SharesCount = member.SharesCount,
            Notes = member.Notes,
            Street = address?.Street,
            HouseNumber = address?.HouseNumber,
            ApartmentNumber = address?.ApartmentNumber,
            PostalCode = address?.PostalCode,
            City = address?.City,
            Country = address?.Country ?? "Polska"
        };
    }

    private void Apply(MemberFormViewModel form, Member member)
    {
        member.MembershipNumber = form.MembershipNumber.Trim();
        member.FirstName = form.FirstName.Trim();
        member.LastName = form.LastName.Trim();
        member.NationalId = Trimmed(form.NationalId);
        member.Email = Trimmed(form.Email);
        member.PhoneNumber = Trimmed(form.PhoneNumber);
        member.BirthDate = form.BirthDate;
        member.JoinDate = form.JoinDate!.Value;
        member.LeaveDate = form.LeaveDate;
        member.Status = form.Status;
        member.SharesCount = form.SharesCount;
        member.Notes = Trimmed(form.Notes);

        ApplyAddress(form, member);
    }

    private void ApplyAddress(MemberFormViewModel form, Member member)
    {
        var address = member.Addresses.FirstOrDefault(a => a.Type == PrimaryAddressType);

        if (!form.HasAddress)
        {
            if (address is not null)
            {
                _db.Addresses.Remove(address);
            }

            return;
        }

        if (address is null)
        {
            address = new Address { Type = PrimaryAddressType };
            member.Addresses.Add(address);
        }

        address.Street = form.Street!.Trim();
        address.HouseNumber = form.HouseNumber!.Trim();
        address.ApartmentNumber = Trimmed(form.ApartmentNumber);
        address.PostalCode = form.PostalCode!.Trim();
        address.City = form.City!.Trim();
        address.Country = Trimmed(form.Country) ?? "Polska";
    }

    private static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
