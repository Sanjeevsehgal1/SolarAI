using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SolarShield.Core.DTOs;
using SolarShield.Core.Entities;
using SolarShield.Core.Enums;
using SolarShield.Infrastructure.Data;

namespace SolarShield.API.Services;

public class AuthService
{
    private readonly ApplicationDbContext _db;
    private readonly AccessControlService _access;
    private readonly IConfiguration _config;

    public AuthService(ApplicationDbContext db, AccessControlService access, IConfiguration config)
    {
        _db = db;
        _access = access;
        _config = config;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var accessibleSites = await _access.GetAccessibleSitesAsync(user.Id, user.Role, user.OrganizationId);
        var token = GenerateToken(user);
        return new LoginResponse(
            token, user.Id, user.FullName, user.Email, user.Role.ToString(), user.PreferredLanguage,
            user.OrganizationId, user.Organization?.Name, accessibleSites);
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (user.OrganizationId.HasValue)
            claims.Add(new Claim("organizationId", user.OrganizationId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
