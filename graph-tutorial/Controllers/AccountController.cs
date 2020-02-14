// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using graph_tutorial.Helpers;
using graph_tutorial.TokenStorage;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace graph_tutorial.Controllers
{
    public class AccountController : Controller
    {
        public void SignIn()
        {
            if (!Request.IsAuthenticated)
            {
                // Signal OWIN to send an authorization request to Azure
                Request.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = "/" },
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
        }

        public ActionResult SignOut()
        {
            if (Request.IsAuthenticated)
            {
                var tokenStore = new SessionTokenStore(null, 
                    System.Web.HttpContext.Current, ClaimsPrincipal.Current);

                tokenStore.Clear();

                Request.GetOwinContext().Authentication.SignOut(
                    CookieAuthenticationDefaults.AuthenticationType);
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<ActionResult> Consent(string code, string state, string error, string error_description)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return RedirectToAction("Error", "Home", new 
                {
                    message = "Error getting consent for additional permissions",
                    debug = $"Error: {(string.IsNullOrEmpty(error) ? "Unknown" : error)}\nDescription: {(string.IsNullOrEmpty(error_description) ? "None" : error_description)}"
                });
            }

            await GraphHelper.RedeemCodeForAdditionalConsent(code);

            return Redirect(state);
        }
    }
}