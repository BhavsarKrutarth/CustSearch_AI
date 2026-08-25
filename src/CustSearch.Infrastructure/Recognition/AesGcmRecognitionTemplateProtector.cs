using System.Security.Cryptography;
using CustSearch.Application.Recognition;
using Microsoft.Extensions.Options;

namespace CustSearch.Infrastructure.Recognition;

/// <summary>Encrypts derived templates with a configured 256-bit key; configuration stores no built-in credential.</summary>
public sealed class AesGcmRecognitionTemplateProtector(IOptions<RecognitionSecurityOptions>options):IRecognitionTemplateProtector
{
    private readonly RecognitionSecurityOptions security=options.Value;
    public ProtectedRecognitionTemplate Protect(ReadOnlySpan<byte>derivedTemplate)
    {
        if(!security.Enabled||!security.HasValidEncryptionConfiguration())throw new RecognitionException("Recognition encryption is disabled or not configured.",RecognitionFailureKind.Unavailable);
        if(derivedTemplate.Length is<32 or>16384)throw new RecognitionException("Derived template must be between 32 and 16384 bytes.",RecognitionFailureKind.Validation);
        var key=Convert.FromBase64String(security.EncryptionKeyBase64);var nonce=RandomNumberGenerator.GetBytes(12);var ciphertext=new byte[derivedTemplate.Length];var tag=new byte[16];
        try{using var aes=new AesGcm(key,16);aes.Encrypt(nonce,derivedTemplate,ciphertext,tag,ReadOnlySpan<byte>.Empty);return new(ciphertext,nonce,tag,security.EncryptionKeyReference,"AES-256-GCM");}
        finally{CryptographicOperations.ZeroMemory(key);}
    }
}
