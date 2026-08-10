using System.IO.Compression;

namespace PanelExtractor
{
    internal static class VtzArchive
    {
        public static async Task<string> CreateAsync(
            string sourceFolder,
            string outputFolder,
            CancellationToken cancellationToken = default)
        {
            string[] vtxFiles = Directory
                .EnumerateFiles(sourceFolder, "*.vtx", SearchOption.TopDirectoryOnly)
                .Take(2)
                .ToArray();

            if (vtxFiles.Length == 0)
            {
                throw new InvalidOperationException("No .vtx file was found in the downloaded display folder.");
            }

            if (vtxFiles.Length > 1)
            {
                throw new InvalidOperationException("Multiple .vtx files were found in the downloaded /display folder.");
            }

            string projectName = Path.GetFileNameWithoutExtension(vtxFiles[0]);
            string outputPath = GetUniqueFilePath(outputFolder, projectName, ".vtz");

            await Task.Run(() =>
                ZipFile.CreateFromDirectory(
                    sourceFolder,
                    outputPath,
                    CompressionLevel.Optimal,
                    includeBaseDirectory: false
                ),
                cancellationToken
            );

            return outputPath;
        }

        private static string GetUniqueFilePath(string folder, string fileNameWithoutExtension, string extension)
        {
            string filePath = Path.Combine(folder, fileNameWithoutExtension + extension);
            int counter = 1;

            while (File.Exists(filePath))
            {
                filePath = Path.Combine(folder, $"{fileNameWithoutExtension}_{counter}{extension}");
                counter++;
            }

            return filePath;
        }
    }
}
