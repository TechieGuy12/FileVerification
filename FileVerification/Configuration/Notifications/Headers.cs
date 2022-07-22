using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileVerification.Configuration.Notifications
{
    public class Headers
    {
        [XmlElement("header")]
        public Collection<Header>? HeaderList { get; set; }

        /// <summary>
        /// Sets the headers for a request.
        /// </summary>
        /// <param name="request">
        /// The request that will include the headers.
        /// </param>
        public void Set(HttpRequestMessage request)
        {
            if (request == null)
            {
                return;
            }

            if (HeaderList == null || HeaderList.Count <= 0)
            {
                return;
            }

            foreach (Header header in HeaderList)
            {
                if (!string.IsNullOrWhiteSpace(header.Name))
                {
                    request.Headers.Add(header.Name, header.Value);
                }
            }
        }
    }
}
