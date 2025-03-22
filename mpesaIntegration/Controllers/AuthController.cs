using Microsoft.AspNetCore.Mvc;
using mpesaIntegration.Models.Authentication;
using mpesaIntegration.Services;

namespace mpesaIntegration.Controllers
{
    /// <summary>
    /// Handles authentication-related requests
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="request">User registration details</param>
        /// <returns>Authentication response with tokens</returns>
        [HttpPost("register")]
        public async Task<ActionResult<AuthenticationResponse>> Register(RegisterRequest request)
        {
            var response = await _authService.RegisterAsync(request);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Authenticates an existing user
        /// </summary>
        /// <param name="request">User login credentials</param>
        /// <returns>Authentication response with tokens</returns>
        [HttpPost("login")]
        public async Task<ActionResult<AuthenticationResponse>> Login(LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);
            return response.IsSuccess ? Ok(response) : Unauthorized(response);
        }
    }
}