using EventGrok.Users.Application.DTOs;

namespace EventGrok.Users.Application.Services;

public interface IUserService
{
    Task<UserInfoDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<TokenResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
}