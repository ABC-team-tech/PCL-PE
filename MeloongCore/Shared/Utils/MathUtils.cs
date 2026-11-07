using System.Numerics;

namespace MeloongCore;
public static class MathUtils {

    #region 进制转换

    public static string ConvertRadix(this string input, int fromRadix, int toRadix) {
        const string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz/+=!?@#$%^&*()[]{}<>;:',";
        if (fromRadix < 2 || fromRadix > digits.Length) throw new ArgumentOutOfRangeException(nameof(fromRadix), $"{nameof(fromRadix)} must be between 2 and 86.");

        if (string.IsNullOrEmpty(input)) return "0";
        bool isNegative = input.StartsWithF("-");
        if (isNegative) input = input.TrimStart('-');

        BigInteger realNum = 0;
        foreach (char c in input) {
            int digit = digits.IndexOf(c);
            if (digit == -1 || digit >= fromRadix) throw new ArgumentException($"Character '{c}' in input '{input}' is not a valid digit for radix {fromRadix}.");
            realNum = realNum * fromRadix + digit;
        }
        return (realNum * (isNegative ? -1 : 1)).ConvertRadix(toRadix);
    }

    public static string ConvertRadix(this long input, int toRadix)
        => ConvertRadix((BigInteger) input, toRadix);

    public static string ConvertRadix(this ulong input, int toRadix)
        => ConvertRadix((BigInteger) input, toRadix);

    public static string ConvertRadix(this int input, int toRadix)
        => ConvertRadix((BigInteger) input, toRadix);

    public static string ConvertRadix(this byte input, int toRadix)
        => ConvertRadix((BigInteger) input, toRadix);

    public static string ConvertRadix(this BigInteger input, int toRadix) {
        const string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz/+=!?@#$%^&*()[]{}<>;:',";
        if (toRadix < 2 || toRadix > digits.Length) throw new ArgumentOutOfRangeException(nameof(toRadix), $"{nameof(toRadix)} must be between 2 and 86.");

        if (input == 0) return "0";
        bool isNegative = input < 0;

        var results = new List<char>();
        BigInteger remaining = BigInteger.Abs(input);
        BigInteger bigRadix = toRadix;
        while (remaining > 0) {
            remaining = BigInteger.DivRem(remaining, bigRadix, out BigInteger remainder);
            results.Add(digits[(int) remainder]);
        }

        if (isNegative) results.Add('-');
        results.Reverse();
        return new([..results]);
    }

    #endregion

    public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T> {
        if (value.CompareTo(min) < 0) return min;
        if (value.CompareTo(max) > 0) return max;
        return value;
    }

    public static double Lerp(double start, double end, double percentage)
        => Math.Round(start + (end - start) * percentage, 7);

    public static double Bezier(double t, double x1, double y1, double x2, double y2, double accuracy = 0.01) {
        if (t < 0.0000001 || double.IsNaN(t)) return 0;
        if (t > 0.9999999) return 1;
        double a = t;
        double b;
        int maxIterations = 1000;
        do {
            b = 3 * a * ((0.33333333 + x1 - x2) * a * a + (x2 - 2 * x1) * a + x1);
            a += (t - b) * 0.5;
            maxIterations--;
        } while (!(Math.Abs(b - t) < accuracy) && maxIterations > 0);
        return 3 * a * ((0.33333333 + y1 - y2) * a * a + (y2 - 2 * y1) * a + y1);
    }

    public static bool IsNear(this double value, double target, double tolerance = 1e-6)
        => Math.Abs(value - target) < tolerance;

    public static IEnumerable<int> GetPrimeFactors(int num) {
        num = Math.Abs(num);
        while (num % 2 == 0 && num > 1) {
            yield return 2;
            num /= 2;
        }
        int i = 3;
        int maxFactor = (int) Math.Sqrt(num);
        while (i <= maxFactor) {
            while (num % i == 0) {
                yield return i;
                num /= i;
                maxFactor = (int) Math.Sqrt(num);
            }
            i += 2;
        }
        if (num > 2) yield return num;
    }

    public static int Gcd(int a, int b) {
        a = Math.Abs(a);
        b = Math.Abs(b);
        while (b != 0) {
            int temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    public static int Gcd(double a, int b)
        => Gcd((int) Math.Round(a), b);

}
