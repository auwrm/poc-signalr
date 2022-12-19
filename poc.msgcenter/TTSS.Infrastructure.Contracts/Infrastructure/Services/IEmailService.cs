namespace TTSS.Infrastructure.Services
{
    public interface IEmailService
    {
        Task<bool> SendAsync(string email, string subject, string mesaage);
    }
}
