namespace StarterKit.Identity.Contracts;

public record ExchangeAuthCodeRequest(
    string Code,
    string CodeVerifier);
