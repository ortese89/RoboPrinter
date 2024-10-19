using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrinterAdapters.ApixAdapter;
using RobotAdapters.DobotAdapter;
using BackEnds.RoboPrinter.Data;
using BackEnds.RoboPrinter.Models;
using BackEnds.RoboPrinter.Services.IServices;
using UseCases.core;

namespace BackEnds.RoboPrinter.Services;

public static class ConfigurationService
{
    public static void ConfigureBackendServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")),
            ServiceLifetime.Singleton); 
        services.AddSingleton(new JwtOptions(configuration["JwtOptions:Secret"], configuration["JwtOptions:Issuer"], configuration["JwtOptions:Audience"]));

        services.AddSingleton<IRobotService, Dobot>();
        services.AddSingleton<IPrinterService, Apix>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<ViewModel>();
        services.AddSingleton<IOExternalCommunication>();
        services.AddSingleton<GPIOManager>();
        services.AddSingleton<ICycleService, CycleService>();

        string activeExternalDevice = configuration["ExternalDevices:Active"];

        switch (activeExternalDevice)
        {
            case "RS232":
                services.AddSingleton<IExternalDeviceCommunication, SerialExternalCommunication>();
                break;

            case "TCP/IP":
                services.AddSingleton<IExternalDeviceCommunication, TcpExternalCommunication>();
                break;

            default:
                services.AddSingleton<IExternalDeviceCommunication>(e => null); 
                break;
        }


        services.AddHostedService<Supervisor>();
        services.AddSingleton<Controller>();
    }
}
