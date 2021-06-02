using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace FileVerification
{
    class Program
    {
        public static int NumFolders { get; set; }

        static void Main(string[] args)
        {            
            FileSystemCrawlerSO fsc = new FileSystemCrawlerSO();
            Stopwatch watch = new Stopwatch();

            string docPath = @"F:\HashTest";
            watch.Start();
            fsc.CollectFolders(docPath);
            fsc.CollectFiles();
            watch.Stop();
            Logger.WriteLine("--------------------------------------------------------------------------------");
            Logger.WriteLine($"Folders:   {fsc.NumFolders}");
            Logger.WriteLine($"Files:     {fsc.NumFiles}");
            Logger.WriteLine($"Time (ms): { watch.ElapsedMilliseconds}");
            Logger.WriteLine("--------------------------------------------------------------------------------");
        }
    }
}
