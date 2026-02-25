using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Data;
using Microsoft.AspNetCore.Identity;
using Sistema_Ferreteria.Models.Seguridad;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Sistema_Ferreteria.Services.ITenantService, Sistema_Ferreteria.Services.TenantService>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Configurar Entity Framework con PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configurar Autenticación por Cookies
builder.Services.AddAuthentication("FerreteriaAuth")
    .AddCookie("FerreteriaAuth", options =>
    {
        options.LoginPath = "/Cuentas/Login";
        options.AccessDeniedPath = "/Cuentas/AccesoDenegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// Configurar Localización
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var app = builder.Build();

// Configurar cultura por defecto (Español)
var supportedCultures = new[] { "es-NI", "es-ES", "es" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    await DbInitializer.Seed(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Seed Multi-Tenancy
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try 
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Ensure Default tenant exists
        if (!context.Tenants.Any(t => t.IdTenant == "Default"))
        {
            context.Tenants.Add(new Sistema_Ferreteria.Models.Seguridad.Tenant
            {
                IdTenant = "Default",
                Nombre = "Ferretería Principal",
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });
            context.SaveChanges();
        }
        try 
        {
            context.Database.ExecuteSqlRaw("UPDATE \"Usuarios\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"Roles\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"UsuarioRol\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"RolPermiso\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"Permisos\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"Productos\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"Categorias\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"UnidadesMedida\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"Presentaciones\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"MovimientosInventario\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"Ventas\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"DetalleVenta\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"Clientes\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"Proveedores\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"Compras\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"DetalleCompra\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
            context.Database.ExecuteSqlRaw("UPDATE \"Configuracion\" SET \"TenantId\" = 'Default' WHERE \"TenantId\" = '' OR \"TenantId\" IS NULL");
        }
        catch (Exception ex)
        {
            // If tables don't have TenantId yet (migration not applied), this will fail silently
            Console.WriteLine($"Nota: No se pudo actualizar TenantId en algunas tablas: {ex.Message}");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
