using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using chessPairingSystem.Areas.Identity.Data;
using chessPairingSystem.Models;

namespace chessPairingSystem
{
    public class Program
    {
        public static async Task Main(string[] args)  
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("chessPairingSystemContextConnection") ?? throw new InvalidOperationException("Connection string 'chessPairingSystemContextConnection' not found.");

            builder.Services.AddDbContext<chessPairingSystemContext>(options => options.UseSqlServer(connectionString));

            builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddRoles<IdentityRole>()  
                .AddEntityFrameworkStores<chessPairingSystemContext>();

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Initialize database and seed data
            await DbInitializer.Initialize(app.Services);  

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();
            app.MapRazorPages();
            app.Run();
        }
    }
}