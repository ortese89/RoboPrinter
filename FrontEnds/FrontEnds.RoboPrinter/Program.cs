#region Imports

using BackEnds.RoboPrinter.Services;
using FrontEnds.RoboPrinter.Components;
using FrontEnds.RoboPrinter.Data;
using Serilog;

#endregion

var builder = WebApplication.CreateBuilder(args);
Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();
builder.Services.AddLocalization();
builder.Services.AddControllers();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(e => {
        e.DetailedErrors = true;
    });

builder.Services.AddSingleton<UserData>();
builder.Services.ConfigureBackendServices(builder.Configuration);

var app = builder.Build();
Log.Information("Test log - applicazione avviata");
string[] supportedCultures = ["it-IT", "en-GB", "ar-AE"];
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);
//app.UseRequestLocalization("it-IT");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

//app.UseAuthentication();
//app.UseAuthorization();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
