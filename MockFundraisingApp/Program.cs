using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<MockFundraisingApp.Services.RequestsStore>();

builder.Services.AddControllersWithViews();

builder.Services.AddSession();

builder.Services.AddSingleton(sp =>
{
    var cfg = builder.Configuration.GetSection("Firebase");
    var projectId = cfg["ProjectId"];
    var serviceAccountPath = cfg["ServiceAccountPath"];

    if (string.IsNullOrWhiteSpace(projectId))
        throw new InvalidOperationException("Firebase:ProjectId is missing in appsettings.json");

    if (string.IsNullOrWhiteSpace(serviceAccountPath))
        throw new InvalidOperationException("Firebase:ServiceAccountPath is missing in appsettings.json");

    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var fullPath = Path.Combine(env.ContentRootPath, serviceAccountPath);

    if (!File.Exists(fullPath))
        throw new InvalidOperationException($"Firebase service account file not found: {fullPath}");

  
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", fullPath);

    var saCredential = CredentialFactory
        .FromFile<ServiceAccountCredential>(fullPath);

    var googleCredential = saCredential.ToGoogleCredential();

    if (FirebaseApp.DefaultInstance == null)
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = googleCredential
        });
    }

    return FirestoreDb.Create(projectId);
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Auth/Login";
        o.LogoutPath = "/Auth/Logout";
        o.AccessDeniedPath = "/Auth/Login";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();

// Bootstrap ASP.NET auth from Firebase session cookie if present
app.Use(async (ctx, next) =>
{
    if (!ctx.User.Identity?.IsAuthenticated ?? true)
    {
        if (ctx.Request.Cookies.TryGetValue("fb_session", out var sessionCookie) &&
            !string.IsNullOrWhiteSpace(sessionCookie))
        {
            try
            {
                // Verifies Firebase session cookie and returns claims (uid etc.)
                var decoded = await FirebaseAuth.DefaultInstance.VerifySessionCookieAsync(sessionCookie, checkRevoked: true);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, decoded.Uid),
                    new Claim("firebase_uid", decoded.Uid)
                };

                if (decoded.Claims.TryGetValue("email", out var emailObj) && emailObj is string email)
                    claims.Add(new Claim(ClaimTypes.Email, email));

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                ctx.User = new ClaimsPrincipal(identity);
            }
            catch
            {
                // Invalid/expired/revoked cookie → clear it
                ctx.Response.Cookies.Delete("fb_session");
            }
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Requests}/{action=Index}/{id?}");

app.Run();
