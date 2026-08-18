using System.Security.Claims;

namespace transferFiles.Auth;

/// <summary>
/// Perfil de usuario según el contrato con meaxHub (Meax One).
/// EmployeeId (nómina, 4-5 dígitos) puede venir vacío si AD no la tiene registrada.
/// </summary>
public sealed record HubUserInfo(
    string PcLoginId,
    string? DisplayName,
    string? Department,
    string? Position,
    string? EmployeeId,
    string? SystemRole);

/// <summary>
/// Nombres de claims del contrato con meaxHub (Meax One).
/// El resto de la aplicación debe consumir únicamente estos claims — nunca AD ni la BD del hub.
/// </summary>
public static class MeaxClaims
{
    // Claims que trae el JWT del hub
    public const string Sub = "sub";
    public const string PcLoginId = "PcLoginId";
    public const string DisplayName = "DisplayName";
    public const string Department = "Department";
    public const string Position = "Position";
    public const string EmployeeId = "EmployeeId";
    public const string SystemRole = "SystemRole";

    /// <summary>
    /// Construye la identidad con el mismo juego de claims sin importar el modo
    /// (hub o dev), para que el resto de la app no tenga ni un solo if de modo.
    /// </summary>
    public static ClaimsIdentity CrearIdentidad(
        string authenticationScheme,
        HubUserInfo info)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, info.PcLoginId),
            new(PcLoginId, info.PcLoginId)
        };

        AgregarSiTieneValor(claims, DisplayName, info.DisplayName ?? info.PcLoginId);
        AgregarSiTieneValor(claims, Department, info.Department);
        AgregarSiTieneValor(claims, Position, info.Position);
        // EmployeeId puede venir vacío si AD no tiene la nómina registrada
        AgregarSiTieneValor(claims, EmployeeId, info.EmployeeId);
        AgregarSiTieneValor(claims, SystemRole, info.SystemRole);

        return new ClaimsIdentity(
            claims,
            authenticationScheme,
            nameType: DisplayName,
            roleType: SystemRole);
    }

    private static void AgregarSiTieneValor(List<Claim> claims, string tipo, string? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor))
        {
            claims.Add(new Claim(tipo, valor.Trim()));
        }
    }
}

/// <summary>
/// Accesos de conveniencia a los claims del contrato. Es el único punto por el
/// que la app lee la identidad del usuario (controladores y vistas).
/// </summary>
public static class MeaxPrincipalExtensions
{
    /// <summary>Login de red del usuario (claim PcLoginId, con fallback a sub).</summary>
    public static string? PcLoginId(this ClaimsPrincipal? user)
        => Valor(user, MeaxClaims.PcLoginId) ?? Valor(user, MeaxClaims.Sub);

    /// <summary>Nombre para mostrar; cae al login de red si el hub no lo mandó.</summary>
    public static string? NombreParaMostrar(this ClaimsPrincipal? user)
        => Valor(user, MeaxClaims.DisplayName) ?? user.PcLoginId();

    public static string? Department(this ClaimsPrincipal? user)
        => Valor(user, MeaxClaims.Department);

    public static string? Position(this ClaimsPrincipal? user)
        => Valor(user, MeaxClaims.Position);

    /// <summary>Nómina (4-5 dígitos). Puede ser null si AD no la tiene registrada.</summary>
    public static string? EmployeeId(this ClaimsPrincipal? user)
        => Valor(user, MeaxClaims.EmployeeId);

    /// <summary>Rol del usuario en ESTE sistema. Solo viene en tokens SSO.</summary>
    public static string? SystemRole(this ClaimsPrincipal? user)
        => Valor(user, MeaxClaims.SystemRole);

    private static string? Valor(ClaimsPrincipal? user, string tipo)
    {
        var valor = user?.FindFirst(tipo)?.Value;
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
