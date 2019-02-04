using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using ColdChainTrack.Auth.Models;
using ColdChainTrack.Auth.Providers;
using ColdChainTrack.Auth.Results;
using ColdChainTrack.Auth.Service;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;

namespace ColdChainTrack.Auth.Controllers
{
    [Authorize]
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        private const string LocalLoginProvider = "Local";
        private ApplicationUserManager _userManager;
        private ApplicationDbContext db = new ApplicationDbContext();
        private bool UseEmailService = Properties.Settings.Default.UseEmailService;
        public bool debug = Properties.Settings.Default.LocalMode;
        private StandardResponse StandardResponse = new StandardResponse();

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager,
            ISecureDataFormat<AuthenticationTicket> accessTokenFormat)
        {
            UserManager = userManager;
            AccessTokenFormat = accessTokenFormat;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ISecureDataFormat<AuthenticationTicket> AccessTokenFormat { get; private set; }

        // GET api/Account/UserInfo
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("UserInfo")]
        public UserInfoViewModel GetUserInfo()
        {
            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            return new UserInfoViewModel
            {
                Email = User.Identity.GetUserName(),
                HasRegistered = externalLogin == null,
                LoginProvider = externalLogin != null ? externalLogin.LoginProvider : null
            };
        }

        // GET api/Account/UserInfo
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("Profile")]
        public ProfileViewModel GetProfile()
        {
            ApplicationUser user = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(HttpContext.Current.User.Identity.GetUserId());


            if (user != null)
            {
                ProfileViewModel UserProfile = new ProfileViewModel();
                UserProfile.DayOfBirth = user.DayOfBirth;
                UserProfile.Email = user.Email;
                UserProfile.Gender = user.Gender;
                UserProfile.Id = user.Id;
                UserProfile.IdNumber = user.IdNumber;
                UserProfile.GivenName = user.Name;
                UserProfile.Surname = user.LastName;
                UserProfile.PhoneNumber = user.PhoneNumber;
                UserProfile.Username = user.UserName;
                UserProfile.EmailConfirmed = user.EmailConfirmed;
                if (!String.IsNullOrWhiteSpace(user.ProfileImage))
                {
                    if (user.ProfileImage.Contains("facebook"))
                    {
                        UserProfile.ProfileImage = user.ProfileImage;
                    }
                    else
                    {
                        UserProfile.ProfileImage = Properties.Settings.Default.ProfileImagesServerPath + user.ProfileImage + "?ts=" + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
                    }
                }
                else
                {
                    UserProfile.ProfileImage = Properties.Settings.Default.ProfileImagesServerPath + "default.jpg" + "?ts=" + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
                }
                var UserLogins = user.Logins.ToList();
                List<UserLoginInfoViewModel> l = new List<UserLoginInfoViewModel>();
                foreach (var Login in UserLogins)
                {
                    UserLoginInfoViewModel lp = new UserLoginInfoViewModel();
                    lp.LoginProvider = Login.LoginProvider;
                    lp.ProviderKey = Login.ProviderKey;
                    l.Add(lp);

                }
                UserProfile.Logins = l;
                return UserProfile;
            }
            else
            {
                throw new HttpResponseException(HttpStatusCode.InternalServerError); //500
            }

        }

        // GET api/Account/UpdateProfile
        [Authorize]
        [Route("UpdateProfile")]
        public IHttpActionResult UpdateProfile(ProfileBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ApplicationUser user = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            user.DayOfBirth = model.DayOfBirth;
            user.Gender = model.Gender;
            user.IdNumber = model.IdNumber;
            user.Name = model.GivenName;
            user.LastName = model.SurName;
            user.PhoneNumber = model.PhoneNumber;

            IdentityResult result = UserManager.Update(user);

            if (!result.Succeeded)
            {
                return BadRequest(ModelState);
            }

            return Ok(new { Code = "OK", Message = "Usuario Actualizado" });
        }

        // POST api/Account/Logout
        [Route("Logout")]
        public IHttpActionResult Logout()
        {
            Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
            return Ok();
        }

        // GET api/Account/ManageInfo?returnUrl=%2F&generateState=true
        [Route("ManageInfo")]
        public async Task<ManageInfoViewModel> GetManageInfo(string returnUrl, bool generateState = false)
        {
            IdentityUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            if (user == null)
            {
                return null;
            }

            List<UserLoginInfoViewModel> logins = new List<UserLoginInfoViewModel>();

            foreach (IdentityUserLogin linkedAccount in user.Logins)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = linkedAccount.LoginProvider,
                    ProviderKey = linkedAccount.ProviderKey
                });
            }

            if (user.PasswordHash != null)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = LocalLoginProvider,
                    ProviderKey = user.UserName,
                });
            }

            return new ManageInfoViewModel
            {
                LocalLoginProvider = LocalLoginProvider,
                Email = user.UserName,
                Logins = logins,
                ExternalLoginProviders = GetExternalLogins(returnUrl, generateState)
            };
        }

        // POST api/Account/ChangePassword
        [Route("ChangePassword")]
        public async Task<IHttpActionResult> ChangePassword(ChangePasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword,
                model.NewPassword);
            
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }
            else
            {
                if (UseEmailService)
                {
                    //enviar de notificacion
                    ApplicationUser user = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
                    List<string> ReplaceData = new List<string>();
                    ReplaceData.Add(user.Name);
                    EmailService EmailService = new EmailService();
                    string xResponse;
                    xResponse = await EmailService.SendEmail_WithTemplate("Notificación de Cambio de Contraseña", "mails", "notify-change-pass", ReplaceData, user.UserName, null, null);
                }
            }
            StandardResponse.SetOk("", "OK");
            return Ok(StandardResponse);
        }
        // POST api/Account/SetPassword
        //[Route("SetPassword")]
        //public async Task<IHttpActionResult> SetPassword(SetPasswordBindingModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);

        //    if (!result.Succeeded)
        //    {
        //        return GetErrorResult(result);
        //    }

        //    return Ok();
        //}

        // POST api/Account/AddExternalLogin
        [Route("AddExternalLogin")]
        public async Task<IHttpActionResult> AddExternalLogin(AddExternalLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

            AuthenticationTicket ticket = AccessTokenFormat.Unprotect(model.ExternalAccessToken);

            if (ticket == null || ticket.Identity == null || (ticket.Properties != null
                && ticket.Properties.ExpiresUtc.HasValue
                && ticket.Properties.ExpiresUtc.Value < DateTimeOffset.UtcNow))
            {
                return BadRequest("External login failure.");
            }

            ExternalLoginData externalData = ExternalLoginData.FromIdentity(ticket.Identity);

            if (externalData == null)
            {
                return BadRequest("The external login is already associated with an account.");
            }

            IdentityResult result = await UserManager.AddLoginAsync(User.Identity.GetUserId(),
                new UserLoginInfo(externalData.LoginProvider, externalData.ProviderKey));

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/RemoveLogin
        [Route("RemoveLogin")]
        public async Task<IHttpActionResult> RemoveLogin(RemoveLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result;

            if (model.LoginProvider == LocalLoginProvider)
            {
                result = await UserManager.RemovePasswordAsync(User.Identity.GetUserId());
            }
            else
            {
                result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(),
                    new UserLoginInfo(model.LoginProvider, model.ProviderKey));
            }

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogin
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalCookie)]
        [AllowAnonymous]
        [Route("ExternalLogin", Name = "ExternalLogin")]
        public async Task<IHttpActionResult> GetExternalLogin(string provider, string error = null)
        {
            string redirectUri = string.Empty;

            if (error != null)
            {
                return Redirect(Url.Content("~/") + "#error=" + Uri.EscapeDataString(error));
            }

            if (!User.Identity.IsAuthenticated)
            {
                return new ChallengeResult(provider, this);
            }

            //var redirectUriValidationResult = ValidateClientAndRedirectUri(this.Request, ref redirectUri);

            //if (!string.IsNullOrWhiteSpace(redirectUriValidationResult))
            //{
            //    return BadRequest(redirectUriValidationResult);
            //}

            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            if (externalLogin == null)
            {
                return InternalServerError();
            }

            if (externalLogin.LoginProvider != provider)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                return new ChallengeResult(provider, this);
            }

            ApplicationUser user = await UserManager.FindAsync(new UserLoginInfo(externalLogin.LoginProvider,
                externalLogin.ProviderKey));

            bool hasRegistered = user != null;

            if (hasRegistered)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                
                 ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(UserManager,
                    OAuthDefaults.AuthenticationType);
                ClaimsIdentity cookieIdentity = await user.GenerateUserIdentityAsync(UserManager,
                    CookieAuthenticationDefaults.AuthenticationType);

                AuthenticationProperties properties = ApplicationOAuthProvider.CreateProperties(user.UserName);
                Authentication.SignIn(properties, oAuthIdentity, cookieIdentity);

                //var accessTokenResponse = GenerateLocalAccessTokenResponse(user.UserName);
                //return Ok(accessTokenResponse);
            }
            else
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                //IEnumerable<Claim> claims = externalLogin.GetClaims();
                //ClaimsIdentity identity = new ClaimsIdentity(claims, OAuthDefaults.AuthenticationType);
                //Authentication.SignIn(identity);
                ExternalLoginData NewExternalLoginData = new ExternalLoginData
                {
                    FirstName = externalLogin.FirstName,
                    LastName = externalLogin.LastName,
                    UserName = externalLogin.UserName,
                    DateOfBirthday = externalLogin.DateOfBirthday,
                    Gender = externalLogin.Gender
                };
                return Ok(NewExternalLoginData);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogins?returnUrl=%2F&generateState=true
        [AllowAnonymous]
        [Route("ExternalLogins")]
        public IEnumerable<ExternalLoginViewModel> GetExternalLogins(string returnUrl, bool generateState = false)
        {
            IEnumerable<AuthenticationDescription> descriptions = Authentication.GetExternalAuthenticationTypes();
            List<ExternalLoginViewModel> logins = new List<ExternalLoginViewModel>();

            string state;

            if (generateState)
            {
                const int strengthInBits = 256;
                state = RandomOAuthStateGenerator.Generate(strengthInBits);
            }
            else
            {
                state = null;
            }

            foreach (AuthenticationDescription description in descriptions)
            {
                ExternalLoginViewModel login = new ExternalLoginViewModel
                {
                    Name = description.Caption,
                    Url = Url.Route("ExternalLogin", new
                    {
                        provider = description.AuthenticationType,
                        response_type = "token",
                        client_id = Startup.PublicClientId,
                        redirect_uri = new Uri(Request.RequestUri, returnUrl).AbsoluteUri,
                        state = state
                    }),
                    State = state
                };
                logins.Add(login);
            }

            return logins;
        }

        [HttpPost]
        [Route("UploadProfileImageBase64")]
        public HttpResponseMessage UploadProfileImageBase64(ImageBase64VM ImageVM)
        {
            ApplicationUser user = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            var bytes = Convert.FromBase64String(ImageVM.Image);// a.base64image 
                                                                //or full path to file in temp location
                                                                //var filePath = Path.GetTempFileName();

            string filePath = Properties.Settings.Default.ProfileImageStoragePath + "\\" + user.Id + ".jpg";

            user.ProfileImage = user.Id + ".jpg";

            IdentityResult result = UserManager.Update(user);

            if (bytes.Length > 0)
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }
            }
            return Request.CreateErrorResponse(HttpStatusCode.Created, user.ProfileImage);
        }

        [HttpPost]
        [Route("UploadProfileImage")]
        public HttpResponseMessage UploadProfileImage()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            ApplicationUser user = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            try
            {
                var httpRequest = HttpContext.Current.Request;

                foreach (string file in httpRequest.Files)
                {
                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.Created);

                    var postedFile = httpRequest.Files[file];
                    if (postedFile != null && postedFile.ContentLength > 0)
                    {

                        int MaxContentLength = 1024 * 1024 * 1; //Size = 1 MB

                        IList<string> AllowedFileExtensions = new List<string> { ".jpg", ".gif", ".png" };
                        var ext = postedFile.FileName.Substring(postedFile.FileName.LastIndexOf('.'));
                        var extension = ext.ToLower();
                        if (!AllowedFileExtensions.Contains(extension))
                        {

                            var message = string.Format("Por favor suba un archivo de tipo .jpg,.gif,.png.");

                            dict.Add("error", message);
                            return Request.CreateResponse(HttpStatusCode.BadRequest, dict);
                        }
                        else if (postedFile.ContentLength > MaxContentLength)
                        {

                            var message = string.Format("Solo puede subir un archivo de hasta 1 Mb.");

                            dict.Add("error", message);
                            return Request.CreateResponse(HttpStatusCode.BadRequest, dict);
                        }
                        else
                        {
                            var filePath = Properties.Settings.Default.ProfileImageStoragePath + "\\" + user.Id + extension;

                            user.ProfileImage = user.Id + extension;

                            IdentityResult result = UserManager.Update(user);

                            //Userimage myfolder name where i want to save my image
                            postedFile.SaveAs(filePath);

                        }
                    }

                    var message1 = user.ProfileImage; //string.Format("Imagen subida correctamente.");
                    if (user.ProfileImage.Contains("facebook"))
                    {
                        message1 = user.ProfileImage;
                    }
                    else
                    {
                        message1 = Properties.Settings.Default.ProfileImagesServerPath + user.ProfileImage;//Properties.Settings.Default.DefaultAPI + "ProfileImages/" + user.Url;
                    }
                    return Request.CreateErrorResponse(HttpStatusCode.Created, message1);
                }
                var res = string.Format("Por favor suba una imagen.");
                dict.Add("error", res);
                return Request.CreateResponse(HttpStatusCode.NotFound, dict);
            }
            catch (Exception ex)
            {
                var res = string.Format(ex.Message);
                dict.Add("error", res);
                return Request.CreateResponse(HttpStatusCode.NotFound, dict);
            }
        }
        // POST api/Account/Register
        [AllowAnonymous]
        [Route("Register")]
        public async Task<IHttpActionResult> Register(RegisterBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var CheckCedula = db.Users.Where(x => x.IdNumber == model.IdNumber).FirstOrDefault();
            var CheckEmail = db.Users.Where(x => x.Email == model.Email).FirstOrDefault();
            if (CheckCedula != null || CheckEmail != null)
            {
                string ErrorMessage = "";
                if (CheckCedula != null)
                {
                    ErrorMessage = "The Identity Document already exists.";
                }
                else if (CheckEmail != null)
                {
                    ErrorMessage = ErrorMessage + "The Email already exists.";
                }
                StandardResponse.SetError("00", ErrorMessage);
                return Ok(StandardResponse);
            }

            var user = new ApplicationUser() { UserName = model.Email, Email = model.Email, Name = model.GivenName, LastName = model.SurName, IdNumber = model.IdNumber };

            IdentityResult result = await UserManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }
            else
            {
                if (UseEmailService)
                {

                    //Send Confirmation Email
                    string code = await this.UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    var callbackUrl = "";

                    if (debug)
                    {
                        //callbackUrl = "http://localhost:" + Properties.Settings.Default.LocalWebPort + Properties.Settings.Default.ProjectName + Properties.Settings.Default.AccountRedirectUri + "ConfirmEmail?userId=" + user.Id + "&code=" + HttpContext.Current.Server.UrlEncode(code);
                    }
                    else
                    {
                        callbackUrl = Properties.Settings.Default.DefaultWebUrl + Properties.Settings.Default.ConfirmEmailWebRoute + "?userId=" + user.Id + "&code=" + HttpContext.Current.Server.UrlEncode(code);
                    }
                    List<string> ReplaceData = new List<string>(); //Agregar los datos de Reemplazo acorde al Template.
                    ReplaceData.Add(user.Name);
                    ReplaceData.Add(callbackUrl);

                    EmailService EmailService = new EmailService();
                    string xResponse;
                    try
                    {
                        xResponse = await EmailService.SendEmail_WithTemplate("Confirma tu Cuenta", "mails", "conf-email", ReplaceData, user.UserName, null, null);
                    }
                    catch (Exception)
                    {
                        StandardResponse.SetError("00", "No se pudo enviar el correo de confirmación de cuenta.");
                        return Ok(StandardResponse);
                    }
                }
            }
            StandardResponse.SetOk("00", "OK");
            return Ok(StandardResponse);
        }

        // POST api/Account/RegisterExternal
        [AllowAnonymous]
        [Route("RegisterExternal")]
        public async Task<IHttpActionResult> RegisterExternal(RegisterExternalBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var verifiedAccessToken = await VerifyExternalAccessToken(model.Provider, model.ExternalAccessToken);
            if (verifiedAccessToken == null)
            {
                return BadRequest("Invalid Provider or External Access Token");
            }

            var CheckCedula = db.Users.Where(x => x.IdNumber == model.IdNumber).FirstOrDefault();
            var CheckEmail = db.Users.Where(x => x.Email == model.Email).FirstOrDefault();
            if(CheckCedula != null || CheckEmail != null)
            {
                string ErrorMessage = "";
                if(CheckCedula != null)
                {
                    ErrorMessage = "The Identity Document already exists";
                }
                else if (CheckEmail != null)
                {
                    ErrorMessage = "The Email already exists";
                }
                return BadRequest(ErrorMessage);
            }

            ApplicationUser user = await UserManager.FindAsync(new UserLoginInfo(model.Provider, verifiedAccessToken.user_id));

            bool hasRegistered = user != null;

            if (hasRegistered)
            {
                return BadRequest("El usuario de " + model.Provider + " ya ha sido registrado.");
            }

            var info = new ExternalLoginInfo()
            {
                DefaultUserName = model.Email,
                Login = new UserLoginInfo(model.Provider, verifiedAccessToken.user_id)
            };
            //var info = await Authentication.GetExternalLoginInfoAsync();
            //if (info == null)
            //{
            //    return InternalServerError();
            //}

            user = new ApplicationUser() {
                UserName = model.Email,
                Email = model.Email,
                IdNumber = model.IdNumber,
                Name = model.GivenName,
                LastName = model.Surname
            };

            IdentityResult result = await UserManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            result = await UserManager.AddLoginAsync(user.Id, info.Login);
            if (!result.Succeeded)
            {
                return GetErrorResult(result); 
            }

            var accessTokenResponse = GenerateLocalAccessTokenResponse(user.UserName, user.Id);
            return Ok(accessTokenResponse);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("ObtainLocalAccessToken")]
        public async Task<IHttpActionResult> ObtainLocalAccessToken(string provider, string externalAccessToken)
        {

            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(externalAccessToken))
            {
                return BadRequest("Provider or external access token is not sent");
            }

            var verifiedAccessToken = await VerifyExternalAccessToken(provider, externalAccessToken);
            if (verifiedAccessToken == null)
            {
                return BadRequest("Invalid Provider or External Access Token");
            }

            IdentityUser user = await UserManager.FindAsync(new UserLoginInfo(provider, verifiedAccessToken.user_id));

            bool hasRegistered = user != null;

            if (!hasRegistered)
            {
                return BadRequest("External user is not registered");
            }

            //generate access token response
            var accessTokenResponse = GenerateLocalAccessTokenResponse(user.UserName, user.Id);

            return Ok(accessTokenResponse);

        }

        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("ResetPassword")]
        public async Task<IHttpActionResult> ResetPassword(string Email)
        {
            if (Email != null)
            {
                ApplicationDbContext db = new ApplicationDbContext();

                ApplicationUser User = db.Users.Where(x => x.Email == Email).FirstOrDefault();

                if (User != null)
                {
                    Guid GuidByEmail = new Guid(User.Id);
                    byte[] time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
                    byte[] key = GuidByEmail.ToByteArray();
                    string code = Convert.ToBase64String(time.Concat(key).ToArray());
                    var callbackUrl = "";
                    //Envia mail
                    if (debug)
                    {
                        //callbackUrl = "http://localhost:" + Properties.Settings.Default.LocalWebPort + Properties.Settings.Default.ProjectName + Properties.Settings.Default.AccountRedirectUri + "auth/ChangePassword?code=" + HttpContext.Current.Server.UrlEncode(code);
                    }
                    else
                    {
                        callbackUrl = Properties.Settings.Default.DefaultWebUrl + Properties.Settings.Default.ResetPasswordWebRoute + "?code=" + HttpContext.Current.Server.UrlEncode(code);
                    }
                    if (UseEmailService)
                    {
                        List<string> ReplaceData = new List<string>();
                        ReplaceData.Add(User.Name);
                        ReplaceData.Add(callbackUrl);

                        EmailService EmailService = new EmailService();
                        string xResponse;
                        try
                        {
                            xResponse = await EmailService.SendEmail_WithTemplate("Cambio de Contraseña", "mails", "reset-pass", ReplaceData, Email, null, null);
                        }
                        catch (Exception)
                        {
                            StandardResponse.SetError("00", "No se pudo enviar el correo de restablecer contraseña.");
                            return Ok(StandardResponse);
                        }
                    }
                }
                else
                {
                    StandardResponse.SetError("00", "No se encontró al usuario.");
                    return Ok(StandardResponse);
                }
            }
            else
            {
                ModelState.AddModelError("00", "El parámetro Email es obligatorio");
                return BadRequest(ModelState);
            }
            StandardResponse.SetOk("","OK");
            return Ok(StandardResponse);
        }

        [Route("ChangeEmail")]
        public async Task<IHttpActionResult> ChangeEmail(ChangeEmailRequest request)
        {
            if (request != null)
            {
                ApplicationDbContext db = new ApplicationDbContext();
                var CheckEmail = db.Users.Where(x => x.Email == request.NewEmail).FirstOrDefault();
                if (CheckEmail != null)
                {
                    StandardResponse.SetError("00", "El email no está disponible.");
                    return Ok(StandardResponse);
                }

                ApplicationUser user = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());

                if (user != null)
                {
                    //Guid GuidByEmail = new Guid(user.Id);
                    byte[] time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
                    byte[] newmail = Encoding.ASCII.GetBytes(request.NewEmail);
                    string code = Convert.ToBase64String(time.Concat(newmail).ToArray());

                    //Envia mail
                    var callbackUrl = "";
                    if (debug)
                    {
                        //callbackUrl = "http://localhost:" + Properties.Settings.Default.LocalWebPort + Properties.Settings.Default.ProjectName + Properties.Settings.Default.AccountRedirectUri + "ConfirmNewEmail?userId=" + user.Id + "&code=" + HttpContext.Current.Server.UrlEncode(code);
                    }
                    else
                    {
                        callbackUrl = Properties.Settings.Default.DefaultWebUrl + Properties.Settings.Default.ChangeEmailWebRoute + "?userId=" + user.Id + "&code=" + HttpContext.Current.Server.UrlEncode(code);
                    }

                    List<string> ReplaceData = new List<string>();
                    ReplaceData.Add(user.Name);
                    ReplaceData.Add(callbackUrl);

                    EmailService EmailService = new EmailService();
                    string xResponse;
                    try
                    {
                        xResponse = await EmailService.SendEmail_WithTemplate("Cambio de Email", "mails",
                                                                                "change-email", ReplaceData, request.NewEmail, null, null);
                    }
                    catch (Exception)
                    {
                        StandardResponse.SetError("00", "No se pudo enviar el mail de cambio de email.");
                        return Ok(StandardResponse);
                    }

                }
                else
                {
                    StandardResponse.SetError("00", "El usuario no existe.");
                    return Ok(StandardResponse);
                }
            }
            else
            {
                StandardResponse.SetError("00", "El parametro es nulo.");
                return Ok(StandardResponse);
            }
            StandardResponse.SetOk("00", "OK");
            return Ok(StandardResponse);
        }
        // GET api/Account/SendConfirmEmail
        [HttpGet]
        [Route("SendConfirmEmail")]
        public async Task<IHttpActionResult> SendConfirmEmail()
        {
            ApplicationUser user = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            //Envia mail
            string code = await this.UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
            var callbackUrl = "";

            if (debug)
            {
                //callbackUrl = "http://localhost:" + Properties.Settings.Default.LocalWebPort + Properties.Settings.Default.ProjectName + Properties.Settings.Default.AccountRedirectUri + "ConfirmEmail?userId=" + user.Id + "&code=" + HttpContext.Current.Server.UrlEncode(code);
            }
            else
            {
                callbackUrl = Properties.Settings.Default.DefaultWebUrl + Properties.Settings.Default.ConfirmEmailWebRoute + "?userId=" + user.Id + "&code=" + HttpContext.Current.Server.UrlEncode(code);
            }

            List<string> ReplaceData = new List<string>(); //Agregar los datos de Reemplazo acorde al Template.
            ReplaceData.Add(user.Name);
            ReplaceData.Add(callbackUrl);

            EmailService EmailService = new EmailService();
            string xResponse;
            try
            {
                xResponse = await EmailService.SendEmail_WithTemplate("Confirma tu Cuenta", "mails", "conf-email", ReplaceData, user.UserName, null, null);
            }
            catch (Exception)
            {
                StandardResponse.SetError("00", "No se pudo enviar el mail de confirmación de cuenta.");
                return Ok(StandardResponse);
            }

            StandardResponse.SetOk("", "OK");
            return Ok(StandardResponse);
        }

        [Authorize(Roles = "AccountAdmin")]
        [HttpPost]
        [Route("CreateByAdmin")]
        public async Task<IHttpActionResult> CreateByAdminAsync(RegisterByAdminBindingModel model)
        {
            string userId = HttpContext.Current.User.Identity.GetUserId();
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ApplicationDbContext db = new ApplicationDbContext();

            var CheckCedula = db.Users.Where(x => x.IdNumber == model.IdNumber).FirstOrDefault();
            var CheckEmail = db.Users.Where(x => x.Email == model.Email).FirstOrDefault();
            if (CheckCedula != null || CheckEmail != null)
            {
                string ErrorMessage = "";
                if (CheckCedula != null)
                {
                    ErrorMessage = "The Identity Document already exists.";
                }
                else if (CheckEmail != null)
                {
                    ErrorMessage = ErrorMessage + "The Email already exists.";
                }
                StandardResponse.SetError("00", ErrorMessage);
                return Ok(StandardResponse);
            }

            if (CheckCedula == null && CheckEmail == null)
            {
                var user = new ApplicationUser() { UserName = model.Email, Email = model.Email, Name = model.Name, IdNumber = model.IdNumber, ProfileImage = "images/default-avatar.png", CreatedBy = userId, CreationDate = DateTime.Now }; // 
                string password = "";

                try
                {
                    password = model.IdNumber;
                }
                catch (Exception ex)
                {
                    password = model.IdNumber;
                }
                IdentityResult result = await UserManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    StandardResponse.SetError("00","No se pudo crear la cuenta.");
                    return Ok(StandardResponse);
                }
                else
                {
                    if (UseEmailService)
                    {
                        //Envia mail
                        string code = await this.UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                        var callbackUrl = "";
                        string codeUrlEncode = "";
                        try
                        {
                            codeUrlEncode = HttpContext.Current.Server.UrlEncode(code);
                        }
                        catch (Exception ex)
                        {
                            codeUrlEncode = "test";
                        }

                        if (debug)
                        {
                            //callbackUrl = Properties.Settings.Default.DefaultWebUrl + Properties.Settings.Default.ConfirmEmailWebRoute + "?userId=" + user.Id + "&code=" + HttpContext.Current.Server.UrlEncode(code);
                        }
                        else
                        {
                            callbackUrl = Properties.Settings.Default.DefaultWebUrl + Properties.Settings.Default.ConfirmEmailWebRoute + "?userId=" + user.Id + "&code=" + HttpContext.Current.Server.UrlEncode(code);
                        }

                        EmailService EmailService = new EmailService();
                        string xResponse;

                        List<string> ReplaceData2 = new List<string>(); //Agregar los datos de Reemplazo acorde al Template.
                        ReplaceData2.Add(user.Name);
                        ReplaceData2.Add(user.UserName);
                        ReplaceData2.Add(password);
                        try
                        {
                            xResponse = await EmailService.SendEmail_WithTemplate("Cuenta creada por administrador", "mails", "register-by-admin", ReplaceData2, user.UserName, null, null);
                        }
                        catch (Exception)
                        {
                        }

                        List<string> ReplaceData = new List<string>(); //Agregar los datos de Reemplazo acorde al Template.
                        ReplaceData.Add(user.Name);
                        ReplaceData.Add(callbackUrl);
                        try
                        {
                            xResponse = await EmailService.SendEmail_WithTemplate("Confirma tu cuenta", "mails", "conf-email", ReplaceData, user.UserName, null, null);
                        }
                        catch (Exception)
                        {
                            StandardResponse.SetError("00", "No se pudo enviar el mail de confirmación.");
                            return Ok(StandardResponse);
                        }
                    }
                }
            }
            StandardResponse.SetOk("","OK");
            return Ok(StandardResponse);
        }

        // GET api/Account/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        [Route("ConfirmEmail", Name = "ConfirmEmailRoute")]
        public async Task<IHttpActionResult> ConfirmEmail(string userId = "", string code = "")
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                ModelState.AddModelError("00", "User Id and Code are required");
                return BadRequest(ModelState);
            }

            IdentityResult result = await this.UserManager.ConfirmEmailAsync(userId, code);

            if (result.Succeeded)
            {
                StandardResponse.SetOk("","OK");
                return Ok(StandardResponse);
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Errors.ElementAt(0).ToString());
                StandardResponse.SetError("00","No se pudo confirmar el email.");
                return Ok(StandardResponse);
            }
        }

        // POST api/Account/ConfirmNewEmail
        [HttpGet]
        [AllowAnonymous]
        [Route("ConfirmNewEmail")]
        public async Task<IHttpActionResult> ConfirmNewEmail(string userId = "", string code = "")
        {
            string NewEmail = "";
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                ModelState.AddModelError("", "User Id and Code are required");
                return BadRequest(ModelState);
            }
            try
            {
                byte[] data = Convert.FromBase64String(HttpContext.Current.Server.UrlDecode(code));
                DateTime when = DateTime.FromBinary(BitConverter.ToInt64(data, 0));
                byte[] m = data.Skip(8).ToArray();
                NewEmail = Encoding.ASCII.GetString(m); //(data,8);

                if (when < DateTime.UtcNow.AddMinutes(-10))
                {
                    StandardResponse.SetError("00", "El codigo ha expirado");
                    return Ok(StandardResponse);
                }
                else
                {
                    ApplicationUser user = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(userId);

                    user.Email = NewEmail;
                    user.UserName = NewEmail;
                    user.EmailConfirmed = true;
                    IdentityResult update = await UserManager.UpdateAsync(user);

                    if (!update.Succeeded)
                    {
                        StandardResponse.SetError("00", "No se pudo cambiar el email.");
                        return Ok(StandardResponse);
                    }
                }
                StandardResponse.SetOk("", "OK.");
                return Ok(StandardResponse);
            }
            catch (Exception ex)
            {
                StandardResponse.SetError("00", "Ocurrió un error");
                return Ok(StandardResponse);
            }
        }
        // POST api/Account/SetNewPassword
        [AllowAnonymous]
        [Route("SetNewPassword")]
        public async Task<IHttpActionResult> SetNewPassword(ResetPasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                byte[] data = Convert.FromBase64String(model.code);
                DateTime when = DateTime.FromBinary(BitConverter.ToInt64(data, 0));
                Guid OriginalGuid = new Guid(data.Skip(8).ToArray());
                if (when < DateTime.UtcNow.AddMinutes(-10))
                {
                    StandardResponse.SetError("00", "El codigo ha expirado.");
                    return Ok(StandardResponse);
                }
                else
                {
                    IdentityResult remove_result = await UserManager.RemovePasswordAsync(OriginalGuid.ToString());

                    if (remove_result.Succeeded)
                    {
                        IdentityResult result = await UserManager.AddPasswordAsync(OriginalGuid.ToString(), model.NewPassword);

                        if (!result.Succeeded)
                        {
                            StandardResponse.SetError("00", "No se pudo cambiar la contraseña.");
                            return Ok(StandardResponse);
                        }
                        else
                        {
                            if (UseEmailService)
                            {
                                //enviar de notificacion
                                ApplicationUser user = db.Users.FirstOrDefault(i => i.Id == OriginalGuid.ToString());
                                
                                List<string> ReplaceData = new List<string>();
                                ReplaceData.Add(user.Name);

                                EmailService EmailService = new EmailService();
                                string xResponse = await EmailService.SendEmail_WithTemplate("Notificación de Cambio de Contraseña", "mails", "notify-change-pass", ReplaceData, user.UserName, null, null);
                            }
                        }
                    }
                    else
                    {
                        StandardResponse.SetError("00", "No se pudo remover la contraseña anterior.");
                        return Ok(StandardResponse);
                    }

                }                
                StandardResponse.SetOk("", "OK");
                return Ok(StandardResponse);
            }
            catch (Exception ex)
            {
                StandardResponse.SetError("00", "Ocurrió un error.");
                return Ok(StandardResponse);
            }

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        #region Helpers

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return InternalServerError();
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        private class ExternalLoginData
        {
            public string LoginProvider { get; set; }
            public string ProviderKey { get; set; }
            public string UserName { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string DateOfBirthday { get; set; }
            public string Gender { get; set; }

            public IList<Claim> GetClaims()
            {
                IList<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, ProviderKey, null, LoginProvider));

                if (UserName != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, UserName, null, LoginProvider));
                }

                return claims;
            }

            public static ExternalLoginData FromIdentity(ClaimsIdentity identity)
            {
                if (identity == null)
                {
                    return null;
                }

                Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

                if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer)
                    || String.IsNullOrEmpty(providerKeyClaim.Value))
                {
                    return null;
                }

                if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
                {
                    return null;
                }

                return new ExternalLoginData
                {
                    LoginProvider = providerKeyClaim.Issuer,
                    ProviderKey = providerKeyClaim.Value,
                    UserName = identity.FindFirstValue(ClaimTypes.Email),
                    DateOfBirthday = identity.FindFirstValue(ClaimTypes.DateOfBirth),
                    FirstName = identity.FindFirstValue(ClaimTypes.GivenName),
                    LastName = identity.FindFirstValue(ClaimTypes.Surname),
                    Gender = identity.FindFirstValue(ClaimTypes.Gender)
                };
            }
        }

        private static class RandomOAuthStateGenerator
        {
            private static RandomNumberGenerator _random = new RNGCryptoServiceProvider();

            public static string Generate(int strengthInBits)
            {
                const int bitsPerByte = 8;

                if (strengthInBits % bitsPerByte != 0)
                {
                    throw new ArgumentException("strengthInBits must be evenly divisible by 8.", "strengthInBits");
                }

                int strengthInBytes = strengthInBits / bitsPerByte;

                byte[] data = new byte[strengthInBytes];
                _random.GetBytes(data);
                return HttpServerUtility.UrlTokenEncode(data);
            }
        }

        private async Task<ParsedExternalAccessToken> VerifyExternalAccessToken(string provider, string accessToken)
        {
            ParsedExternalAccessToken parsedToken = null;

            var verifyTokenEndPoint = "";

            if (provider == "Facebook")
            {
                //You can get it from here: https://developers.facebook.com/tools/accesstoken/
                //More about debug_tokn here: http://stackoverflow.com/questions/16641083/how-does-one-get-the-app-access-token-for-debug-token-inspection-on-facebook

                var appToken = Properties.Settings.Default.FacebookAppToken;
                verifyTokenEndPoint = string.Format("https://graph.facebook.com/debug_token?input_token={0}&access_token={1}", accessToken, appToken);
            }
            else if (provider == "Google")
            {
                verifyTokenEndPoint = string.Format("https://www.googleapis.com/oauth2/v1/tokeninfo?access_token={0}", accessToken);
            }
            else
            {
                return null;
            }

            var client = new HttpClient();
            var uri = new Uri(verifyTokenEndPoint);
            var response = await client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                dynamic jObj = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(content);

                parsedToken = new ParsedExternalAccessToken();

                if (provider == "Facebook")
                {
                    parsedToken.user_id = jObj["data"]["user_id"];
                    parsedToken.app_id = jObj["data"]["app_id"];

                    if (!string.Equals(Startup.facebookAuthOptions.AppId, parsedToken.app_id, StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                }
                else if (provider == "Google")
                {
                    parsedToken.user_id = jObj["user_id"];
                    parsedToken.app_id = jObj["audience"];

                    if (!string.Equals(Startup.googleAuthOptions.ClientId, parsedToken.app_id, StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                }

            }

            return parsedToken;
        }

        private JObject GenerateLocalAccessTokenResponse(string userName, string userId)
        {

            var tokenExpiration = TimeSpan.FromDays(14);

            ClaimsIdentity identity = new ClaimsIdentity(OAuthDefaults.AuthenticationType);

            identity.AddClaim(new Claim(ClaimTypes.Name, userName));
            identity.AddClaim(new Claim("role", "user"));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));

            var props = new AuthenticationProperties()
            {
                IssuedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.Add(tokenExpiration),
            };

            var ticket = new AuthenticationTicket(identity, props);

            var accessToken = Startup.OAuthOptions.AccessTokenFormat.Protect(ticket);

            JObject tokenResponse = new JObject(
                                        new JProperty("userName", userName),
                                        new JProperty("access_token", accessToken),
                                        new JProperty("token_type", "bearer"),
                                        new JProperty("expires_in", tokenExpiration.TotalSeconds.ToString()),
                                        new JProperty(".issued", ticket.Properties.IssuedUtc.ToString()),
                                        new JProperty(".expires", ticket.Properties.ExpiresUtc.ToString()));

            return tokenResponse;
        }
        
        //private string ValidateClientAndRedirectUri(HttpRequestMessage request, ref string redirectUriOutput)
        //{

        //    Uri redirectUri;

        //    var redirectUriString = GetQueryString(Request, "redirect_uri");

        //    if (string.IsNullOrWhiteSpace(redirectUriString))
        //    {
        //        return "redirect_uri is required";
        //    }

        //    bool validUri = Uri.TryCreate(redirectUriString, UriKind.Absolute, out redirectUri);

        //    if (!validUri)
        //    {
        //        return "redirect_uri is invalid";
        //    }

        //    var clientId = GetQueryString(Request, "client_id");

        //    if (string.IsNullOrWhiteSpace(clientId))
        //    {
        //        return "client_Id is required";
        //    }

        //    var client = _repo.FindClient(clientId);

        //    if (client == null)
        //    {
        //        return string.Format("Client_id '{0}' is not registered in the system.", clientId);
        //    }

        //    if (!string.Equals(client.AllowedOrigin, redirectUri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
        //    {
        //        return string.Format("The given URL is not allowed by Client_id '{0}' configuration.", clientId);
        //    }

        //    redirectUriOutput = redirectUri.AbsoluteUri;

        //    return string.Empty;

        //}

        //private string GetQueryString(HttpRequestMessage request, string key)
        //{
        //    var queryStrings = request.GetQueryNameValuePairs();

        //    if (queryStrings == null) return null;

        //    var match = queryStrings.FirstOrDefault(keyValue => string.Compare(keyValue.Key, key, true) == 0);

        //    if (string.IsNullOrEmpty(match.Value)) return null;

        //    return match.Value;
        //}
        #endregion
    }
}
