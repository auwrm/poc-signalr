using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace TTSS.WebFacing.Common.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        [HttpHead]
        [Route("")]
        [Route("check")]
        public async Task<ActionResult<string>> GetVersion()
        {
            var healthy = await CheckAppHealth();

            if (!healthy)
                return Problem(
                    "Service unhealthy",
                    statusCode: (int)HttpStatusCode.ServiceUnavailable,
                    title: "Health");

            return "Healthy";
        }

        protected virtual Task<bool> CheckAppHealth()
            => Task.FromResult(true);
    }
}
