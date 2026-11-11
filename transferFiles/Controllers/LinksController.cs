using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using transferFiles.Auth;
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
        // Login de red del usuario según el contrato con meaxHub (claim PcLoginId).
        var winUser = User.PcLoginId() ?? "unknown";

        var nowUtc = DateTime.UtcNow;

        var items = await _db.TransferLinkLogs
            .Where(x => x.CreatedByLogin == winUser && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new TransferLinkRowVm
            {
                Id = x.LinkId,
                WindowsUser = x.CreatedByLogin,
                CreatedByName = x.CreatedByName,
                CreatedByDepartment = x.CreatedByDepartment,
                Link = x.Link,
                CreatedAtUtc = x.CreatedAtUtc,
                FileName = x.FileName,
                ExpiresAtUtc = x.ExpiresAtUtc,
                IsExpired = nowUtc >= x.ExpiresAtUtc,
                Password = x.Password
            })
            .ToListAsync();

        ViewBag.CurrentUser = winUser;
        ViewBag.ValidityDays = _validityDays;
        return View(items);
    }

    /// <summary>
    /// Vista de administración: todos los links del sistema, con quién los generó
    /// y de qué departamento. A propósito NO muestra las contraseñas: el
    /// administrador necesita saber quién compartió qué, no poder abrir los
    /// archivos de los demás.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AdminPolicy.Name)]
    public async Task<IActionResult> All()
    {
        var nowUtc = DateTime.UtcNow;

        var items = await _db.TransferLinkLogs
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new AdminLinksVm.Row
            {
                Id = x.LinkId,
                Login = x.CreatedByLogin,
                Nombre = x.CreatedByName,
                Departamento = x.CreatedByDepartment,
                FileName = x.FileName,
                Link = x.Link,
                Reason = x.Reason,
                CreatedAtUtc = x.CreatedAtUtc,
                ExpiresAtUtc = x.ExpiresAtUtc,
                IsExpired = nowUtc >= x.ExpiresAtUtc
            })
            .ToListAsync();

        var vm = new AdminLinksVm
        {
            Items = items,
            Total = items.Count,
            Activos = items.Count(x => !x.IsExpired),
            Expirados = items.Count(x => x.IsExpired),
            UsuariosDistintos = items.Select(x => x.Login).Distinct().Count(),
            DepartamentosDistintos = items.Where(x => !string.IsNullOrWhiteSpace(x.Departamento))
                                          .Select(x => x.Departamento!).Distinct().Count(),
            Departamentos = items.Where(x => !string.IsNullOrWhiteSpace(x.Departamento))
                                 .Select(x => x.Departamento!)
                                 .Distinct().OrderBy(d => d).ToList()
        };

        return View(vm);
    }
}
