using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using mpesaIntegration.Models.Payments;
using mpesaIntegration.Data;
using Microsoft.EntityFrameworkCore;

namespace mpesaIntegration.Services.Payments
{
    /// <summary>
    /// Service for handling M-Pesa API integration operations
    /// </summary>

    public interface IMpesaService
    {
        /// <summary>
        /// Generates an access token for M-Pesa API authentication
        /// </summary>
        /// 
        Task<string> GetAccessTokenAsync();

        /// <summary>
        /// Initiates an STK push request to the customer's phone
        /// </summary>
        /// T
        /// 
        Task<MpesaStkPushResponse> InitiateStkPushAsync(Guid userId, StkPushRequest request);
        /// <summary>
        /// Checks the status of an STK push request
        /// </summary>

        Task<string> CheckStkPushStatusAsync(string CheckoutRequestId);

    }
    /// <summary>
    /// Implementation of the M-Pesa API service
    /// </summary>
    /// 

    public class MpesaService : IMpesaService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MpesaService> _logger;
        private readonly ApplicationDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the MpesaService
        /// </summary>
        /// 

        public MpesaService
        (
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<MpesaService> logger,
        ApplicationDbContext dbContext

        )
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Generates an access token for M-Pesa API authentication
        /// </summary>
        /// 
        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                string consumerKey = _configuration["MpesaSettings:ConsumerKey"];
                string consumerSecret = _configuration["MpesaSettings:ConsumerSecret"];
                string url = _configuration["MpesaSettings:OAuthUrl"];

                // Create authorization header
                string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{consumerKey}:{consumerSecret}"));
                //set the request
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
                //send the request 
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                //parse the response
                string responseBody = await response.Content.ReadAsStringAsync();
                JsonDocument jsonDoc = JsonDocument.Parse(responseBody);


                //Extract the access token \
                string accessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();
                return accessToken;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating M-Pesa access token");
                throw new ApplicationException("Error generating M-Pesa access token", ex);

            }
        }

        /// <summary>
        /// Initiates an STK push request to the customer's phone
        /// </summary>
        /// 

        public async Task<MpesaStkPushResponse> InitiateStkPushAsync(Guid userId, StkPushRequest request)
        {
            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new ApplicationException("User not found");
                }

                string phoneNumber = user.MobileNumber.Trim();
                if (phoneNumber.StartsWith("0"))
                {
                    phoneNumber = "254" + phoneNumber.Substring(1);
                }

                string businessShortCode = _configuration["MpesaSettings:BusinessShortCode"];
                string passKey = _configuration["MpesaSettings:PassKey"];
                string callbackUrl = _configuration["MpesaSettings:CallbackUrl"];
                string transactionType = _configuration["MpesaSettings:TransactionType"];

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string password = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{businessShortCode}{passKey}{timestamp}"));

                var stkPushRequest = new MpesaStkPushRequest
                {
                    BusinessShortCode = businessShortCode,
                    Password = password,
                    Timestamp = timestamp,
                    TransactionType = transactionType,
                    Amount = request.Amount.ToString(),
                    PartyA = phoneNumber,
                    PartyB = businessShortCode,
                    PhoneNumber = phoneNumber,
                    CallBackURL = callbackUrl,
                    AccountReference = request.AccountReference,
                    TransactionDesc = request.Description
                };

                string accessToken = await GetAccessTokenAsync();
                string url = _configuration["MpesaSettings:StkPushUrl"];
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                string jsonRequest = JsonSerializer.Serialize(stkPushRequest);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                string responseBody = await response.Content.ReadAsStringAsync();
                var stkPushResponse = JsonSerializer.Deserialize<MpesaStkPushResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Update the existing payment record with CheckoutRequestId
                var payment = await _dbContext.Payments
                    .Where(p => p.UserId == userId && p.Status == PaymentStatus.Initiated)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();

                if (payment != null)
                {
                    payment.CheckoutRequestId = stkPushResponse.CheckoutRequestID;
                    payment.Status = PaymentStatus.StkPushSent;
                    payment.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }

                return stkPushResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating STK push request");
                throw new ApplicationException("Error initiating STK push request", ex);
            }
        }

        /// <summary>
        /// Checks the status of an STK push request
        /// </summary>
        /// 
        public async Task<string> CheckStkPushStatusAsync(string CheckoutRequestId)
        {
            try
            {
                // Get configuration
                string businessShortCode = _configuration["MpesaSettings:BusinessShortCode"];
                string passkey = _configuration["MpesaSettings:Passkey"];
                // Generate timestamp (YYYYMMDDHHmmss)
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                // Generate password (base64 of businessShortCode + passkey + timestamp)
                string password = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{businessShortCode}{passkey}{timestamp}")
                );
                // Create request payload
                var requestPayload = new
                {
                    BusinessShortCode = businessShortCode,
                    Password = password,
                    Timestamp = timestamp,
                    CheckoutRequestID = CheckoutRequestId
                };
                // Get access token
                string accessToken = await GetAccessTokenAsync();
                // Set up the request
                string url = _configuration["MpesaSettings:StkQueryUrl"];
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                // Send the request
                string jsonRequest = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);
                // Return the raw response for now (to be parsed by the caller)
                return await response.Content.ReadAsStringAsync();


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking STK push status");
                throw new ApplicationException("Error checking STK push status", ex);

            }
        }
    }
}