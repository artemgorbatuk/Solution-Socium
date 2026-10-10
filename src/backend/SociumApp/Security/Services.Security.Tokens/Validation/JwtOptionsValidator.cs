using Microsoft.Extensions.Options;
using Services.Security.Tokens.Options;
using Services.Security.Tokens.Texts;
using System.Text;

namespace Services.Security.Tokens.Validation;

/// <summary>
/// Регистрируется с <c>ValidateOnStart</c>: без ключа подписи приложение не стартует.
/// </summary>
public class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            errors.Add(TokenTexts.Messages.Validation.IssuerRequired);
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            errors.Add(TokenTexts.Messages.Validation.AudienceRequired);
        }

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            errors.Add(TokenTexts.Messages.Validation.SigningKeyRequired);
        }
        else if (Encoding.UTF8.GetByteCount(options.SigningKey) < JwtOptions.MinimumSigningKeyBytes)
        {
            errors.Add(TokenTexts.Messages.Validation.SigningKeyTooShort);
        }

        if (options.AccessTokenMinutes <= 0)
        {
            errors.Add(TokenTexts.Messages.Validation.AccessTokenMinutesInvalid);
        }

        if (options.RefreshTokenDays <= 0)
        {
            errors.Add(TokenTexts.Messages.Validation.RefreshTokenDaysInvalid);
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
