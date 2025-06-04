using Case2;   
using AspNetCoreRateLimit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();      // ���������� ������� SignalR
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ������������ ���������� rate limiting (��������, � �������������� AspNetCoreRateLimit)
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Limit = 1000,
            Period = "1m"
        }
    };
});

// ���������� ������� rate limiting � ��������� ������ � ������
builder.Services.AddInMemoryRateLimiting();
// ����������� ������������ rate limiting
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddControllers();
var app = builder.Build();
app.UseIpRateLimiting();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();  // Подключаем все контроллеры
    endpoints.MapHub<ChatHub>("/chat");   
});

app.UseHttpsRedirection();
app.UseStatusCodePages();

app.UseExceptionHandler("/Error");
app.UseHsts(); // Включает HSTS для браузеров
app.Run();