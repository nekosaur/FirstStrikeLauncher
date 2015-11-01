using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Ionic.Zip;

namespace ModPacker
{
    static class Packer
    {
        private static List<string> _zipNames = new List<string>();

        private static Dictionary<string, ZipFile> _zipFiles = new Dictionary<string, ZipFile>();

        private static int _entriesTotal = 0;
        private static int _entriesSaved = 0;

        static public int Run(string baseFolderPath, string modFolderName)
        {
            string fullFolderPath = Path.Combine(baseFolderPath, modFolderName);

            if (!Directory.Exists(fullFolderPath))
                return 0;

            string[] files = Directory.GetFiles(fullFolderPath, "*.*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string path = file.Replace("\\", "/");

                if (!ModRegex.IsZipFile.IsMatch(path) || path.EndsWith("server.zip") || path.EndsWith("client.zip"))
                    continue;

                Match match = ModRegex.IsZipFile.Match(path);

                string zipName = match.Groups[1].Captures[0].Value.ToLower();

                string zipSuffix = "";

                string zipFilePath = match.Groups[2].Captures[0].Value.ToLower();

                if (zipName.Contains("levels/"))
                    zipSuffix = @"\";
                else
                    zipSuffix = @"_";

                if (!_zipFiles.ContainsKey(zipName + zipSuffix + "server.zip"))
                {
                    //create new zip (delete existing ones)
                    try
                    {
                        File.Delete(Path.Combine(baseFolderPath, zipName + zipSuffix + "server.zip"));
                        File.Delete(Path.Combine(baseFolderPath, zipName + zipSuffix + "client.zip"));
                    }
                    catch (FileNotFoundException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                  
                    _zipFiles.Add(zipName + zipSuffix + "server.zip", new ZipFile(Path.Combine(baseFolderPath, zipName + zipSuffix + "server.zip")));
                    _zipFiles.Add(zipName + zipSuffix + "client.zip", new ZipFile(Path.Combine(baseFolderPath, zipName + zipSuffix + "client.zip")));


                    if (ModRegex.IsServerFile.IsMatch(path))
                    {
                        _zipFiles[zipName + zipSuffix + "server.zip"].AddFile(file, Path.GetDirectoryName(zipFilePath).ToLower());
                    }
                    else
                    {
                        _zipFiles[zipName + zipSuffix + "client.zip"].AddFile(file, Path.GetDirectoryName(zipFilePath).ToLower());
                    }
                }
                else
                {
                    //add to existing zip
                    if (ModRegex.IsServerFile.IsMatch(path))
                    {
                        _zipFiles[zipName + zipSuffix + "server.zip"].AddFile(file, Path.GetDirectoryName(zipFilePath));
                    }
                    else
                    {
                        _zipFiles[zipName + zipSuffix + "client.zip"].AddFile(file, Path.GetDirectoryName(zipFilePath));
                    }

                }
            }

            foreach (KeyValuePair<string, ZipFile> kvp in _zipFiles)
            {
                if (kvp.Value.Count > 0)
                {
                    _entriesTotal = 0;
                    _entriesSaved = 0;

                    kvp.Value.SaveProgress += HandleZipSaveProgress;

                    Console.WriteLine("Saving file {0}:", kvp.Key);
                    kvp.Value.Save();
                }

                kvp.Value.Dispose();

            }

            Console.WriteLine("Deleting files and folders...");

            foreach (string file in files)
            {
                string path = file.Replace("\\", "/");
                if (ModRegex.IsZipFile.IsMatch(path))
                    File.Delete(file);
            }

            RemoveEmptyFolders(Path.Combine(baseFolderPath, modFolderName));

            Console.WriteLine("  Done!");

            return -1;
        }

        private static void RemoveEmptyFolders(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                RemoveEmptyFolders(directory);
                if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }


        static void HandleZipSaveProgress(object sender, SaveProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Saving_Started)
            {
                //Console.WriteLine("Saving file: {0}", e.ArchiveName);
                _entriesTotal = e.EntriesTotal;
            }
            else if (e.EventType == ZipProgressEventType.Saving_BeforeWriteEntry)
            {
                _entriesSaved++;
                _entriesTotal = e.EntriesTotal;
            }
            else if (e.EventType == ZipProgressEventType.Saving_EntryBytesRead)
            {
                double percent = (int)(_entriesSaved / (0.01 * _entriesTotal));
                double entrypercent = e.BytesTransferred / (0.01 * e.TotalBytesToTransfer);

                string entry = string.Format("{0:00}%", entrypercent);

                string spaces = "";
                for (int i = 0; i <= 4 - entry.Length; i++)
                {
                    spaces += " ";
                }

                Console.Write("{0}{1}({2:00}%)\r", entry, spaces, percent);
            }
            else if (e.EventType == ZipProgressEventType.Saving_BeforeWriteEntry)
            {
                //Console.WriteLine("{0} ({1}/{2})", e.CurrentEntry.FileName, e.EntriesSaved + 1, e.EntriesTotal);


            }
            else if (e.EventType == ZipProgressEventType.Saving_Completed)
            {
                Console.WriteLine("  Done!         ");
            }
        }		
    }
}
