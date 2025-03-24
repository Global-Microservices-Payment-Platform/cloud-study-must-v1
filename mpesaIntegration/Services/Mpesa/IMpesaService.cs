using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using mpesaIntegration.Models.Payments;
using mpesaIntegration.Data;

namespace mpesaIntegration.Services.MpesaPayment
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
        Task<MpesaStkPushResponse> InitiateStpPushAsync(Guid userId, StkPushRequest request);
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
        public async Task <string>GetAccessTokenAsync( )
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
            catch(Exception ex) {
                _logger.LogError(ex, "Error generating M-Pesa access token");

            }
        }
    }
}