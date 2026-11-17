
using System;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace FrontEnd.Services
{
    public class LoanIdProtector
    {
        private readonly IDataProtector _protector;

        public LoanIdProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector(
                "BibliotecaCalidad.Loans.Details.Id.v1");
        }

        public string Protect(int id)
        {
            return _protector.Protect(
                id.ToString(CultureInfo.InvariantCulture));
        }

        public bool TryUnprotect(string? protectedId, out int id)
        {
            id = 0;

            if (string.IsNullOrWhiteSpace(protectedId))
                return false;

            try
            {
                string originalId = _protector.Unprotect(protectedId);

                return int.TryParse(
                    originalId,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out id) && id > 0;
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
