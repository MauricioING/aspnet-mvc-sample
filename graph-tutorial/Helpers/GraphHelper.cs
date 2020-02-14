// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using graph_tutorial.TokenStorage;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace graph_tutorial.Helpers
{
    public static class GraphHelper
    {
        // Load configuration settings from PrivateSettings.config
        private static string appId = ConfigurationManager.AppSettings["ida:AppId"];
        private static string appSecret = ConfigurationManager.AppSettings["ida:AppSecret"];
        private static string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
        private static List<string> graphScopes = new List<string>(ConfigurationManager.AppSettings["ida:AppScopes"].Split(' '));

        public static async Task<CachedUser> GetUserDetailsAsync(string accessToken)
        {
            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", accessToken);
                        return Task.FromResult(0);
                    }));

            // Get the user's details from Graph
            // We only want displayName, mail, 
            // mailboxSettings, and userPrincipalName
            var user = await graphClient.Me.Request()
                .Select(u => new { 
                    u.DisplayName,
                    u.Mail,
                    u.MailboxSettings,
                    u.UserPrincipalName
                })
                .GetAsync();

            // Get the user's profile photo
            var profilePhotoUri = await GetUserPhotoAsDataUriAsync(graphClient, "48x48");

            return new CachedUser
            {
                Avatar = profilePhotoUri,
                DateFormat = user.MailboxSettings.DateFormat,
                DisplayName = user.DisplayName,
                // Personal accounts don't have their mail property set
                // In this case, fallback on userPrincipalName
                Email = string.IsNullOrEmpty(user.Mail) ? 
                    user.UserPrincipalName : user.Mail,
                TimeFormat = user.MailboxSettings.TimeFormat,
                TimeZone = user.MailboxSettings.TimeZone
            };
        }

        public static async Task<string> GetUserPhotoAsDataUriAsync(GraphServiceClient graphClient = null, string size = null)
        {
            if (graphClient == null)
            {
                graphClient = GetAuthenticatedClient();
            }

            Stream photoStream;

            // If no size specified, get the default photo
            if (string.IsNullOrEmpty(size))
            {
                photoStream = await graphClient.Me
                    .Photo.Content.Request().GetAsync();
            }
            else
            {
                photoStream = await graphClient.Me
                    .Photos[size].Content.Request().GetAsync();
            }

            // Copy the stream to a MemoryStream to get the data
            // out as a byte array
            var memoryStream = new MemoryStream();
            photoStream.CopyTo(memoryStream);

            var photoBytes = memoryStream.ToArray();

            // Return a data URI
            return $"data:image/png;base64,{Convert.ToBase64String(photoBytes)}";
        }

        public static async Task<User> GetUserProfileAsync()
        {
            var graphClient = GetAuthenticatedClient();

            // The default set of properties on a user object is small,
            // you must request non-default properties explicitly
            var userProfile = await graphClient.Me
                .Request()
                .Select(u => new
                {
                    u.AboutMe,
                    u.Birthday,
                    u.BusinessPhones,
                    u.City,
                    u.Country,
                    u.Department,
                    u.DisplayName,
                    u.EmployeeId,
                    u.Interests,
                    u.JobTitle,
                    u.MobilePhone,
                    u.MySite,
                    u.OfficeLocation,
                    u.PostalCode,
                    u.Responsibilities,
                    u.Schools,
                    u.Skills,
                    u.State,
                    u.StreetAddress
                })
                .GetAsync();

            return userProfile;
        }

        public static async Task UpdateUserProfileAsync(User userProfile)
        {
            var graphClient = GetAuthenticatedClient();

            await graphClient.Me.Request().UpdateAsync(userProfile);
        }

        public static async Task<IEnumerable<Event>> GetEventsAsync()
        {
            var graphClient = GetAuthenticatedClient();

            var events = await graphClient.Me.Events.Request()
                .Select("subject,organizer,start,end")
                .OrderBy("createdDateTime DESC")
                .GetAsync();

            return events.CurrentPage;
        }

        public static async Task<Uri> GetConsentUriForScopesIfNeeded(string[] scopes, string redirect)
        {
            // Combine the requested scopes with the default set of scopes
            // requested at sign in
            var combinedScopes = graphScopes.Union(scopes);

            // Create an MSAL client and token cache
            var idClient = ConfidentialClientApplicationBuilder.Create(appId)
                            .WithRedirectUri(redirectUri)
                            .WithClientSecret(appSecret)
                            .Build();

            var tokenStore = new SessionTokenStore(idClient.UserTokenCache,
                HttpContext.Current, ClaimsPrincipal.Current);

            var accounts = await idClient.GetAccountsAsync();

            try
            {
                // See if there is a token in the cache that has all of the required scopes
                // If so, the user has already granted the permission we need
                var result = await idClient
                    .AcquireTokenSilent(combinedScopes, accounts.FirstOrDefault())
                    .ExecuteAsync();

                return null;
            }
            catch (MsalUiRequiredException)
            {
                // This exception indicates that the user needs to consent
                // to one or more of the required scopes.

                // Save the page the user is on into the state parameter
                var stateParam = new Dictionary<string, string>();
                stateParam.Add("state", redirect);

                // Build the authorization URL
                var uri = await idClient.GetAuthorizationRequestUrl(scopes)
                    .WithAccount(accounts.FirstOrDefault())
                    .WithRedirectUri($"{redirectUri}Account/Consent")
                    .WithExtraQueryParameters(stateParam)
                    .ExecuteAsync();

                // Add the "prompt=consent" query parameter
                var queryParams = HttpUtility.ParseQueryString(uri.Query);
                queryParams["prompt"] = "consent";

                var builder = new UriBuilder(uri);

                builder.Query = queryParams.ToString();
                return builder.Uri;
            }
        }

        public static async Task RedeemCodeForAdditionalConsent(string code)
        {
            // Create the MSAL client with a special redirect
            var idClient = ConfidentialClientApplicationBuilder.Create(appId)
                            .WithRedirectUri($"{redirectUri}Account/Consent")
                            .WithClientSecret(appSecret)
                            .Build();

            var tokenStore = new SessionTokenStore(idClient.UserTokenCache,
                HttpContext.Current, ClaimsPrincipal.Current);

            // Exchange the code for a token
            var result = await idClient
                .AcquireTokenByAuthorizationCode(graphScopes, code)
                .ExecuteAsync();
        }

        private static GraphServiceClient GetAuthenticatedClient()
        {
            return new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        var idClient = ConfidentialClientApplicationBuilder.Create(appId)
                            .WithRedirectUri(redirectUri)
                            .WithClientSecret(appSecret)
                            .Build();

                        var tokenStore = new SessionTokenStore(idClient.UserTokenCache, 
                            HttpContext.Current, ClaimsPrincipal.Current);

                        var accounts = await idClient.GetAccountsAsync();

                        // By calling this here, the token can be refreshed
                        // if it's expired right before the Graph call is made

                            var result = await idClient
                            .AcquireTokenSilent(graphScopes, accounts.FirstOrDefault())
                            .ExecuteAsync();

                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", result.AccessToken);
                    }));
        }
    }
}