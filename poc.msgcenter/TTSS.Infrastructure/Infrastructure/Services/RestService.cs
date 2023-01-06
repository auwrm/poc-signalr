using Flurl.Http;
using Newtonsoft.Json;
using TTSS.Infrastructure.Services.Models;

namespace TTSS.Infrastructure.Services
{
    public class RestService : IRestService
    {
        public Task Get(string endpoint) => throw new NotImplementedException();
        public Task<RestResponse<Response>> Get<Response>(string endpoint) where Response : class => throw new NotImplementedException();

        public Task Put(string endpoint) => throw new NotImplementedException();
        public Task Put<Request>(string endpoint, Request request) where Request : class => throw new NotImplementedException();
        public Task<RestResponse<Response>> Put<Response>(string endpoint) where Response : class => throw new NotImplementedException();
        public async Task<RestResponse<Response>> Put<Request, Response>(string endpoint, Request request) where Request : class where Response : class
        {
            var rsp = await endpoint.PutJsonAsync(request);
            var data = await rsp.GetStringAsync();
            return new RestResponse<Response>()
            {
                StatusCode = rsp.StatusCode,
                Data = await rsp.GetJsonAsync<Response>() ?? default,
                IsSuccessStatusCode = rsp?.ResponseMessage?.IsSuccessStatusCode ?? false,
            };
        }

        public Task Post(string endpoint) => throw new NotImplementedException();
        public Task Post<Request>(string endpoint, Request request) where Request : class => throw new NotImplementedException();
        public Task<RestResponse<Response>> Post<Response>(string endpoint) where Response : class => throw new NotImplementedException();
        public async Task<RestResponse<Response>> Post<Request, Response>(string endpoint, Request request) where Request : class where Response : class
        {
            var rsp = await endpoint.PostJsonAsync(request);
            var data = await rsp.GetStringAsync();
            return new RestResponse<Response>()
            {
                StatusCode = rsp.StatusCode,
                Data = await rsp.GetJsonAsync<Response>() ?? default,
                IsSuccessStatusCode = rsp?.ResponseMessage?.IsSuccessStatusCode ?? false,
            };
        }

        public Task Delete(string endpoint) => throw new NotImplementedException();
    }
}
