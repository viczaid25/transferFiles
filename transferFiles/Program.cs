using Microsoft.EntityFrameworkCore;
using transferFiles.Data;
using transferFiles.Options;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.Features;



var builder = WebApplication.CreateBuilder(args);
var maxSize = 2L * 1024 * 1024 * 1024; // 2 GB

// EF Core
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = maxSize;
});

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = maxSize;
});


// Options TransferNow

builder.Services.Configure<TransferNowOptions>(
    builder.Configuration.GetSection("TransferNow"));

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<TransferNowClient>((sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<TransferNowOptions>>().Value;
    http.BaseAddress = new Uri(opt.BaseUrl);
    http.DefaultRequestHeaders.Add("x-api-key", opt.ApiKey);
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseStaticFiles();
app.MapDefaultControllerRoute();
app.Run();
