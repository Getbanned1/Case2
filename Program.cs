using SignalRApp;   
using AspNetCoreRateLimit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();      // ���������� ������� SignalR
builder.Services.AddMemoryCache();


// ����������� ����������� � ������

// ������������ ���������� rate limiting (��������, � �������������� AspNetCoreRateLimit)
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Limit = 100,
            Period = "1m"
        }
    };
});

// ���������� ������� rate limiting � ��������� ������ � ������
builder.Services.AddInMemoryRateLimiting();

// ����������� ������������ rate limiting
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();
app.UseIpRateLimiting();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseStatusCodePages();
app.MapHub<ChatHub>("/chat");   
// app.MapControllers();
app.UseExceptionHandler("/Error");
app.UseHsts(); // Включает HSTS для браузеров
app.Run();