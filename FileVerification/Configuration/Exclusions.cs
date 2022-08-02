using System;
using TE.FileVerification.IO;

namespace TE.FileVerification.Configuration
{
    /// <summary>
    /// An exclusions node in the XML file.
    /// </summary>
    public class Exclusions : MatchBase
    {
        /// <summary>
        /// Returns the flag indicating if the file/folder is to be ignored.
        /// </summary>
        /// <<param name="fullPath">
        /// The full path to the file or folder.
        /// </param>
        /// <returns>
        /// True if the file/folder is to be ignored, otherwise false.
        /// </returns>
        public bool Exclude(string fullPath)
        {
            FilterTypeName = "Exclude";
            return IsMatchFound(fullPath);
        }
    }
}
