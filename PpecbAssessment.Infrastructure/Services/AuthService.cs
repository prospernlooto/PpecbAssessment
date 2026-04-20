using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PpecbAssessment.Application.Auth.Dtos;
using PpecbAssessment.Application.Auth.Interfaces;
using PpecbAssessment.Application.Common;
using PpecbAssessment.Domain.Entities;
using PpecbAssessment.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PpecbAssessment.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<AppUser> _passwordHasher;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<AppUser>();
        }

        public async Task<Result> RegisterAsync(RegisterRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            if (await _context.AppUsers.AnyAsync(x => x.Email == email))
                return Result.Failure("Email is already registered.");

            var user = new AppUser
            {
                AppUserId = Guid.NewGuid(),
                Email = email,
                CreatedDate = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            return Result.Success("User registered successfully.");
        }

        public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.Email == email);
            if (user == null)
                return Result<AuthResponse>.Failure("Invalid email or password.");

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verifyResult == PasswordVerificationResult.Failed)
                return Result<AuthResponse>.Failure("Invalid email or password.");

            var token = GenerateJwtToken(user);

            return Result<AuthResponse>.Success(new AuthResponse
            {
                Token = token,
                Email = user.Email,
                UserId = user.AppUserId.ToString()
            });
        }

        private string GenerateJwtToken(AppUser user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "THIS_IS_A_DEMO_SECRET_KEY_CHANGE_IT";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "PpecbAssessment";

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.AppUserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
