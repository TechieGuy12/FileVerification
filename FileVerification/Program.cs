using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.CommandLine;
using System.CommandLine.Invocation;
using TE.FileVerification.Configuration;
using System.Xml.Serialization;
using System.Reflection;

namespace TE.FileVerification
{
    class Program
    {
        // Success return code
        private const int SUCCESS = 0;

        // Error return code
        private const int ERROR = -1;

        // The default configuration file name
        const string DEFAULT_SETTINGS_FILE = "config.xml";

        public static int NumFolders { get; set; }

        static int Main(string[] args)
        {
            RootCommand rootCommand = new RootCommand(
                description: "Generates the hash of all files in a folder tree and stores the hashes in text files in each folder.");

            var folderOption = new Option<string>(
                    aliases: new string[] { "--folder", "-f" },
                    description: "The folder containing the files to verify with a hash."
                );

            folderOption.IsRequired = true;
            rootCommand.AddOption(folderOption);

            var settingsFileOption = new Option<string>(
                    aliases: new string[] { "--settingsFile", "-sfi" },
                    description: "The name of the settings XML file."
                );
            rootCommand.AddOption(settingsFileOption);

            var setingsFolderOption = new Option<string>(
                    aliases: new string[] { "--settingsFolder", "-sfo" },
                    description: "The folder containing the settings XML file."
                );
            rootCommand.AddOption(setingsFolderOption);

            rootCommand.Handler = CommandHandler.Create<string, string, string>(Verify);
            return rootCommand.Invoke(args);
        }

        /// <summary>
        /// Gets the folder path containing the settings file.
        /// </summary>
        /// <param name="path">
        /// The folder path.
        /// </param>
        /// <returns>
        /// The folder path of the files, otherwise null.
        /// </returns>
        private static string GetFolderPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                try
                {                    
                    path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"The folder name is null or empty. Couldn't get the current location. Reason: {ex.Message}");
                    return null;
                }
            }

            if (Directory.Exists(path))
            {
                return path;
            }
            else
            {
                Console.WriteLine("The folder does not exist.");
                return null;
            }
        }

        /// <summary>
        /// Gets the full path to the settings file.
        /// </summary>
        /// <param name="path">
        /// The path to the settings file.
        /// </param>
        /// <param name="name">
        /// The name of the settings file.
        /// </param>
        /// <returns>
        /// The full path to the settings file, otherwise null.
        /// </returns>
        private static string GetSettingsFilePath(string path, string name)
        {
            string folderPath = GetFolderPath(path);
            if (folderPath == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = DEFAULT_SETTINGS_FILE;
            }

            try
            {
                string fullPath = Path.Combine(folderPath, name);
                if (File.Exists(fullPath))
                {
                    Console.WriteLine($"Settings file: {fullPath}.");
                    return fullPath;
                }
                else
                {
                    Console.WriteLine($"The settings file '{fullPath}' was not found.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not get the path to the settings file. Reason: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Reads the settings XML file.
        /// </summary>
        /// <param name="path">
        /// The path to the settings XML file.
        /// </param>
        /// <returns>
        /// A <see cref="Settings"/> object if the file was read successfully, otherwise null.
        /// </returns>
        private static Settings ReadSettingsFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("The settings file path was null or empty.");
                return null;
            }

            if (!File.Exists(path))
            {
                Console.WriteLine($"The settings file path '{path}' does not exist.");
                return null;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                using FileStream fs = new FileStream(path, FileMode.Open);
                return (Settings)serializer.Deserialize(fs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"The settings file could not be read. Reason: {ex.Message}");
                return null;
            }
        }

        static int Verify(string folder, string settingsFile, string settingsFolder)
        {
            string settingsFilePath = GetSettingsFilePath(settingsFolder, settingsFile);
            if (string.IsNullOrWhiteSpace(settingsFilePath))
            {
                return ERROR;
            }

            Settings settings = ReadSettingsFile(settingsFilePath);

            Logger.WriteLine("--------------------------------------------------------------------------------");
            Logger.WriteLine($"Folder:        {folder}");

            FileSystemCrawlerSO fsc = new FileSystemCrawlerSO();
            Stopwatch watch = new Stopwatch();
            
            Logger.WriteLine("--------------------------------------------------------------------------------");

            watch.Start();
            fsc.CollectFolders(folder);
            fsc.CollectFiles();
            watch.Stop();            

            Logger.WriteLine("--------------------------------------------------------------------------------");
            Logger.WriteLine($"Folders:       {fsc.NumFolders}");
            Logger.WriteLine($"Files:         {fsc.NumFiles}");
            Logger.WriteLine($"Time (ms):     { watch.ElapsedMilliseconds}");
            Logger.WriteLine("--------------------------------------------------------------------------------");

            if (settings.Notifications != null)
            {                
                settings.Notifications.Send(Logger.Lines);
            }

            return SUCCESS;
        }
    }
}
