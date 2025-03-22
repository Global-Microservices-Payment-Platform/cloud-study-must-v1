using mpesaIntegration.Models.Authentication;
using mpesaIntegration.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace mpesaIntegration.Services
{
    /// <summary>
    /// Service handling authentication business logic
    /// </summary>
    public interface IAuthService
    {
        Task<AuthenticationResponse> RegisterAsync(RegisterRequest request);
        Task<AuthenticationResponse> LoginAsync(LoginRequest request);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Handles user registration with password hashing
        /// </summary>
        public async Task<AuthenticationResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _userRepository.GetUserByEmailAsync(request.Email) != null)
                return AuthenticationResponse.Failure("Email already registered");

            CreatePasswordHash(request.Password, out var hash, out var salt);

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = Convert.ToBase64String(hash), //convert  password hash to  string
                PasswordSalt = Convert.ToBase64String(salt), // convert salt to string
                MobileNumber = request.MobileNumber,
                Role = request.Role,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddUserAsync(user);
            return AuthenticationResponse.Success(user, "dummy-token", "refresh-token", DateTime.UtcNow.AddHours(1));
        }

        /// <summary>
        /// Handles user login with password verification
        /// </summary>
        public async Task<AuthenticationResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
                return AuthenticationResponse.Failure("User not found");

            if (!VerifyPasswordHash(request.Password, Convert.FromBase64String(user.PasswordHash), Convert.FromBase64String(user.PasswordSalt)))
                return AuthenticationResponse.Failure("Invalid password");

            return AuthenticationResponse.Success(user, "dummy-token", "refresh-token", DateTime.UtcNow.AddHours(1));
        }

        /// <summary>
        /// Generates password hash and salt using HMACSHA512
        /// </summary>
        private void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
        {
            using var hmac = new HMACSHA512();
            salt = hmac.Key;
            hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        /// <summary>
        /// Verifies password against stored hash and salt
        /// </summary>
        private bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            using var hmac = new HMACSHA512(storedSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(storedHash);
        }
    }
}