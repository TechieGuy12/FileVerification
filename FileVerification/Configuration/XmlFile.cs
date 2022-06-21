using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileVerification.Configuration
{
    /// <summary>
    /// The XML settings file.
    /// </summary>
    public class XmlFile : ISettingsFile
    {
        // The default configuration file name
        const string DEFAULT_SETTINGS_FILE = "config.xml";

        // Full path to the settings XML file
        private readonly string? _fullPath;

        /// <summary>
        /// Initialize an instance of the <see cref="XmlFile"/> class when
        /// provided with the path and name of the settings file.
        /// </summary>
        /// <param name="path">
        /// The full path to the settings file.
        /// </param>
        public XmlFile(string path)
        {
            _fullPath = CheckFullPath(path);
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
        private string? GetFolderPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
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
        /// The full path to the settings file.
        /// </param>
        /// <returns>
        /// The full path to the settings file, otherwise null.
        /// </returns>
        private string? CheckFullPath(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    Console.WriteLine($"Settings file: {path}.");
                    return path;
                }
                else
                {
                    Console.WriteLine($"The settings file '{path}' was not found.");
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
        /// A <see cref="Settings"/> object if the file was read successfully,
        /// otherwise <c>null</c>.
        /// </returns>
        public Settings? Read()
        {
            if (string.IsNullOrWhiteSpace(_fullPath))
            {
                Console.WriteLine("The settings file path was null or empty.");
                return null;
            }

            if (!File.Exists(_fullPath))
            {
                Console.WriteLine($"The settings file path '{_fullPath}' does not exist.");
                return null;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                using FileStream fs = new FileStream(_fullPath, FileMode.Open);
                return (Settings?)serializer.Deserialize(fs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"The settings file could not be read. Reason: {ex.Message}");
                return null;
            }
        }
    }
}
