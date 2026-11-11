using Microsoft.AspNetCore.Authorization;

namespace transferFiles.Auth
{
    /// <summary>
    /// Quién es administrador de ESTE sistema.
    ///
    /// Fuente principal: el claim SystemRole del hub con valor "Admin" — es la
    /// forma correcta, porque la autorización vive en Meax One y no aquí.
    ///
    /// Fuente secundaria: la lista Admin:Logins de configuración. Existe porque
    /// hoy los usuarios de TFR en el hub están asignados sin rol, así que nadie
    /// recibiría el claim y la vista sería inalcanzable. En cuanto se asignen
    /// roles en Gestión de sistemas, esta lista se puede vaciar sin tocar código.
    /// </summary>
    public static class AdminPolicy
    {
        public const string Name = "EsAdministrador";
        public const string RolAdmin = "Admin";

        public static IServiceCollection AddAdminPolicy(this IServiceCollection services, IConfiguration config)
        {
            var logins = config.GetSection("Admin:Logins").Get<string[]>() ?? Array.Empty<string>();

            services.AddAuthorizationBuilder()
                .AddPolicy(Name, policy => policy.RequireAssertion(ctx => EsAdmin(ctx.User, logins)));

            // Para que las vistas puedan preguntar lo mismo sin repetir la regla.
            services.AddSingleton(new AdminLogins(logins));

            return services;
        }

        public static bool EsAdmin(System.Security.Claims.ClaimsPrincipal? user, IEnumerable<string> logins)
        {
            if (user?.Identity?.IsAuthenticated != true) return false;

            if (string.Equals(user.SystemRole(), RolAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var login = user.PcLoginId();

            return !string.IsNullOrWhiteSpace(login)
                && logins.Any(l => string.Equals(l, login, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>Lista de logins admin, para consultarla desde las vistas.</summary>
    public sealed record AdminLogins(string[] Logins)
    {
        public bool EsAdmin(System.Security.Claims.ClaimsPrincipal? user) => AdminPolicy.EsAdmin(user, Logins);
    }
}
