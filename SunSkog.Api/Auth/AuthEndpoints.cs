using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SunSkog.Api.Contracts;
using SunSkog.Api.Filters;
using SunSkog.Api.Models;

namespace SunSkog.Api.Auth
{
    public static class AuthEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth").WithTags("SunSkog.Api").AllowAnonymous();

            group.MapPost("/register", Register)
                 .AddEndpointFilter(new Validate<RegisterDto>());

            group.MapPost("/login", Login)
                 .AddEndpointFilter(new Validate<LoginDto>());
        }

        private static async Task<IResult> Register(
            RegisterDto dto,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            var exists = await userManager.FindByEmailAsync(dto.Email!);
            if (exists is not null) return Results.Conflict("User already exists.");

            if (!await roleManager.RoleExistsAsync(dto.Role!))
            {
                await roleManager.CreateAsync(new IdentityRole(dto.Role!));
            }

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName
            };

            var result = await userManager.CreateAsync(user, dto.Password!);
            if (!result.Succeeded) return Results.BadRequest(result.Errors);

            await userManager.AddToRoleAsync(user, dto.Role!);

            return Results.Ok(new { message = "Registered" });
        }

        private static async Task<IResult> Login(
            LoginDto dto,
            UserManager<ApplicationUser> userManager,
            IConfiguration cfg)
        {
            var user = await userManager.FindByEmailAsync(dto.Email!);
            if (user is null) return Results.Unauthorized();

            if (!await userManager.CheckPasswordAsync(user, dto.Password!))
                return Results.Unauthorized();

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.Email, user.Email ?? ""),
                new(ClaimTypes.Name, user.FullName ?? user.Email ?? ""),
            };

            var roles = await userManager.GetRolesAsync(user);
            if (roles.Count > 0)
            {
                // první role
                claims.Add(new(ClaimTypes.Role, roles[0]));
            }

            var issuer   = cfg["Jwt:Issuer"]   ?? "SunSkog.Api";
            var audience = cfg["Jwt:Audience"] ?? "SunSkog.Client";
            var keyStr   = cfg["Jwt:Key"]      ?? "THIS_IS_DEV_ONLY_32B_MINIMUM_KEY_123456";
            var keyBytes = Encoding.UTF8.GetBytes(keyStr);
            if (keyBytes.Length < 32)
                throw new InvalidOperationException("JWT key too short. Need >= 32 bytes.");

            var creds  = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
            var expiresMinutes = int.TryParse(cfg["Jwt:ExpiresMinutes"], out var m) ? m : 60;

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return Results.Ok(new { token = jwt });
        }
    }
}