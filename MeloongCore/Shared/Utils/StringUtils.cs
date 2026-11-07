using System.Globalization;
using System.Text.RegularExpressions;

namespace MeloongCore;
public static class StringUtils {

    #region 转义

    public static string XmlEscape(string str) {
        if (str.StartsWithF("{")) str = "{}" + str;
        return str
            .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&apos;")
            .Replace("\"", "&quot;").ReplaceLineEndings("&#xa;");
    }

    public static string LikePatternEscape(string input) {
        var builder = new StringBuilder();
        foreach (char c in input) {
            if (c is '[' or ']' or '*' or '?' or '#') {
                builder.Append('[').Append(c).Append(']');
            } else {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RegexEscape(string value)
        => Regex.Escape(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RegexUnescape(string value)
        => Regex.Unescape(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string UrlEscape(string value)
        => Uri.EscapeDataString(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string UrlUnescape(string value)
        => Uri.UnescapeDataString(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormUrlEscape(string value)
        => WebUtility.UrlEncode(value)!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormUrlUnescape(string value)
        => WebUtility.UrlDecode(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string HtmlEscape(string value)
        => WebUtility.HtmlEncode(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string HtmlUnescape(string value)
        => WebUtility.HtmlDecode(value);

    #endregion

    #region 格式化

    public static string FormatByteSize(long byteSize) {
        var sign = byteSize < 0 ? "-" : "";
        if (byteSize < 0) byteSize *= -1;
        if (byteSize < 1000) {
            return $"{sign}{byteSize} B";
        } else if (byteSize < 1024 * 1000) {
            var roundResult = Math.Round(byteSize / 1024D).ToString(CultureInfo.InvariantCulture);
            return $"{sign}{Math.Round(byteSize / 1024D, (3 - roundResult.Length).Clamp(0, 2))} K";
        } else if (byteSize < 1024 * 1024 * 1000) {
            var roundResult = Math.Round(byteSize / 1024D / 1024D).ToString(CultureInfo.InvariantCulture);
            return $"{sign}{Math.Round(byteSize / 1024D / 1024D, (3 - roundResult.Length).Clamp(0, 2))} M";
        } else {
            var roundResult = Math.Round(byteSize / 1024D / 1024D / 1024D).ToString(CultureInfo.InvariantCulture);
            return $"{sign}{Math.Round(byteSize / 1024D / 1024D / 1024D, (3 - roundResult.Length).Clamp(0, 2))} G";
        }
    }

    public static string FormatTimeSpan(TimeSpan span, bool isShort) {
        var suffix = span.TotalMilliseconds > 0 ? "后" : "前";
        if (span.TotalMilliseconds < 0) span = -span;
        var totalMonths = (int) Math.Floor(span.Days / 30D);
        if (isShort) {
            if (totalMonths >= 12) {

                return $"{Math.Floor(totalMonths / 12D)} 年{suffix}";
            } else if (totalMonths >= 2) {

                return $"{totalMonths} 个月{suffix}";
            } else if (span.TotalDays >= 2) {

                return $"{span.Days} 天{suffix}";
            } else if (span.TotalHours >= 1) {

                return $"{(int) span.TotalHours} 小时{suffix}";
            } else if (span.TotalMinutes >= 1) {

                return $"{span.Minutes} 分钟{suffix}";
            } else if (span.TotalSeconds >= 1) {

                return $"{span.Seconds} 秒{suffix}";
            } else {

                return $"1 秒{suffix}";
            }
        } else {
            if (totalMonths >= 61) {

                return $"{Math.Floor(totalMonths / 12D)} 年{suffix}";
            } else if (totalMonths >= 12) {

                return $"{Math.Floor(totalMonths / 12D)} 年{(totalMonths % 12 > 0 ? $" {totalMonths % 12} 个月" : "")}{suffix}";
            } else if (totalMonths >= 4) {

                return $"{totalMonths} 个月{suffix}";
            } else if (totalMonths >= 1) {

                return $"{totalMonths} 月{(span.Days % 30 > 0 ? $" {span.Days % 30} 天" : "")}{suffix}";
            } else if (span.TotalDays >= 4) {

                return $"{span.Days} 天{suffix}";
            } else if (span.TotalDays >= 1) {

                return $"{span.Days} 天{(span.Hours > 0 ? $" {span.Hours} 小时" : "")}{suffix}";
            } else if (span.TotalHours >= 10) {

                return $"{span.Hours} 小时{suffix}";
            } else if (span.TotalHours >= 1) {

                return $"{span.Hours} 小时{(span.Minutes > 0 ? $" {span.Minutes} 分钟" : "")}{suffix}";
            } else if (span.TotalMinutes >= 10) {

                return $"{span.Minutes} 分钟{suffix}";
            } else if (span.TotalMinutes >= 1) {

                return $"{span.Minutes} 分{(span.Seconds > 0 ? $" {span.Seconds} 秒" : "")}{suffix}";
            } else if (span.TotalSeconds >= 1) {

                return $"{span.Seconds} 秒{suffix}";
            } else {

                return $"1 秒{suffix}";
            }
        }
    }

    #endregion

    public static Version ParseVersionWithDefaults(string value) {
        while (value.Count(c => c == '.') < 3) value += ".0";
        return Version.Parse(value);
    }

}
