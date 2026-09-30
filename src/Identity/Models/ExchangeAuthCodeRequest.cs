namespace StarterKit.Modules.Identity.Models;

public record ExchangeAuthCodeRequest(string Code, string CodeVerifier);
