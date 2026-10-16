namespace ReveilMusical.Infrastructure.Music;

/// <summary>Le fournisseur ne peut pas répondre (quota atteint, réponse invalide, …).</summary>
public sealed class ProviderUnavailableException(string provider, string reason, Exception? inner = null)
    : Exception($"Fournisseur '{provider}' indisponible : {reason}", inner)
{
    public string Provider { get; } = provider;
}
