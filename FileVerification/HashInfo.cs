using System;
using System.Collections.Generic;
using System.Text;
using Cryptography = System.Security.Cryptography;
using System.IO;

namespace TE.FileVerification
{
    public enum HashAlgorithm
    {
        SHA256,
        MD5,
        SHA1,        
        SHA512
    }
    public class HashInfo
    {
        /// <summary>
        /// The separator used in the checksum file.
        /// </summary>
        public const char Separator = '|';

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
        /// Gets the full path to the file.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        public string FileName 
        { 
            get
            {
                return Path.GetFileName(FilePath);
            }
        }

        /// <summary>
        /// Initializes an instance of the <see cref="HashInfo"/> class when
        /// provided with the full path to the file.
        /// </summary>
        /// <param name="filePath">
        /// The full path, including the directory, to the file.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="filePath"/> parameter is null or empty.
        /// </exception>
        private HashInfo(string filePath)
        {
            if (filePath == null || string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            FilePath = filePath;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="HashInfo"/> class when
        /// provided with the full path to the file, the string representation
        /// of the hash algorithm and the file hash.
        /// </summary>
        /// <param name="filePath">
        /// The full path, including the directory, to the file.
        /// </param>
        /// <param name="algorithm">
        /// The string representation of the hash algorithm used to create
        /// the hash.
        /// </param>
        /// <param name="hash">
        /// The hash value of the file.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// A parameter is null or empty.
        /// </exception>
        public HashInfo(string filePath, string algorithm, string hash)
            : this(filePath, algorithm)        
        {
            if (hash == null || string.IsNullOrWhiteSpace(hash))
            {
                throw new ArgumentNullException(nameof(hash));
            }

            Hash = hash;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="HashInfo"/> class when
        /// provided with the full path to the file, and the string
        /// representation of the hash algorithm.
        /// </summary>
        /// <param name="filePath">
        /// The full path, including the directory, to the file.
        /// </param>
        /// <param name="algorithm">
        /// The string representation of the hash algorithm.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// A parameter is null or empty.
        /// </exception>
        public HashInfo(string filePath, string algorithm) 
            : this(filePath)
        {           
            if (algorithm == null || string.IsNullOrWhiteSpace(algorithm))
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            Algorithm = GetAlgorithm(algorithm);
            Hash = GetFileHash(FilePath, Algorithm);
        }

        /// <summary>
        /// Initializes an instance of the <see cref="HashInfo"/> class when
        /// provided with the full path to the file, and the hash algorithm.
        /// </summary>
        /// <param name="filePath">
        /// The full path, including the directory, to the file.
        /// </param>
        /// <param name="algorithm">
        /// The hash algorithm.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// A parameter is null or empty.
        /// </exception>
        public HashInfo(string filePath, HashAlgorithm algorithm) 
            : this(filePath)
        {
            Algorithm = algorithm;
            Hash = GetFileHash(FilePath, Algorithm);
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
        private static HashAlgorithm GetAlgorithm(string algorithm)
        {            
            if (string.Compare(algorithm, "md5", true) == 0)
            {
                return HashAlgorithm.MD5;
            }
            else if (string.Compare(algorithm, "sha1", true) == 0)
            {
                return HashAlgorithm.SHA1;
            }
            else if (string.Compare(algorithm, "sha512", true) == 0)
            {
                return HashAlgorithm.SHA512;
            }
            else
            {
                return HashAlgorithm.SHA256;
            }
        }

        /// <summary>
        /// Gets the hash of the file for the specified hash algorithm.
        /// </summary>
        /// <param name="file">
        /// The full path, including directory, of the file.
        /// </param>        
        /// <param name="algorithm">
        /// The algorithm used to generate the hash.
        /// </param>
        /// <returns>
        /// The hash of the file, or <c>null</c> if the hash could not be
        /// generated.
        /// </returns>
        public static string? GetFileHash(string file, HashAlgorithm algorithm)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                return null;
            }
            int maxSize = 64 * 1024; // Megabyte;

            Cryptography.HashAlgorithm? hashAlgorithm = null;

            try
            {                
                switch (algorithm)
                {
                    case HashAlgorithm.MD5:
                        hashAlgorithm = Cryptography.MD5.Create();
                        break;
                    case HashAlgorithm.SHA1:
                        hashAlgorithm = Cryptography.SHA1.Create();
                        break;
                    case HashAlgorithm.SHA256:
                        hashAlgorithm = Cryptography.SHA256.Create();
                        break;
                    case HashAlgorithm.SHA512:
                        hashAlgorithm = Cryptography.SHA512.Create();
                        break;
                }

                if (hashAlgorithm == null)
                {
                    Logger.WriteLine($"Couldn't create hash. Reason: Hash was not provided.");
                    return null;
                }

                try
                {
                    using var stream =
                    //new FileStream(
                    //    file,
                    //    FileMode.Open,
                    //    FileAccess.Read,
                    //    FileShare.None);
                    new FileStream(
                        file,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.None,
                        maxSize);

                    var hash = hashAlgorithm.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
                catch
                {

                    return null;
                }
            }
            finally
            {
                if (hashAlgorithm != null)
                {
                    hashAlgorithm.Clear();
                    hashAlgorithm.Dispose();
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
        public bool IsHashEqual(string hash)
        {
            if (string.IsNullOrWhiteSpace(Hash))
            {
                return string.IsNullOrWhiteSpace(hash);
            }

            return Hash.Equals(hash);
        }

        /// <summary>
        /// Returns a string representing the hash information.
        /// </summary>
        /// <returns>
        /// A string representation of the hash information.
        /// </returns>
        public override string ToString()
        {
            return $"{FileName}{Separator}{Algorithm.ToString().ToLower()}{Separator}{Hash}";
        }
    }
}
