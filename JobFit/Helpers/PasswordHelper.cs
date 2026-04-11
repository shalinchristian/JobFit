using Microsoft.AspNetCore.Identity;

namespace JobFit.Helpers
{
    // Passwords are never stored as plain text, only as a salted hash.
    public static class PasswordHelper
    {
        private static readonly PasswordHasher<string> hasher = new PasswordHasher<string>();

        public static string Hash(string username, string password)
        {
            return hasher.HashPassword(username, password);
        }

        public static bool Verify(string username, string hash, string password)
        {
            return hasher.VerifyHashedPassword(username, hash, password) != PasswordVerificationResult.Failed;
        }
    }
}
