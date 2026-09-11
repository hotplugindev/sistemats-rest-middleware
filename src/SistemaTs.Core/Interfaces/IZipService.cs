namespace SistemaTs.Core.Interfaces;

public interface IZipService
{
    byte[] CompressToZip(string content, string entryName);
}
