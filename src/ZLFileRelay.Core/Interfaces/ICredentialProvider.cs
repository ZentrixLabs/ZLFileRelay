namespace ZLFileRelay.Core.Interfaces;

/// <summary>
/// Interface for secure credential storage and retrieval
/// </summary>
public interface ICredentialProvider
{
    /// <summary>
    /// Store an encrypted credential
    /// </summary>
    void StoreCredential(string key, string value);
    
    /// <summary>
    /// Retrieve and decrypt a credential
    /// </summary>
    string? GetCredential(string key);
    
    /// <summary>
    /// Check if a credential exists
    /// </summary>
    bool HasCredential(string key);
    
    /// <summary>
    /// Delete a credential
    /// </summary>
    void DeleteCredential(string key);
    
    /// <summary>
    /// Clear all credentials
    /// </summary>
    void ClearAllCredentials();
}
