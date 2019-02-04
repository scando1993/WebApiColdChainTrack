using Microsoft.Owin.Security.Facebook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace ColdChainTrack.Auth.Providers
{
    public class FacebookAuthProvider : FacebookAuthenticationProvider
    {
        public override Task Authenticated(FacebookAuthenticatedContext context)
        {
            context.Identity.AddClaim(new Claim("ExternalAccessToken", context.AccessToken));
            context.Identity.AddClaim(new Claim(ClaimTypes.DateOfBirth, context.User["birthday"].ToString()));
            context.Identity.AddClaim(new Claim(ClaimTypes.GivenName, context.User["first_name"].ToString()));
            context.Identity.AddClaim(new Claim(ClaimTypes.Surname, context.User["last_name"].ToString()));
            context.Identity.AddClaim(new Claim(ClaimTypes.Gender, context.User["gender"].ToString()));
            //context.Identity.AddClaim(new Claim("profile_picture", "https://graph.facebook.com/" + context.Id + "/picture?type=large&w%E2%80%8C%E2%80%8Bidth=720&height=720"));
            return Task.FromResult<object>(null);
        }
    }
}