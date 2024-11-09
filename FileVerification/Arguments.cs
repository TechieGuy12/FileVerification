using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TE.FileVerification
{
    public class Arguments
    {

        public string? File { get; set; }
        
        public string? ChecksumFile { get; set; }
        
        public bool ExcludeSubDir { get; set; }

        public HashAlgorithm Algorithm { get; set; }

        public string? Hash {  get; set; }  

        public bool HashOnly { get; set; }
        
        public int? Threads { get; set; }

        public string? SettingsFile { get; set; }

        public bool RemoveFile { get; set; }
    }
}
