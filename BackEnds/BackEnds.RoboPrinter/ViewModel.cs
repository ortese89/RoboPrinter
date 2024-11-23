using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BackEnds.RoboPrinter.Data;
using BackEnds.RoboPrinter.Models;

namespace BackEnds.RoboPrinter;

public class ViewModel
{
    #region Private Properties

    private readonly ApplicationDbContext _context;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    #endregion

    #region Public Properties

    public int ActiveProductId = 0;

    public int RobotOverride = 0;

    #endregion

    public ViewModel(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task AddNewHistory(int productId, string serialNumber, int activeOperativeModeId)
    {
        var newHistory = new History()
        {
            ProductId = productId,
            SerialNumber = serialNumber,
            OperativeModeId = activeOperativeModeId
        };

        await _semaphore.WaitAsync();
        try
        {
            await _context.Histories.AddAsync(newHistory);
            await _context.SaveChangesAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<int> AddNewProduct(string productName)
    {
        var newProduct = new Product
        {
            Description = productName
        };

        await _semaphore.WaitAsync();
        try
        {
            await _context.Products.AddAsync(newProduct);
            await _context.SaveChangesAsync();
            return newProduct.Id;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task AppendNewRouteStep(RouteStep routeStep)
    {
        await _semaphore.WaitAsync();
        try
        {
            var subsequentRouteSteps = _context.RouteSteps
                .Where(step => step.RouteId == routeStep.RouteId &&
                               step.StepOrder >= routeStep.StepOrder)
                .ExecuteUpdateAsync(step => step.SetProperty(x => x.StepOrder, x => x.StepOrder + 1));
            await _context.RouteSteps.AddAsync(routeStep);
            await _context.SaveChangesAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task CloneLabelByProductId(int productId, int newProductId)
    {
        var label = await GetLabel(productId);

        if (label is null) return;

        var newLabel = new Label()
        {
            ProductId = newProductId,
            Content = label.Content
        };

        await _semaphore.WaitAsync();
        try
        {

            await _context.Labels.AddAsync(newLabel);
            await _context.SaveChangesAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task CloneRoutesByProductId(int productId, int newProductId)
    {
        var routes = await GetRoutes(productId);

        if (routes.Length == 0) return;

        await _semaphore.WaitAsync();
        try
        {
            foreach (var route in routes)
            {
                var newRoute = new Route()
                {
                    ProductId = newProductId,
                    RouteTypeId = route.RouteTypeId
                };

                List<RouteStep> routeSteps = [];

                foreach (var routeStep in route.RouteSteps)
                {
                    var robotPoint = new RobotPoint()
                    {
                        X = routeStep.RobotPoint.X,
                        Y = routeStep.RobotPoint.Y,
                        Z = routeStep.RobotPoint.Z,
                        Yaw = routeStep.RobotPoint.Yaw,
                        Pitch = routeStep.RobotPoint.Pitch,
                        Roll = routeStep.RobotPoint.Roll,
                        PointTypeId = routeStep.RobotPoint.PointTypeId
                    };

                    var newRouteStep = new RouteStep()
                    {
                        RobotPoint = robotPoint,
                        StepOrder = routeStep.StepOrder,
                        Speed = routeStep.Speed,
                        ClearZone = routeStep.ClearZone
                    };

                    routeSteps.Add(newRouteStep);
                }

                newRoute.RouteSteps = routeSteps;
                await _context.Routes.AddAsync(newRoute);
            }

            await _context.SaveChangesAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task CloneProduct(int productId, string newProductName)
    {
        var product = GetProductById(productId);

        if (product is null) return;

        int newProductId = await AddNewProduct(newProductName);
        await CloneLabelByProductId(productId, newProductId);
        await CloneRoutesByProductId(productId, newProductId);
    }

    public async Task DeleteProduct(int productId)
    {
        await _semaphore.WaitAsync();
        try
        {
            await _context.Products
                .Where(p => p.Id == productId)
                .ExecuteDeleteAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteRobotPoint(int robotPointId)
    {
        await _semaphore.WaitAsync();
        try
        {
            await _context.RobotPoints
                .Where(x => x.Id == robotPointId)
                .ExecuteDeleteAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteRouteStep(int routeStepId)
    {
        await _semaphore.WaitAsync();
        try
        {
            await _context.RouteSteps
                .Where(x => x.Id == routeStepId)
                .ExecuteDeleteAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task<int> GetActiveOperativeMode()
    {
        await _semaphore.WaitAsync();
        try
        {
            var appSetting = await _context.AppSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Description == "ActiveOperativeMode");

            return Convert.ToInt16(appSetting?.Value);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int> GetActiveProduct()
    {
        await _semaphore.WaitAsync();
        try
        {
            var appSetting = await _context.AppSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Description == "ActiveProduct");

            ActiveProductId = Convert.ToInt16(appSetting?.Value);
            return ActiveProductId;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> GetDigitalIOSignalsConfiguration()
    {
        await _semaphore.WaitAsync();
        try
        {
            var appSetting = await _context.AppSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Description == "DigitalIOSignalsEnabled");

            return Convert.ToBoolean(appSetting?.Value);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task<bool> GetCycleConfiguration()
    {
        await _semaphore.WaitAsync();
        try
        {
            var appSetting = await _context.AppSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Description == "ExecuteEntireCycleEnabled");

            return Convert.ToBoolean(appSetting?.Value);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Label?> GetLabel(int productId)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.Labels
                .AsNoTracking()
                .Where(x => x.ProductId == productId)
                .FirstOrDefaultAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string?> GetLabelContent(int productId)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.Labels
                .AsNoTracking()
                .Where(x => x.ProductId == productId)
                .Select(x => x.Content)
                .FirstOrDefaultAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Label[]> GetLabels()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.Labels
                .AsNoTracking()
                .Include(x => x.Product)
                .ToArrayAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int> GetLastExecutedRoute()
    {
        await _semaphore.WaitAsync();
        try
        {
            var lastExecutedRoute = await _context.AppSettings
                .AsNoTracking()
                .Where(a => a.Description == "LastExecutedRoute")
                .Select(a => a.Value)
                .FirstOrDefaultAsync();

            if (lastExecutedRoute is not null && int.TryParse(lastExecutedRoute, out int lastRouteId))
            {
                return lastRouteId;
            }

            return 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }
  
    public async Task<int> GetLastExecutedRouteStep()
    {
        await _semaphore.WaitAsync();
        try
        {
            var lastExecutedRouteStep = await _context.AppSettings
                .AsNoTracking()
                .Where(a => a.Description == "LastExecutedRouteStep")
                .Select(a => a.Value)
                .FirstOrDefaultAsync();

            if (lastExecutedRouteStep is not null && 
                int.TryParse(lastExecutedRouteStep, out int lastRouteStepId))
            {
                return lastRouteStepId;
            }

            return 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<OperativeMode[]> GetOperativeModes()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.OperativeModes
                .AsNoTracking()
                .ToArrayAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task<Product?> GetProductById(int id)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.Products
                .FirstOrDefaultAsync(x => x.Id == id);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public async Task<Product?> GetProductByDescription(string description)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.Products
                .FirstOrDefaultAsync(x => x.Description == description);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<RouteStep[]> GetRobotRoute(int routeId)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.RouteSteps
                .AsNoTracking()
                .Include(x => x.RobotPoint)
                    .ThenInclude(x => x.PointType)
                .Where(x => x.RouteId == routeId)
                .OrderBy(x => x.StepOrder)
                .ToArrayAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }
   
    public async Task<RouteStep[]> GetRobotPickupRoute(int productId)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.RouteSteps
                .AsNoTracking()
                .Include(x => x.RobotPoint)
                    .ThenInclude(x => x.PointType)
                .Where(x => x.Route.ProductId == productId &&
                            x.Route.RouteType.Description == "Pickup Route")
                .OrderBy(x => x.StepOrder)
                .ToArrayAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<RouteStep[]> GetRobotApplicationRoute(int productId)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.RouteSteps
                .AsNoTracking()
                .Include(x => x.RobotPoint)
                    .ThenInclude(x => x.PointType)
                .Where(x => x.Route.ProductId == productId &&
                            x.Route.RouteType.Description == "Application Route")
                .OrderBy(x => x.StepOrder)
                .ToArrayAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<RobotPosition> GetPickupRouteHomePosition(int productId)
    {
        var robotPosition = new RobotPosition();

        await _semaphore.WaitAsync();
        try
        {

            var robotPoint = await _context.RouteSteps
                .AsNoTracking()
                .Include(x => x.RobotPoint)
                    .ThenInclude(x => x.PointType)
                .Where(x => x.Route.ProductId == productId &&
                            x.Route.RouteType.Description == "Pickup Route" &&
                            x.RobotPoint.PointType.Description == "Home")
                .Select(x => x.RobotPoint)
                .FirstOrDefaultAsync();

            if (robotPoint is not null)
            {
                robotPosition.X = robotPoint.X;
                robotPosition.Y = robotPoint.Y;
                robotPosition.Z = robotPoint.Z;
                robotPosition.Yaw = robotPoint.Yaw;
                robotPosition.Pitch = robotPoint.Pitch;
                robotPosition.Roll = robotPoint.Roll;
            }

            return robotPosition;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<RobotPosition> GetApplicationRouteHomePosition(int productId)
    {
        var robotPosition = new RobotPosition();

        await _semaphore.WaitAsync();
        try
        {

            var robotPoint = await _context.RouteSteps
                .AsNoTracking()
                .Include(x => x.RobotPoint)
                    .ThenInclude(x => x.PointType)
                .Where(x => x.Route.ProductId == productId &&
                            x.Route.RouteType.Description == "Application Route" &&
                            x.RobotPoint.PointType.Description == "Home")
                .Select(x => x.RobotPoint)
                .FirstOrDefaultAsync();

            if (robotPoint is not null)
            {
                robotPosition.X = robotPoint.X;
                robotPosition.Y = robotPoint.Y;
                robotPosition.Z = robotPoint.Z;
                robotPosition.Yaw = robotPoint.Yaw;
                robotPosition.Pitch = robotPoint.Pitch;
                robotPosition.Roll = robotPoint.Roll;
            }

            return robotPosition;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int> GetRobotOverride()
    {
        await _semaphore.WaitAsync();
        try
        {
            var appSetting = await _context.AppSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Description == "RobotOverride");

            RobotOverride = Convert.ToInt16(appSetting?.Value);
            return RobotOverride;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<History[]> GetAllHistories()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.Histories
                .AsNoTracking()
                .ToArrayAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<PointType[]> GetPointTypes()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.PointTypes
                .AsNoTracking()
                .ToArrayAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Product[]> GetProducts()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.Products
                .AsNoTracking()
                .ToArrayAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Route?> GetRoute(int routeId)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.Routes
                .AsNoTracking()
                .Include(x => x.RouteSteps)
                    .ThenInclude(x => x.RobotPoint)
                .FirstOrDefaultAsync(x => x.Id == routeId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int?> GetRouteIdByRouteStepId(int routeStepId)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.RouteSteps
                .AsNoTracking()
                .Where(x => x.Id == routeStepId)
                .Select(x => x.Route.Id)
                .FirstOrDefaultAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Route[]> GetRoutes(int productId)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.Routes
                .AsNoTracking()
                .Include(x => x.RouteSteps)
                    .ThenInclude(x => x.RobotPoint)
                .Where(x => x.ProductId == productId)
                .ToArrayAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<RouteStep[]> GetRouteSteps(int productId, int routeTypeId)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.RouteSteps
                .AsNoTracking()
                .Include(x => x.RobotPoint)
                    .ThenInclude(x => x.PointType)
                .Where(x => x.Route.RouteTypeId == routeTypeId &&
                            x.Route.ProductId == productId)
                .ToArrayAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public async Task<RouteStep[]> GetRouteStepsByProduct(int productId)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.RouteSteps
                .AsNoTracking()
                .Include(x => x.RobotPoint)
                    .ThenInclude(x => x.PointType)
                .Where(x => x.Route.ProductId == productId)
                .ToArrayAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<RouteType[]> GetRouteTypes()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _context.RouteTypes
                .AsNoTracking()
                .ToArrayAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<History[]> GetFilteredHistories(HistoryFilters filters)
    {
        await _semaphore.WaitAsync();
        try
        {
            var query = _context.Histories
                .AsNoTracking()
                .Include(x => x.Product)
                .Include(x => x.OperativeMode)
                .AsQueryable();

            if (filters.ProductId != 0)
                query = query.Where(x => x.ProductId == filters.ProductId);

            if (filters.OperativeModeId != 0)
                query = query.Where(x => x.OperativeModeId == filters.OperativeModeId);

            if (!string.IsNullOrEmpty(filters.SerialNumber))
                query = query.Where(x => x.SerialNumber.Contains(filters.SerialNumber));

            if (filters.PickupTimeFrom.HasValue)
                query = query.Where(x => x.PickupTime >= filters.PickupTimeFrom.Value);

            if (filters.PickupTimeTo.HasValue)
                query = query.Where(x => x.PickupTime <= filters.PickupTimeTo.Value);

            if (filters.ApplicationTimeFrom.HasValue)
                query = query.Where(x => x.ApplicationTime >= filters.ApplicationTimeFrom.Value);

            if (filters.ApplicationTimeTo.HasValue)
                query = query.Where(x => x.ApplicationTime <= filters.ApplicationTimeTo.Value);

            return await query
              .Skip((filters.StartIndex - 1) * filters.Size)
              .Take(filters.Size)
              .ToArrayAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteFilteredHistories(HistoryFilters filters)
    {
        await _semaphore.WaitAsync();
        try
        {
            var query = _context.Histories.AsQueryable();

            if (filters.ProductId > 0)
            {
                query = query.Where(h => h.ProductId == filters.ProductId);
            }

            if (!string.IsNullOrEmpty(filters.SerialNumber))
            {
                query = query.Where(h => h.SerialNumber.Contains(filters.SerialNumber));
            }

            if (filters.PickupTimeFrom.HasValue)
            {
                query = query.Where(h => h.PickupTime >= filters.PickupTimeFrom.Value);
            }

            if (filters.PickupTimeTo.HasValue)
            {
                query = query.Where(h => h.PickupTime <= filters.PickupTimeTo.Value);
            }

            if (filters.ApplicationTimeFrom.HasValue)
            {
                query = query.Where(h => h.ApplicationTime >= filters.ApplicationTimeFrom.Value);
            }

            if (filters.ApplicationTimeTo.HasValue)
            {
                query = query.Where(h => h.ApplicationTime <= filters.ApplicationTimeTo.Value);
            }

            _context.Histories.RemoveRange(query);
            await _context.SaveChangesAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<(IdentityUser User, IList<string> Roles)> Login(string username, string password)
    {
        await _semaphore.WaitAsync();
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName.ToLower() == username.ToLower());

            if (user == null)
            {
                return (null, null);
            }

            var passwordHasher = new PasswordHasher<IdentityUser>();
            try
            {
                var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
                if (result == PasswordVerificationResult.Failed)
                {
                    return (null, null);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var roles = await _context.UserRoles
                   .Where(ur => ur.UserId == user.Id)
                   .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                   .ToListAsync();

            return (user, roles);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveActiveOperativeMode(int operativeModeId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var appSetting = await _context.AppSettings
                .FirstOrDefaultAsync(x => x.Description == "ActiveOperativeMode");

            if (appSetting is not null)
            {
                appSetting.Value = operativeModeId.ToString();
                await _context.SaveChangesAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveActiveProduct(int activeProduct)
    {
        await _semaphore.WaitAsync();
        try
        {
            var appSetting = await _context.AppSettings
                .FirstOrDefaultAsync(x => x.Description == "ActiveProduct");

            if (appSetting is not null)
            {
                appSetting.Value = activeProduct.ToString();
                await _context.SaveChangesAsync();
                ActiveProductId = activeProduct;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveLastExecutedRoute(int routeId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var appSetting = await _context.AppSettings
                .FirstOrDefaultAsync(x => x.Description == "LastExecutedRoute");

            if (appSetting is not null)
            {
                appSetting.Value = routeId.ToString();
                await _context.SaveChangesAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveLastExecutedRouteStep(int routeStepId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var appSetting = await _context.AppSettings
                .FirstOrDefaultAsync(x => x.Description == "LastExecutedRouteStep");

            if (appSetting is not null)
            {
                appSetting.Value = routeStepId.ToString();
                await _context.SaveChangesAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task SaveRobotOverride(int robotOverride)
    {
        await _semaphore.WaitAsync();
        try
        {
            var appSetting = await _context.AppSettings
                .FirstOrDefaultAsync(x => x.Description == "RobotOverride");

            if (appSetting is not null)
            {
                appSetting.Value = robotOverride.ToString();
                await _context.SaveChangesAsync();
                RobotOverride = robotOverride;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveDigitalIOSignalsConfiguration(bool enabled)
    {
        await _semaphore.WaitAsync();
        try
        {
            var appSetting = await _context.AppSettings
                .FirstOrDefaultAsync(x => x.Description == "DigitalIOSignalsEnabled");

            if (appSetting is not null)
            {
                appSetting.Value = enabled.ToString();
                await _context.SaveChangesAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveCycleConfiguration(bool enabled)
    {
        await _semaphore.WaitAsync();
        try
        {
            var appSetting = await _context.AppSettings
                .FirstOrDefaultAsync(x => x.Description == "ExecuteEntireCycleEnabled");

            if (appSetting is not null)
            {
                appSetting.Value = enabled.ToString();
                await _context.SaveChangesAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateApplicationTime(int productId, string serialNumber)
    {
        await _semaphore.WaitAsync();
        try
        {
            var history = await _context.Histories
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.SerialNumber == serialNumber);

            if (history is not null)
            {
                history.ApplicationTime = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateLabelContent(int labelId, string content)
    {
        await _semaphore.WaitAsync();
        try
        {
            var label = await _context.Labels.FirstOrDefaultAsync(x => x.Id == labelId);

            if (label is not null)
            {
                label.Content = content;
                await _context.SaveChangesAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdatePickupTime(int productId, string serialNumber)
    {
        await _semaphore.WaitAsync();
        try
        {
            var history = await _context.Histories.FirstOrDefaultAsync(x => x.ProductId == productId && x.SerialNumber == serialNumber);

            if (history is not null)
            {
                history.PickupTime = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateRobotPoint(int robotPointId, RobotPosition robotPosition)
    {
        await _semaphore.WaitAsync();
        try
        {
            var robotPoint = await _context.RobotPoints.FirstOrDefaultAsync(x => x.Id == robotPointId);

            if (robotPoint is not null)
            {
                robotPoint.X = robotPosition.X;
                robotPoint.Y = robotPosition.Y;
                robotPoint.Z = robotPosition.Z;
                robotPoint.Yaw = robotPosition.Yaw;
                robotPoint.Pitch = robotPosition.Pitch;
                robotPoint.Roll = robotPosition.Roll;
                await _context.SaveChangesAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateRouteStep(int routeStepId, int speed, bool clearZone)
    {
        await _semaphore.WaitAsync();
        try
        {
            var routeStep = await _context.RouteSteps.FirstOrDefaultAsync(x => x.Id == routeStepId);

            if (routeStep is not null)
            {
                routeStep.Speed = speed;
                routeStep.ClearZone = clearZone;
                await _context.SaveChangesAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
