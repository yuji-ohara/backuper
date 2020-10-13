using System;
using System.IO.Compression;

namespace Backuper
{
    public class Backuper
    {
        internal void CreateZipFolder(string path, string destinationFilePath)
        {
            ZipFile.CreateFromDirectory(path, destinationFilePath);
        }
    }
}
