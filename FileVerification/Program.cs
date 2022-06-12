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

            var settingsFileOption = new Option<string>(
                    aliases: new string[] { "--settingsFile", "-sfi" },
                    description: "The name of the settings XML file."
            );
            rootCommand.AddOption(settingsFileOption);

            var settingsFolderOption = new Option<string>(
                    aliases: new string[] { "--settingsFolder", "-sfo" },
                    description: "The folder containing the settings XML file."
            );
            rootCommand.AddOption(settingsFolderOption);

            rootCommand.SetHandler((fileOptionValue, algorithmOptionValue, hashOption, settingsFileOptionValue, settingsFolderOptionValue) =>
            {
                Run(fileOptionValue, algorithmOptionValue, hashOption, settingsFileOptionValue, settingsFolderOptionValue);
            },
            fileOption, algorithmOption, hashOption, settingsFileOption, settingsFolderOption);
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
        static int Run(string? file, HashAlgorithm? algorithm, string hashOption, string? settingsFile, string? settingsFolder)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(file))
                {
                    Logger.WriteLine("The file or folder was not specified.");
                    return ERROR;
                }

                if (algorithm == null)
                {
                    algorithm = HashAlgorithm.SHA256;
                }

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
                Logger.WriteLine("--------------------------------------------------------------------------------");

                if (string.IsNullOrWhiteSpace(hashOption))
                {
                    PathInfo path = new PathInfo(file);
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    path.Crawl(true);
                    if (path.Files != null)
                    {
                        path.Check((HashAlgorithm)algorithm);
                        watch.Stop();

                        Logger.WriteLine("--------------------------------------------------------------------------------");
                        Logger.WriteLine($"Folders:         {path.DirectoryCount}");
                        Logger.WriteLine($"Files:           {path.FileCount}");
                        Logger.WriteLine($"Time (ms):       {watch.ElapsedMilliseconds}");
                        Logger.WriteLine("--------------------------------------------------------------------------------");
                    }

                    // If settings  gdwere specified, then send the notifications
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
            catch (Exception ex)
            {
                Logger.WriteLine($"An error occurred. Error: {ex.Message}");
                return ERROR;
            }
        }
    }
}
