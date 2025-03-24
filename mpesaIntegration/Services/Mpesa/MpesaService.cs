using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using mpesaIntegration.Data;
using mpesaIntegration.Models.Payments;


namespace mpesaIntegration.Services.Payments
{
    /// <summary>
    /// Service for handling payment operations
    /// </summary>
    /// 
    public interface IPaymentService
    {
        /// <summary>
        /// Creates a new payment request
        /// </summary>
        Task<Payment> CreatePaymentAsync(Guid userId, StkPushRequest request);

        /// <summary>
        /// Updates payment status based on M-Pesa callback
        /// </summary>
        Task UpdatePaymentStatusFromCallbackAsync(MpesaStkCallback callback);
        /// <summary>
        /// Gets payment by ID
        /// </summary>
        Task<Payment> GetPaymentByIdAsync(Guid paymentId);

        /// <summary>
        /// Gets all payments for a user
        /// </summary>
        Task<List<Payment>> GetUserPaymentsAsync(Guid userId);
        /// <summary>
        /// Gets payment by M-Pesa checkout request ID
        /// </summary>
        Task<Payment> GetPaymentByCheckoutRequestIdAsync(string checkoutRequestId);

    }
    /// <summary>
    /// Implementation of the payment service
    /// </summary>
    /// 
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<PaymentService> _logger;

        /// <summary>
        /// Initializes a new instance of the PaymentService
        /// </summary>
        /// 
        public PaymentService(ApplicationDbContext dbContext, ILogger<PaymentService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;

        }

        /// <summary>
        /// Creates a new payment request
        /// </summary>
        /// 
        public async Task<Payment> CreatePaymentAsync(Guid userId, StkPushRequest request)
        {
            try
            {
                // Get user details
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new ApplicationException("User not found");
                }
                // Format phone number
                string phoneNumber = user.MobileNumber.Trim();
                if (phoneNumber.StartsWith("0"))
                {
                    phoneNumber = "254" + phoneNumber.Substring(1);
                }
                else if (!phoneNumber.StartsWith("254"))
                {
                    phoneNumber = "254" + phoneNumber;
                }
                // Create payment record
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Amount = request.Amount,
                    PhoneNumber = phoneNumber,
                    Description = request.Description,
                    AccountReference = request.AccountReference,
                    Status = PaymentStatus.Initiated,
                    CreatedAt = DateTime.UtcNow
                };
                await _dbContext.Payments.AddAsync(payment);
                await _dbContext.SaveChangesAsync();

                return payment;


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment");
                throw;


            }
        }
        /// <summary>
        /// Updates payment status based on M-Pesa callback
        /// </summary>
        /// 
        public async Task UpdatePaymentStatusFromCallbackAsync(MpesaStkCallback callback)
        {
            try
            {
                var checkoutRequestId = callback.Body.CheckoutRequestID;
                var resultCode = callback.Body.ResultCode;
                var resultDesc = callback.Body.ResultDesc;

                // Find the payment by checkout request ID
                var payment = await _dbContext.Payments
                    .FirstOrDefaultAsync(p => p.CheckoutRequestId == checkoutRequestId);

                if (payment == null)
                {
                    _logger.LogWarning($"Payment not found for checkout request ID: {checkoutRequestId}");
                    return;
                }

                // Update payment status based on result code
                if (resultCode == 0) // Success
                {
                    payment.Status = PaymentStatus.Completed;

                    // Extract additional details from callback metadata
                    if (callback.Body.CallbackMetadata != null && callback.Body.CallbackMetadata.Item != null)
                    {
                        foreach (var item in callback.Body.CallbackMetadata.Item)
                        {
                            switch (item.Name)
                            {
                                case "MpesaReceiptNumber":
                                    payment.MpesaReceiptNumber = item.Value.ToString();
                                    break;
                                case "Amount":
                                    // You could validate the amount here if needed
                                    break;
                                case "TransactionDate":
                                    // Store transaction date if needed
                                    break;
                                case "PhoneNumber":
                                    // You could validate the phone number here if needed
                                    break;
                            }
                        }
                    }
                }
                else // Failed
                {
                    payment.Status = PaymentStatus.Failed;
                }

                payment.ResultCode = resultCode;
                payment.ResultDescription = resultDesc;
                payment.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status from callback");
                throw;
            }
        }
        /// <summary>
        /// Gets payment by ID
        /// </summary>
        public async Task<Payment> GetPaymentByIdAsync(Guid paymentId)
        {
            return await _dbContext.Payments
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == paymentId);
        }
        /// <summary>
        /// Gets all payments for a user
        /// </summary>
        public async Task<List<Payment>> GetUserPaymentsAsync(Guid userId)
        {
            return await _dbContext.Payments
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        /// <summary>
        /// Gets payment by M-Pesa checkout request ID
        /// </summary>
        public async Task<Payment> GetPaymentByCheckoutRequestIdAsync(string checkoutRequestId)
        {
            return await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.CheckoutRequestId == checkoutRequestId);
        }
    }
}