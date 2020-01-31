// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using graph_tutorial.TokenStorage;
using Microsoft.Identity.Client;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.IO;
using System;
using System.Net.Http;

namespace graph_tutorial.Helpers
{
    public static class GraphHelper
    {
        // Load configuration settings from PrivateSettings.config
        private static string appId = ConfigurationManager.AppSettings["ida:AppId"];
        private static string appSecret = ConfigurationManager.AppSettings["ida:AppSecret"];
        private static string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
        private static string graphScopes = ConfigurationManager.AppSettings["ida:AppScopes"];

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
            // We only want displayName, mail, and userPrincipalName
            var user = await graphClient.Me.Request()
                .Select(u => new { 
                    u.DisplayName,
                    u.Mail,
                    //u.MailboxSettings, TEMPORARY UNTIL SERVICE BUG IS FIXED
                    u.UserPrincipalName
                })
                .GetAsync();

            // Get the user's profile photo
            var profilePhotoUri = await GetUserPhotoAsDataUriAsync(graphClient, "48x48");

            // Get the user's mailbox settings, for
            // timezone and date/time format
            var mailboxSettings = await GetUserMailboxSettingsAsync(graphClient);

            return new CachedUser
            {
                Avatar = profilePhotoUri,
                DateFormat = mailboxSettings.DateFormat,
                DisplayName = user.DisplayName,
                // Personal accounts don't have their mail property set
                // In this case, fallback on userPrincipalName
                Email = string.IsNullOrEmpty(user.Mail) ? 
                    user.UserPrincipalName : user.Mail,
                TimeFormat = mailboxSettings.TimeFormat,
                TimeZone = mailboxSettings.TimeZone
            };
        }

        public static async Task<string> GetUserPhotoAsDataUriAsync(GraphServiceClient graphClient = null, string size = null)
        {
            if (graphClient == null)
            {
                graphClient = GetAuthenticatedClient();
            }

            Stream photoStream;

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

            return $"data:image/png;base64,{Convert.ToBase64String(photoBytes)}";

        }

        // Temporary workaround for service bug
        // The way Graph SDK requests mailbox settings errors
        // unless you have MailboxSettings.ReadWrite
        public static async Task<MailboxSettings> GetUserMailboxSettingsAsync(GraphServiceClient graphClient)
        {
            var requestUrl = graphClient.Me.AppendSegmentToRequestUrl("/mailboxsettings");
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            var response = await graphClient.HttpProvider.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return graphClient.HttpProvider.Serializer.DeserializeObject<MailboxSettings>(content);
            }
            else
            {
                throw new ServiceException(
                    new Error
                    {
                        Code = response.StatusCode.ToString(),
                        Message = await response.Content.ReadAsStringAsync()
                    });
            }
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
                    var scopes = graphScopes.Split(' ');
                        var result = await idClient.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();

                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", result.AccessToken);
                    }));
        }
    }
}