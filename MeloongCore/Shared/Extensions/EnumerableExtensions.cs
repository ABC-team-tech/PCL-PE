namespace MeloongCore.Extensions;
public static class EnumerableExtensions {

    #region 查找

    public static TSource? FirstOrDefault<TSource>(this IEnumerable<TSource> source, params Func<TSource, bool>[] predicates) {
        foreach (var predicate in predicates) {
            foreach (var item in source) {
                if (predicate(item)) return item;
            }
        }
        return default;
    }

    #endregion

    #region 去重

    public static IEnumerable<T> Distinct<T>(this IList<T> list, Func<T, T, bool> comparer) {
        var seen = new List<T>();
        foreach (var item in list) {
            if (!seen.Any(s => comparer(s, item))) {
                seen.Add(item);
                yield return item;
            }
        }
    }

    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) {
        var seen = new HashSet<TKey>();
        foreach (var element in source) {
            if (seen.Add(selector(element))) yield return element;
        }
    }

    public static ParallelQuery<TSource> DistinctBy<TSource, TKey>(this ParallelQuery<TSource> source, Func<TSource, TKey> selector) {
        var seen = new ConcurrentDictionary<TKey, bool>();
        return source.Where(element => seen.TryAdd(selector(element), true));
    }

    #endregion

    #region 最大 / 最小

    public static List<TSource> MaxByAll<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) where TKey : IComparable<TKey> {
        var results = new List<TSource>();
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return results;
        TSource maxItem = enumerator.Current;
        TKey maxValue = selector(maxItem);
        results.Add(maxItem);
        while (enumerator.MoveNext()) {
            TSource currentItem = enumerator.Current;
            TKey currentValue = selector(currentItem);
            int comparisonResult = currentValue.CompareTo(maxValue);
            if (comparisonResult > 0) {
                maxValue = currentValue;
                results.Clear();
                results.Add(currentItem);
            } else if (comparisonResult == 0) {
                results.Add(currentItem);
            }
        }
        return results;
    }

    public static List<TSource> MaxByAll<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer) {
        var results = new List<TSource>();
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return results;
        TSource maxItem = enumerator.Current;
        TKey maxValue = selector(maxItem);
        results.Add(maxItem);
        while (enumerator.MoveNext()) {
            TSource currentItem = enumerator.Current;
            TKey currentValue = selector(currentItem);
            int comparisonResult = comparer.Compare(currentValue, maxValue);
            if (comparisonResult > 0) {
                maxValue = currentValue;
                results.Clear();
                results.Add(currentItem);
            } else if (comparisonResult == 0) {
                results.Add(currentItem);
            }
        }
        return results;
    }

    public static List<TSource> MinByAll<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) where TKey : IComparable<TKey> {
        var results = new List<TSource>();
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return results;
        TSource minItem = enumerator.Current;
        TKey minValue = selector(minItem);
        results.Add(minItem);
        while (enumerator.MoveNext()) {
            TSource currentItem = enumerator.Current;
            TKey currentValue = selector(currentItem);
            int comparisonResult = currentValue.CompareTo(minValue);
            if (comparisonResult < 0) {
                minValue = currentValue;
                results.Clear();
                results.Add(currentItem);
            } else if (comparisonResult == 0) {
                results.Add(currentItem);
            }
        }
        return results;
    }

    public static List<TSource> MinByAll<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer) {
        var results = new List<TSource>();
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return results;
        TSource minItem = enumerator.Current;
        TKey minValue = selector(minItem);
        results.Add(minItem);
        while (enumerator.MoveNext()) {
            TSource currentItem = enumerator.Current;
            TKey currentValue = selector(currentItem);
            int comparisonResult = comparer.Compare(currentValue, minValue);
            if (comparisonResult < 0) {
                minValue = currentValue;
                results.Clear();
                results.Add(currentItem);
            } else if (comparisonResult == 0) {
                results.Add(currentItem);
            }
        }
        return results;
    }

    public static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) where TKey : IComparable<TKey> {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return default;
        TSource maxItem = enumerator.Current;
        TKey maxValue = selector(maxItem);
        while (enumerator.MoveNext()) {
            TKey value = selector(enumerator.Current);
            if (value.CompareTo(maxValue) <= 0) continue;
            maxItem = enumerator.Current;
            maxValue = value;
        }
        return maxItem;
    }

    public static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer) {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return default;
        TSource maxItem = enumerator.Current;
        TKey maxValue = selector(maxItem);
        while (enumerator.MoveNext()) {
            TKey value = selector(enumerator.Current);
            if (comparer.Compare(value, maxValue) <= 0) continue;
            maxItem = enumerator.Current;
            maxValue = value;
        }
        return maxItem;
    }

    public static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) where TKey : IComparable<TKey> {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) { return default; }
        TSource minItem = enumerator.Current;
        TKey minValue = selector(minItem);
        while (enumerator.MoveNext()) {
            TKey value = selector(enumerator.Current);
            if (value.CompareTo(minValue) >= 0) { continue; }
            minItem = enumerator.Current;
            minValue = value;
        }
        return minItem;
    }

    public static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer) {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) { return default; }
        TSource minItem = enumerator.Current;
        TKey minValue = selector(minItem);
        while (enumerator.MoveNext()) {
            TKey value = selector(enumerator.Current);
            if (comparer.Compare(value, minValue) >= 0) { continue; }
            minItem = enumerator.Current;
            minValue = value;
        }
        return minItem;
    }

    #endregion

    #region 排序

    public static IEnumerable<T> Ordered<T>(this IEnumerable<T> source) where T : IComparable<T>
        => source.OrderBy(x => x);

    public static IEnumerable<T> OrderedDescending<T>(this IEnumerable<T> source) where T : IComparable<T>
        => source.OrderByDescending(x => x);

    #endregion

    #region Join

    public static string Join(this IEnumerable list, char split) {
        var builder = new StringBuilder();
        bool isFirst = true;
        foreach (var element in list) {
            if (isFirst)
                isFirst = false;
            else
                builder.Append(split);
            if (element is not null) builder.Append(element.ToString());
        }
        return builder.ToString();
    }

    public static string Join(this IEnumerable list, string split) {
        if (split.IsSingle()) return list.Join(split[0]);
        var builder = new StringBuilder();
        bool isFirst = true;
        foreach (var element in list) {
            if (isFirst)
                isFirst = false;
            else
                builder.Append(split);
            if (element is not null) builder.Append(element);
        }
        return builder.ToString();
    }

    #endregion

    #region Dictionary

    public static bool Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key)
        => dict.TryRemove(key, out _);

    public static Dictionary<T, T> ToDictionary<T>(this T[,] arr) where T : notnull {
        Dictionary<T, T> result = [];
        if (arr.Length == 0) return result;
        if (arr.GetLength(1) != 2) throw new ArgumentException("数组必须为两列，第一列为 Key，第二列为 Value。");
        for (int i = 0; i < arr.GetLength(0); i++) result[arr[i, 0]] = arr[i, 1];
        return result;
    }

    public static TValue? GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue? defaultValue = default)
        => dict.TryGetValue(key, out var result) ? result : defaultValue;

    public static void AddIntoValueCollection<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, TValue value) {
        if (dict.TryGetValue(key, out var collection)) {
            collection.Add(value);
        } else {
            dict.Add(key, [value]);
        }
    }

    #endregion

    public static IEnumerable<T> Flags<T>(this T value) where T : struct, Enum
        => EnumUtils.GetAllFlags<T>().Where(element => value.HasFlag(element));

    public static void RemoveIf<T>(this ICollection<T> source, Func<T, bool> predicate) {
        var itemsToRemove = source.Where(predicate).ToList();
        foreach (var item in itemsToRemove) source.Remove(item);
    }

    public static void RemoveAll<T>(this ICollection<T> source, IEnumerable<T> itemsToRemove) {
        foreach (var item in itemsToRemove) source.Remove(item);
    }

    public static void KeepIf<T>(this ICollection<T> source, Func<T, bool> predicate) {
        var itemsToRemove = source.Where(item => !predicate(item)).ToList();
        foreach (var item in itemsToRemove) source.Remove(item);
    }

    public static IEnumerable<T> Except<T>(this IEnumerable<T> source, T element)
        => source.Except([element]);

    public static bool IsSingle<T>(this IEnumerable<T>? source) {
        if (source is null) return false;
        if (source is IList<T> list) return list.Count == 1;
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return false;
        if (enumerator.MoveNext()) return false;
        return true;
    }

    public static IEnumerable<T> ForAll<T>(this IEnumerable<T> source, Action<T> action) {
        foreach (T item in source) action(item);
        return source;
    }

}
