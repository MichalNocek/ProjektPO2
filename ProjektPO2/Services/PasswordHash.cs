using System;
using System.Security.Cryptography;
using System.Text;

namespace ProjektPO2.Services;

// Prosty hash hasła: SHA-256 zapisany jako ciąg szesnastkowy (małe litery).
// Bez soli — celowo proste: to samo hasło zawsze daje ten sam hash, więc login
// porównuje hash wpisanego hasła z hashem w bazie.
// (Format pokrywa się z postgresowym encode(sha256(...), 'hex') — patrz migracja HashPasswords.)
public static class PasswordHash
{
    public static string Hash(string password)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
