namespace StarterKit.Modules.Identity.Application.Common.Models;

public record ExchangeAuthCodeRequest(string Code, string CodeVerifier);
