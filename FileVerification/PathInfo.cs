using System;
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
        /// <summary>
        /// Gets the path value.
        /// </summary>
        public string Path { get; private set; }

        public Queue<string>? Files { get; private set; }

        public List<ChecksumFile>? ChecksumFiles { get; private set; }

        /// <summary>
        /// Initializes an instance of the <see cref="Path"/> class when
        /// provided with the full path.
        /// </summary>
        /// <param name="path">
        /// The full path.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="path"/> argument is <c>null</c> or empty.
        /// </exception>
        internal PathInfo(string path)
        {
            if (path == null || string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(path);
            }

            Path = path;
        }

        public void Crawl(bool includeSubDir, string checksumFileName)
        {
            
            // If the path does not exist, then just return null
            if (!Exists())
            {
                return;
            }
            
            // If the path is a file, then just return a string array with the
            // path value as there is no directory to be crawled
            if (IsFile())
            {
                Files = new Queue<string>();
                Files.Enqueue(Path);
                return;
            }

            Files = new Queue<string>(CrawlDirectory(includeSubDir));
            
            if (ChecksumFiles == null)
            {
                ChecksumFiles = new List<ChecksumFile>();
            }

            foreach (string file in GetChecksumFiles(checksumFileName, includeSubDir))
            {
                ChecksumFiles.Add(new ChecksumFile(file));
            }
        }

        //public List<string>? CrawlCheckSumFiles(bool includeSubDir, string checksumFileName)
        //{
        //    // If the path does not exist, then just return null
        //    if (!Exists())
        //    {
        //        return null;
        //    }

        //    return new List<string>(GetChecksumFiles(checksumFileName, includeSubDir));
        //}

        public void Check()
        {
            if (Files == null || ChecksumFiles == null)
            {
                return;
            }

            foreach (string file in Files)
            {
                ChecksumFile? checksumFile = ChecksumFiles.FirstOrDefault(c => c.Checksums.ContainsKey(file));

                // A checksum file was found containing the file, so get the
                // hash information for the file
                if (checksumFile != null)
                {
                    HashInfo? hashInfo = checksumFile.GetFileData(file);

                }
            }
        }

        /// <summary>
        /// Returns a value indicating the path is a valid directory.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the path is a valid directory, otherwise <c>false</c>.
        /// </returns>
        public bool IsDirectory()
        {
            return Directory.Exists(Path);
        }

        /// <summary>
        /// Returns a value indicating the path is a valid file.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the path is a valid file, othersize <c>false</c>.
        /// </returns>
        public bool IsFile()
        {
            return File.Exists(Path);
        }

        /// <summary>
        /// Returns a value indicating the path exists.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the path exists, otherwise <c>false</c>.
        /// </returns>
        public bool Exists()
        {
            return IsDirectory() || IsFile();
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
        private IEnumerable<string> CrawlDirectory(bool includeSubDir)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(Path);
            IEnumerable<FileInfo> files = 
                dirInfo.EnumerateFiles("*", GetSearchOption(includeSubDir));

            foreach (var file in files)
            {
                yield return file.FullName;
            }
        }

        //private void CrawlDirectory2(bool includeSubDir)
        //{
        //    try
        //    {
                
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"{ex.GetType()} {ex.Message}\n{ex.StackTrace}");
        //    }
        //}

        //private void CrawlDirectory2(bool includeSubDir, DirectoryInfo dirInfo)
        //{
        //    try
        //    {

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"{ex.GetType()} {ex.Message}\n{ex.StackTrace}");
        //    }
        //}

        private IEnumerable<string> GetChecksumFiles(string checksumFileName, bool includeSubDir)
        {
            if (string.IsNullOrWhiteSpace(checksumFileName))
            {
                yield break;
            }

            DirectoryInfo dirInfo = new DirectoryInfo(Path);
            IEnumerable<FileInfo> checksumFiles =
                dirInfo.EnumerateFiles(checksumFileName, GetSearchOption(includeSubDir));

            foreach (var checksumFile in checksumFiles)
            {
                yield return checksumFile.FullName;
            }
        }

        private SearchOption GetSearchOption(bool includeSubDir)
        {
            return includeSubDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        }
    }
}
