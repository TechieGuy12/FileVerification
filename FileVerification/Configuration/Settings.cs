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
        public Notifications.Notifications Notifications { get; set; }
    }
}
