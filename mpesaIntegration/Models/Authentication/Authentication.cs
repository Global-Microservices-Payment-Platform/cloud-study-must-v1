using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace mpesaIntegration.Models.Authentication
{
    /// <summary>
    /// Defines the possible user roles within the system.
    /// </summary>
    public enum Role
    {
        /// <summary>
        /// Standard individual user account
        /// </summary>
        Individual = 1,

        /// <summary>
        /// Business or organization account with extended privileges
        /// </summary>
        Business = 2
    }

    /// <summary>
    /// Represents a user in the authentication system.
    /// Maps directly to the users table in the database.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique identifier for the user
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User's full name as it appears on their national ID
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        /// <summary>
        /// User's email address, used as username for authentication
        /// </summary>
        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; }

        /// <summary>
        /// Hashed password - never store plain text passwords
        /// </summary>
        [Required]
        public string PasswordHash { get; set; }

        /// <summary>
        /// Random salt used in password hashing for additional security
        /// </summary>
        [Required]
        public string PasswordSalt { get; set; }

        /// <summary>
        /// User's mobile number for verification and communication
        /// </summary>
        [Required]
        [Phone]
        [MaxLength(20)]
        public string MobileNumber { get; set; }

        /// <summary>
        /// User's role in the system (Individual or Business)
        /// </summary>
        [Required]
        public Role Role { get; set; }

        /// <summary>
        /// Flag indicating whether the account email has been verified
        /// </summary>
        public bool IsEmailVerified { get; set; }

        /// <summary>
        /// Date and time when the user account was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date and time of the last update to the user account
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Date and time of the user's last login
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// JWT refresh token for maintaining persistent sessions
        /// </summary>
        /// 
        [MaxLength(500)]
        public string? RefreshToken { get; set; } //set to nullable

        /// <summary>
        /// Expiration date for the refresh token
        /// </summary>
        public DateTime? RefreshTokenExpiryTime { get; set; }

    }

    /// <summary>
    /// Data transfer object for login requests.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// User's email address used for authentication
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        /// <summary>
        /// User's password (plain text in request, never stored)
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }
    }

    /// <summary>
    /// Data transfer object for user registration requests.
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// User's full name as it appears on their national ID
        /// </summary>
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        public string FullName { get; set; } = null!;

        /// <summary>
        /// User's email address, will be used as username for authentication
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;

        /// <summary>
        /// User's desired password (plain text in request, will be hashed before storage)
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character")]
        public string Password { get; set; } = null!;

        /// <summary>
        /// Confirmation of the user's password to prevent typos
        /// </summary>
        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        /// <summary>
        /// User's mobile number for verification and communication
        /// </summary>
        [Required(ErrorMessage = "Mobile number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string MobileNumber { get; set; }

        /// <summary>
        /// User's role in the system (Individual or Business)
        /// </summary>
        [Required(ErrorMessage = "Role is required")]
        public Role Role { get; set; }
    }

    /// <summary>
    /// Response object returned after authentication operations.
    /// Contains authentication results and relevant user information.
    /// </summary>
    /// 
    /// <summary>
    /// Data transfer object for refresh token requests
    /// </summary>
    public class RefreshTokenRequest
    {
        /// <summary>
        /// Expired JWT access token
        /// </summary>
        [Required(ErrorMessage = "Access token is required")]
        public string AccessToken { get; set; }

        /// <summary>
        /// Valid refresh token to get new access token
        /// </summary>
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; }
    }
    /// <summary>
    /// Data transfer object for user profile responses
    /// </summary>
    public class UserProfileResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string MobileNumber { get; set; }
        public Role Role { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Calculated property showing account age
        /// </summary>
        public TimeSpan AccountAge => DateTime.UtcNow - CreatedAt;
    }
    public class AuthenticationResponse
    {
        /// <summary>
        /// Indicates whether the authentication operation was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Authentication message (success confirmation or error details)
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// JWT access token for authenticated requests
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Token expiration time in UTC
        /// </summary>
        public DateTime? TokenExpiration { get; set; }

        /// <summary>
        /// JWT refresh token for obtaining new access tokens
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// User ID of the authenticated user
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Email of the authenticated user
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Full name of the authenticated user
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Role of the authenticated user
        /// </summary>
        public Role? Role { get; set; }

        /// <summary>
        /// Creates a successful authentication response
        /// </summary>
        /// <param name="user">User who was successfully authenticated</param>
        /// <param name="token">JWT access token</param>
        /// <param name="refreshToken">JWT refresh token</param>
        /// <param name="tokenExpiration">Token expiration time</param>
        /// <returns>Authentication response with user details and tokens</returns>
        public static AuthenticationResponse Success(User user, string token, string refreshToken, DateTime tokenExpiration)
        {
            return new AuthenticationResponse
            {
                IsSuccess = true,
                Message = "Authentication successful",
                Token = token,
                RefreshToken = refreshToken,
                TokenExpiration = tokenExpiration,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role
            };
        }

        /// <summary>
        /// Creates a failed authentication response
        /// </summary>
        /// <param name="message">Error message explaining why authentication failed</param>
        /// <returns>Authentication response with failure details</returns>
        public static AuthenticationResponse Failure(string message)
        {
            return new AuthenticationResponse
            {
                IsSuccess = false,
                Message = message
            };
        }
    }
}