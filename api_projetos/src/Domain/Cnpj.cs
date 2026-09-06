using System.Text.RegularExpressions;

namespace API_Gestao_Eventos.src.Domain
{
    public class Cnpj
    {
        public string Number { get; }

        private Cnpj(string number)
        {
            Number = number;
        }

        public static Cnpj Create(string cnpj)
        {
            var clean = Normalize(cnpj);

            if (!IsValid(clean))
                throw new ArgumentException("CNPJ inválido.", nameof(cnpj));

            return new Cnpj(clean);
        }

        private static string Normalize(string cnpj) =>
            cnpj.Replace(".", "").Replace("/", "").Replace("-", "").ToUpperInvariant();

        public static bool IsValid(string cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj)) return false;

            var clean = Normalize(cnpj);

            if (!Regex.IsMatch(clean, @"^[A-Z0-9]{12}\d{2}$")) return false;

            var baseChars = clean[..12];
            var dv1Expected = clean[12] - '0';
            var dv2Expected = clean[13] - '0';

            var dv1 = CalculateDv(baseChars, Weights1);
            if (dv1 != dv1Expected) return false;

            var dv2 = CalculateDv(baseChars + dv1, Weights2);
            return dv2 == dv2Expected;
        }

        private static readonly int[] Weights1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        private static readonly int[] Weights2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        private static int CalculateDv(string input, int[] weights)
        {
            var sum = 0;
            for (var i = 0; i < input.Length; i++)
                sum += (input[i] - '0') * weights[i];

            var remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }

        public override string ToString() =>
            $"{Number[..2]}.{Number[2..5]}.{Number[5..8]}/{Number[8..12]}-{Number[12..14]}";
        public override bool Equals(object? obj) => obj is Cnpj other && Number == other.Number;
        public override int GetHashCode() => Number.GetHashCode();
    }
}