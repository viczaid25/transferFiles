using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using transferFiles.Data;
using transferFiles.Models.ViewModels;
using transferFiles.Options;

public class LinksController : Controller
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly int _validityDays;

    public LinksController(AppDbContext db, IHttpContextAccessor http, IOptions<TransferNowOptions> opt)
    {
        _db = db;
        _http = http;
        _validityDays = opt.Value.DefaultValidityDays; // típicamente 7
    }

    [HttpGet]
    public async Task<IActionResult> Mine()
    {
        var winUser =
            _http.HttpContext?.User?.Identity?.Name
            ?? User?.Identity?.Name
            ?? Environment.UserName
            ?? "unknown";

        var nowUtc = DateTime.UtcNow;

        var items = await _db.TransferLinkLogs
            .Where(x => x.WindowsUser == winUser)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new TransferLinkRowVm
            {
                Id = x.Id,
                WindowsUser = x.WindowsUser,
                Link = x.Link,
                CreatedAtUtc = x.CreatedAtUtc,
                FileName = x.FileName,
                ExpiresAtUtc = x.CreatedAtUtc.AddDays(_validityDays),
                IsExpired = nowUtc >= x.CreatedAtUtc.AddDays(_validityDays),
                Password = x.Password
            })
            .ToListAsync();

        ViewBag.CurrentUser = winUser;
        ViewBag.ValidityDays = _validityDays;
        return View(items);
    }
}
