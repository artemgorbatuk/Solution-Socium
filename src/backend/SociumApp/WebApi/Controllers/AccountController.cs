using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Services.Security.Api;
using Services.Security.Models;
using Services.Shared.Enums;
using WebApi.Controllers.Shared;
using WebApi.Middleware;

namespace WebApi.Controllers;

/// <summary>
/// Не CRUD: вход, обновление токенов, выход и профиль текущего пользователя.
/// Контроллер выставляет и удаляет refresh-cookie, а refresh/logout читают токен из неё.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountController : ApiControllerBase
{
    private readonly IServiceAccount serviceAccount;

    public AccountController(IServiceAccount serviceAccount)
    {
        this.serviceAccount = serviceAccount;
    }

    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationRegistration.AuthRateLimitPolicy)]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] AccountLoginRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceAccount.LoginAsync(request, cancellationToken);
        if (responseInfo.Response is { } tokens)
        {
            RefreshTokenCookie.Append(Response, tokens);
        }

        return FromResponse(responseInfo);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var request = new AccountRefreshRequest { RefreshToken = RefreshTokenCookie.Read(Request) };
        var responseInfo = await serviceAccount.RefreshAsync(request, cancellationToken);
        if (responseInfo.Response is { } tokens)
        {
            RefreshTokenCookie.Append(Response, tokens);
        }
        else if (responseInfo.MessageInfo.MessageType == MessageType.UNAUTHORIZED)
        {
            RefreshTokenCookie.Delete(Response);
        }

        return FromResponse(responseInfo);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var request = new AccountLogoutRequest { RefreshToken = RefreshTokenCookie.Read(Request) };
        var responseInfo = await serviceAccount.LogoutAsync(request, cancellationToken);
        RefreshTokenCookie.Delete(Response);
        return FromResponse(responseInfo);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile(
        [FromQuery] AccountProfilePageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceAccount.DisplayProfilePageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> Profile(
        [FromBody] AccountProfileUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceAccount.UpdateProfileAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [AllowAnonymous]
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] AccountConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceAccount.ConfirmEmailAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationRegistration.MailRateLimitPolicy)]
    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation(
        [FromBody] AccountResendConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceAccount.ResendConfirmationAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationRegistration.MailRateLimitPolicy)]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] AccountForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceAccount.ForgotPasswordAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] AccountResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceAccount.ResetPasswordAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }
}
