using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Services;

/// <summary>
/// Реализация IUserContext, читающая ID пользователя из заголовка X-User-Id.
/// Если заголовок не передан — генерирует случайный GUID.
/// </summary>
public class HttpContextUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId => GetUserId();
    public string UserName => GetUserName();
    public bool IsAuthenticated => true;

    private Guid GetUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Request?.Headers.TryGetValue("X-User-Id", out var userIdHeader) == true
            && Guid.TryParse(userIdHeader, out var userId))
        {
            return userId;
        }

        return Guid.NewGuid();
    }

    private string GetUserName()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Request?.Headers.TryGetValue("X-User-Id", out var userIdHeader) == true
            && Guid.TryParse(userIdHeader, out _))
        {
            return $"user-{userIdHeader}";
        }

        return "anonymous";
    }
}
