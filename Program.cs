using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace LightvnTools
{
    public class Program
    {
        static readonly string VERSION = "1.1.0";

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
            string zipPassword = Encoding.UTF8.GetString(KEY);

            if (File.Exists(input))
                Unpack(input, Path.GetFileNameWithoutExtension(input), zipPassword);

            if (Directory.Exists(input))
                Repack(input, zipPassword);

            Console.ReadLine();
        }

        /// <summary>
        /// Extract `.vndat` file.
        /// </summary>
        /// <param name="vndatFile"></param>
        /// <param name="outputFolder"></param>
        /// <param name="password"></param>
        static void Unpack(string vndatFile, string outputFolder, string? password = "")
        {
            if (!IsVndat(vndatFile))
            {
                Console.WriteLine($"{Path.GetFileName(vndatFile)} isn\'t a `.vndat` (zip) file!");
                return;
            }

            bool usePassword = IsPasswordProtectedZip(vndatFile);

            using ZipFile zipFile = new(vndatFile);
            Directory.CreateDirectory(outputFolder);

            // Old Light.vn encrypt the `.vndat` file with `KEY` as the password.
            if (usePassword)
            {
                Console.WriteLine($"{Path.GetFileName(vndatFile)} are password protected. Using `{password}` as the password.");
                zipFile.Password = password;
            }

            if (zipFile.Count > 0)
            {
                Console.WriteLine($"Extracting {Path.GetFileName(vndatFile)}...");

                foreach (ZipEntry entry in zipFile)
                {
                    string? entryPath = Path.Combine(outputFolder, entry.Name);
                    Directory.CreateDirectory(Path.GetDirectoryName(entryPath));

                    if (!entry.IsDirectory)
                    {
                        try
                        {
                            Console.WriteLine($"Writing {entryPath}...");

                            using Stream inputStream = zipFile.GetInputStream(entry);
                            using FileStream outputStream = File.Create(entryPath);
                            inputStream.CopyTo(outputStream);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to write {entryPath}! {ex.Message}");
                        }
                    }
                }

                Console.WriteLine("Done.");
            }

            // Only XOR `.vndat` contents that's not password protected.
            if (!usePassword)
            {
                string[] files = GetFilesRecursive(outputFolder);

                if (files.Length > 0)
                {
                    foreach (string file in files)
                    {
                        Console.WriteLine($"Decrypting {file}...");
                        XorVndatContent(file);
                    }

                    Console.WriteLine("Done.");
                }
            }
        }

        /// <summary>
        /// Archive folder as `.vndat` file.
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="password"></param>
        static void Repack(string sourceFolder, string? password = "")
        {
            string outputFile = $"{Path.GetFileName(sourceFolder)}.vndat";
            string[] files = GetFilesRecursive(sourceFolder);
            string? tempFolder = $"{sourceFolder}_temp";

            // Only backup original file once
            string backupFile = $"{outputFile}.bak";
            if (!File.Exists(backupFile))
            {
                Console.WriteLine($"Backup the original file as {Path.GetFileName(backupFile)}...");
                File.Copy(outputFile, backupFile);
            }

            bool usePassword = IsPasswordProtectedZip(backupFile);

            using ZipOutputStream zipStream = new(File.Create(outputFile));

            // Uses the backup file to check if it's encrypted to bypass
            // the file is being used by another process exception.
            if (usePassword)
            {
                Console.WriteLine($"Encrypting {Path.GetFileName(outputFile)} using `{password}` as the password...");
                zipStream.Password = password;
            }
            else
            {
                Console.WriteLine($"Creating a temporary copy of {Path.GetFileName(sourceFolder)} to perform XOR encryption...");

                CopyFolder(sourceFolder, tempFolder);
                files = GetFilesRecursive(tempFolder);

                foreach (string file in files)
                {
                    Console.WriteLine($"Encrypting {Path.GetRelativePath(sourceFolder, file)}...");
                    XorVndatContent(file);
                }
            }

            Console.WriteLine($"Creating {outputFile} archive...");

            foreach (string filePath in files)
            {
                FileInfo file = new(filePath);
                // Keep file structure by including the folder
                string entryName = filePath[usePassword ? sourceFolder.Length.. : tempFolder.Length..].TrimStart('\\');
                ZipEntry entry = new(entryName)
                {
                    DateTime = DateTime.Now,
                    Size = file.Length
                };
                zipStream.PutNextEntry(entry);

                using FileStream fileStream = file.OpenRead();
                byte[] buffer = new byte[4096]; // Optimum size
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    zipStream.Write(buffer, 0, bytesRead);
                }
            }

            if (!usePassword)
            {
                Console.WriteLine("Cleaning up temporary files...");
                Directory.Delete(tempFolder, true);
            }

            Console.WriteLine("Done.");
        }

        /// <summary>
        /// Check if the given file is `.vndat` file (Zip) or not.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        static bool IsVndat(string filePath)
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
        /// Check if the ZIP file is password protected.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        static bool IsPasswordProtectedZip(string filePath)
        {
            try
            {
                using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
                using ZipInputStream zipStream = new(fileStream);

                ZipEntry entry;
                while ((entry = zipStream.GetNextEntry()) != null)
                {
                    if (entry.IsCrypted)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Encrypt (XOR) `.vndat` file content.
        /// </summary>
        /// <param name="filePath"></param>
        static void XorVndatContent(string filePath)
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

        /// <summary>
        /// Copy entire files in a folder.
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="destinationDirectory"></param>
        static void CopyFolder(string sourceDirectory, string destinationDirectory)
        {
            if (!Directory.Exists(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            string[] files = GetFilesRecursive(sourceDirectory);

            foreach (string sourceFilePath in files)
            {
                string relativePath = sourceFilePath[sourceDirectory.Length..].TrimStart('\\');
                string destinationFilePath = Path.Combine(destinationDirectory, relativePath);

                string destinationFileDirectory = Path.GetDirectoryName(destinationFilePath);
                if (!Directory.Exists(destinationFileDirectory))
                    Directory.CreateDirectory(destinationFileDirectory);

                File.Copy(sourceFilePath, destinationFilePath, true);
            }
        }
    }
}
