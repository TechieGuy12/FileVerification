using System.Diagnostics;
using System.IO;
using System.CommandLine;
using System.CommandLine.Invocation;
using TE.FileVerification.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TE.FileVerification
{
    class Program
    {
        // Success return code
        private const int SUCCESS = 0;

        // Error return code
        private const int ERROR = -1;

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

            rootCommand.SetHandler((fileOptionValue, algorithmOptionValue, settingsFileOptionValue, settingsFolderOptionValue) =>
            {
                Run(fileOptionValue, algorithmOptionValue, settingsFileOptionValue, settingsFolderOptionValue);
            },
            fileOption, algorithmOption, settingsFileOption, settingsFolderOption);
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
        static int Run(string? file, HashAlgorithm? algorithm, string? settingsFile, string? settingsFolder)
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
            Logger.WriteLine($"Folder:        {file}");
            Logger.WriteLine("--------------------------------------------------------------------------------");

            PathInfo path = new PathInfo(file);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            path.Crawl(true);
            if (path.Files != null)
            {
                path.Check();
                watch.Stop();

                Logger.WriteLine("--------------------------------------------------------------------------------");
                Logger.WriteLine($"Folders:         {path.DirectoryCount}");
                Logger.WriteLine($"Files:           {path.FileCount}");
                //Logger.WriteLine($"Checksum Files: {checksumFilesCount}");                
                Logger.WriteLine($"Time (ms):       {watch.ElapsedMilliseconds}");
                Logger.WriteLine("--------------------------------------------------------------------------------");
            }

            // If settings were specified, then send the notifications
            if (settings != null)
            {
                settings.Send();
            }

            return SUCCESS;
        }
    }
}
