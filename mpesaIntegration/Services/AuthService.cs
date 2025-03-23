using mpesaIntegration.Models.Authentication;
using mpesaIntegration.Repositories;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace mpesaIntegration.Services
{
    /// <summary>
    /// Service handling authentication business logic
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user in the system
        /// </summary>
        Task<AuthenticationResponse> RegisterAsync(RegisterRequest request);
        
        /// <summary>
        /// Authenticates a user with their credentials
        /// </summary>
        Task<AuthenticationResponse> LoginAsync(LoginRequest request);
        
        /// <summary>
        /// Refreshes an expired JWT token using a valid refresh token
        /// </summary>
        Task<AuthenticationResponse> RefreshTokenAsync(string accessToken, string refreshToken);
        
        /// <summary>
        /// Revokes a user's refresh token, effectively logging them out
        /// </summary>
        Task<bool> RevokeTokenAsync(string userId);
        
        /// <summary>
        /// Gets a user's profile information
        /// </summary>
        Task<User> GetUserProfileAsync(string userId);
        
        /// <summary>
        /// Marks an account for deletion (soft delete)
        /// </summary>
        Task<bool> RequestAccountDeletionAsync(string userId);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository, 
            IJwtService jwtService,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _logger = logger;
        }

        /// <summary>
        /// Handles user registration with password hashing and initial token generation
        /// </summary>
        public async Task<AuthenticationResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Processing registration request for email: {Email}", request.Email);
                
                // Check if user already exists
                if (await _userRepository.GetUserByEmailAsync(request.Email) != null)
                {
                    _logger.LogWarning("Registration failed: Email {Email} already registered", request.Email);
                    return AuthenticationResponse.Failure("Email already registered");
                }

                // Create password hash and salt
                CreatePasswordHash(request.Password, out var hash, out var salt);

                // Generate a refresh token
                var refreshToken = _jwtService.GenerateRefreshToken();
                var refreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Refresh tokens typically last longer

                // Create new user entity
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    FullName = request.FullName,
                    Email = request.Email.ToLower().Trim(), // Normalize email
                    PasswordHash = Convert.ToBase64String(hash),
                    PasswordSalt = Convert.ToBase64String(salt),
                    MobileNumber = request.MobileNumber,
                    Role = request.Role,
                    CreatedAt = DateTime.UtcNow,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiryTime = refreshTokenExpiryTime,
                    IsEmailVerified = false // Requires verification
                };

                // Save user to database
                await _userRepository.AddUserAsync(user);
                
                // Generate JWT token
                (string token, DateTime expiration) = _jwtService.GenerateJwtToken(user);
                
                _logger.LogInformation("User registered successfully: {UserId}", user.Id);
                
                // Return success response with tokens
                return AuthenticationResponse.Success(user, token, refreshToken, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for email: {Email}", request.Email);
                return AuthenticationResponse.Failure("Registration failed due to an internal error");
            }
        }

        /// <summary>
        /// Handles user login with password verification and token generation
        /// </summary>
        public async Task<AuthenticationResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Processing login request for email: {Email}", request.Email);
                
                // Find user by email
                var user = await _userRepository.GetUserByEmailAsync(request.Email.ToLower().Trim());
                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found for email {Email}", request.Email);
                    return AuthenticationResponse.Failure("Invalid email or password");
                }

                // Verify password
                if (!VerifyPasswordHash(request.Password, Convert.FromBase64String(user.PasswordHash), Convert.FromBase64String(user.PasswordSalt)))
                {
                    _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);
                    return AuthenticationResponse.Failure("Invalid email or password");
                }

                // Generate new tokens
                var (token, expiration) = _jwtService.GenerateJwtToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();
                
                // Update user's refresh token
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                user.LastLoginAt = DateTime.UtcNow;
                
                // Save changes to database
                await _userRepository.UpdateUserAsync(user);
                
                _logger.LogInformation("User logged in successfully: {UserId}", user.Id);
                
                // Return success response with tokens
                return AuthenticationResponse.Success(user, token, refreshToken, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login for email: {Email}", request.Email);
                return AuthenticationResponse.Failure("Login failed due to an internal error");
            }
        }

        /// <summary>
        /// Refreshes an expired JWT token using a valid refresh token
        /// </summary>
public async Task<AuthenticationResponse> RefreshTokenAsync(string accessToken, string refreshToken)
{
    try
    {
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("Empty tokens in refresh request");
            return AuthenticationResponse.Failure("Invalid token request");
        }

        var principal = _jwtService.GetPrincipalFromExpiredToken(accessToken);
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            _logger.LogWarning("Invalid user ID in refresh token");
            return AuthenticationResponse.Failure("Invalid token");
        }

        var user = await _userRepository.GetUserByIdAsync(userGuid);
        if (user == null)
        {
            _logger.LogWarning("User not found during token refresh");
            return AuthenticationResponse.Failure("Invalid token");
        }

        if (user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            _logger.LogWarning("Invalid or expired refresh token for user {UserId}", userId);
            return AuthenticationResponse.Failure("Invalid refresh token");
        }

        // Generate new tokens
        var (newAccessToken, expiration) = _jwtService.GenerateJwtToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // Update user
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateUserAsync(user);

        return AuthenticationResponse.Success(user, newAccessToken, newRefreshToken, expiration);
    }
    catch (SecurityTokenException ex)
    {
        _logger.LogWarning(ex, "Security token validation failed during refresh");
        return AuthenticationResponse.Failure("Invalid security token");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during token refresh");
        return AuthenticationResponse.Failure("Token refresh failed");
    }
}
        /// <summary>
        /// Revokes a user's refresh token, effectively logging them out
        /// </summary>
        public async Task<bool> RevokeTokenAsync(string userId)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(Guid.Parse(userId));
                if (user == null)
                {
                    _logger.LogWarning("Token revocation failed: User not found for ID {UserId}", userId);
                    return false;
                }
                
                // Clear refresh token
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                
                // Save changes to database
                await _userRepository.UpdateUserAsync(user);
                
                _logger.LogInformation("Token revoked successfully for user: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token revocation for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Gets a user's profile information
        /// </summary>
        public async Task<User?> GetUserProfileAsync(string userId)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(Guid.Parse(userId));
                
                // For security, clear sensitive fields before returning
                if (user != null)
                {
                    user.PasswordHash = null;
                    user.PasswordSalt = null;
                    user.RefreshToken = null;
                }
                
                return user ?? null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile for user {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Marks an account for deletion (soft delete)
        /// </summary>
        public async Task<bool> RequestAccountDeletionAsync(string userId)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(Guid.Parse(userId));
                if (user == null)
                {
                    return false;
                }
                
                // Mark for deletion - implement your soft delete approach here
                // For example, you might set a DeleteRequested flag and timestamp
                
                // Here we'll just log it for demonstration
                _logger.LogInformation("Account deletion requested for user: {UserId}", userId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting account deletion for user {UserId}", userId);
                return false;
            }
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