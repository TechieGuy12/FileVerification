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
        public Notifications.Notifications? Notifications { get; set; }

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
