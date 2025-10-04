using Microsoft.AspNetCore.Identity;

namespace PortalAcademico.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

            const string rol = "Coordinador";
            if (!await roleManager.RoleExistsAsync(rol))
                await roleManager.CreateAsync(new IdentityRole(rol));

            const string email = "coordinador@demo.com";
            const string pass = "Passw0rd!"; // solo para demo

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                var create = await userManager.CreateAsync(user, pass);
                if (create.Succeeded)
                    await userManager.AddToRoleAsync(user, rol);
            }
            else
            {
                if (!await userManager.IsInRoleAsync(user, rol))
                    await userManager.AddToRoleAsync(user, rol);
            }
        }
    }
}
