﻿using Microsoft.Owin.Security.Google;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace ColdChainTrack.Auth.Providers
{
    public class GoogleAuthProvider : IGoogleOAuth2AuthenticationProvider
    {
        public void ApplyRedirect(GoogleOAuth2ApplyRedirectContext context)
        {
            context.Response.Redirect(context.RedirectUri);
        }

        public Task Authenticated(GoogleOAuth2AuthenticatedContext context)
        {
            context.Identity.AddClaim(new Claim("ExternalAccessToken", context.AccessToken));
            //context.Identity.AddClaim(new Claim(ClaimTypes.DateOfBirth, context.User["birthday"].ToString()));
            //context.Identity.AddClaim(new Claim(ClaimTypes.GivenName, context.User["name"]["givenname"].ToString()));
            //context.Identity.AddClaim(new Claim(ClaimTypes.Surname, context.User["name"]["familyname"].ToString()));
            //context.Identity.AddClaim(new Claim(ClaimTypes.Gender, context.User["gender"].ToString()));
            return Task.FromResult<object>(null);
        }

        public Task ReturnEndpoint(GoogleOAuth2ReturnEndpointContext context)
        {
            return Task.FromResult<object>(null);
        }
    }
}