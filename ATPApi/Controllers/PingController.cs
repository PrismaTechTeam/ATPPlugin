using System;
using System.Web.Http;
using ATPApi.AutoCount;

namespace ATPApi.Controllers
{
    /// <summary>
    /// Health-check endpoint. Returns AutoCount session info.
    /// GET /api/ping
    /// </summary>
    [RoutePrefix("api/ping")]
    public class PingController : ApiController
    {
        [HttpGet, Route("")]
        public IHttpActionResult Get()
        {
            try
            {
                var s = SessionManager.UserSession;
                return Ok(new
                {
                    ok       = true,
                    profile  = SessionManager.CurrentProfile,
                    database = SessionManager.DatabaseName,
                    loggedIn = s != null,
                    user     = s?.LoginUserID
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
