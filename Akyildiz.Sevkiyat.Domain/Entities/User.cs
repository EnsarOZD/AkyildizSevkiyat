using Akyildiz.Sevkiyat.Domain.Enums;
using System;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class User
    {
        public int Id { get; private set; }
        public string Email { get; private set; } = string.Empty;
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;

        // Security
        public string PasswordHash { get; private set; } = string.Empty;
        public string PasswordSalt { get; private set; } = string.Empty;

        // Authorization
        public UserRole Role { get; private set; }
        public bool IsActive { get; private set; } = true;

        // Audit
        public DateTime CreatedAt { get; private set; }

        protected User() { }

        public static User Create(string email, string firstName, string lastName,
            string passwordHash, string passwordSalt, UserRole role)
        {
            return new User
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void UpdateProfile(string email, string firstName, string lastName)
        {
            Email = email;
            FirstName = firstName;
            LastName = lastName;
        }

        public void UpdateRole(UserRole newRole) => Role = newRole;

        public void ResetPassword(string newHash, string newSalt)
        {
            PasswordHash = newHash;
            PasswordSalt = newSalt;
        }

        public void SetActive(bool active) => IsActive = active;
    }
}
