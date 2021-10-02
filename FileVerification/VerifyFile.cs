using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TE.FileVerification
{
    public class VerifyFile
    {
        private const char Separator = '|';
        private string fileName;

        private DirectoryInfo directory;

        public string FilePath { get; private set; }

        /// <summary>
        /// Initializes an instance of the <see cref="VerifyFile"/> class when
        /// provided with the name of the verify file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the verify file.
        /// </param>
        /// <param name="directory">
        /// The directory that should contain the verify file.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when an argument is not valid.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when an argument is null.
        /// </exception>
        public VerifyFile(string fileName, DirectoryInfo directory)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            this.fileName = fileName;
            this.directory = directory;

            FilePath = Path.Combine(this.directory.FullName, fileName);            
        }

        /// <summary>
        /// Returns a value indicating whether the verify file exists.
        /// </summary>
        /// <returns>
        /// True if the verify file exists, false if the file doesn't exist.
        /// </returns>
        public bool Exists()
        {
            return File.Exists(FilePath);
        }

        /// <summary>
        /// Reads all the lines in the verify file, and then parses each of the
        /// lines into a <see cref="Dictionary{TKey, TValue}"/> of <see cref="HashInfo"/>
        /// objects using the file name as the key.
        /// </summary>
        /// <returns>
        /// A <see cref="Dictionary{TKey, TValue}"/> of <see cref="HashInfo"/>
        /// objects with the file name as the key. a value of <c>null</c> is
        /// returned if there is an issue reading the file.
        /// </returns>
        public Dictionary<string, HashInfo> Read()
        {
            Dictionary<string, HashInfo> files = new Dictionary<string, HashInfo>();

            if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
            {
                return files;
            }

            try
            {
                using (var reader = new StreamReader(FilePath))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] values = line.Split(Separator);
                        if (values.Length != Enum.GetNames(typeof(VerifyFileLayout)).Length)
                        {
                            Logger.WriteLine($"WARNING: Record size incorrect (record will be created using the current file data). File: {FilePath}, Record: {line}.");
                            continue;
                        }
                        HashInfo info = new HashInfo(values[(int)VerifyFileLayout.HASH_ALGORITHM], values[(int)VerifyFileLayout.HASH]);
                        files.Add(values[(int)VerifyFileLayout.NAME], info);
                    }
                }

                return files;
            }
            catch (UnauthorizedAccessException)
            {
                Logger.WriteLine($"ERROR: Not authorized to write to {FilePath}.");
                return null;
            }
            catch (IOException ex)
            {
                Logger.WriteLine($"ERROR: Can't read the file. Reason: {ex.Message}");                
                return null;
            }
        }

        /// <summary>
        /// Writes to the verify file using the data stored in the files
        /// parameter into the directory specified by the folder parameter.
        /// </summary>
        /// <param name="files">
        /// A <see cref="Dictionary{TKey, TValue}"/> object containing the data
        /// to write to the file.
        /// </param>
        /// <param name="folder">
        /// The directory where the file is to be written.
        /// </param>
        public void Write(Dictionary<string, HashInfo> files, DirectoryInfo folder)
        {
            if (folder == null)
            {
                return;
            }

            // Initialize the StringBuilder object that will contain the
            // contents of the verify file
            StringBuilder sb = new StringBuilder();

            foreach (KeyValuePair<string, HashInfo> file in files)
            {
                HashInfo hashInfo = file.Value;
                sb.AppendLine($"{file.Key}{Separator}{hashInfo.Algorithm.ToString().ToLower()}{Separator}{hashInfo.Value}");
            }

            try
            {
                using (StreamWriter sw = new StreamWriter(FilePath))
                {
                    sw.Write(sb.ToString());
                }
            }
            catch (DirectoryNotFoundException)
            {
                Logger.WriteLine($"ERROR: The directory {folder.FullName} was not found.");
            }
            catch (PathTooLongException)
            {
                Logger.WriteLine($"ERROR: The path {FilePath} is too long.");
            }
            catch (UnauthorizedAccessException)
            {
                Logger.WriteLine($"ERROR: Not authorized to write to {FilePath}.");
            }
            catch (IOException ex)
            {
                Logger.WriteLine($"ERROR: Can't write to file. Reason: {ex.Message}");                
            }
        }
    }
}
