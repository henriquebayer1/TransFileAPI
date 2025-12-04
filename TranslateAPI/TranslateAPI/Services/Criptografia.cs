namespace TranslateAPI.Services
{
    public static class Criptografia
    {
        public static string HashGenerate(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool HashComparer(string formPassword, string passwordData)
        {
            return BCrypt.Net.BCrypt.Verify(formPassword, passwordData);
        }
    }
}
