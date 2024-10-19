using System.Drawing;
using System.IO.Compression;

namespace Utilities.core;

public class ZipManager
{
    public string[] ReadAllLineFromArchive(string zipName, string fileName)
    {
        List<string> results = new() { };
        string line;

        using var zipStream = new FileStream(zipName, FileMode.Open);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            if (entry.Name == fileName)
            {
                using var stream = entry.Open();
                using StreamReader reader = new(stream);

                while ((line = reader.ReadLine()) != null)
                {
                    results.Add(line);
                }
            }
        }
        
        return results.ToArray();
    }

    public string WriteFileFromArchive(string zipName, string fileName, string destFileName)
    {
        string result = null;

        using ZipArchive archive = ZipFile.OpenRead(zipName);

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.Name == fileName)
                entry.ExtractToFile(destFileName, true);
        }

        return result;
    }

    public Image ReadImageFromArchive(string zipName, string fileName)
    {
        Image result = null;

        using var zipStream = new FileStream(zipName, FileMode.Open);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            if (entry.Name == fileName)
            {
                using var stream = entry.Open();
                result = Image.FromStream(stream);
            }
        }

        return result;
    }
}
