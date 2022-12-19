using Microsoft.AspNetCore.Mvc;

namespace TTSS.WebFacing.Common.Controllers
{
    [Route("ver")]
    [ApiController]
    public abstract class VerControllerBase : ControllerBase
    {
        private readonly string appName;
        private readonly string version;

        public VerControllerBase(string appName, string version)
        {
            this.appName = appName;
            this.version = version;
        }

        [HttpGet]
        [HttpHead]
        public virtual string GetVersion() => $"{appName} version {version}";
    }
}
