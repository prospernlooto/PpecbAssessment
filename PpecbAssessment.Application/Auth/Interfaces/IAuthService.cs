using PpecbAssessment.Application.Auth.Dtos;
using PpecbAssessment.Application.Common;

namespace PpecbAssessment.Application.Auth.Interfaces
{
    public interface IAuthService
    {
        Task<Result> RegisterAsync(RegisterRequest request);
        Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
    }
}
