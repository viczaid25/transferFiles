using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using transferFiles.Data;
using transferFiles.Models.ViewModels;
using transferFiles.Options;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly int _validityDays;

    public HomeController(AppDbContext db, IHttpContextAccessor http, IOptions<TransferNowOptions> opt)
    {
        _db = db;
        _http = http;
        _validityDays = opt.Value.DefaultValidityDays;
    }

    public async Task<IActionResult> Index()
    {
        var user =
            _http.HttpContext?.User?.Identity?.Name
            ?? User?.Identity?.Name
            ?? Environment.UserName
            ?? "unknown";

        var now = DateTime.UtcNow;

        // Stats
        var myQuery = _db.TransferLinkLogs.Where(x => x.WindowsUser == user);

        var total = await myQuery.CountAsync();
        var active = await myQuery.CountAsync(x => now < x.CreatedAtUtc.AddDays(_validityDays));
        var expired = total - active;

        // Últimos 5
        var recent = await myQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .Select(x => new HomeIndexVm.LinkRow
            {
                Id = x.Id,
                FileName = x.FileName,
                Link = x.Link,
                CreatedAtUtc = x.CreatedAtUtc,
                ExpiresAtUtc = x.CreatedAtUtc.AddDays(_validityDays),
                IsExpired = now >= x.CreatedAtUtc.AddDays(_validityDays)
            })
            .ToListAsync();

        var vm = new HomeIndexVm
        {
            CurrentUser = user,
            ValidityDays = _validityDays,
            TotalMyLinks = total,
            ActiveMyLinks = active,
            ExpiredMyLinks = expired,
            Recent = recent
        };

        return View(vm);
    }
}
