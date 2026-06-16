using System.Text;
using ClaudeUsageTracker.Services;
using Xunit;

namespace ClaudeUsageTracker.Tests;

public class ClaudeOAuthLoginTests
{
    [Fact]
    public void ParseQuery_extracts_and_unescapes_code_and_state()
    {
        var query = ClaudeOAuthLogin.ParseQuery("/callback?code=abc%2F123&state=xy%20z");

        Assert.Equal("abc/123", query["code"]);
        Assert.Equal("xy z", query["state"]);
    }

    [Fact]
    public void ParseQuery_returns_empty_when_no_query_string()
    {
        Assert.Empty(ClaudeOAuthLogin.ParseQuery("/callback"));
    }

    [Fact]
    public void ParseQuery_keys_are_case_insensitive()
    {
        var query = ClaudeOAuthLogin.ParseQuery("/callback?Code=abc");
        Assert.Equal("abc", query["code"]);
    }

    [Fact]
    public void Base64Url_is_url_safe_and_unpadded()
    {
        // 0xFF,0xFF produces "//8=" in standard Base64 → "__8" url-safe, no padding.
        var encoded = ClaudeOAuthLogin.Base64Url(new byte[] { 0xFF, 0xFF });

        Assert.Equal("__8", encoded);
        Assert.DoesNotContain('+', encoded);
        Assert.DoesNotContain('/', encoded);
        Assert.DoesNotContain('=', encoded);
    }
}
