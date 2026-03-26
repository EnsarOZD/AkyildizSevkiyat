using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Infrastructure.Persistence.Seeding
{
    public static class UserSeeder
    {
        public static async Task SeedAsync(IApplicationDbContext context, IPasswordHasher passwordHasher, string adminPassword)
        {
            if (!await context.Users.AnyAsync())
            {
                var users = new List<User>
                {
                    CreateUser("Admin", "User", "admin@akyildiz.com", UserRole.Admin, passwordHasher, adminPassword),
                    CreateUser("Fatma", "Yılmaz", "accounting@akyildiz.com", UserRole.Accounting, passwordHasher, adminPassword),
                    CreateUser("Mehmet", "Depo", "warehouse@akyildiz.com", UserRole.Warehouse, passwordHasher, adminPassword),
                    CreateUser("Ali", "Dağıtıcı", "dispatcher@akyildiz.com", UserRole.Dispatcher, passwordHasher, adminPassword)
                };

                context.Users.AddRange(users);
                await context.SaveChangesAsync(CancellationToken.None);
            }
        }

        private static User CreateUser(string firstName, string lastName, string email, UserRole role, IPasswordHasher passwordHasher, string password)
        {
            var hash = passwordHasher.CreateHash(password, out string salt);
            
            return User.Create(email, firstName, lastName, hash, salt, role);
        }
    }
}
