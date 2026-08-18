using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using transferFiles.Auth;
using transferFiles.Data;
using transferFiles.Diagnostics;
using transferFiles.Options;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Builder;



var builder = WebApplication.CreateBuilder(args);
var maxSize = 2L * 1024 * 1024 * 1024; // 2 GB

// EF Core
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = maxSize;
});

// Kestrel solo aplica cuando se corre en local (dotnet run). Bajo IIS con
// hostingModel="inprocess" el servidor es IISHttpServer y hay que subirle el
// límite por separado, o cualquier subida mayor a ~30 MB (el default) falla.
// El límite de IIS mismo va en web.config → requestLimits/maxAllowedContentLength.
builder.Services.Configure<IISServerOptions>(o =>
{
    o.MaxRequestBodySize = maxSize;
});

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = maxSize;
});


// Options TransferNow

builder.Services.Configure<TransferNowOptions>(
    builder.Configuration.GetSection("TransferNow"));

builder.Services.AddHttpContextAccessor();

// Handler con el proxy de salida de TransferNow:ProxyUrl (vacío = directo).
// Sin proxy, en servidores sin salida a internet por 443 la creación del link
// muere con SocketException 10060 después del timeout.
static HttpClientHandler CrearHandlerTransferNow(IServiceProvider sp)
{
    var opt = sp.GetRequiredService<IOptions<TransferNowOptions>>().Value;
    var handler = new HttpClientHandler();

    if (!string.IsNullOrWhiteSpace(opt.ProxyUrl))
    {
        handler.Proxy = new WebProxy(opt.ProxyUrl) { BypassProxyOnLocal = true };
        handler.UseProxy = true;
    }

    return handler;
}

builder.Services.AddHttpClient<TransferNowClient>((sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<TransferNowOptions>>().Value;
    http.BaseAddress = new Uri(opt.BaseUrl);
    http.DefaultRequestHeaders.Add("x-api-key", opt.ApiKey);
})
.ConfigurePrimaryHttpMessageHandler(CrearHandlerTransferNow);

// Cliente aparte para subir las partes a las URLs firmadas: mismo proxy, pero
// sin la cabecera x-api-key (esas URLs son de un almacenamiento externo tipo S3
// y no tienen por qué recibir nuestra llave). Antes se creaba un HttpClient
// nuevo por parte dentro del servicio, que además ignoraba el proxy.
builder.Services.AddHttpClient(TransferNowClient.UploadClientName, http =>
{
    http.Timeout = TimeSpan.FromMinutes(30); // partes grandes
})
.ConfigurePrimaryHttpMessageHandler(CrearHandlerTransferNow);

// Log de excepciones a archivo (ver Diagnostics/ErrorLog.cs: bajo IIS la carpeta
// de la app es de solo lectura, así que el log de ANCM no sirve).
var errorLog = ErrorLog.Crear(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(errorLog);

// ── Autenticación ──────────────────────────────────────────────
// Doble candado: el login de desarrollo solo existe si el entorno es
// Development Y Auth:DevLogin es true (en IIS el entorno es Production,
// así que el modo dev es imposible ahí aunque el flag se copie por error).
var devLoginActivo = builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue<bool>("Auth:DevLogin");

builder.Services.AddSingleton(new AuthMode(devLoginActivo));

if (devLoginActivo)
{
    builder.Services.AddDevAuth();
}
else
{
    builder.Services.AddHubAuth(builder.Configuration);
}

// Toda la app requiere usuario autenticado, salvo [AllowAnonymous] explícito
// (hoy AccountController para el login de desarrollo y Home/Error).
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ── Manejo de errores ──────────────────────────────────────────
// Sin esto, cualquier excepción no controlada sale como un 500 vacío y sin
// rastro en ninguna parte. Home/Error escribe el detalle al log y le da al
// usuario un id para correlacionar.
app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

if (!devLoginActivo)
{
    // Convierte el token SSO recibido por query (?meax_token=) en cookie de
    // sesión local y limpia la URL.
    app.UseHubSsoTokenBridge();
}

app.UseAuthorization();

app.MapDefaultControllerRoute().RequireAuthorization();

app.Logger.LogInformation(
    "transferFiles iniciado. Modo dev: {Dev}. Log de errores: {Log}",
    devLoginActivo,
    errorLog.Ruta ?? "(ninguna carpeta escribible)");

app.Run();
