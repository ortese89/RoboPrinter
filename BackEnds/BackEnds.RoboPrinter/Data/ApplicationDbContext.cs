using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using BackEnds.RoboPrinter.Models;

namespace BackEnds.RoboPrinter.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public ApplicationDbContext()
    {
        
    }
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
    {
        WaitForConnection();

        if (Database.GetPendingMigrations().Any())
        {
            // Se vuoi eliminare database prima
            Database.EnsureDeleted();
            Database.Migrate();
        }

        if (!PointTypes.Any())
            ExecuteInitializationScript();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlServer();

    private void WaitForConnection()
    {
        bool canConnect = false;

        while (!canConnect)
        {
            try
            {
                string connectionString = new SqlConnectionStringBuilder(Database.GetConnectionString())
                {
                    InitialCatalog = "master"
                }.ConnectionString;
                using var connection = new SqlConnection(connectionString);
                connection.Open();
                canConnect = true;
            }
            catch
            {
                Thread.Sleep(1000);
            }
        }

    }

    private void ExecuteInitializationScript()
    {
        string scriptFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InitializationScript.sql");
        using StreamReader reader = new(scriptFileName);
        string sqlScript = reader.ReadToEnd();
        Database.ExecuteSqlRaw(sqlScript);
    }

    public DbSet<DataType> DataTypes { get; set; }
    public DbSet<OperativeMode> OperativeModes { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Label> Labels { get; set; }
    public DbSet<PointType> PointTypes { get; set; }
    public DbSet<RobotPoint> RobotPoints { get; set; }
    public DbSet<RouteType> RouteTypes { get; set; }
    public DbSet<Route> Routes { get; set; }
    public DbSet<RouteStep> RouteSteps { get; set; }
    public DbSet<History> Histories { get; set; }

}
