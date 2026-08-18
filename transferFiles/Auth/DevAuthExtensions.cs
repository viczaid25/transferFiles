using Microsoft.AspNetCore.Authentication.Cookies;

namespace transferFiles.Auth;

/// <summary>
/// Autenticación de desarrollo: cookie con login simplificado en /Account/Login
/// (usuario libre + contraseña fija Auth:DevPassword). Emite exactamente los
/// mismos claims que el JWT del hub, tomados de Auth:DevUser en
/// appsettings.Development.json. Solo se cablea cuando el entorno es
/// Development Y Auth:DevLogin es true.
/// </summary>
public static class DevAuthExtensions
{
    public static IServiceCollection AddDevAuth(this IServiceCollection services)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/Login";
                options.Cookie.Name = HubAuthExtensions.LocalCookieName;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });

        return services;
    }
}
