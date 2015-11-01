using System;
using System.Collections.Generic;
using System.IO;
using Ionic.Zip;
using System.Text.RegularExpressions;

namespace BF2Utils
{
	public static class Updater
	{
		private static Mod _mod;
		private static Dictionary<ZipInfo, List<ZipCall>> _zipLists = new Dictionary<ZipInfo, List<ZipCall>>();
		
		private static int _entriesTotal;
		private static int _entriesExtracted;
		private static int _entriesSaved;
		
		private static List<string> _removeList = new List<string>();
		
		private static Regex _zipRegex = new Regex(@"(firststrike/(?:common|sound|menu|objects/[a-z0-9_]*|levels/[a-z0-9_]*))/((?!info)[a-z0-9-_/ ()=\.]{0,})", RegexOptions.Compiled|RegexOptions.IgnoreCase);
		private static Regex _serverRegex = new Regex(@"\.(con|tweak|ske|baf|inc|collisionmesh|tai|emi|dat|cfg|ahm|qtr|ai|mat|clb|(?<!terraindata\.)raw)", RegexOptions.Compiled|RegexOptions.IgnoreCase);
        private static Regex _macRegex = new Regex(@"\._\.ds_store|\.ds_store", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex _svnRegex = new Regex(@"svn(\/|\\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		
		public static void Process(Mod mod, string[] updates)
		{
			_mod = mod;

            CleanTempFiles();
			
			foreach (string update in updates)
			{
				string fileName = "fsbuild_" + update + ".zip";
				
                try
                {   
                    ProcessUpdate(fileName);
                }
                catch (FileNotFoundException fnfex)
                {
                    Console.WriteLine(" ERROR: {0}", fnfex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.LogInfo(ex);
                    throw;
                }
				
			}
			
            // Saving zip files with game data. _zipLists contains all relevant data, gathered from ProcessUpdate()
			foreach (KeyValuePair<ZipInfo, List<ZipCall>> kvp in _zipLists)
			{
				Console.WriteLine("Applying updates to {0}", kvp.Key.FullPath);
				
                // If zip does not exist, create it
				if (!File.Exists(Path.Combine(_mod.ParentPath, kvp.Key.FullPath)))
				{
					Console.WriteLine("Zip does not yet exist, creating...");
					if (!Directory.Exists(Path.Combine(_mod.ParentPath, kvp.Key.FolderPath)))
						Directory.CreateDirectory(Path.Combine(_mod.ParentPath, kvp.Key.FolderPath));
						
					using (ZipFile zip = new ZipFile())
					{
						zip.Save(Path.Combine(_mod.ParentPath, kvp.Key.FullPath));
						Console.WriteLine("Created {0}", kvp.Key.FullPath);
					}
				}
				
			    // Saving files to zip, showing progress
				using (ZipFile zip = ZipFile.Read(Path.Combine(_mod.ParentPath, kvp.Key.FullPath)))
				{
					zip.ParallelDeflateThreshold = -1;
					zip.SaveProgress += HandleZipSaveProgress;
					
					_entriesSaved = 0;
					_entriesTotal = 0;
					
					foreach (ZipCall call in kvp.Value)
					{
						switch (call.Type)
						{
							case CallType.Remove:
								Console.WriteLine("Trying to remove {0}", call.ZipPath);
								// If folder, select all entries in it and remove	
								if (!call.ZipPath.Contains("."))
								{
									zip.RemoveSelectedEntries("name = "+call.ZipPath.Replace("/", "\\")+"\\*.*");
								}
								else
								{
									if (zip[call.ZipPath] != null)
									{
										zip.RemoveEntry(call.ZipPath);
									}
								}
								break;
							case CallType.Add:
								string localPath = Path.Combine(_mod.TempPath, Path.Combine(kvp.Key.FolderPath, call.ZipPath));
								string zipPath = Path.GetDirectoryName(call.ZipPath);
								zip.UpdateFile(localPath, zipPath.ToLower());
								zip[call.ZipPath].LastModified = new DateTime(1999, 11, 30, 0, 0, 0, 0);
								break;
						}
					}
					
					zip.Save();
					
				}
			}
            
            CleanTempFiles();
		}

        private static void CleanTempFiles()
        {
            // Delete temp files (make sure it's really temp)
            if (Directory.Exists(_mod.TempPath) && _mod.TempPath.EndsWith("temp"))
            {
                Console.WriteLine("Deleting temporary files...");
                try
                {
                    Directory.Delete(_mod.TempPath, true);
                    Console.WriteLine("  Done!");
                }
                catch (IOException)
                {
                    Console.WriteLine("  NOTICE: Temporary files are still locked. Delete manually.");
                }
            }
        }

		private static void HandleZipSaveProgress (object sender, SaveProgressEventArgs e)
		{
			if (e.EventType == ZipProgressEventType.Saving_Started)
			{
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
				
				if (_mod.SimpleProgress)
					Console.Write("{0:00}%\r", percent);
				else
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
		
		private static void ProcessUpdate(string filePath)
		{
			using (ZipFile zip = ZipFile.Read(Path.Combine(_mod.ParentPath, filePath)))
			{
				zip.BufferSize = 65536;
				zip.CodecBufferSize = 65536;
				zip.ExtractProgress += HandleZipExtractProgress;
				
				Console.WriteLine("Processing update {0}", filePath);
				
				_entriesTotal = zip.Entries.Count;
				_entriesExtracted = 0;
				
				foreach (ZipEntry file in zip)
				{
					string fileName = file.FileName.ToLower();
					
					if (file.IsDirectory)
						continue;
										
					// Check for removelist and process it
					if (fileName.Contains("removelist.txt"))
					{
						ProcessRemoveList(file);
						continue;
					}

                    //ignore some files
                    if (_svnRegex.IsMatch(fileName) || _macRegex.IsMatch(fileName))
                        continue;
					
					// If we don't have a match, and it's not a folder, we're dealing with a loose file. Just extract
					if (!_zipRegex.IsMatch(fileName) && fileName.Contains("."))
					{
						Console.WriteLine("Found loose file ({0}), extracting...", fileName);
						file.Extract(_mod.ParentPath, ExtractExistingFileAction.OverwriteSilently);
						continue;
					}
						
				
					Match match = _zipRegex.Match(fileName);
					
					string fileSuffix = "";
					string folderPath = "";
					string zipPath = "";
					
					if (fileName.Contains("levels/"))
						fileSuffix = "/";
					else
						fileSuffix = "_";
					
					folderPath = match.Groups[1].Captures[0].Value.ToLower();
					zipPath = match.Groups[2].Captures[0].Value.ToLower();
					
					if (_serverRegex.IsMatch(zipPath))
					{
						AddZipCall(new ZipInfo(folderPath, fileSuffix + "server.zip"), zipPath, CallType.Add);
						
						file.Extract(_mod.TempPath, ExtractExistingFileAction.OverwriteSilently);
					}
					else
					{
						AddZipCall(new ZipInfo(folderPath, fileSuffix + "client.zip"), zipPath, CallType.Add);
						
						file.Extract(_mod.TempPath, ExtractExistingFileAction.OverwriteSilently);
					}
					
				}
			}
			
			Console.WriteLine("  Done!             ");
		}

		private static void HandleZipExtractProgress (object sender, ExtractProgressEventArgs e)
		{
			if (e.EventType == ZipProgressEventType.Extracting_BeforeExtractEntry)
			{
				_entriesExtracted++;
				
			} else if (e.EventType == ZipProgressEventType.Extracting_EntryBytesWritten)
			{
				double percent = _entriesExtracted / (0.01 * _entriesTotal);
				
				double entrypercent = e.BytesTransferred / (0.01 * e.TotalBytesToTransfer);
				
				string entry = string.Format("{0:00}%", entrypercent);
				
				string spaces = "";
				for (int i = 0; i <= 4 - entry.Length; i++)
				{
					spaces += " ";
				}
				
				if (_mod.SimpleProgress)
					Console.Write("{0:00}%)\r", percent);
				else
					Console.Write("{0}{1}({2:00}%)\r", entry, spaces, percent);
			}
		}
		
		private static void AddZipCall(ZipInfo info, string zipPath, CallType type)
		{
			if (_zipLists.ContainsKey(info))
			{
				_zipLists[info].Add(new ZipCall(zipPath, type));
			}
			else
			{
				_zipLists.Add(info, new List<ZipCall>());
				
				_zipLists[info].Add(new ZipCall(zipPath, type));
			}
		}
					
		private static void ProcessRemoveList(ZipEntry file)
		{
			MemoryStream ms = new MemoryStream();
			file.Extract(ms);
			
			ms.Seek(0, SeekOrigin.Begin);
			
			StreamReader reader = new StreamReader(ms);
			
			string line = "";
			
			while ((line = reader.ReadLine()) != null)
			{
				if (string.IsNullOrEmpty(line))
					continue;
				
				line = line.Replace("\\", "/");
				
				string folderPath = "";
				string fileSuffix = "";
				string zipPath = "";
					
				if (line.Contains("levels/"))
					fileSuffix = "/";
				else
					fileSuffix = "_";
				
				// We're removing a (top) folder
				if (!_zipRegex.IsMatch(line))
				{
				    folderPath = line;

				    // Make sure we're not deleting the root mod folder
				    if (folderPath.Length <= 0)
				        continue;

				    // Try removing folder from the list, if it's been previously added by an update.
				    try
				    {

				        _zipLists.Remove(new ZipInfo(folderPath, fileSuffix + "server.zip"));
				        _zipLists.Remove(new ZipInfo(folderPath, fileSuffix + "client.zip"));
				        Console.WriteLine("Removed {0} from zip list", folderPath);
				    }
				    catch (IndexOutOfRangeException ex)
				    {
                        Console.WriteLine(" NOTICE: {0}", ex.Message);
				    }

				    // Try removing folder/file from mod, if it's already in there.
				    try
				    {
				        string path = Path.Combine(_mod.ParentPath, folderPath.Replace("/", "\\"));

				        FileAttributes attr = File.GetAttributes(path);

				        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
				        {
				            Directory.Delete(path, true);
				        }
				        else
				        {
				            File.Delete(path);
				        }

				        Console.WriteLine("Removed {0} from mod", folderPath);
				    }
				    catch (DirectoryNotFoundException ex)
				    {
                        Console.WriteLine(" NOTICE: {0}", ex.Message);
				    }
				    catch (FileNotFoundException ex)
				    {
                        Console.WriteLine(" NOTICE: {0}", ex.Message);
				    }

			        continue;
				}
				else
				{
					Match match = _zipRegex.Match(line);
					
					folderPath = match.Groups[1].Captures[0].Value.ToLower();
					zipPath = match.Groups[2].Captures[0].Value.ToLower();
					
					// If it's a directory we need to delete it from both server and client files
					// TODO: Better way than checking for dot? What if filename has dot? (this.object.con)
					if (!zipPath.Contains("."))
					{
						AddZipCall(new ZipInfo(folderPath, fileSuffix + "server.zip"), zipPath, CallType.Remove);
						AddZipCall(new ZipInfo(folderPath, fileSuffix + "client.zip"), zipPath, CallType.Remove);
						
						continue;
					}
					
					if (_serverRegex.IsMatch(zipPath))
					{
						AddZipCall(new ZipInfo(folderPath, fileSuffix + "server.zip"), zipPath, CallType.Remove);
					}
					else
					{
						AddZipCall(new ZipInfo(folderPath, fileSuffix + "client.zip"), zipPath, CallType.Remove);
					}
				}
			}
				
		}
		
	}
}