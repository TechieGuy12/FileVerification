using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Text.Json;
using System.Globalization;
using TE.FileVerification.Net;

namespace TE.FileVerification.Configuration
{
    public class Notification
    {
        // The message to send with the request.
        private readonly StringBuilder _message;

        /// <summary>
        /// Gets or sets the URL of the request.
        /// </summary>
        [XmlElement("url")]
        public string? Url { get; set; }

        /// <summary>
        /// Gets the URI value of the string URL.
        /// </summary>
        [XmlIgnore]
        public Uri? Uri
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(Url))
                    {
                        return null;
                    }

                    Uri uri = new Uri(Url);
                    return uri;
                }
                catch (Exception ex)
                    when (ex is ArgumentNullException || ex is UriFormatException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the string representation of the request method.
        /// </summary>
        [XmlElement("method")]
        public string MethodString { get; set; } = "Post";

        /// <summary>
        /// Gets the request method.
        /// </summary>
        [XmlIgnore]
        public HttpMethod Method
        {
            get
            {
                HttpMethod method = HttpMethod.Post;
                if (string.IsNullOrEmpty(MethodString))
                {
                    return method;
                }

                try
                {
                    method = (HttpMethod)Enum.Parse(typeof(HttpMethod), MethodString.ToUpper(CultureInfo.CurrentCulture), true);
                }
                catch (Exception ex)
                    when (ex is ArgumentNullException || ex is ArgumentException || ex is OverflowException)
                {
                    method = HttpMethod.Post;
                }

                return method;
            }
        }

        /// <summary>
        /// Gets or sets the data to send for the request.
        /// </summary>
        [XmlElement("data")]
        public Data? Data { get; set; }

        /// <summary>
        /// Returns a value indicating if there is a message waiting to be sent
        /// for the notification.
        /// </summary>
        [XmlIgnore]
        public bool HasMessage
        {
            get
            {
                if (_message == null)
                {
                    return false;
                }

                return _message.Length > 0;
            }
        }

        /// <summary>
        /// Initializes an instance of the <see cref="Notification"/>class.
        /// </summary>
        public Notification()
        {
            _message = new StringBuilder();
        }

        /// <summary>
        /// Sends the notification.
        /// </summary>
        /// <param name="message">
        /// The value that replaces the <c>[message]</c> placeholder.
        /// </param>
        internal void QueueRequest(string message)
        {
            _message.Append(CleanMessage(message) + @"\n");
        }

        /// <summary>
        /// Send the notification request.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the URL is null or empty.
        /// </exception>
        internal Response? Send()
        {
            // If there isn't a message to be sent, then just return
            if (_message == null || _message.Length <= 0)
            {
                return null;
            }

            if (GetUri() == null)
            {
                throw new InvalidOperationException("The URL is null or empty.");
            }

            if (Data == null)
            {
                throw new InvalidOperationException("Data for the request was not provided.");
            }

            string content = string.Empty;
            if (Data.Body != null)
            {
                content = Data.Body.Replace("[message]", _message.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            Response response =
                Request.Send(
                    Method,
                    GetUri(),
                    Data.Headers,
                    content,
                    Data.MimeType);

            _message.Clear();
            return response;
        }

        /// <summary>
        /// Send the notification request.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the URL is null or empty.
        /// </exception>
        internal async Task<Response?> SendAsync()
        {
            // If there isn't a message to be sent, then just return
            if (_message == null || _message.Length <= 0)
            {
                return null;
            }

            if (GetUri() == null)
            {
                throw new InvalidOperationException("The URL is null or empty.");
            }

            if (Data == null)
            {
                throw new InvalidOperationException("Data for the request was not provided.");
            }

            string content = string.Empty;
            if (Data.Body != null)
            {
                content = Data.Body.Replace("[message]", _message.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            Response response =
                await Request.SendAsync(
                    Method,
                    GetUri(),
                    Data.Headers,
                    content,
                    Data.MimeType).ConfigureAwait(false);

            _message.Clear();
            return response;
        }

        public static string CleanMessage(string s)
        {
            if (s == null || s.Length == 0)
            {
                return "";
            }

            char c = '\0';
            int i;
            int len = s.Length;
            StringBuilder sb = new(len + 4);
            string t;

            for (i = 0; i < len; i += 1)
            {
                c = s[i];
                switch (c)
                {
                    case '\\':
                    case '"':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '/':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    default:
                        if (c < ' ')
                        {
                            t = "000" + string.Format(CultureInfo.CurrentCulture, "{0:X}", c);
                            sb.Append(string.Concat("\\u", t.AsSpan(t.Length - 4)));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets the URI value of the string URL.
        /// </summary>
        /// <exception cref="UriFormatException">
        /// Thrown if the URL is not in a valid format.
        /// </exception>
        private Uri GetUri()
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                throw new UriFormatException();
            }

            Uri uri = new(Url);
            return uri;
        }
    }
}
