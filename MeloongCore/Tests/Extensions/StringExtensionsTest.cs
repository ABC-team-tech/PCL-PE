namespace MeloongCore.Tests;

/// <summary>
/// 覆盖 <see cref="StringExtensions.Split(string, string, bool)"/> 等重载。
/// 该组重载曾用 <c>[splitStr]</c> 集合表达式，在 Roslyn 4.8（C# 12）起会因 string[] / char[] 两个候选而报 CS0121，
/// 导致整个解决方案编译失败；这里锁定行为，防止以后又被改回歧义写法。
/// </summary>
public class StringExtensionsTest : TestBase {

    private static string Join(IEnumerable<string> values) => string.Join("|", values);

    [Test]
    public async Task Split_字符串分隔符_单字符() {
        var result = "a,b,,c".Split(",", false);
        await Assert.That(Join(result)).IsEqualTo("a|b||c");
    }

    [Test]
    public async Task Split_字符串分隔符_移除空项() {
        var result = "a,b,,c".Split(",", true);
        await Assert.That(Join(result)).IsEqualTo("a|b|c");
    }

    [Test]
    public async Task Split_字符串分隔符_多字符() {
        var result = "a::b::c".Split("::", true);
        await Assert.That(Join(result)).IsEqualTo("a|b|c");
    }

    [Test]
    public async Task Split_空分隔符_返回原串() {
        var result = "a,b".Split("", false);
        await Assert.That(Join(result)).IsEqualTo("a,b");
    }

    [Test]
    public async Task Split_字符分隔符() {
        var result = "a|b|c".Split('|', true);
        await Assert.That(Join(result)).IsEqualTo("a|b|c");
    }

    [Test]
    public async Task Split_字符数组分隔符() {
        var result = "a,b;c".Split(new[] { ',', ';' }, true);
        await Assert.That(Join(result)).IsEqualTo("a|b|c");
    }

    [Test]
    public async Task Split_字符串数组分隔符() {
        var result = "a::b;c".Split(new[] { "::", ";" }, true);
        await Assert.That(Join(result)).IsEqualTo("a|b|c");
    }

}
