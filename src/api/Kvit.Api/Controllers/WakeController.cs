using Kvit.Api.Settings;
using Kvit.Api.Wake;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Route("wake")]
    public sealed class WakeController(IConfiguration _configuration) : BaseController
    {
        private const string ReturnToParameter = "returnTo";

        [HttpGet]
        [AllowAnonymous]
        public RedirectResult Wake()
        {
            Uri siteOrigin = WebsiteSetting.ReadOrigin(_configuration);
            string returnPath = WakeReturnPath.From(Request.Query[ReturnToParameter]);
            Response.Headers.CacheControl = "no-store";

            return Redirect(siteOrigin.GetLeftPart(UriPartial.Authority) + returnPath);
        }
    }
}
