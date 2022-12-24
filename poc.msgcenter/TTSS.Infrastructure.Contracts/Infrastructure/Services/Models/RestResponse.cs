namespace TTSS.Infrastructure.Services.Models
{
    public class RestResponse<T>
    {
        public int StatusCode { get; set; }
        public bool IsSuccessStatusCode { get; set; }
        public T Data { get; set; }
    }
}
