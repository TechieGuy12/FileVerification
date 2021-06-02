using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;


namespace FileVerification
{
    enum VerifyFileLayout
    {
        NAME,
        HASH_ALGORITHM,
        HASH
    }

    public class FileSystemCrawlerSO
    {
        const string VERIFY_FILE_NAME = "__fv.txt";

        public int NumFolders { get; set; }

        public int NumFiles { get; set; }

        public string FolderPath { get; set; }

        private List<DirectoryInfo> directories = new List<DirectoryInfo>();

        private readonly ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
        private readonly ConcurrentBag<Task> fileTasks = new ConcurrentBag<Task>();

        public void CollectFolders(string path)
        {
            FolderPath = path;
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            tasks.Add(Task.Run(() => CrawlFolder(directoryInfo)));

            Task taskToWaitFor;
            while (tasks.TryTake(out taskToWaitFor))
            {
                NumFolders++;
                taskToWaitFor.Wait();
            }
        }

        public void CollectFiles()
        {            
            foreach (var dir in directories)
            {                
                fileTasks.Add(Task.Run(() => GetFiles(dir)));
            }

            Task taskToWaitFor;
            while (fileTasks.TryTake(out taskToWaitFor))
            {
                taskToWaitFor.Wait();
            }

        }

        private void CrawlFolder(DirectoryInfo dir)
        {
            try
            {
                DirectoryInfo[] directoryInfos = dir.GetDirectories();
                foreach (DirectoryInfo childInfo in directoryInfos)
                {
                    // here may be dragons using enumeration variable as closure!!
                    DirectoryInfo di = childInfo;
                    tasks.Add(Task.Run(() => CrawlFolder(di)));
                }
                directories.Add(dir);                
            }
            catch (Exception ex)
                when (ex is DirectoryNotFoundException || ex is System.Security.SecurityException || ex is UnauthorizedAccessException)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }

        private void GetFiles(DirectoryInfo dir)
        {
            FileInfo[] files = dir.GetFiles();
            VerifyFile verifyFile = new VerifyFile(VERIFY_FILE_NAME, dir);

            // Read the verify file, if it exists, but if the read method
            // returns null, indicating an exception, and the verify file file
            // exists, then assume there is an issue and don't continue with
            // the hashing and verification
            Dictionary<string, HashInfo> filesData = verifyFile.Read();
            if (filesData == null && verifyFile.Exists())
            {
                return;
            }

            foreach (var file in files)
            {
                if (file.Name.Equals(VERIFY_FILE_NAME))
                {
                    continue;
                }

                NumFiles++;
                if (filesData.TryGetValue(file.Name, out HashInfo fileHashInfo))
                {
                    if (!fileHashInfo.IsEqual(file.FullName))
                    {
                        Console.WriteLine($"Hash mismatch: {file.FullName}.");
                    }
                }
                else
                {
                    filesData.Add(file.Name, new HashInfo(file, Algorithm.SHA256));
                }
            }

            if (filesData.Count > 0)
            {
                verifyFile.Write(filesData, dir);
            }
        }        
    }
}
