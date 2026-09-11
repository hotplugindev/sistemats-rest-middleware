using System.IO.Compression;
using System.Text;
using SistemaTs.Core.Interfaces;

namespace SistemaTs.Infrastructure.Services;

public sealed class ZipCompressionService : IZipService
{
    public byte[] CompressToZip(string content, string entryName)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            var bytes = Encoding.UTF8.GetBytes(content);
            entryStream.Write(bytes);
        }
        return ms.ToArray();
    }
}
