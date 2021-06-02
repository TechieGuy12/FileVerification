using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace FileVerification
{
    class Program
    {
        public static int NumFolders { get; set; }

        static int Main(string[] args)
        {
            RootCommand rootCommand = new RootCommand(
              description: "Generates the hash of all files in a folder tree and stores the hashes in text files in each folder.");

            var folderOption = new Option<string>(
                    aliases: new string[] { "--folder", "-f" }, 
                    description: "The folder containing the files to verify with a hash."
                );
            folderOption.IsRequired = true;
            rootCommand.AddOption(folderOption);
            rootCommand.Handler = CommandHandler.Create<string>(Verify);
            return rootCommand.Invoke(args);
        }

        static void Verify(string folder)
        {
            FileSystemCrawlerSO fsc = new FileSystemCrawlerSO();
            Stopwatch watch = new Stopwatch();

            //string docPath = @"F:\HashTest";
            watch.Start();
            fsc.CollectFolders(folder);
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
