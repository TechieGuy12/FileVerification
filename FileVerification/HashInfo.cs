using System;
using System.Collections.Generic;
using System.Text;
using Cryptography = System.Security.Cryptography;
using System.IO;

namespace TE.FileVerification
{
    public enum HashAlgorithm
    {
        MD5,
        SHA,
        SHA256,
        SHA512
    }
    public class HashInfo
    {
        // A megabyte
        private const int Megabyte = 1024 * 1024;

        /// <summary>
        /// Gets the hash algorithm used to create the hash of the file.
        /// </summary>
        public HashAlgorithm Algorithm { get; private set;}

        /// <summary>
        /// Gets the hash associated with the file.
        /// </summary>
        public string? Hash { get; private set; }

        /// <summary>
        /// Gets the information about the file.
        /// </summary>
        public string FilePath { get; private set; }

        private HashInfo(string filePath)
        {
            if (filePath == null || string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            FilePath = filePath;
        }

        public HashInfo(string filePath, string algorithm, string hash)
            : this(filePath, algorithm)        
        {
            if (hash == null || string.IsNullOrWhiteSpace(hash))
            {
                throw new ArgumentNullException(nameof(hash));
            }

            Hash = hash;
        }

        public HashInfo(string filePath, string algorithm) 
            : this(filePath)
        {           
            if (algorithm == null || string.IsNullOrWhiteSpace(algorithm))
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            Algorithm = GetAlgorithm(algorithm);
            Hash = GetFileHash();
        }

        public HashInfo(string filePath, HashAlgorithm algorithm) 
            : this(filePath)
        {
            Algorithm = algorithm;
            Hash = GetFileHash();
        }

        /// <summary>
        /// Gets the hash algorithm value of the algorithm string name.
        /// </summary>
        /// <param name="hash">
        /// The name of the algorithm.
        /// </param>
        /// <returns>
        /// The enum value of the algorithm.
        /// </returns>
        private static HashAlgorithm GetAlgorithm(string hash)
        {
            if (string.Compare(hash, "sha256", true) == 0)
            {
                return HashAlgorithm.SHA256;
            }
            else
            {
                return HashAlgorithm.SHA512;
            }
        }

        private string? GetFileHash()
        {
            int maxSize = 16 * Megabyte;

            using Cryptography.HashAlgorithm? hashAlgorithm =
                Cryptography.HashAlgorithm.Create(Algorithm.ToString());
            if (hashAlgorithm == null)
            {
                return null;
            }

            try
            {
                using var stream = 
                    new FileStream(
                        FilePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        maxSize);

                var hash = hashAlgorithm.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "");
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Checks to see if the hash of a specific file is equal to this hash
        /// value.
        /// </summary>
        /// <param name="filePath">
        /// The path to the file that will be used to generate the hash to
        /// compare to this hash.
        /// </param>
        /// <returns>
        /// True if the hashes are equal, false if the hashes are not equal.
        /// </returns>
        /// <remarks>
        /// The hash algorithm used will be the same one that is set for this
        /// object.
        /// </remarks>
        public bool IsHashEqual(string hash)
        {
            if (string.IsNullOrWhiteSpace(Hash))
            {
                return string.IsNullOrWhiteSpace(hash);
            }

            return Hash.Equals(hash);
        }
    }
}
