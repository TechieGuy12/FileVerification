using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileVerification
{
    static class Logger
    {
        // The default name for the log file
        const string DEFAULT_NAME = "fv.log";      

        // Full path to the log file
        static string fullPath;

        static Logger()
        {
            Initialize(Path.GetTempPath(), DEFAULT_NAME);
        }

        private static void Initialize(string logFolder, string logName)
        {
            fullPath = Path.Combine(logFolder, logName);
            Clear();
        }

        private static void Clear()
        {
            try
            {
                File.Delete(fullPath);
            }
            catch (Exception)
            {
                return;
            }
        }

        public static void WriteLine(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(fullPath, true))
                {
                    writer.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: Couldn't write to log file. Reason: {ex.Message}");
            }
        }
    }
}
