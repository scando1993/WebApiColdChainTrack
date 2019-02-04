using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.OAuth;
using Owin;
using ColdChainTrack.Auth.Providers;
using ColdChainTrack.Auth.Models;
using Microsoft.Owin.Security.Facebook;

namespace ColdChainTrack.Auth
{
    public partial class Startup
    {
        public static OAuthAuthorizationServerOptions OAuthOptions { get; private set; }
        public static GoogleOAuth2AuthenticationOptions googleAuthOptions { get; private set; }
        public static FacebookAuthenticationOptions facebookAuthOptions { get; private set; }

        public static string PublicClientId { get; private set; }

        // For more information on configuring authentication, please visit https://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Configure the db context and user manager to use a single instance per request
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);

            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Configure the application for OAuth based flow
            PublicClientId = "self";
            OAuthOptions = new OAuthAuthorizationServerOptions
            {
                TokenEndpointPath = new PathString("/api/Account/Token"),
                Provider = new ApplicationOAuthProvider(PublicClientId),
                AuthorizeEndpointPath = new PathString("/api/Account/ExternalLogin"),
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(14),
                // In production mode set AllowInsecureHttp = false
                AllowInsecureHttp = true
            };
            //Enable CORS
            app.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);
            // Enable the application to use bearer tokens to authenticate users
            app.UseOAuthBearerTokens(OAuthOptions);

            //Configure Google External Login
            googleAuthOptions = new GoogleOAuth2AuthenticationOptions()
            {
                ClientId = Properties.Settings.Default.GoogleClientId,
                ClientSecret = Properties.Settings.Default.GoogleClientSecret,
                Provider = new GoogleAuthProvider()
            };
            googleAuthOptions.Scope.Add("https://www.googleapis.com/auth/userinfo.email");
            googleAuthOptions.Scope.Add("https://www.googleapis.com/auth/userinfo.profile");
            googleAuthOptions.Scope.Add("https://www.googleapis.com/auth/plus.me");
            googleAuthOptions.Scope.Add("https://www.googleapis.com/auth/plus.login");
            app.UseGoogleAuthentication(googleAuthOptions);

            //Configure Facebook External Login
            facebookAuthOptions = new FacebookAuthenticationOptions()
            {
                SignInAsAuthenticationType = DefaultAuthenticationTypes.ExternalCookie,
                AppId = Properties.Settings.Default.FacebookAppId,
                AppSecret = Properties.Settings.Default.FacebookAppSecret,
                Provider = new FacebookAuthProvider()
            };
            facebookAuthOptions.Scope.Add("email");
            facebookAuthOptions.Scope.Add("public_profile");
            facebookAuthOptions.Scope.Add("user_birthday");

            facebookAuthOptions.Fields.Add("email");
            facebookAuthOptions.Fields.Add("first_name");
            facebookAuthOptions.Fields.Add("last_name");
            facebookAuthOptions.Fields.Add("birthday");
            facebookAuthOptions.Fields.Add("picture");
            facebookAuthOptions.Fields.Add("gender");
            //facebookAuthOptions.Scope.Add("user_birhtday");
            //facebookAuthOptions.Scope.Add("birthday");
            //facebookAuthOptions.Scope.Add("gender");
            //facebookAuthOptions.Scope.Add("profile_pic");
            app.UseFacebookAuthentication(facebookAuthOptions);

            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //    consumerKey: "",
            //    consumerSecret: "");

            //app.UseFacebookAuthentication(
            //    appId: "",
            //    appSecret: "");

            //app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
            //{
            //    ClientId = "",
            //    ClientSecret = ""
            //});
        }
    }
}
