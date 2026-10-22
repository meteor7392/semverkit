using SemVerKit;
using Xunit;

namespace SemVerKit.Tests;

public class VersionTests
{
    [Fact]
    public void Parse_RoundTripsVersionWithPreReleaseAndBuildMetadata()
    {
        var version = SemanticVersion.Parse("1.2.3-rc.1+build.5");
        Assert.Equal("1.2.3-rc.1+build.5", version.ToString());
    }

    [Fact]
    public void Compare_IgnoresBuildMetadata()
    {
        var left = SemanticVersion.Parse("1.2.3+build.1");
        var right = SemanticVersion.Parse("1.2.3+build.2");
        Assert.Equal(0, left.CompareTo(right));
    }

    [Fact]
    public void PreReleaseVersionIsLessThanReleaseVersion()
    {
        Assert.True(SemanticVersion.Parse("1.2.3-rc.1") < SemanticVersion.Parse("1.2.3"));
    }

    [Theory]
    [InlineData("^1.2.0", "1.2.3", true)]
    [InlineData("^1.2.0", "2.0.0", false)]
    [InlineData("~1.2.3", "1.2.9", true)]
    [InlineData("~1.2.3", "1.3.0", false)]
    [InlineData("1.0.0 - 2.0.0", "1.5.0", true)]
    [InlineData(">=1.0.0 <2.0.0", "1.9.9", true)]
    [InlineData("1.0.0 || 2.0.0", "2.0.0", true)]
    public void RangeMatching_Works(string range, string version, bool expected)
    {
        var actual = VersionRange.IsSatisfiedBy(range, SemanticVersion.Parse(version));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("^0", "0.9.0", true)]
    [InlineData("^0", "1.0.0", false)]
    [InlineData("^0.0", "0.0.9", true)]
    [InlineData("^0.0", "0.1.0", false)]
    [InlineData("^0.3", "0.3.9", true)]
    [InlineData("^0.3", "0.4.0", false)]
    [InlineData("^1.2", "1.9.0", true)]
    [InlineData("^1.2", "2.0.0", false)]
    public void CaretPartialRanges_RespectPrecision(string range, string version, bool expected)
    {
        var actual = VersionRange.IsSatisfiedBy(range, SemanticVersion.Parse(version));
        Assert.Equal(expected, actual);
    }
}
