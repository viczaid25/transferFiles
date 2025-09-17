using Microsoft.EntityFrameworkCore;
using transferFiles.Data;
using transferFiles.Options;
using Microsoft.Extensions.Options;



var builder = WebApplication.CreateBuilder(args);

// EF Core
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
