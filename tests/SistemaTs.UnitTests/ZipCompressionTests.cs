using System.IO.Compression;
using System.Text;
using SistemaTs.Infrastructure.Services;

namespace SistemaTs.UnitTests;

public class ZipCompressionTests
{
    private readonly ZipCompressionService _sut = new();

    [Fact]
    public void CompressToZip_ReturnsValidZip()
    {
        var content = "<xml>test content</xml>";
        var result = _sut.CompressToZip(content, "test.xml");

        Assert.NotEmpty(result);
        using var ms = new MemoryStream(result);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        Assert.Single(archive.Entries);
        Assert.Equal("test.xml", archive.Entries[0].Name);
    }

    [Fact]
    public void CompressToZip_PreservesContent()
    {
        var content = "<xml>hello world</xml>";
        var result = _sut.CompressToZip(content, "data.xml");

        using var ms = new MemoryStream(result);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        using var entryStream = archive.Entries[0].Open();
        using var reader = new StreamReader(entryStream);
        var extracted = reader.ReadToEnd();

        Assert.Equal(content, extracted);
    }

    [Fact]
    public void CompressToZip_EmptyContent_ProducesValidZip()
    {
        var result = _sut.CompressToZip("", "empty.xml");

        Assert.NotEmpty(result);
        using var ms = new MemoryStream(result);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        Assert.Single(archive.Entries);
    }

    [Fact]
    public void CompressToZip_LargeContent_ProducesSmallerOutput()
    {
        var content = new string('A', 100_000);
        var result = _sut.CompressToZip(content, "large.xml");

        Assert.True(result.Length < content.Length);
    }

    [Fact]
    public void CompressToZip_UsesSpecifiedEntryName()
    {
        var result = _sut.CompressToZip("content", "730_precompilata.xml");

        using var ms = new MemoryStream(result);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        Assert.Equal("730_precompilata.xml", archive.Entries[0].FullName);
    }
}
