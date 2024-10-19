using Microsoft.AspNetCore.Identity;

namespace BackEnds.RoboPrinter.Services.IServices;

public interface IJwtTokenGenerator
{
    string GenerateToken(IdentityUser identityUser, IEnumerable<string> roles);
}
