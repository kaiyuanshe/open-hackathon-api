using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Kaiyuanshe.OpenHackathon.Server.Auth
{
    public static class ClaimsHelper
    {
        public static ClaimsPrincipal NewClaimsPrincipal(IEnumerable<Claim> claims)
        {
            var identity = new ClaimsIdentity(claims, AuthConstant.AuthType.Token);
            var claimsPrincipal = new ClaimsPrincipal(identity);
            return claimsPrincipal;
        }

        public static ClaimsPrincipal NewClaimsPrincipal(string userId, bool isPlatformAdmin = false)
        {
            var claims = new List<Claim>();
            claims.Add(UserId(userId));
            if (isPlatformAdmin)
            {
                claims.Add(PlatformAdministrator(userId));
            }

            return NewClaimsPrincipal(claims);
        }

        public static Claim UserId(string userId)
        {
            return new Claim(
                    AuthConstant.ClaimType.UserId,
                    userId,
                    ClaimValueTypes.String,
                    AuthConstant.Issuer.Default);
        }

        public static Claim UserDisplayName(string userDisplayName)
        {
            return new Claim(
                    AuthConstant.ClaimType.UserDisplayName,
                    userDisplayName,
                    ClaimValueTypes.String,
                    AuthConstant.Issuer.Default);
        }

        public static Claim PlatformAdministrator(string userId)
        {
            return new Claim(
                    AuthConstant.ClaimType.PlatformAdministrator,
                    userId,
                    ClaimValueTypes.String,
                    AuthConstant.Issuer.Default);
        }

        public static bool IsPlatformAdministrator(this ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal == null)
                return false;

            return claimsPrincipal.HasClaim(c =>
            {
                return c.Type == AuthConstant.ClaimType.PlatformAdministrator;
            });
        }

        public static string GetUserId(this ClaimsPrincipal claimsPrincipal)
        {
            var userIdClaim = claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == AuthConstant.ClaimType.UserId);
            return userIdClaim?.Value ?? string.Empty;
        }

        public static string GetUserDisplayName(this ClaimsPrincipal claimsPrincipal)
        {
            var userIdClaim = claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == AuthConstant.ClaimType.UserDisplayName);
            return userIdClaim?.Value ?? string.Empty;
        }
    }
}
