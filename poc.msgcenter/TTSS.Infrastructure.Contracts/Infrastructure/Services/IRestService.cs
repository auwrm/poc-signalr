using TTSS.Infrastructure.Services.Models;

namespace TTSS.Infrastructure.Services
{
    public interface IRestService
    {
        Task Get(string endpoint);
        Task<RestResponse<Response>> Get<Response>(string endpoint) where Response : class;

        Task Put(string endpoint);
        Task Put<Request>(string endpoint, Request request) where Request : class;
        Task<RestResponse<Response>> Put<Response>(string endpoint) where Response : class;
        Task<RestResponse<Response>> Put<Request, Response>(string endpoint, Request request) where Request : class where Response : class;

        Task Post(string endpoint);
        Task Post<Request>(string endpoint, Request request) where Request : class;
        Task<RestResponse<Response>> Post<Response>(string endpoint) where Response : class;
        Task<RestResponse<Response>> Post<Request, Response>(string endpoint, Request request) where Request : class where Response : class;

        Task Delete(string endpoint);
    }
}
