using System.IO.Compression;
using System.Text;

namespace LightvnTools
{
    public class Program
    {
        static readonly string VERSION = "1.0.0";

        // PKZip signature
        static readonly byte[] PKZIP = { 0x50, 0x4B, 0x03, 0x04 };

        // Key used to decrypt the file header and footer (reverse)
        // Text: `d6c5fKI3GgBWpZF3Tz6ia3kF0`
        // Source: https://github.com/morkt/GARbro/issues/440
        static readonly byte[] KEY = { 0x64, 0x36, 0x63, 0x35, 0x66, 0x4B, 0x49, 0x33, 0x47, 0x67, 0x42, 0x57, 0x70, 0x5A, 0x46, 0x33, 0x54, 0x7A, 0x36, 0x69, 0x61, 0x33, 0x6B, 0x46, 0x30 };
        static readonly byte[] REVERSED_KEY = { 0x30, 0x46, 0x6B, 0x33, 0x61, 0x69, 0x36, 0x7A, 0x54, 0x33, 0x46, 0x5A, 0x70, 0x57, 0x42, 0x67, 0x47, 0x33, 0x49, 0x4B, 0x66, 0x35, 0x63, 0x36, 0x64 };

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine($"Light.vnTools v{VERSION}");
                Console.WriteLine();
                Console.WriteLine(
                    "Light.vnTools is an unpack and repacking tool for Light.vn game engine (lightvn.net)."
                );
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("  Unpack: Drag and drop '.vndat' file to 'LightvnTools.exe'");
                Console.WriteLine("  Repack: Drag and drop unpacked folder to 'LightvnTools.exe'");
                Console.ReadLine();
                return;
            }

            string input = args[0];

            // Unpack .vndat
            if (File.Exists(input))
            {
                if (!IsZip(input))
                {
                    Console.WriteLine("Not a .vndat (zip) file!");
                    Console.ReadLine();
                    return;
                }

                string outputFolder = Path.GetFileNameWithoutExtension(input);

                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                // Extract .vndat file
                Console.WriteLine($"Extracting {Path.GetFileName(input)} to ./{outputFolder}/...");
                ZipFile.ExtractToDirectory(input, outputFolder, true);

                // Decrypt .vndat file contents
                string[] files = GetFilesRecursive(outputFolder);

                foreach (string file in files)
                {
                    Console.WriteLine($"Decrypting {file}...");
                    XORFile(file);
                }

                Console.WriteLine("Done.");
            }

            // Repack
            if (Directory.Exists(input))
            {
                string[] files = GetFilesRecursive(input);

                // Encrypting the files back
                foreach (string file in files)
                {
                    Console.WriteLine($"Encrypting {Path.GetFileName(file)}...");
                    XORFile(file);
                }

                // Archiving to .vndat
                string fileName = $"{input}.vndat";

                Console.WriteLine($"Archiving as {fileName}...");
                File.Copy(fileName, $"{fileName}.bak");
                File.Delete(fileName);
                ZipFile.CreateFromDirectory(input, fileName, CompressionLevel.Optimal, false);

                Console.WriteLine("Done.");
            }

            Console.ReadLine();
        }

        /// <summary>
        /// Check if the given file is Zip or not.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        static bool IsZip(string filePath)
        {
            try
            {
                byte[] fileSignature = new byte[4];

                using FileStream file = File.OpenRead(filePath);
                file.Read(fileSignature, 0, fileSignature.Length);

                for (int i = 0; i < fileSignature.Length; i++)
                {
                    if (fileSignature[i] != PKZIP[i])
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading {Path.GetFileName(filePath)}. {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// XOR the given file.
        /// </summary>
        /// <param name="filePath"></param>
        static void XORFile(string filePath)
        {
            try
            {
                byte[] buffer;
                int bufferLength;

                using (FileStream inputStream = File.OpenRead(filePath))
                {
                    buffer = new byte[bufferLength = (int)inputStream.Length];
                    inputStream.Read(buffer, 0, bufferLength);
                }

                if (bufferLength < 100)
                {
                    if (bufferLength == 0)
                    {
                        Console.WriteLine($"Skipping {filePath}. File is empty.");
                        return;
                    }

                    Console.WriteLine($"File size is smaller than 100 bytes: {filePath}");

                    // XOR entire bytes
                    for (int i = 0; i < bufferLength; i++)
                        buffer[i] ^= REVERSED_KEY[i % KEY.Length];
                }
                else
                {
                    // XOR the first 100 bytes
                    for (int i = 0; i < 100; i++)
                        buffer[i] ^= KEY[i % KEY.Length];

                    // XOR the last 100 bytes
                    for (int i = 0; i < 99; i++)
                        buffer[bufferLength - 99 + i] ^= REVERSED_KEY[i % KEY.Length];
                }

                using FileStream outputStream = File.OpenWrite(filePath);
                outputStream.Write(buffer, 0, bufferLength);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all files from a folder.
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <returns>File paths.</returns>
        static string[] GetFilesRecursive(string sourceFolder)
        {
            return Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);
        }
    }
}
