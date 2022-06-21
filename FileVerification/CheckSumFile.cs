using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TE.FileVerification
{
    /// <summary>
    /// The fields used in the checksum file.
    /// </summary>
    public enum ChecksumFileLayout
    {
        /// <summary>
        /// The file name.
        /// </summary>
        NAME,
        /// <summary>
        /// The string representation of the hash algorithm.
        /// </summary>
        HASH_ALGORITHM,
        /// <summary>
        /// The hash of the file.
        /// </summary>
        HASH

    }
    public class ChecksumFile
    {
        /// <summary>
        /// The default checksum file name.
        /// </summary>
        public const string DEFAULT_CHECKSUM_FILENAME = "__fv.txt";

        /// <summary>
        /// Gets the directory where the checksum file is located.
        /// </summary>
        public string Directory { get; private set; }

        /// <summary>
        /// Gets the full path of the checksum file.
        /// </summary>
        public string FullPath { get; private set; }

        /// <summary>
        /// Gets the dictionary of checksums for the checksum file.
        /// </summary>
        public ConcurrentDictionary<string, HashInfo> Checksums { get; private set; }

        /// <summary>
        /// Gets the number of files in the checksum file.
        /// </summary>
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

            string? directory = Path.GetDirectoryName(FullPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException(
                    "The directory name could not be determined from the full path to the checksum file.");
            }
            Directory = directory;

            Checksums = new ConcurrentDictionary<string, HashInfo>();

            if (File.Exists(FullPath))
            {
                Read();
            }
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

            if (string.IsNullOrWhiteSpace(Directory))
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

                    string[] values = line.Split(HashInfo.Separator);
                    if (values.Length != Enum.GetNames(typeof(ChecksumFileLayout)).Length)
                    {
                        Logger.WriteLine($"WARNING: Record size incorrect (record will be created using the current file data). File: {FullPath}, Record: {line}.");
                        continue;
                    }

                    string fileName = values[(int)ChecksumFileLayout.NAME];
                    HashInfo info =
                        new HashInfo(
                            fileName,
                            values[(int)ChecksumFileLayout.HASH_ALGORITHM],
                            values[(int)ChecksumFileLayout.HASH]);

                    // Get the full path to the file to use as the key to make
                    // it unique so it can be used for searching
                    Checksums.TryAdd(Path.Combine(Directory, fileName), info);
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

        /// <summary>
        /// Adds a checksum for a file.
        /// </summary>
        /// <param name="file">
        /// The full path, including the directory, of the file to add.
        /// </param>
        /// <param name="hashAlgorithm">
        /// The hash algorithm to use for files added to the checksum file.
        /// </param>
        public void Add(string file, HashAlgorithm hashAlgorithm)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                Logger.WriteLine("Could not add file to the checksum file because the path was not specified.");
                return;
            }

            if (!File.Exists(file))
            {
                Logger.WriteLine($"Could not add file '{file}' to the checksum file because the file does not exist.");
                return;
            }

            try
            {
                Checksums.TryAdd(file, new HashInfo(file, hashAlgorithm));
            }
            catch(ArgumentNullException ex)
            {
                Logger.WriteLine($"Could not add file '{file}' to the checksum file. Reason: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the checksum data for a file.
        /// </summary>
        /// <param name="fullPath">
        /// The full path to the file.
        /// </param>
        /// <returns>
        /// The data in a <see cref="HashInfo"/> object, or <c>null</c> if the
        /// data could not be retrieved.
        /// </returns>
        public HashInfo? GetFileData(string fullPath)
        {
            Checksums.TryGetValue(fullPath, out HashInfo? hashInfo);
            return hashInfo;
        }

        /// <summary>
        /// Validates the hash information of a file matches what is stored in
        /// the checksum file.
        /// </summary>
        /// <param name="file">
        /// The full path, including the directory, of the file.
        /// </param>
        /// <param name="hashAlgorithm">
        /// The hash algorithm to use for files added to the checksum file.
        /// Existing files will use the hash algorithm stored in the checksum
        /// file.
        /// </param>
        public bool IsMatch(string file, HashAlgorithm hashAlgorithm)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                Logger.WriteLine("Could not validate file to the checksum file because the path was not specified.");
                return false;
            }

            if (!File.Exists(file))
            {
                Logger.WriteLine($"Could not validate file '{file}' to the checksum file because the file does not exist.");
                return false;
            }

            // Get the stored hash information for the file from the
            // checksum data
            HashInfo? hashInfo = GetFileData(file);

            // Check if the file is in the checksum file
            if (hashInfo != null)
            {
                string? hash = HashInfo.GetFileHash(file, hashInfo.Algorithm);
                if (string.IsNullOrWhiteSpace(hash))
                {
                    Logger.WriteLine($"Validating file '{file}' failed because the hash for the file could not be created using {hashInfo.Algorithm}.");
                    return false;
                }

                return hashInfo.IsHashEqual(hash);
            }
            else
            {
                // Add the file if it didn't exist in the checksum file and
                // then return true as it would match the hash that was just
                // generated
                Add(file, hashAlgorithm);
                return true;
            }
        }

        /// <summary>
        /// Writes the checksum file.
        /// </summary>
        public void Write()
        {            
            if (string.IsNullOrWhiteSpace(FullPath))
            {
                Logger.WriteLine("Could not write checksum file as the path of the file was not provided.");
                return;
            }

            // Initialize the StringBuilder object that will contain the
            // contents of the verify file
            ConcurrentBag<string> info = new ConcurrentBag<string>();

            // Loop through the file checksum information and append the file
            // information to the string builder so it can be written to the
            // checksum file
            Parallel.ForEach(Checksums, checksumInfo =>
            {
                info.Add(checksumInfo.Value.ToString() + Environment.NewLine);
            });

            try
            {
                // Write the file hash information to the checksum file
                using StreamWriter sw = new StreamWriter(FullPath);
                sw.Write(string.Join("", info));
            }
            catch (DirectoryNotFoundException)
            {
                Logger.WriteLine($"Could not write the checksum file because the directory {Directory} was not found.");
            }
            catch (PathTooLongException)
            {
                Logger.WriteLine($"Could not write the checksum file because the path {FullPath} is too long.");
            }
            catch (UnauthorizedAccessException)
            {
                Logger.WriteLine($"Could not write the checksum file because the user is not authorized to write to {FullPath}.");
            }
            catch (Exception ex)
                when (ex is ArgumentException || ex is ArgumentNullException || ex is IOException || ex is System.Security.SecurityException)
            {
                Logger.WriteLine($"Could not write the checksum file. Reason: {ex.Message}");
            }
        }
    }
}
