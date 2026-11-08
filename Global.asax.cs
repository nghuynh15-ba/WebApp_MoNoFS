using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Helpers;

namespace WebApp_MoNoFS
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            

            
            System.Web.Helpers.AntiForgeryConfig.SuppressIdentityHeuristicChecks = true;
        }
    }
}
