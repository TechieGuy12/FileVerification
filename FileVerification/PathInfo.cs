using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TE.FileVerification
{
    /// <summary>
    /// Contains the properties and methods that is used to work with a
    /// path.
    /// </summary>
    public class PathInfo
    {
        private readonly string? _directory;

        private readonly string? _checksumFileName;

        private readonly ConcurrentBag<Task> _tasks = new ConcurrentBag<Task>();

        /// <summary>
        /// Gets the path value.
        /// </summary>
        public string FullPath { get; private set; }

        /// <summary>
        /// Gets all the files in the path.
        /// </summary>
        public ConcurrentQueue<string>? Files { get; private set; }

        /// <summary>
        /// Gets the number of files.
        /// </summary>
        public int FileCount
        {
            get
            {
                return Files != null ? Files.Count : 0;
            }
        }

        /// <summary>
        /// Gets the number of directories.
        /// </summary>
        public int DirectoryCount { get; private set; }

        /// <summary>
        /// Gets all the checksum files in the path.
        /// </summary>
        public List<ChecksumFile>? ChecksumFileInfo { get; private set; }

        /// <summary>
        /// Initializes an instance of the <see cref="PathInfo"/> class when
        /// provided with the full path.
        /// </summary>
        /// <param name="path">
        /// The full path to a directory or a file.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="path"/> parameter is <c>null</c> or
        /// empty.
        /// </exception>
        public PathInfo(string path)
        {
            if (path == null || string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(path);
            }

            FullPath = path;

            // Check to see if the full path points to a file rather than a
            // directory, and if it does, then extract and store the directory
            // name
            if (IsFile(FullPath))
            {
                _directory = Path.GetDirectoryName(FullPath);
                if (string.IsNullOrWhiteSpace(_directory))
                {
                    throw new InvalidOperationException("The directory for the path could not be determined.");
                }
            }
            else
            {
                _directory = FullPath;
            }

            _checksumFileName = ChecksumFile.DEFAULT_CHECKSUM_FILENAME;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="PathInfo"/> class when
        /// provided with the full path and the checksum file name.
        /// </summary>
        /// <param name="path">
        /// The full path to a directory or a file.
        /// </param>
        /// <param name="checksumFileName">
        /// The name of the checksum file.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if a paramter is <c>null</c> or empty.
        /// </exception>
        public PathInfo(string path, string checksumFileName)
            : this(path)
        {
            if (!string.IsNullOrWhiteSpace(checksumFileName))
            {
                _checksumFileName = checksumFileName;
            }
        }

        public void Crawl(bool includeSubDir)
        {
            
            // If the path does not exist, then just return null
            if (!Exists())
            {
                return;
            }
            
            // If the path is a file, then just return a string array with the
            // path value as there is no directory to be crawled
            if (IsFile(FullPath))
            {
                Files = new ConcurrentQueue<string>();
                Files.Enqueue(FullPath);                
            }
            else
            {
                CrawlDirectory(includeSubDir);
            }

            GetChecksumFiles(includeSubDir);
        }

        /// <summary>
        /// Check the hash values of the file against the stored hashes in the
        /// checksum files. If the file hashes aren't in the checksum files,
        /// then add the file and its hash to the checksum files.
        /// </summary>
        public void Check()
        {
            if (Files == null || ChecksumFileInfo == null)
            {
                return;
            }

            ParallelOptions options = new ParallelOptions();
            options.MaxDegreeOfParallelism = Environment.ProcessorCount;
            Parallel.ForEach(Files, options, file =>
            {                
                if (Path.GetFileName(file).Equals(_checksumFileName) || IsSystemFile(file))
                {
                    return;
                }

                // Find the checksum file that contains the file
                ChecksumFile? checksumFile =
                    ChecksumFileInfo.FirstOrDefault(
                        c => c.Checksums.ContainsKey(file));

                // A checksum file was found containing the file, so get the
                // hash information for the file
                if (checksumFile != null)
                {
                    // Check if the current file matches the hash information
                    // stored in the checksum file
                    if (!checksumFile.IsMatch(file))
                    {
                        Logger.WriteLine($"FAIL: Hash mismatch: {file}.");
                    }
                }
                else
                {
                    // Get the file directory so it can be used to find the
                    // checksum file for the directory
                    string? fileDir = Path.GetDirectoryName(file);
                    if (string.IsNullOrWhiteSpace(fileDir))
                    {
                        Logger.WriteLine($"Could not get the directory from '{file}'.");
                        return;
                    }

                    // Find the checksum file for the directory
                    checksumFile =
                        ChecksumFileInfo.FirstOrDefault(
                            c => c.Directory.Equals(fileDir));
                    if (checksumFile == null)
                    {
                        // If no checksum file was located in the directory,
                        // create a new checksum file and then add it to the
                        // list
                        checksumFile = new ChecksumFile(Path.Combine(fileDir, ChecksumFile.DEFAULT_CHECKSUM_FILENAME));
                        ChecksumFileInfo.Add(checksumFile);
                    }

                    // Add the file to the checksum file
                    checksumFile.Add(file);

                }
            });

            foreach(ChecksumFile checksum in ChecksumFileInfo)
            {
                checksum.Write();
            }
        }

        /// <summary>
        /// Returns a value indicating the path is a valid directory.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the path is a valid directory, otherwise <c>false</c>.
        /// </returns>
        public static bool IsDirectory(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// Returns a value indicating the path is a valid file.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the path is a valid file, othersize <c>false</c>.
        /// </returns>
        public static bool IsFile(string path)
        {
            return File.Exists(path);
        }

        /// <summary>
        /// Returns a value indicating the path exists.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the path exists, otherwise <c>false</c>.
        /// </returns>
        public bool Exists()
        {
            return IsDirectory(FullPath) || IsFile(FullPath);
        }

        /// <summary>
        /// Crawls the path and returns the files.
        /// </summary>
        /// <param name="includeSubDir">
        /// Value indicating if the subdirectories are to be crawled.
        /// </param>
        /// <returns>
        /// Returns an enumerable collection of file paths.
        /// </returns>
        private void CrawlDirectory(bool includeSubDir)
        {
            if (string.IsNullOrWhiteSpace(_directory))
            {
                return; ;
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(_directory);
            _tasks.Add(Task.Run(() => CrawlDirectory(directoryInfo, includeSubDir)));

            while (_tasks.TryTake(out Task? taskToWaitFor))
            {
                if (taskToWaitFor != null)
                {
                    DirectoryCount++;
                    taskToWaitFor.Wait();
                }
            }
        }

        /// <summary>
        /// Crawls the path and returns the files.
        /// </summary>
        /// <param name="dir">
        /// The <see cref="DirectoryInfo"/> object of the current directory.
        /// </param>
        /// <param name="includeSubDir">
        /// Value indicating if the subdirectories are to be crawled.
        /// </param>
        private void CrawlDirectory(DirectoryInfo dir, bool includeSubDir)
        {
            try
            {                
                if (Files == null)
                {
                    Files = new ConcurrentQueue<string>();
                }

                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    Files.Enqueue(file.FullName);
                }

                if (includeSubDir)
                {
                    DirectoryInfo[] directoryInfo = dir.GetDirectories();
                    foreach (DirectoryInfo childInfo in directoryInfo)
                    {
                        _tasks.Add(Task.Run(() => CrawlDirectory(childInfo, includeSubDir)));
                    }
                }
            }
            catch (Exception ex)
                when (ex is DirectoryNotFoundException || ex is System.Security.SecurityException || ex is UnauthorizedAccessException)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the checksum files from each directory.
        /// </summary>
        /// <param name="includeSubDir">
        /// Include subdirectories when searching for checksum files.
        /// </param>
        private void GetChecksumFiles(bool includeSubDir)
        {
            if (string.IsNullOrWhiteSpace(_checksumFileName) || string.IsNullOrWhiteSpace(_directory))
            {
                return;
            }

            // Get all the checksums in the directory and sub-directories,
            // if specified
            string[] checksumFiles = Directory.GetFiles(_directory, _checksumFileName, GetSearchOption(includeSubDir));
            ChecksumFileInfo = new List<ChecksumFile>(checksumFiles.Length);

            // Loop through each of the checksum files and add the information
            // in the checksum list
            foreach (string file in checksumFiles)
            {
                ChecksumFileInfo.Add(new ChecksumFile(file));
            }
        }

        /// <summary>
        /// Gets the search option used to search the directories.
        /// </summary>
        /// <param name="includeSubDir">
        /// Indicates if the subdirectories are to be included in the search.
        /// </param>
        /// <returns>
        /// A <see cref="SearchOption"/> value.
        /// </returns>
        private SearchOption GetSearchOption(bool includeSubDir)
        {
            return includeSubDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        }

        /// <summary>
        /// Indicates if a file is a system file.
        /// </summary>
        /// <param name="file">
        /// The full path, including the directory, of the file.
        /// </param>
        /// <returns>
        /// <c>true</c> if the file is a system file, otherwise <c>false</c>.
        /// </returns>
        private static bool IsSystemFile(string file)
        {
            FileAttributes attributes = File.GetAttributes(file);
            return ((attributes & FileAttributes.System) == FileAttributes.System);
        }
    }
}
