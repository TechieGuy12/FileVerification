using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TE.FileVerification.Configuration
{
    /// <summary>
    /// Configuration file interface.
    /// </summary>
    interface ISettingsFile
    {
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
        public Settings? Read();
    }
}
