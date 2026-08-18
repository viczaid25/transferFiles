namespace transferFiles.Auth;

/// <summary>
/// Indica el modo de autenticación activo en esta instancia.
/// DevLoginActivo solo puede ser true si el entorno es Development
/// Y la config Auth:DevLogin es true (doble candado: en IIS/producción
/// el entorno es Production, por lo que el modo dev es imposible ahí
/// aunque el flag se copie por error).
/// </summary>
public sealed record AuthMode(bool DevLoginActivo);
