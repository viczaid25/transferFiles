using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using transferFiles.Auth;
using transferFiles.Data;
using transferFiles.Diagnostics;
using transferFiles.Models;
using transferFiles.Models.ViewModels;
using transferFiles.Options;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly int _validityDays;
    private readonly ErrorLog _errorLog;
    private readonly IConfiguration _config;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        AppDbContext db,
        IHttpContextAccessor http,
        IOptions<TransferNowOptions> opt,
        ErrorLog errorLog,
        IConfiguration config,
        ILogger<HomeController> logger)
    {
        _db = db;
        _http = http;
        _validityDays = opt.Value.DefaultValidityDays;
        _errorLog = errorLog;
        _config = config;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // Login de red del usuario según el contrato con meaxHub (claim PcLoginId).
        // Es el mismo valor que se guarda en TransferLinkLogs.WindowsUser.
        var user = User.PcLoginId() ?? "unknown";

        var now = DateTime.UtcNow;

        // Stats
        var myQuery = _db.TransferLinkLogs.Where(x => x.WindowsUser == user);

        var total = await myQuery.CountAsync();
        var active = await myQuery.CountAsync(x => now < x.CreatedAtUtc.AddDays(_validityDays));
        var expired = total - active;

        // �ltimos 5
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

    /// <summary>
    /// Página de error. La invoca UseExceptionHandler ante cualquier excepción no
    /// controlada: deja el detalle completo en el log de archivo y muestra al
    /// usuario un id para correlacionarlo. AllowAnonymous porque el fallo puede
    /// ocurrir antes de autenticar.
    /// </summary>
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var ex = feature?.Error;
        var correlationId = HttpContext.TraceIdentifier;
        var ruta = feature?.Path ?? Request.Path.Value;

        var detalle =
            $"{Request.Method} {Request.PathBase}{ruta}\n" +
            $"usuario: {User.PcLoginId() ?? "(sin autenticar)"}\n\n" +
            (ex?.ToString() ?? "(sin excepción en el contexto)");

        _logger.LogError(ex, "Error no controlado en {Ruta} (id {CorrelationId})", ruta, correlationId);

        var archivo = _errorLog.Escribir(correlationId, detalle);

        Response.StatusCode = StatusCodes.Status500InternalServerError;

        return View(new ErrorViewModel
        {
            RequestId = correlationId,
            Path = ruta,
            Detail = _config.GetValue<bool>("Diagnostics:ShowDetailedErrors") ? detalle : null,
            LogPath = archivo ?? _errorLog.Ruta
        });
    }
}
