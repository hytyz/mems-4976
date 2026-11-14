using System.Security.Claims;
using System.Text;
using MEMS.Api.Infrastructure;
using MEMS.Data.Entities;
using MEMS.Shared;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MEMS.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[Tags("Authentication")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    IOptions<JwtSettings> jwtOptions,
    ILogger<AuthController> logger) : ControllerBase
{
    public const string LoginRateLimit = "login";

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimit)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        const string failure = "The username or password is incorrect.";

        var user = await userManager.FindByNameAsync(request.UserName.Trim());
        if (user is null)
        {
            logger.LogWarning("API sign-in failed for unknown user {UserName}", request.UserName);
            return Problem(failure, statusCode: StatusCodes.Status401Unauthorized, title: "Sign-in failed");
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            logger.LogWarning("API sign-in refused: account {UserName} is locked out", user.UserName);
            return Problem("This account is temporarily locked after too many failed attempts. Try again in a few minutes.",
                statusCode: StatusCodes.Status401Unauthorized, title: "Account locked");
        }

        if (!result.Succeeded)
        {
            logger.LogWarning("API sign-in failed for {UserName}", user.UserName);
            return Problem(failure, statusCode: StatusCodes.Status401Unauthorized, title: "Sign-in failed");
        }

        var principal = await claimsFactory.CreateAsync(user);
        string role = principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        string? tenantId = principal.FindFirstValue(AppClaims.TenantId);
        string? tenantSlug = principal.FindFirstValue(AppClaims.TenantSlug);

        if (role == AppRoles.MunicipalityAdmin && tenantId is null)
        {
            logger.LogWarning("API sign-in refused: {UserName} is not assigned to a municipality", user.UserName);
            return Problem("This account is not assigned to a municipality. Ask a Super Admin for help.",
                statusCode: StatusCodes.Status401Unauthorized, title: "Sign-in failed");
        }

        var jwt = jwtOptions.Value;
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id,
            [JwtRegisteredClaimNames.Name] = user.UserName!,
            ["role"] = role
        };
        if (tenantId is not null && tenantSlug is not null)
        {
            claims[AppClaims.TenantId] = tenantId;
            claims[AppClaims.TenantSlug] = tenantSlug;
        }

        DateTime expires = DateTime.UtcNow.AddMinutes(jwt.ExpiryMinutes);
        string token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Claims = claims,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)), SecurityAlgorithms.HmacSha256)
        });

        logger.LogInformation("API sign-in succeeded for {UserName} with role {Role}", user.UserName, role);
        return new LoginResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expires,
            UserName = user.UserName!,
            Role = role,
            TenantSlug = tenantSlug
        };
    }
}
