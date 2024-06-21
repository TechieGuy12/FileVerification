using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace TE.FileVerification.Configuration
{
    [XmlRoot("settings")]
    public class Settings
    {
        /// <summary>
        /// Gets or sets the notifications for the verification.
        /// </summary>
        [XmlElement("notifications")]
        public Notifications? Notifications { get; set; }

        /// <summary>
        /// Gets or sets the exclusions for the verification.
        /// </summary>
        [XmlElement("exclusions")]
        public Exclusions? Exclusions { get; set; }

        /// <summary>
        /// Gets or sets the value indicating that a file can be removed from the
        /// checksum file if it doesn't exist.
        /// </summary>
        [XmlElement("allowremove")]
        public bool? AllowRemove { get; set; }

        /// <summary>
        /// Initializes an instance of the <see cref="Settings"/> class.
        /// </summary>
        public Settings()
        {
            AllowRemove = false;
        }

        /// <summary>
        /// Sends the notifications.
        /// </summary>
        public void Send()
        {
            if (Notifications == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Logger.Lines))
            {
                return;
            }
            
            Notifications.Send(Logger.Lines);            
        }
    }
}
