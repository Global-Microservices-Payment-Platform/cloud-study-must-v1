using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mpesaIntegration.Models.Authentication;


namespace mpesaIntegration.Models.Payments
{
    /// <summary>
    /// Defines the possible states of an M-Pesa payment transaction
    /// </summary>
    /// 

    public enum PaymentStatus
    {
        /// <summary>
        /// Payment has been initiated but STK push not yet sent
        /// </summary>
        /// 
        Initiated = 1,

        /// <summary>
        /// STK push has been sent to the user's phone
        /// </summary>
        /// 

        StkPushSent = 2,
        /// <summary>
        /// Payment has been successfully completed
        /// </summary>  
        /// 

        Completed = 3,
        /// <summary>
        /// Payment was cancelled by the user
        /// </summary>
        /// 
        Cancelled = 4,

        /// <summary>
        /// Payment failed due to technical or other issues
        /// </summary>
        /// 
        Failed = 5,
        /// <summary>
        /// Payment request timed out (user didn't respond to STK push)
        /// </summary>
        /// 
        TimedOut = 6


    }

    /// <summary>
    /// Represents a payment transaction in the system.
    /// Maps directly to the payments table in the database.
    /// </summary>


    public class Payment
    {
        /// <summary>
        /// Unique identifier for the payment transaction
        /// </summary>
        /// 
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Reference to the user who initiated the payment
        /// </summary>
        /// 

        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Navigation property to the User entity
        /// </summary>
        /// 

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        /// <summary>
        /// Amount to be paid in the transaction
        /// </summary>
        /// 

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        /// <summary>
        /// Phone number used for the M-Pesa transaction (derived from user's mobile number)
        /// </summary>
        /// T

        [Required]
        [MaxLength(20)]
        public required string PhoneNumber { get; set; }


        /// <summary>
        /// Brief description of what the payment is for
        /// </summary>
        /// 
        [Required]
        [MaxLength(200)]
        public required string Description { get; set; }

        /// <summary>
        /// Account reference for the transaction (e.g., order number, invoice number)
        /// </summary>
        /// 

        [Required]
        [MaxLength(100)]
        public required string AccountReference { get; set; }

        /// <summary>
        /// Current status of the payment transaction
        /// </summary>
        [Required]
        public PaymentStatus Status { get; set; }
        /// <summary>
        /// Date and time when the payment was initiated
        /// </summary>
        /// 
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date and time when the payment status was last updated
        /// </summary>
        /// 

        public DateTime UpdatedAt { get; set; }
        /// <summary>
        /// Transaction ID returned by M-Pesa after successful payment
        /// </summary>
        /// 

        [MaxLength(100)]
        public string? MpesaReceiptNumber { get; set; }
        /// <summary>
        /// Checkout request ID returned by M-Pesa when STK push is initiated
        /// </summary>
        /// 
        [MaxLength(100)]
        public string? CheckoutRequestId { get; set; }

        /// <summary>
        /// Result code returned by M-Pesa API
        /// </summary>
        public int? ResultCode { get; set; }
        /// <summary>
        /// Result description returned by M-Pesa API
        /// </summary>
        [MaxLength(200)]
        public string? ResultDescription { get; set; }


    }

    /// <summary>
    /// Data transfer object for initiating an STK push payment request
    /// </summary>
    /// 

    public class StkPushRequest
    {
        /// <summary>
        /// Amount to be paid in the transaction
        /// </summary>
        [Required(ErrorMessage = "Amount is required")]
        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }
        /// <summary>
        /// Brief description of what the payment is for
        /// </summary>
        [Required(ErrorMessage = "Description is required")]
        [MaxLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string? Description { get; set; }
        /// <summary>
        /// Account reference for the transaction (e.g., order number, invoice number)
        /// </summary>
        [Required(ErrorMessage = "Account reference is required")]
        [MaxLength(100, ErrorMessage = "Account reference cannot exceed 100 characters")]
        public string? AccountReference { get; set; }

    }

    /// <summary>
    /// Data transfer object representing the M-Pesa STK push request payload sent to the M-Pesa API
    /// </summary>
    /// 
    public class MpesaStkPushRequest
    {
        /// <summary>
        /// Business shortcode (Paybill or Till number)
        /// </summary>

        public string? BusinessShortCode { get; set; }

        /// <summary>
        /// Password for the STK push request (Base64 encoded)
        /// </summary>
        /// 

        public string? Password { get; set; }

        /// <summary>
        /// Timestamp of the transaction in YYYYMMDDHHmmss format
        /// </summary>

        public string? Timestamp { get; set; }
        /// <summary>
        /// Type of transaction (CustomerPayBillOnline or CustomerBuyGoodsOnline)
        /// </summary>
        public string? TransactionType { get; set; }
        /// <summary>
        /// Amount to be charged
        /// </summary>
        public string? Amount { get; set; }
        /// <summary>
        /// Phone number sending money (format: 254XXXXXXXXX)
        /// </summary>
        public string ?PartyA { get; set; }
        /// <summary>
        /// Business shortcode receiving payment (Paybill or Till number)
        /// </summary>
        public string? PartyB { get; set; }
        /// <summary>
        /// Phone number to receive STK push (format: 254XXXXXXXXX)
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// URL to send callback after payment completion
        /// </summary>
        public string? CallBackURL { get; set; }
        /// <summary>
        /// Account reference (e.g., order number, invoice number)
        /// </summary>
        public string? AccountReference { get; set; }
        /// <summary>
        /// Transaction description
        /// </summary>
        public string? TransactionDesc { get; set; }

    }

    /// <summary>
    /// Data transfer object representing the M-Pesa STK push response from the M-Pesa API
    /// </summary>
    /// 
    public class MpesaStkPushResponse 
    {
         /// <summary>
        /// Response code from M-Pesa API (0 means success)
        /// </summary>
        public string? ResponseCode { get; set; }
          /// <summary>
        /// Human-readable description of the response status
        /// </summary>
        public string? ResponseDescription { get; set; }
         /// <summary>
        /// Request ID for the STK push
        /// </summary>
        public string? MerchantRequestID { get; set; }

        /// <summary>
        /// ID used to track the STK push request status
        /// </summary>
        public string? CheckoutRequestID { get; set; }
          /// <summary>
        /// Additional message about the request status
        /// </summary>
        /// 
        public string? CustomerMessage { get; set; }

    }

        /// <summary>
    /// Data transfer object representing the M-Pesa STK push callback received after payment
    /// </summary>
    /// 
    public class MpesaStkCallback 
    {
           /// <summary>
        /// Main body of the callback payload
        /// </summary>
        public StkCallbackBody? Body { get; set; }

    }

     /// <summary>
    /// Body content of the M-Pesa STK push callback
    /// </summary>

    public class StkCallbackBody 
    {
                /// <summary>
        /// Status of the STK transaction
        /// </summary>
        public string? ResultDesc { get; set; }

          /// <summary>
        /// Result code (0 means success)
        /// </summary>
        public int ResultCode { get; set; }

          /// <summary>
        /// Original request ID
        /// </summary>
        public string? MerchantRequestID { get; set; }
        /// <summary>
        /// Original checkout request ID
        /// </summary>
        public string? CheckoutRequestID { get; set; }
         /// <summary>
        /// Transaction details in case of successful payment
        /// </summary>
        public CallbackMetadata? CallbackMetadata { get; set; }


    }


    /// <summary>
    /// Metadata containing transaction details in the M-Pesa callback
    /// </summary>
    /// 
    public class CallbackMetadata 
    {
            /// <summary>
        /// List of items with transaction details
        /// </summary>
        public List<CallbackItem>? Item { get; set; }

    }

     /// <summary>
    /// Individual item in the callback metadata
    /// </summary>

    public class CallbackItem 
    {
        /// <summary>
        /// Name of the metadata item (Amount, PhoneNumber, etc.)
        /// </summary>

        public string? Name { get; set; }

        /// <summary>
        /// Value of the metadata item
        /// </summary>
        public object? Value { get; set; }


    }
    /// <summary>
    /// Data transfer object for payment status response
    /// </summary>
    /// 
    public class PaymentStatusResponse 
    {
        /// <summary>
        /// Unique identifier of the payment
        /// </summary>
        /// 
        public Guid PaymentId { get; set; }
        /// <summary>
        /// Current status of the payment
        /// </summary>
        public PaymentStatus Status { get; set; }
        /// <summary>
        /// M-Pesa receipt number for completed transactions
        /// </summary>
        public string? MpesaReceiptNumber { get; set; }
         /// <summary>
        /// Description of the payment status
        /// </summary>
        public string? StatusDescription { get; set; }

        /// <summary>
        /// Amount paid
        /// </summary>
        public decimal Amount { get; set; }
         /// <summary>
        /// Phone number used for the transaction
        /// </summary>
        /// 
        public string? PhoneNumber { get; set; }
          /// <summary>
        /// Date and time of the transaction
        /// </summary>
        public DateTime TransactionDate { get; set; }

    }
    
    


}