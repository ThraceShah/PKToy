using System.Text;
using PKToy.Lib;

namespace PKToy.Test.Lib;

public class FrustrumEncodingTest
{
    [Fact]
    public void NonWindowsUsesUtf8()
    {
        Assert.Same(Encoding.UTF8, Frustrum.GetFileNameEncoding(false, 936));
    }

    [Fact]
    public void Utf8WindowsUsesUtf8()
    {
        Assert.Same(Encoding.UTF8, Frustrum.GetFileNameEncoding(true, 65001));
    }

    [Fact]
    public void LegacyWindowsUsesActiveCodePage()
    {
        Assert.Equal(936, Frustrum.GetFileNameEncoding(true, 936).CodePage);
    }

    [Fact]
    public void LegacyChineseWindowsDecodesChinesePathWithActiveCodePage()
    {
        const string path = @"C:\\模型\\零件.x_t";
        var encoding = Frustrum.GetFileNameEncoding(true, 936);

        Assert.Equal(path, Frustrum.DecodeFileName(encoding.GetBytes(path), true, 936));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Utf8PlatformsDecodeChinesePathAsUtf8(bool isWindows)
    {
        const string path = "/模型/零件.x_t";

        Assert.Equal(path, Frustrum.DecodeFileName(Encoding.UTF8.GetBytes(path), isWindows, 65001));
    }
}
