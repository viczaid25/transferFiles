using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using transferFiles.Auth;
using transferFiles.Models.ViewModels;

namespace transferFiles.Controllers;

/// <summary>
/// Login SOLO para modo desarrollo (sin Meax One). En producción estas acciones
/// no aplican: cualquier request sin token redirige al SSO del hub.
/// El login de dev emite un ClaimsPrincipal con exactamente los mismos claims
/// que el JWT del hub, para que el resto de la app no distinga el modo.
/// </summary>
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly AuthMode _authMode;
    private readonly IConfiguration _config;

    public AccountController(AuthMode authMode, IConfiguration config)
    {
        _authMode = authMode;
        _config = config;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (!_authMode.DevLoginActivo)
        {
            // En producción el login lo hace el hub; ir a inicio dispara el SSO.
            return Redirect("~/");
        }

        var vm = new DevLoginVm
        {
            ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "~/" : returnUrl,
            Username = _config["Auth:DevUser:PcLoginId"] ?? string.Empty
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(DevLoginVm vm)
    {
        if (!_authMode.DevLoginActivo)
        {
            return Redirect("~/");
        }

        vm.ReturnUrl = string.IsNullOrWhiteSpace(vm.ReturnUrl) ? "~/" : vm.ReturnUrl;

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var devPassword = _config["Auth:DevPassword"] ?? "dev";

        if (!string.Equals(vm.Password, devPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "Contraseña de desarrollo incorrecta.");
            return View(vm);
        }

        var info = ConstruirHubUserInfo(vm.Username.Trim());

        var identity = MeaxClaims.CrearIdentidad(
            CookieAuthenticationDefaults.AuthenticationScheme,
            info);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        if (!Url.IsLocalUrl(vm.ReturnUrl))
        {
            vm.ReturnUrl = "~/";
        }

        return LocalRedirect(vm.ReturnUrl!);
    }

    /// <summary>
    /// Cierra la sesión local. En modo dev vuelve al login simplificado; en modo
    /// hub manda al Hub (la cookie .MEAX.JWT es del hub, no de esta app).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (_authMode.DevLoginActivo)
        {
            return RedirectToAction(nameof(Login));
        }

        var hubBaseUrl = _config["Hub:BaseUrl"]?.TrimEnd('/');

        return string.IsNullOrWhiteSpace(hubBaseUrl)
            ? Redirect("~/")
            : Redirect(hubBaseUrl);
    }

    /// <summary>
    /// Si el usuario tecleado es el de Auth:DevUser se usa su perfil completo;
    /// con otro usuario se conserva el SystemRole de config pero sin nómina ni
    /// puesto (evita matches falsos contra Tress/Expedientes).
    /// </summary>
    private HubUserInfo ConstruirHubUserInfo(string username)
    {
        var devUser = _config.GetSection("Auth:DevUser");
        var configLogin = devUser["PcLoginId"] ?? string.Empty;

        if (string.Equals(username, configLogin, StringComparison.OrdinalIgnoreCase))
        {
            return new HubUserInfo(
                configLogin,
                devUser["DisplayName"],
                devUser["Department"],
                devUser["Position"],
                devUser["EmployeeId"],
                devUser["SystemRole"]);
        }

        return new HubUserInfo(
            username,
            username,
            devUser["Department"],
            null,
            null,
            devUser["SystemRole"]);
    }
}
