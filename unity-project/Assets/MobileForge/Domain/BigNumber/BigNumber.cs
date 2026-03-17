using System;

namespace MobileForge.Domain
{
    /// <summary>
    /// Big number system for idle games.
    /// Float mantissa + int exponent. Formats as "1.23M", "4.56B", etc.
    /// </summary>
    public struct BigNumber : IComparable<BigNumber>
    {
        public double Mantissa;
        public int Exponent;

        private static readonly string[] Suffixes =
            { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };

        public BigNumber(double mantissa, int exponent)
        {
            Mantissa = mantissa;
            Exponent = exponent;
            Normalize();
        }

        public static BigNumber FromDouble(double value)
        {
            if (value == 0) return new BigNumber(0, 0);
            int e = (int)Math.Floor(Math.Log10(Math.Abs(value)));
            double m = value / Math.Pow(10, e);
            return new BigNumber(m, e);
        }

        public double ToDouble() => Mantissa * Math.Pow(10, Exponent);

        public BigNumber Add(BigNumber other)
        {
            if (other.Mantissa == 0) return this;
            if (Mantissa == 0) return other;
            int diff = Exponent - other.Exponent;
            if (diff >= 15) return this;
            if (diff <= -15) return other;
            double m; int e;
            if (diff >= 0) { m = Mantissa + other.Mantissa * Math.Pow(10, -diff); e = Exponent; }
            else { m = Mantissa * Math.Pow(10, diff) + other.Mantissa; e = other.Exponent; }
            return new BigNumber(m, e);
        }

        public BigNumber Multiply(BigNumber other) =>
            new(Mantissa * other.Mantissa, Exponent + other.Exponent);

        public BigNumber MultiplyScalar(double scalar) =>
            new(Mantissa * scalar, Exponent);

        public int CompareTo(BigNumber other)
        {
            if (Exponent != other.Exponent) return Exponent.CompareTo(other.Exponent);
            return Mantissa.CompareTo(other.Mantissa);
        }

        public string Format(int decimals = 2)
        {
            if (Mantissa == 0) return "0";
            int suffixIndex = Exponent / 3;
            if (suffixIndex < 0)
                return Math.Round(ToDouble(), decimals).ToString();
            if (suffixIndex >= Suffixes.Length)
                return $"{Mantissa.ToString($"F{decimals}")}e{Exponent}";
            double display = Mantissa * Math.Pow(10, Exponent % 3);
            return $"{display.ToString($"F{decimals}")}{Suffixes[suffixIndex]}";
        }

        private void Normalize()
        {
            if (Mantissa == 0) { Exponent = 0; return; }
            while (Math.Abs(Mantissa) >= 10) { Mantissa /= 10; Exponent++; }
            while (Math.Abs(Mantissa) < 1 && Mantissa != 0) { Mantissa *= 10; Exponent--; }
        }

        public override string ToString() => Format();
    }
}
