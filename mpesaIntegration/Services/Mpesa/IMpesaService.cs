
using System.Threading.Tasks;
using mpesaIntegration.Models.Mpesa;
public interface IMpesaService 
{
    Task<MpesaResponse> InitiateStkPush (Guid id, decimal amount, string accountReference, string transactionDescription);
}