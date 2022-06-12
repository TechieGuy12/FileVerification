﻿using System;
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
        // The directory associated with the path
        private readonly string? _directory;

        // The name of the checksum files
        private readonly string? _checksumFileName;

        // A queue of tasks used to crawl the directory tree
        private readonly ConcurrentQueue<Task> _tasks = 
            new ConcurrentQueue<Task>();

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
        public ConcurrentDictionary<string, ChecksumFile>? ChecksumFileInfo { get; private set; }

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
        /// <exception cref="InvalidOperationException">
        /// Thrown if the directory of the path could not be determined.
        /// </exception>
        /// <exception cref=">"
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

        /// <summary>
        /// Crawl the directory associated with the path.
        /// </summary>
        /// <param name="includeSubDir">
        /// Indicates if subdirectories are to be crawled.
        /// </param>
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
        /// <param name="hashAlgorithm">
        /// The hash algorithm to use for files added to the checksum file.
        /// Existing files will use the hash algorithm stored in the checksum
        /// file.
        /// </param>
        public void Check(HashAlgorithm hashAlgorithm)
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

                // Get the file directory so it can be used to find the
                // checksum file for the directory
                string? fileDir = Path.GetDirectoryName(file);
                if (string.IsNullOrWhiteSpace(fileDir))
                {
                    Logger.WriteLine($"Could not get the directory from '{file}'.");
                    return;
                }

                // Find the checksum file for the directory containing the file
                ChecksumFile? checksumFile = 
                    ChecksumFileInfo.FirstOrDefault(
                        c => c.Key.Equals(fileDir)).Value;

                // A checksum file was found containing the file, so get the
                // hash information for the file
                if (checksumFile != null)
                {
                    // Check if the current file matches the hash information
                    // stored in the checksum file
                    if (!checksumFile.IsMatch(file, hashAlgorithm))
                    {
                        Logger.WriteLine($"FAIL: Hash mismatch: {file}.");
                    }
                }
                else
                {
                    // If no checksum file was located in the directory, create
                    // a new checksum file and then add it to the list
                    checksumFile = 
                        new ChecksumFile
                            (Path.Combine(
                                fileDir, 
                                ChecksumFile.DEFAULT_CHECKSUM_FILENAME));

                    // If the new checksum fle could not be added, then another
                    // thread had it created at the same time, so try and grab
                    // the other checksum file
                    if (!ChecksumFileInfo.TryAdd(fileDir, checksumFile))
                    {
                        // Find the checksum file for the directory containing the file
                        checksumFile =
                            ChecksumFileInfo.FirstOrDefault(
                                c => c.Key.Equals(fileDir)).Value;

                        if (checksumFile == null)
                        {
                            Logger.WriteLine("The checksum file could not be determined. The file was not added.");
                            return;
                        }
                    }

                    // Add the file to the checksum file
                    checksumFile.Add(file, hashAlgorithm);                    
                }
            });

            // Write out each checksum file with the updated information
            foreach (var keyPair in ChecksumFileInfo)
            {
                keyPair.Value.Write();
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

            // Get the directory information, and then enqueue the task
            // to crawl the directory
            DirectoryInfo directoryInfo = new DirectoryInfo(_directory);
            _tasks.Enqueue(Task.Run(() => CrawlDirectory(directoryInfo, includeSubDir)));

            // Preform each directory crawl task while there are still crawl
            // tasks - waiting for each task to be completed
            while (_tasks.TryDequeue(out Task? taskToWaitFor))
            {
                if (taskToWaitFor != null)
                {
                    DirectoryCount++;
                    try
                    {
                        taskToWaitFor.Wait();
                    }
                    catch (AggregateException ae)
                    {
                        foreach (var ex in ae.Flatten().InnerExceptions )
                        {
                            Logger.WriteLine($"A directory could not be crawled. Reason: {ex.Message}");
                        }
                    }
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
            // If no files have been stored, create a new queue for storing
            // the files
            if (Files == null)
            {
                Files = new ConcurrentQueue<string>();
            }

            try
            {
                // Get the files and then add them to the queue for verifying
                IEnumerable<FileInfo> files =
                    dir.EnumerateFiles(
                        "*",
                        SearchOption.TopDirectoryOnly);
                foreach (FileInfo file in files)
                {
                    if (file.Name.Equals(_checksumFileName) || IsSystemFile(file.FullName))
                    {
                        continue;
                    }
                    Files.Enqueue(file.FullName);
                }
            }
            catch (Exception ex)
                when (ex is ArgumentNullException || ex is ArgumentOutOfRangeException || ex is DirectoryNotFoundException || ex is System.Security.SecurityException)
            {
                Console.WriteLine($"Could not get files from '{dir.FullName}'. Reason: {ex.Message}");
            }

            try
            {
                if (includeSubDir)
                {
                    // Enumerate all directories within the current directory
                    // and add them to the task array so they can be crawled
                    IEnumerable<DirectoryInfo> directoryInfo = dir.EnumerateDirectories();
                    foreach (DirectoryInfo childInfo in directoryInfo)
                    {
                        _tasks.Enqueue(Task.Run(() => CrawlDirectory(childInfo, includeSubDir)));
                    }
                }
            }
            catch (Exception ex)
                when (ex is DirectoryNotFoundException || ex is System.Security.SecurityException)
            {
                Console.WriteLine($"Could not get subdirectories for '{dir.FullName}'. Reason: {ex.Message}");
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

            IEnumerable<string>? checksumFiles;

            try
            {

                // Get all the checksums in the directory and sub-directories,
                // if specified
                checksumFiles =
                    Directory.EnumerateFiles(
                        _directory,
                        _checksumFileName,
                        GetSearchOption(includeSubDir));
                ChecksumFileInfo = new ConcurrentDictionary<string, ChecksumFile>();
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Could not get checksum files for directory '{_directory}'. Reason: {ex.Message}");
                return;
            }

            // Loop through each of the checksum files and add the information
            // in the checksum list
            foreach (string file in checksumFiles)
            {
                try
                {
                    // Get the directory of the file as it will be used as the
                    // key for the checksum file dictionary
                    string? fileDir = Path.GetDirectoryName(file);
                    if (!string.IsNullOrWhiteSpace(fileDir))
                    {
                        ChecksumFileInfo.TryAdd(fileDir, new ChecksumFile(file));
                    }
                    else
                    {
                        Logger.WriteLine($"Could not get directory of checksum file '{file}'.");                        
                    }
                }
                catch (Exception ex)
                    when (ex is ArgumentException || ex is PathTooLongException)
                {
                    Logger.WriteLine($"Could not get directory of checksum file '{file}'. Reason: {ex.Message}");
                }
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
        private static SearchOption GetSearchOption(bool includeSubDir)
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
