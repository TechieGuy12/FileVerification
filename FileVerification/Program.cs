using System.Diagnostics;
using System.IO;
using System.CommandLine;
using System.CommandLine.Invocation;
using TE.FileVerification.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace TE.FileVerification
{
    class Program
    {
        // Success return code
        private const int SUCCESS = 0;

        // Error return code
        private const int ERROR = 1;

        // The path is not a file
        private const int ERROR_NOT_FILE = 2;

        // The hash could not be generated
        private const int ERROR_NO_HASH = 3;

        // The hash of the file does not match the provided hash
        private const int ERROR_HASH_NOT_MATCH = 4;

        public static int NumFolders { get; set; }

        static int Main(string[] args)
        {
            RootCommand rootCommand = new RootCommand(
                description: "Generates the hash of all files in a folder tree and stores the hashes in text files in each folder.");

            var fileOption = new Option<string>(
                    aliases: new string[] { "--file", "-f" },
                    description: "The file or folder to verify with a hash."
            );
            fileOption.IsRequired = true;
            rootCommand.AddOption(fileOption);

            var checksumFileOption = new Option<string>(
                aliases: new string[] { "--checksumfile", "-cf" },
                description: "The name of the checksum file."
            );
            rootCommand.AddOption(checksumFileOption);

            var algorithmOption = new Option<HashAlgorithm>(
                    aliases: new string[] { "--algorithm", "-a" },
                    description: "The hash algorithm to use."
            );
            rootCommand.AddOption(algorithmOption);

            var hashOption = new Option<string>(
                aliases: new string[] { "--hash", "-ha" },
                description: "The hash of the file to verify."
            );
            rootCommand.AddOption(hashOption);

            var threadsOption = new Option<int>(
                aliases: new string[] { "--threads", "-t" },
                description: "The number of threads to use to verify the files."
            );
            rootCommand.AddOption(threadsOption);

            var getHashOnlyOption = new Option<bool>(
                aliases: new string[] { "--hashonly", "-ho" },
                description: "Generate and display the file hash."
            );
            rootCommand.AddOption(getHashOnlyOption);

            var settingsFileOption = new Option<string>(
                    aliases: new string[] { "--settingsfile", "-sfi" },
                    description: "The name of the settings XML file."
            );
            rootCommand.AddOption(settingsFileOption);

            var settingsFolderOption = new Option<string>(
                    aliases: new string[] { "--settingsfolder", "-sfo" },
                    description: "The folder containing the settings XML file."
            );
            rootCommand.AddOption(settingsFolderOption);

            rootCommand.SetHandler(
                (
                    fileOptionValue,
                    checksumFileOptionValue,
                    algorithmOptionValue,
                    hashOptionValue,
                    getHashOnlyOptionValue,
                    threadsOptionValue,
                    settingsFileOptionValue,
                    settingsFolderOptionValue
                ) =>
                {
                    Run(
                        fileOptionValue,
                        checksumFileOptionValue,
                        algorithmOptionValue,
                        hashOptionValue,
                        getHashOnlyOptionValue,
                        threadsOptionValue,
                        settingsFileOptionValue,
                        settingsFolderOptionValue);
                },
                fileOption,
                checksumFileOption,
                algorithmOption,
                hashOption,
                getHashOnlyOption,
                threadsOption,
                settingsFileOption,
                settingsFolderOption
            );
            return rootCommand.Invoke(args);
        }

        /// <summary>
        /// Runs the necessary hashing for the file or folder.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="algorithm"></param>
        /// <param name="settingsFile"></param>
        /// <param name="settingsFolder"></param>
        /// <returns></returns>
        static int Run(
            string? file,
            string? checksumFile,
            HashAlgorithm? algorithm,
            string hashOption,
            bool hashOnlyOption,
            int? threads,
            string? settingsFile,
            string? settingsFolder)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(file))
                {
                    Logger.WriteLine("The file or folder was not specified.");
                    return ERROR;
                }

                if (checksumFile == null || string.IsNullOrEmpty(checksumFile))
                {
                    checksumFile = ChecksumFile.DEFAULT_CHECKSUM_FILENAME;
                }

                if (algorithm == null)
                {
                    algorithm = HashAlgorithm.SHA256;
                }

                if (threads == null || threads == default(int))
                {
                    threads = Environment.ProcessorCount;
                }
                else if (threads <= 0)
                {
                    threads = 1;
                }

                // Trim the double-quote from the path, since it can cause an
                // issue if the path ends with a slash ('\'), because the code
                // will interpret the slash and double-quote as an escape
                // character for the double quote ('\"' to '"')
                file = file.Trim('"');

                // If the hash option has not been specified, or the hash only
                // option is false then continue with cralwing the directory to
                // generate and verify the hashes of the files
                if (string.IsNullOrWhiteSpace(hashOption) && !hashOnlyOption)
                {
                    // Read the settings file if one was provided as an argument
                    Settings? settings = null;
                    if (!string.IsNullOrWhiteSpace(settingsFile) && !string.IsNullOrWhiteSpace(settingsFolder))
                    {
                        ISettingsFile xmlFile = new XmlFile(settingsFolder, settingsFile);
                        settings = xmlFile.Read();
                    }

                    Logger.WriteLine("--------------------------------------------------------------------------------");
                    Logger.WriteLine($"Folder/File:         {file}");
                    Logger.WriteLine($"Hash Algorithm:      {algorithm}");
                    Logger.WriteLine($"Threads:             {threads}");
                    Logger.WriteLine("--------------------------------------------------------------------------------");

                    PathInfo path = new PathInfo(file, checksumFile);
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    path.Crawl(true);
                    if (path.Files != null)
                    {
                        path.Check((HashAlgorithm)algorithm, (int)threads);
                        watch.Stop();

                        Logger.WriteLine("--------------------------------------------------------------------------------");
                        Logger.WriteLine($"Folders:             {path.DirectoryCount}");
                        Logger.WriteLine($"Files:               {path.FileCount}");
                        Logger.WriteLine($"Time (ms):           {watch.ElapsedMilliseconds}");
                        Logger.WriteLine("--------------------------------------------------------------------------------");
                    }

                    // If settings were specified, then send the notifications
                    if (settings != null)
                    {
                        settings.Send();
                    }

                    return SUCCESS;
                }
                else
                {
                    if (!PathInfo.IsFile(file))
                    {
                        Logger.WriteLine($"The file '{file}' is not a valid file.");
                        return ERROR_NOT_FILE;
                    }

                    string? fileHash = HashInfo.GetFileHash(file, (HashAlgorithm)algorithm);
                    if (string.IsNullOrWhiteSpace(fileHash))
                    {
                        Logger.WriteLine($"The hash for file '{file}' could not be generated.");
                        return ERROR_NO_HASH;
                    }

                    // If the hash only option was specified, then just display
                    // the hash of the file
                    if (hashOnlyOption)
                    {
                        Logger.WriteLine(fileHash);
                        return SUCCESS;
                    }

                    // The the hash option was specified, compare the file hash
                    // with the hash passed through the argument
                    if (!string.IsNullOrWhiteSpace(hashOption))
                    {
                        int returnValue = string.Compare(fileHash, hashOption, true) == 0 ? SUCCESS : ERROR_HASH_NOT_MATCH;

                        if (returnValue == SUCCESS)
                        {
                            Logger.WriteLine($"The file hash matches the hash '{hashOption}'");
                        }
                        else
                        {
                            Logger.WriteLine($"The file hash '{fileHash}' does not match the hash '{hashOption}'");
                        }

                        return returnValue;
                    }                    
                }

                return SUCCESS;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"An error occurred. Error: {ex.Message}");
                return ERROR;
            }
        }
    }
}
