
using System.Web;
using System.Web.Mvc;

namespace ĐỀ_ÁN_CUỐI_KỲ_NHÓM_MINISOCIALNETWOK.Controllers
{
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly string role;

        public RoleAuthorizeAttribute(string role)
        {
            this.role = role;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext.Session["UserId"] == null)
                return false;

            string currentRole =
                httpContext.Session["Role"] as string;

            return currentRole == role;
        }

        protected override void HandleUnauthorizedRequest(
            AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.Session["UserId"] == null)
            {
                filterContext.Result =
                    new RedirectToRouteResult(
                        new System.Web.Routing.RouteValueDictionary
                        {
                            { "controller", "Account" },
                            { "action", "Login" }
                        });
            }
            else
            {
                filterContext.Result = new HttpStatusCodeResult(403);
            }
        }
    }
}
