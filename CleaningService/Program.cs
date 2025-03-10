using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CleaningService.Services;
using CleaningService.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ��������� ��������� ������������ � ��������������� (MVC)
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// ����������� ��������
builder.Services.AddSingleton<IVehicleRegistry, VehicleRegistry>();
builder.Services.AddSingleton<ICapacityService, CapacityService>();
builder.Services.AddSingleton<ICommModeService, CommModeService>();
builder.Services.AddScoped<IGroundControlClient, GroundControlClient>();
builder.Services.AddScoped<ICleaningProcessService, CleaningProcessService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ������������ SignalR
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

// ������� ���� ��� ���������� ������� ������������ �������
app.MapHub<VehicleStatusHub>("/vehiclestatushub");

app.Run();
