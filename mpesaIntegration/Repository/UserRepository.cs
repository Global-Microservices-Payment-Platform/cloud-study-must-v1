using Microsoft.EntityFrameworkCore;
using mpesaIntegration.Data;
using mpesaIntegration.Models.Authentication;
using System.Threading.Tasks;

namespace mpesaIntegration.Repositories
{
    /// <summary>
    /// Repository for handling user data operations
    /// </summary>
    public interface IUserRepository
    {
        Task<User> AddUserAsync(User user);
        Task<User> GetUserByEmailAsync(string email);
        Task UpdateUserAsync(User user);
        Task SaveChangesAsync();
    }

    /// <summary>
    /// Implementation of user repository using Entity Framework Core
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Adds a new user to the database
        /// </summary>
        public async Task<User> AddUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        /// <summary>
        /// Retrieves a user by email address
        /// </summary>
        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <summary>
        /// Updates an existing user record
        /// </summary>
        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Persists changes to the database
        /// </summary>
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}