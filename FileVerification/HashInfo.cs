using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace FileVerification
{
    public enum Algorithm
    {
        MD5,
        SHA,
        SHA256,
        SHA512
    }

    public class HashInfo
    {
        public Algorithm Algorithm { get; set; }

        public string Value { get; set; }

        public HashInfo(string algorithm, string hash)
        {
            if (algorithm == null || string.IsNullOrWhiteSpace(algorithm))
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            if (hash == null || string.IsNullOrWhiteSpace(hash))
            {
                throw new ArgumentNullException(nameof(hash));
            }

            Algorithm = GetHash(algorithm);
            Value = hash;
        }

        public HashInfo(FileInfo file, string algorithm)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (algorithm == null || string.IsNullOrWhiteSpace(nameof(algorithm)))
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            Algorithm = GetHash(algorithm);
            Value = CreateFileHash(file.FullName);
        }

        public HashInfo(FileInfo file, Algorithm algorithm)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            Algorithm = algorithm;
            Value = CreateFileHash(file.FullName);
        }

        /// <summary>
        /// Gets the hash enumeration value of the hash string name.
        /// </summary>
        /// <param name="hash">
        /// The name of the hash.
        /// </param>
        /// <returns>
        /// The enum value of the hash.
        /// </returns>
        private Algorithm GetHash(string hash)
        {
            if (string.Compare(hash, "sha256", true) == 0)
            {
                return Algorithm.SHA256;
            }
            else
            {
                return Algorithm.SHA512;
            }
        }

        private string CreateFileHash(string filePath)
        {
            using (var hashAlgorithm = HashAlgorithm.Create(this.Algorithm.ToString()))
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = hashAlgorithm.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
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
        public bool IsEqual(string filePath)
        {
            if (filePath == null || string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            string fileHash = CreateFileHash(filePath);
            return Value.Equals(fileHash);
        }
    }
}
