using System.Text.RegularExpressions;

namespace MeloongCore.Extensions;
public static class StringExtensions {

    #region 区域性 / 大小写 简化

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWithF(this string? value, string prefix, bool ignoreCase = false)
        => value?.StartsWith(prefix, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) ?? false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWithF(this string? value, char prefix, bool ignoreCase = false)
        => value?.StartsWith(prefix.ToString(), ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) ?? false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWithF(this string? value, string suffix, bool ignoreCase = false)
        => value?.EndsWith(suffix, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) ?? false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWithF(this string? value, char suffix, bool ignoreCase = false)
        => value?.EndsWith(suffix.ToString(), ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) ?? false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsIgnoreCase(this string value, string subString)
        => value.IndexOf(subString, StringComparison.OrdinalIgnoreCase) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfF(this string value, string subString, bool ignoreCase = false)
        => value.IndexOf(subString, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfF(this string value, string subString, int startIndex, bool ignoreCase = false)
        => value.IndexOf(subString, startIndex, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOfF(this string value, string subString, bool ignoreCase = false)
        => value.LastIndexOf(subString, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOfF(this string value, string subString, int startIndex, bool ignoreCase = false)
        => value.LastIndexOf(subString, startIndex, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Lower(this string str)
        => str.ToLowerInvariant();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Upper(this string str)
        => str.ToUpperInvariant();

    #endregion

    #region Split

    public static string[] Split(this string fullStr, string splitStr, bool removeEmptyEntries = false) {
        // 原先的 [splitStr] 集合表达式在 string[] / char[] 两个重载之间无法推断，自 Roslyn 4.8（C# 12）起会直接报 CS0121
        if (string.IsNullOrEmpty(splitStr)) return new[] { fullStr };
        if (splitStr.IsSingle()) {
            return fullStr.Split(new[] { splitStr[0] }, ToSplitOptions(removeEmptyEntries));
        } else {
            return fullStr.Split(new[] { splitStr }, ToSplitOptions(removeEmptyEntries));
        }
    }

    public static string[] Split(this string fullStr, char splitChar, bool removeEmptyEntries = false)
        => fullStr.Split(new[] { splitChar }, ToSplitOptions(removeEmptyEntries));

    public static string[] Split(this string fullStr, string[] splitStrings, bool removeEmptyEntries = false)
        => fullStr.Split(splitStrings, ToSplitOptions(removeEmptyEntries));

    public static string[] Split(this string fullStr, char[] splitChars, bool removeEmptyEntries = false)
        => fullStr.Split(splitChars, ToSplitOptions(removeEmptyEntries));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static StringSplitOptions ToSplitOptions(bool removeEmptyEntries)
        => removeEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;

    public static string[] SplitLines(this string input, bool skipEmptyLines = false)
        => input.ReplaceLineEndings("\n").Split('\n', skipEmptyLines);

    #endregion

    #region 裁切

    public static string BeforeFirst(this string str, string text, bool ignoreCase = false) {
        int pos = string.IsNullOrEmpty(text) ? -1 : str.IndexOfF(text, ignoreCase);
        if (pos < 0) return str;
        return str[..pos];
    }

    public static string BeforeFirstOfAny(this string str, IEnumerable<string>? texts, bool ignoreCase = false) {
        if (texts == null) return str;
        int pos = -1;
        foreach (string text in texts) {
            if (string.IsNullOrEmpty(text)) continue;
            int p = str.IndexOfF(text, ignoreCase);
            if (p >= 0 && (pos < 0 || pos > p)) pos = p;
        }
        if (pos < 0) return str;
        return str[..pos];
    }

    public static string BeforeLast(this string str, string text, bool ignoreCase = false) {
        int pos = string.IsNullOrEmpty(text) ? -1 : str.LastIndexOfF(text, ignoreCase);
        if (pos < 0) return str;
        return str[..pos];
    }

    public static string BeforeLastOfAny(this string str, IEnumerable<string>? texts, bool ignoreCase = false) {
        if (texts == null) return str;
        int pos = -1;
        foreach (string text in texts) {
            if (string.IsNullOrEmpty(text)) continue;
            int p = str.LastIndexOfF(text, ignoreCase);
            if (p >= 0 && (pos < 0 || pos < p)) pos = p;
        }
        if (pos < 0) return str;
        return str[..pos];
    }

    public static string AfterFirst(this string str, string? text, bool ignoreCase = false) {
        int pos = string.IsNullOrEmpty(text) ? -1 : str.IndexOfF(text!, ignoreCase);
        if (pos < 0) return str;
        return str[(pos + text!.Length)..];
    }

    public static string AfterFirstOfAny(this string str, IEnumerable<string>? texts, bool ignoreCase = false) {
        if (texts == null) return str;
        int pos = -1;
        int len = 0;
        foreach (string text in texts) {
            if (string.IsNullOrEmpty(text)) continue;
            int p = str.IndexOfF(text, ignoreCase);
            if (p >= 0 && (pos < 0 || pos > p)) {
                pos = p;
                len = text.Length;
            }
        }
        if (pos < 0) return str;
        return str[(pos + len)..];
    }

    public static string AfterLast(this string str, string? text, bool ignoreCase = false) {
        int pos = string.IsNullOrEmpty(text) ? -1 : str.LastIndexOfF(text!, ignoreCase);
        if (pos < 0) return str;
        return str[(pos + text!.Length)..];
    }

    public static string AfterLastOfAny(this string str, IEnumerable<string>? texts, bool ignoreCase = false) {
        if (texts == null) return str;
        int pos = -1;
        int len = 0;
        foreach (string text in texts) {
            if (string.IsNullOrEmpty(text)) continue;
            int p = str.LastIndexOfF(text, ignoreCase);
            if (p >= 0 && (pos < 0 || pos < p)) {
                pos = p;
                len = text.Length;
            }
        }
        if (pos < 0) return str;
        return str[(pos + len)..];
    }

    public static string Between(this string str, string after, string before, bool ignoreCase = false) {
        int startPos = string.IsNullOrEmpty(after) ? -1 : str.LastIndexOfF(after, ignoreCase);
        if (startPos >= 0) {
            startPos += after.Length;
        } else {
            startPos = 0;
        }

        int endPos = string.IsNullOrEmpty(before) ? -1 : str.IndexOfF(before, startPos, ignoreCase);
        if (endPos >= 0) {
            return str[startPos..endPos];
        } else if (startPos > 0) {
            return str[startPos..];
        } else {
            return str;
        }
    }

    #endregion

    #region 替换

    public static string Replace(this string str, string oldValue, Func<string> newValueGetter)
        => str.Contains(oldValue) ? str.Replace(oldValue, newValueGetter()) : str;

    public static string ReplaceLineEndings(this string input, string newValue, bool mergeMultiple = false)
        => (mergeMultiple ? regexLineEndingAndMerge : regexLineEnding).Replace(input, newValue.Replace("$", "$$"));
    private static readonly Regex regexLineEndingAndMerge = new(@"(?:\r\n|[\n\r\f\u0085\u2028\u2029])+", RegexOptions.Compiled);
    private static readonly Regex regexLineEnding = new(@"\r\n|[\n\r\f\u0085\u2028\u2029]", RegexOptions.Compiled);

    #endregion

    #region 正则

    public static IEnumerable<string> RegexSearch(this string str, string pattern, RegexOptions options = RegexOptions.None) {
        try {
            return Regex.Matches(str, pattern, options).Cast<Match>().Select(m => m.Value);
        } catch (Exception ex) {
            Logger.Warn(ex, $"{nameof(RegexSearch)} 失败（{pattern}）");
            return [];
        }
    }

    public static string? RegexSeek(this string str, string pattern, RegexOptions options = RegexOptions.None) {
        try {
            var match = Regex.Match(str, pattern, options);
            return match.Success ? match.Value : null;
        } catch (Exception ex) {
            Logger.Warn(ex, $"{nameof(RegexSeek)} 失败（{pattern}）");
            return null;
        }
    }

    public static bool RegexCheck(this string str, string pattern, RegexOptions options = RegexOptions.None) {
        try {
            return Regex.IsMatch(str, pattern, options);
        } catch (Exception ex) {
            Logger.Warn(ex, $"{nameof(RegexCheck)} 失败（{pattern}）");
            return false;
        }
    }

    public static string RegexReplace(this string allContents, string searchRegex, string replaceTo, RegexOptions options = RegexOptions.None) {
        return Regex.Replace(allContents, searchRegex, replaceTo, options);
    }

    public static string RegexReplace(this string allContents, string searchRegex, MatchEvaluator replaceTo, RegexOptions options = RegexOptions.None) {
        return Regex.Replace(allContents, searchRegex, replaceTo, options);
    }

    #endregion

    public static string ReadString(this Stream stream, Encoding? encoding = null) {
        using var memoryStream = new MemoryStream();
        if (stream.CanSeek && stream.Position != 0) stream.Seek(0, SeekOrigin.Begin);
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray().GetString(encoding);
    }

    public static string GetString(this byte[] bytes, Encoding? encoding = null) {
        if (encoding is not null) return encoding.GetString(bytes);
        int length = bytes.Length;

        if (length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) {
            return Encoding.UTF8.GetString(bytes, 3, length - 3);
        } else if (length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) {
            return Encoding.BigEndianUnicode.GetString(bytes, 2, length - 2);
        } else if (length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) {
            return Encoding.Unicode.GetString(bytes, 2, length - 2);
        }

        try {
            return new UTF8Encoding(false, true).GetString(bytes);
        } catch (DecoderFallbackException) {
            return Encoding.GetEncoding("GB18030").GetString(bytes);
        }
    }

    public static string Capitalize(this string word) {
        if (string.IsNullOrEmpty(word)) return word;
        return $"{word[..1].Upper()}{word[1..].Lower()}";
    }

    public static string EnsureLength(this string? str, char code, int length) {
        str ??= "";
        return str.Length > length ? str[..length] : str.PadLeft(length, code);
    }

    public static bool IsAsciiOnly(this string input)
        => input.All(c => c < 128);

    public static ulong GetStableHashCode(this string str) {
        ulong result = 5381;
        foreach (char v in str) result = (result << 5) ^ result ^ v;
        return result ^ 0xA98F501BC684032FUL;
    }

    public static JToken? DeserializeJson(this string data) {
        try {
            return JsonConvert.DeserializeObject<JToken>(data, new JsonSerializerSettings {DateTimeZoneHandling = DateTimeZoneHandling.Local});
        } catch (Exception ex) {
            int length = (data ?? "").Length;
            throw new FormatException("JSON deserialize failed: " +
                (length > 2000
                    ? $"{data![..500]}...(total {length} characters)...{data[^500..]}"
                    : (data ?? "")), ex);
        }
    }

    public static T? DeserializeJson<T>(this string data) where T : JToken
        => (T?) DeserializeJson(data);

}
