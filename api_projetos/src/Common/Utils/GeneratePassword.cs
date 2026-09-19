using System.Security.Cryptography;

namespace API_Gestao_Eventos.src.Common.Utils
{
    public static class GeneratePassword
    {
        private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
        private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Digits = "0123456789";
        private const string Special = "!@#$%&*-_+=";
        private const string AllChars = Lowercase + Uppercase + Digits + Special;
        public static string Generate(int length = 8)
        {
            if (length < 8)
                throw new ArgumentException("A senha deve ter pelo menos 8 caracteres para ser segura.", nameof(length));

            var passwordChars = new char[length];

            passwordChars[0] = Lowercase[RandomNumberGenerator.GetInt32(Lowercase.Length)];
            passwordChars[1] = Uppercase[RandomNumberGenerator.GetInt32(Uppercase.Length)];
            passwordChars[2] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
            passwordChars[3] = Special[RandomNumberGenerator.GetInt32(Special.Length)];

            for (int i = 4; i < length; i++)
            {
                passwordChars[i] = AllChars[RandomNumberGenerator.GetInt32(AllChars.Length)];
            }

            for (int i = passwordChars.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (passwordChars[i], passwordChars[j]) = (passwordChars[j], passwordChars[i]);
            }

            return new string(passwordChars);
        }
    }
}
