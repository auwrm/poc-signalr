namespace TTSS.Infrastructure.Services
{
    public interface ISmsService
    {
        Task<bool> SendAsync(string countryCode, string phoneNumber, string message);
    }
}
