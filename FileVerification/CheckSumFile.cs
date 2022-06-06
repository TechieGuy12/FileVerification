using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TE.FileVerification
{
    enum ChecksumFileLayout
    {
        NAME,
        HASH_ALGORITHM,
        HASH

    }
    public class ChecksumFile
    {
        private const char Separator = '|';

        private readonly string? directory;

        /// <summary>
        /// Gets the full path of the checksum file.
        /// </summary>
        public string FullPath { get; private set; }

        public Dictionary<string, HashInfo> Checksums { get; private set; }

        public int FileCount
        {
            get
            {
                return Checksums != null ? Checksums.Count : 0;
            }
        }

        /// <summary>
        /// Creates an instance of the <see cref="ChecksumFile"/> class when
        /// provided with the full path to the checksum file.
        /// </summary>
        /// <param name="fullPath">
        /// Full path to the checksum file.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="fullPath"/> parameter is null or empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The directory name to the checksum file could not be determined.
        /// </exception>
        public ChecksumFile(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                throw new ArgumentNullException(nameof(fullPath));
            }

            FullPath = fullPath;
            directory = Path.GetDirectoryName(FullPath);

            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException(
                    "The directory name could not be determined from the full path to the checksum file.");
            }

            Checksums = new Dictionary<string, HashInfo>();

            Read();
        }

        /// <summary>
        /// Reads the checksum file.
        /// </summary>
        public void Read()
        {
            if (!File.Exists(FullPath))
            {
                Logger.WriteLine($"The checksum file '{FullPath}' was not found.");
                return;
            }

            if (string.IsNullOrWhiteSpace(directory))
            {
                Logger.WriteLine("The directory value is null or empty.");
                return;
            }

            try
            {
                using var reader = new StreamReader(FullPath);

                while (!reader.EndOfStream)
                {
                    string? line = reader.ReadLine();
                    if (line == null)
                    {
                        continue;
                    }

                    string[] values = line.Split(Separator);
                    if (values.Length != Enum.GetNames(typeof(ChecksumFileLayout)).Length)
                    {
                        Logger.WriteLine($"WARNING: Record size incorrect (record will be created using the current file data). File: {FullPath}, Record: {line}.");
                        continue;
                    }

                    string fileName = values[(int)VerifyFileLayout.NAME];
                    HashInfo info =
                        new HashInfo(
                            fileName,
                            values[(int)ChecksumFileLayout.HASH_ALGORITHM],
                            values[(int)ChecksumFileLayout.HASH]);

                    // Get the full path to the file to use as the key to make
                    // it unique so it can be used for searching
                    string filePath = Path.Combine(directory, fileName);
                    Checksums.Add(filePath, info);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Logger.WriteLine($"ERROR: Not authorized to write to {FullPath}.");
                return;
            }
            catch (IOException ex)
            {
                Logger.WriteLine($"ERROR: Can't read the file. Reason: {ex.Message}");
                return;
            }
        }

        public HashInfo? GetFileData(string fullPath)
        {
            HashInfo? hashInfo;
            Checksums.TryGetValue(fullPath, out hashInfo);
            return hashInfo;
        }
    }
}
