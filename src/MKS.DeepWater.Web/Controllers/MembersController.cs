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
    private readonly ApplicationDbContext _db;

    public MembersController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, MemberStatus? status, int? feeYear)
    {
        var year = feeYear ?? DateTime.Today.Year;

        var query = _db.Members.AsNoTracking();

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
                    .Select(a => a.Street + " " + a.HouseNumber + ", " + a.PostalCode + " " + a.City)
                    .FirstOrDefault(),
                Status = m.Status,
                SharesCount = m.SharesCount,
                JoinDate = m.JoinDate,
                PaidInSelectedYear = m.Payments
                    .Where(p => p.FeeYear == year)
                    .Sum(p => p.Amount)
            })
            .ToListAsync();

        return View(new MemberListViewModel
        {
            Search = search,
            Status = status,
            FeeYear = year,
            Items = items
        });
    }
}
