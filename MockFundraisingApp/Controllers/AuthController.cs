using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MockFundraisingApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && !Url.IsLocalUrl(returnUrl))
                returnUrl = "/";

            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;

            ViewBag.Firebase = new
            {
                apiKey = _config["Firebase:WebApiKey"],
                authDomain = _config["Firebase:AuthDomain"],
                projectId = _config["Firebase:ProjectId"]
            };

            return View(new ViewModels.LoginVm());
        }


        public sealed class SessionRequest
        {
            public string IdToken { get; set; } = "";
            public string? ReturnUrl { get; set; }
            public bool AccountCreated { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Session([FromBody] SessionRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.IdToken))
                return BadRequest("Missing idToken.");

            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(req.IdToken);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, decoded.Uid),
                new Claim("firebase_uid", decoded.Uid)
            };

            if (decoded.Claims.TryGetValue("email", out var emailObj) && emailObj is string email)
                claims.Add(new Claim(ClaimTypes.Email, email));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

    
            if (req.AccountCreated)
            {
                TempData["Ok"] = "Your account has been created successfully.";
            }

  
            var returnUrl = "/";
            if (!string.IsNullOrWhiteSpace(req.ReturnUrl) && Url.IsLocalUrl(req.ReturnUrl))
                returnUrl = req.ReturnUrl;

            return Ok(new { returnUrl });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}
