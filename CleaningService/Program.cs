using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CleaningService.Services;
using CleaningService.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Добавляем поддержку контроллеров с представлениями (MVC)
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// Регистрация сервисов
builder.Services.AddSingleton<IVehicleRegistry, VehicleRegistry>();
builder.Services.AddSingleton<ICapacityService, CapacityService>();
builder.Services.AddSingleton<ICommModeService, CommModeService>();
builder.Services.AddScoped<IGroundControlClient, GroundControlClient>();
builder.Services.AddScoped<ICleaningProcessService, CleaningProcessService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрируем SignalR
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseHttpsRedirection();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Index}/{id?}");

// Маппинг хаба для обновления статуса транспортных средств
app.MapHub<VehicleStatusHub>("/vehiclestatushub");

app.Run();
