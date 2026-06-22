using EventGrok.Application.DTOs;

namespace EventGrok.Application.Services;

public interface IUserService
{
    Task<UserInfoDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<TokenResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
}