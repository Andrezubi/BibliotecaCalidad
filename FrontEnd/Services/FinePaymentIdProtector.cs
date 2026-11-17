using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace FrontEnd.Services;

public sealed class FinePaymentIdProtector
{
    private readonly IDataProtector _protector;
    public FinePaymentIdProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("BibliotecaCalidad.FinePayments.Receipt.Id.v1");
    }

    public string Protect(int id) => _protector.Protect(id.ToString(CultureInfo.InvariantCulture));

    public bool TryUnprotect(string? value, out int id)
    {
        id = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            return int.TryParse(_protector.Unprotect(value), NumberStyles.None,
                CultureInfo.InvariantCulture, out id) && id > 0;
        }
        catch (CryptographicException) { return false; }
        catch (FormatException) { return false; }
    }
}
