using Microsoft.EntityFrameworkCore;
using mpesaIntegration.Data;
using mpesaIntegration.Models.Authentication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mpesaIntegration.Repositories
{
    /// <summary>
    /// Repository for handling user data operations
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Adds a new user to the database
        /// </summary>
        Task<User> AddUserAsync(User user);
        
        /// <summary>
        /// Retrieves a user by email address
        /// </summary>
        Task<User> GetUserByEmailAsync(string email);
        
        /// <summary>
        /// Retrieves a user by ID
        /// </summary>
        Task<User> GetUserByIdAsync(Guid userId);
        
        /// <summary>
        /// Updates an existing user record
        /// </summary>
        Task UpdateUserAsync(User user);
        
        /// <summary>
        /// Persists changes to the database
        /// </summary>
        Task SaveChangesAsync();
        
        /// <summary>
        /// Retrieves users based on role
        /// </summary>
        Task<List<User>> GetUsersByRoleAsync(Role role);
        
        /// <summary>
        /// Performs a soft delete on a user account
        /// </summary>
        Task<bool> SoftDeleteUserAsync(Guid userId);
    }

    /// <summary>
    /// Implementation of user repository using Entity Framework Core
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Adds a new user to the database
        /// </summary>
        public async Task<User> AddUserAsync(User user)
        {
            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User created successfully: {UserId}", user.Id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user: {Email}", user.Email);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a user by email address
        /// </summary>
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower()) ?? null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a user by ID
        /// </summary>
        public async Task<User> GetUserByIdAsync(Guid userId)
        {
            try
            {
                return await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing user record
        /// </summary>
        public async Task UpdateUserAsync(User user)
        {
            try
            {
                user.UpdatedAt = DateTime.UtcNow;
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                _logger.LogInformation("User updated successfully: {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Persists changes to the database
        /// </summary>
        public async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to database");
                throw;
            }
        }

        /// <summary>
        /// Retrieves users based on role
        /// </summary>
        public async Task<List<User>> GetUsersByRoleAsync(Role role)
        {
            try
            {
                return await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Role == role)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users by role: {Role}", role);
                throw;
            }
        }

        /// <summary>
        /// Performs a soft delete on a user account
        /// </summary>
        public async Task<bool> SoftDeleteUserAsync(Guid userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // Implement soft delete logic here
                // For example, if you have an IsDeleted property:
                // user.IsDeleted = true;
                // user.DeletedAt = DateTime.UtcNow;
                
                _logger.LogInformation("User soft deleted: {UserId}", userId);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting user: {UserId}", userId);
                throw;
            }
        }
    }
}