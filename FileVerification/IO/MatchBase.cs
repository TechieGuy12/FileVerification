using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace TE.FileVerification.IO
{
    /// <summary>
    /// A base class containing the properties and methods for filtering the
    /// files and folders of the watch.
    /// </summary>
    public abstract class MatchBase
    {
        // The set of full path to the folders to ignore
        private protected HashSet<string>? _folders;

        // The set of full path to the paths to ignore
        private protected HashSet<string>? _paths;

        // Sets the flag indicating the ignore lists have been populated
        private protected bool _initialized;

        /// <summary>
        /// Gets or sets the files node.
        /// </summary>
        [XmlElement("files")]
        public Files? Files { get; set; }

        /// <summary>
        /// Gets or sets the folders node.
        /// </summary>
        [XmlElement("folders")]
        public Folders? Folders { get; set; }

        /// <summary>
        /// Gets or sets the paths node.
        /// </summary>
        [XmlElement("paths")]
        public Paths? Paths { get; set; }

        /// <summary>
        /// Gets or sets the attributes node.
        /// </summary>
        [XmlElement("attributes")]
        public Attributes? Attributes { get; set; }

        /// <summary>
        /// Gets or sets the type of filter used for logging.
        /// </summary>
        [XmlIgnore]
        private protected string FilterTypeName { get; set; } = "Filter";

        /// <summary>
        /// Gets a value indicating if at least one valid filtering value has
        /// been specified. An empty element could be added to the XML file,
        /// so this method ensures a filtering element has a valid value
        /// specified.
        /// </summary>
        /// <returns>
        /// <c>true</c> if at least one filtering value is specified, otherwise
        /// <c>false</c>.
        /// </returns>
        public bool IsSpecified()
        {
            bool isSpecified = false;
            if (Files != null && Files.Name.Count > 0)
            {
                isSpecified = true;
            }

            if (Folders != null && Folders.Name.Count > 0)
            {
                isSpecified = true;
            }

            if (Attributes != null && Attributes.Attribute.Count > 0)
            {
                isSpecified = true;
            }

            if (Paths != null && Paths.Path.Count > 0)
            {
                isSpecified = true;
            }

            return isSpecified;
        }

        /// <summary>
        /// Returns the flag indicating whether the attribute for a file that
        /// is changed matches the attributes from the configuration file.
        /// When a file is deleted, the attributes of the file cannot be checked
        /// since the file is no longer available, so the attributes cannot be
        /// determined, so on deletion this function will always return 
        /// <c>false</c>.
        /// </summary>
        /// <param name="path">
        /// The full path to the file.
        /// </param>
        /// <returns>
        /// True if the file change is a match, otherwise false.
        /// </returns>
#pragma warning disable CA1031
        private protected bool AttributeMatch(string path)
        {
            if (Attributes == null || Attributes.Attribute.Count <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return false;
            }

            bool hasAttribute = false;
            try
            {                 
                FileAttributes fileAttributes = File.GetAttributes(path);
                foreach (FileAttributes attribute in Attributes.Attribute)
                {
                    if (fileAttributes.HasFlag(attribute))
                    {
                        Logger.WriteLine($"{FilterTypeName}: The path '{path}' has the attribute '{attribute}'.");
                        hasAttribute = true;
                        break;
                    }
                }
            }
            catch
            {
                hasAttribute = false;
            }
            return hasAttribute;
        }
#pragma warning restore CA1031

        /// <summary>
        /// Returns the flag indicating whether the current file changed is
        /// a match that is found for files.
        /// </summary>
        /// <param name="fullPath">
        /// The full path of the file.
        /// </param>
        /// <returns>
        /// True if the file change is a match, otherwise false.
        /// </returns>
        private protected bool FileMatch(string fullPath)
        {
            if (Files == null || Files.Name.Count <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return false;
            }

            string? name = Path.GetFileName(fullPath);
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            bool isMatch = false;
            foreach (Name fileName in Files.Name)
            {
                isMatch = fileName.IsMatch(name);
                if (isMatch)
                {
                    Logger.WriteLine($"{FilterTypeName}: The match pattern '{fileName.Pattern}' is a match for file {name}.");
                    break;
                }
            }

            return isMatch;
        }

        /// <summary>
        /// Returns the flag indicating whether the current folder is a match.
        /// </summary>
        /// <param name="path">
        /// The path of the folder that was changed.
        /// </param>
        /// <returns>
        /// True if the folder is a match, otherwise false.
        /// </returns>
        private protected bool FolderMatch(string path)
        {
            if (Folders == null || Folders.Name.Count <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            bool isMatch = false;
            foreach (Name folder in Folders.Name)
            {
                isMatch = folder.IsMatch(path);
                if (isMatch)
                {
                    Logger.WriteLine($"{FilterTypeName}: The match pattern '{folder.Pattern}' is a match for folder '{path}'.");
                    break;
                }
            }

            return isMatch;
        }

        /// <summary>
        /// Returns the flag indicating whether the current path is a match.
        /// </summary>
        /// <param name="path">
        /// The full path.
        /// </param>
        /// <returns>
        /// True if the path is a match, otherwise false.
        /// </returns>
        private protected bool PathMatch(string path)
        {
            if (_paths == null || _paths.Count <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            bool isMatch = false;
            foreach (string aPath in _paths)
            {
                if (path.Contains(aPath, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLine($"{FilterTypeName}: The path '{path}' contains the path '{aPath}'.");
                    isMatch = true;
                    break;
                }
            }

            return isMatch;
        }

        /// <summary>
        /// Gets a value indicating if a match is found between the
        /// file/folder data, and the specified patterns.
        /// </summary>
        /// <param name="fullPath">
        /// The full path to the file or folder.
        /// </param>
        /// <returns>
        /// <c>true</c> of a match is found, otherwise <c>false</c>.
        /// </returns>
        private protected bool IsMatchFound(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return false;
            }
           
            bool isMatch = false;
            if (Files != null && Files.Name.Count > 0)
            {
                isMatch |= FileMatch(fullPath);
            }

            if (Folders != null && Folders.Name.Count > 0)
            {
                isMatch |= FolderMatch(fullPath);
            }

            if (Attributes != null && Attributes.Attribute.Count > 0)
            {
                isMatch |= AttributeMatch(fullPath);
            }

            if (Paths != null && Paths.Path.Count > 0)
            {
                isMatch |= PathMatch(fullPath);
            }

            return isMatch;
        }
    }
}
