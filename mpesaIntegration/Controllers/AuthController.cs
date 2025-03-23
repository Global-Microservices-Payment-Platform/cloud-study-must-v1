using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mpesaIntegration.Models.Authentication;
using mpesaIntegration.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;

namespace mpesaIntegration.Controllers
{
    /// <summary>
    /// Handles authentication-related requests using ASP.NET Core's controller architecture
    /// Demonstrates RESTful principles with JWT authentication
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Constructor demonstrating dependency injection in .NET
        /// </summary>
        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="request">User registration details using RegisterRequest DTO</param>
        /// <returns>Authentication response with JWT tokens</returns>
        /// <remarks>
        /// Demonstrates:
        /// - Model validation using DataAnnotations
        /// - Async/await pattern for database operations
        /// - JSON serialization/deserialization
        /// - .NET's ActionResult pattern for REST responses
        /// </remarks>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthenticationResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // ModelState validation using [ApiController] attribute
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid registration request: {Errors}", 
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(ModelState);
                }
                
                var response = await _authService.RegisterAsync(request);
                
                return response.IsSuccess 
                    ? Ok(response) 
                    : BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing registration request");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    AuthenticationResponse.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Authenticates a user and returns JWT tokens
        /// </summary>
        /// <param name="request">Login credentials using LoginRequest DTO</param>
        /// <returns>Authentication tokens and user details</returns>
        /// <remarks>
        /// Demonstrates:
        /// - Secure password handling
        /// - JWT token generation
        /// - Cookie-free authentication
        /// </remarks>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthenticationResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                
                var response = await _authService.LoginAsync(request);
                
                return response.IsSuccess 
                    ? Ok(response) 
                    : Unauthorized(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing login request");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    AuthenticationResponse.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Refreshes JWT tokens using a valid refresh token
        /// </summary>
        /// <param name="refreshRequest">Token refresh request DTO</param>
        /// <returns>New set of authentication tokens</returns>
        /// <remarks>
        /// Demonstrates:
        /// - JWT token validation
        /// - Refresh token rotation
        /// - Claims principal extraction
        /// </remarks>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthenticationResponse>> RefreshToken([FromBody] RefreshTokenRequest refreshRequest)
        {
            try
            {
                if (string.IsNullOrEmpty(refreshRequest.AccessToken) || string.IsNullOrEmpty(refreshRequest.RefreshToken))
                {
                    return BadRequest(AuthenticationResponse.Failure("Tokens are required"));
                }
                
                var response = await _authService.RefreshTokenAsync(
                    refreshRequest.AccessToken, 
                    refreshRequest.RefreshToken
                );
                
                return response.IsSuccess 
                    ? Ok(response) 
                    : Unauthorized(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    AuthenticationResponse.Failure("An unexpected error occurred"));
            }
        }

        /// <summary>
        /// Logs out user by revoking refresh token
        /// </summary>
        /// <returns>Success status</returns>
        /// <remarks>
        /// Demonstrates:
        /// - [Authorize] attribute usage
        /// - Claim principal access
        /// - Token revocation pattern
        /// </remarks>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }
                
                var result = await _authService.RevokeTokenAsync(userId);
                
                return result 
                    ? Ok(new { message = "Logged out successfully" }) 
                    : BadRequest(new { message = "Logout failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "An unexpected error occurred" });
            }
        }

        /// <summary>
        /// Retrieves authenticated user's profile
        /// </summary>
        /// <returns>User profile information</returns>
        /// <remarks>
        /// Demonstrates:
        /// - Authorization requirement
        /// - Data projection to DTO
        /// - Secure data handling
        /// </remarks>
[Authorize]
[HttpGet("profile")]
public async Task<ActionResult<UserProfileResponse>> GetProfile()
{
    try
    {
        // Get user ID from multiple possible claim locations
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                   ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid token claims" });
        }

        var user = await _authService.GetUserProfileAsync(userGuid.ToString());
        if (user == null)
        {
            _logger.LogWarning("User not found for ID: {UserId}", userId);
            return NotFound(new { message = "User not found" });
        }

        var profileResponse = new UserProfileResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            MobileNumber = user.MobileNumber,
            Role = user.Role,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return Ok(profileResponse);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving profile");
        return StatusCode(500, new { message = "An error occurred" });
    }
}
        /// <summary>
        /// Initiates account deletion process
        /// </summary>
        /// <returns>Deletion request status</returns>
        /// <remarks>
        /// Demonstrates:
        /// - Soft delete pattern
        /// - Authorization requirements
        /// - Audit logging
        /// </remarks>
        [Authorize]
        [HttpPost("request-deletion")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RequestAccountDeletion()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }
                
                var result = await _authService.RequestAccountDeletionAsync(userId);
                
                return result 
                    ? Ok(new { message = "Deletion request received" }) 
                    : BadRequest(new { message = "Deletion request failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting account deletion");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "An unexpected error occurred" });
            }
        }
    }
}