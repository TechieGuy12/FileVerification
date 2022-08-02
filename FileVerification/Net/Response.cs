﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TE.FileVerification.Net
{
    /// <summary>
    /// The information from a request response.
    /// </summary>
    internal class Response
    {
        /// <summary>
        /// Gets the status code.
        /// </summary>
        internal HttpStatusCode StatusCode { get; private set; }

        /// <summary>
        /// Gets the reason phrase.
        /// </summary>
        internal string? ReasonPhrase { get; private set; }

        /// <summary>
        /// Gets the content for the response.
        /// </summary>
        internal string? Content { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Response"/> class
        /// when provided with the status code, reason phrase and the
        /// content.
        /// </summary>
        /// <param name="statusCode">
        /// The response status code.
        /// </param>
        /// <param name="reasonPhrase">
        /// The reason phrase.
        /// </param>
        /// <param name="content">
        /// The content from the request.
        /// </param>
        internal Response(HttpStatusCode statusCode, string? reasonPhrase, string? content)
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Content = content;
        }
    }
}
