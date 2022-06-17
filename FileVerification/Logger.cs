using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TE.FileVerification
{
    static class Logger
    {
        // The default name for the log file
        const string DEFAULT_NAME = "fv.log";      

        // Full path to the log file
        static string fullPath;

        // The lines that have been logged
        static StringBuilder _lines;

        /// <summary>
        /// Gets the lines that have been logged.
        /// </summary>
        public static string Lines
        {
            get
            {
                return _lines != null ? _lines.ToString() : null;
            }
        }

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
                if (_lines != null)
                {
                    _lines.Clear();
                }
                else
                {
                    _lines = new StringBuilder();
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        public static void WriteLine(string message)
        {

            Console.WriteLine(message);

            try
            {
                using (StreamWriter writer = new StreamWriter(fullPath, true))
                {
                    writer.WriteLine(message);
                }

                _lines.AppendLine(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: Couldn't write to log file. Reason: {ex.Message}");
            }
        }
    }
}
