using FluentAssertions;
using InertiaKit.AspNetCore;

namespace InertiaKit.AspNetCore.Tests;

public class InertiaLocationResultTests
{
    // ── Safe URLs ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("/dashboard")]
    [InlineData("/users/42")]
    [InlineData("https://payment.example.com/checkout")]
    [InlineData("http://external.example.com/callback")]
    public void Safe_urls_are_accepted(string url)
    {
        var act = () => new InertiaLocationResult(url);
        act.Should().NotThrow();
    }

    // ── Unsafe URLs ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("//evil.com/phish")]          // protocol-relative
    [InlineData("ftp://files.example.com")]   // non-http scheme
    [InlineData("")]
    [InlineData("   ")]
    public void Unsafe_urls_throw_argument_exception(string url)
    {
        var act = () => new InertiaLocationResult(url);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsSafeRedirectUrl_accepts_relative_paths()
    {
        InertiaLocationResult.IsSafeRedirectUrl("/users").Should().BeTrue();
        InertiaLocationResult.IsSafeRedirectUrl("/").Should().BeTrue();
    }

    [Fact]
    public void IsSafeRedirectUrl_rejects_protocol_relative()
    {
        InertiaLocationResult.IsSafeRedirectUrl("//evil.com").Should().BeFalse();
    }
}
