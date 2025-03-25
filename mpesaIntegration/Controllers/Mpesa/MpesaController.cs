using System;
using System.Collections.Generic;
using System.Security.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using mpesaIntegration.Models.Payments;
using mpesaIntegration.Services;
using mpesaIntegration.Services.Payments;
using System.Security.Claims;


namespace mpesaIntegration.Controllers.Payment
{
    [Route("api/mpesa")]
    [ApiController]

    public class PaymentsController : ControllerBase
    {
        private readonly IMpesaService _mpesaService;
        private readonly IPaymentService _paymentService;

        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IMpesaService mpesaService,
            IPaymentService paymentService,
            ILogger<PaymentsController> logger
        )
        {
            _mpesaService = mpesaService;
            _paymentService = paymentService;
            _logger = logger;

        }
        /// <summary>
        /// Initiates an STK push payment request
        /// </summary>
        /// 
        [HttpPost("initiate-stk-push")]
        [Authorize]
        public async Task<IActionResult> InitiatePayment([FromBody] StkPushRequest request)
        {
            try
            {
                //get the current user id
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))

                {
                    return Unauthorized("User id not found or invalid");
                }
                //create payment  record first (status: initiated)
                var payment = await _paymentService.CreatePaymentAsync(userId, request);
                //initiate stk push 
                var response = await _mpesaService.InitiateStkPushAsync(userId, request);
                //return the response
                return Ok(new
                {
                    paymentId = payment.Id,
                    Message = "STK push sent to your phone. Please enter your M-Pesa PIN to complete the payment.",
                    CheckoutRequestId = response.CheckoutRequestID

                }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating STK push");
                return StatusCode(500, "An error occurred while processing your payment request");

            }
        }

        /// <summary>
        /// Receives M-Pesa STK push callbacks
        /// </summary>
        /// 

        [HttpPost("stk-push-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> MpesaCallBack([FromBody] MpesaStkCallback callback)
        {
            try
            {
                await _paymentService.UpdatePaymentStatusFromCallbackAsync(callback);
                return Ok();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing M-Pesa callback");
                return StatusCode(500, "An error occurred while processing the callback");

            }
        }
        /// <summary>
        /// Gets the status of a payment
        /// </summary>
        /// 
        [HttpGet("payment-status/{paymentId}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentStatus(Guid paymentId)
        {
            try
            {
                //get the current user id
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    return Unauthorized("User id not found or invalid ");

                }

                //get the payment
                var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
                if (payment == null)
                {
                    return NotFound("Payment not found");


                }

                //ensure () the payment belongs to the current user
                if (payment.UserId != userId)
                {
                    return Forbid("You do not have permission to view this payment");
                }

                // Create the response
                var response = new PaymentStatusResponse
                {
                    PaymentId = payment.Id,
                    Status = payment.Status,
                    MpesaReceiptNumber = payment.MpesaReceiptNumber,
                    StatusDescription = payment.ResultDescription ?? GetStatusDescription(payment.Status),
                    Amount = payment.Amount,
                    PhoneNumber = payment.PhoneNumber,
                    TransactionDate = payment.UpdatedAt != default(DateTime) ? payment.UpdatedAt : payment.CreatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status");
                return StatusCode(500, "An error occurred while retrieving payment status");
            }

        }

        /// <summary>
        /// Gets all payments for the current user
        /// </summary>
        /// [\
        /// ]
        /// 
        [HttpGet("all-user-payments")]
        [Authorize]

        public async Task<IActionResult> GetUserPayments()
        {
            try
            {
                // get the current user id
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    return Unauthorized("User id not found or invalid");
                }

                //get the payments
                var payments = await _paymentService.GetUserPaymentsAsync(userId);

                //map the response objects
                var response = payments.Select(p => new PaymentStatusResponse
                {
                    PaymentId = p.Id,
                    Status = p.Status,
                    MpesaReceiptNumber = p.MpesaReceiptNumber,
                    StatusDescription = p.ResultDescription ?? GetStatusDescription(p.Status),
                    Amount = p.Amount,
                    PhoneNumber = p.PhoneNumber,
                    TransactionDate = p.UpdatedAt != default(DateTime) ? p.UpdatedAt : p.CreatedAt
                }).ToList();

                return Ok (response);



            }
            catch (Exception ex)
            {
                  _logger.LogError(ex, "Error getting user payments");
                return StatusCode(500, "An error occurred while retrieving payments");

            }
        }



        /// <summary>
        /// Gets a human-readable description for payment statuses
        /// </summary>
        /// 
        private string GetStatusDescription(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.Initiated => "Payment initiated",
                PaymentStatus.StkPushSent => "STK push sent to your phone",
                PaymentStatus.Completed => "Payment completed successfully",
                PaymentStatus.Cancelled => "Payment cancelled",
                PaymentStatus.Failed => "Payment failed",
                PaymentStatus.TimedOut => "Payment request timed out",
                _ => "Unknown status"
            };
        }
    }
}
