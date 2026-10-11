using System.Text;
using FluentAssertions;
using JobFit.Services;
using Xunit;

public class DocumentTextExtractorTests
{
    private static Stream S(string t) => new MemoryStream(Encoding.UTF8.GetBytes(t));

    [Fact]
    public async Task ExtractsPlainText()
        => (await DocumentTextExtractor.ExtractPlainTextAsync(S("  hello resume  "), "cv.txt")).Should().Be("hello resume");

    [Fact]
    public async Task RejectsUnsupportedExtension()
        => await FluentActions.Invoking(() => DocumentTextExtractor.ExtractPlainTextAsync(S("x"), "cv.exe"))
            .Should().ThrowAsync<ArgumentException>();

    [Fact]
    public async Task RejectsEmptyFile()
        => await FluentActions.Invoking(() => DocumentTextExtractor.ExtractPlainTextAsync(S("   "), "cv.txt"))
            .Should().ThrowAsync<ArgumentException>();
}

public class UploadLimitTests
{
    [Fact]
    public async Task RejectsFilesOverLimit_WithFriendlyMessage()
    {
        var big = new MemoryStream(new byte[DocumentTextExtractor.MaxFileBytes + 1]);
        (await FluentActions.Invoking(() => DocumentTextExtractor.ExtractPlainTextAsync(big, "cv.txt"))
            .Should().ThrowAsync<ArgumentException>()).WithMessage("*too large*10 MB*");
    }

    [Theory]
    [InlineData("cv.csv")]
    [InlineData("cv.json")]
    public async Task OnlyAdvertisedTypesAreAccepted(string name)
    {
        (await FluentActions.Invoking(() => DocumentTextExtractor.ExtractPlainTextAsync(new MemoryStream(new byte[] { 65 }), name))
            .Should().ThrowAsync<ArgumentException>()).WithMessage("*" + DocumentTextExtractor.HumanTypes + "*");
    }

    [Fact]
    public void HintAndAcceptListMatchServerList() =>
        DocumentTextExtractor.AcceptAttribute.Split(',').Should().Equal(DocumentTextExtractor.AllowedExtensions);
}
