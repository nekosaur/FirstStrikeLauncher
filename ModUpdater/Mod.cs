using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.ComponentModel;
using Ionic.Zip;
using System.Net;

namespace BF2Utils
{
	public class Mod
	{
		public int CurrentVersion
		{
			get { return _currentVersion; }
			set { _currentVersion = value; }
		}
		private int _currentVersion = 0;
		
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}
		private string _name = "";
		
		public string ParentPath
		{
			get { return _parentPath; }
			set { _parentPath = value; }
		}
		private string _parentPath = "";
		
		public string ModPath
		{
			get { return Path.Combine(_parentPath, _name); }
		}
		
		public string UpdatesPath
		{
			get { return _updatesPath; }
			set { _updatesPath = value; }
		}
		private string _updatesPath = "";
		
		public string UpdatesUrl
		{
			get { return _updatesUrl; }
			set { _updatesUrl = value; }
		}
		private string _updatesUrl;
		
		public string TempPath
		{
			get { return Path.Combine(_parentPath, "temp"); }
		}
		
		public bool IsLocalUpdate
		{
			get { return _isLocalUpdate; }
			set { _isLocalUpdate = value; }
		}
		private bool _isLocalUpdate;
		
		public bool SimpleProgress
		{
			get { return _simpleProgress; }
			set { _simpleProgress = value; }
		}
		private bool _simpleProgress;
		
		public Mod (string name, string parentFolderPath, string updatesUrl)
		{
			_name = name;
			_parentPath = parentFolderPath;		
			_updatesUrl = updatesUrl;
			_currentVersion = GetCurrentVersion();
            _isLocalUpdate = false;
		}
		
		public int GetCurrentVersion()
		{
			// TODO: Add error handling
			int version = 0;

            try
            {
                using (TextReader reader = new StreamReader(Path.Combine(ModPath, "version.txt")))
                {
                    if (reader == null)
                        return -1;

                    version = int.Parse(reader.ReadLine());
                }
            }
            catch (Exception ex)
            {
                if (ex is DirectoryNotFoundException || ex is FileNotFoundException)
                {
                    return -1;
                }
                else
                {
                    throw;
                }
            }

			
			return version;
		}
		
		public void SetCurrentVersion(string update)
		{
			using (TextWriter writer = new StreamWriter(Path.Combine(ModPath, "version.txt")))
			{
				writer.WriteLine(update);
			}
		}

        private string[] GetLocalUpdates()
        {
            Console.WriteLine("Getting local updates...");

            List<string> updates = new List<string>();

            // Parse file
            try
            {
                using (TextReader reader = File.OpenText(Path.Combine(ParentPath, "updatelist.txt")))
                {
                    string line = "";

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (int.Parse(line) > CurrentVersion)
                        {
                            updates.Add(line);
                        }
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                throw ex;
            }

            // Return strings
            return updates.ToArray();
        }

		private string[] GetNewUpdates()
		{	
			Console.WriteLine("Retrieving updatelist...");
			
			if (!IsLocalUpdate)
			{
				if (!Downloader.Get(UpdatesUrl + "updatelist.txt", Path.Combine(ParentPath, "updatelist.txt"), true))
				{
					return null;
				}
			}

			List<string> updates = new List<string>();
			
			try
			{				
				using (TextReader reader = File.OpenText(Path.Combine(ParentPath, "updatelist.txt")))
				{
					string line = "";
                    
					while ((line = reader.ReadLine()) != null)
					{
                        int version;

                        if (int.TryParse(line, out version))
                        {
                            if (version > CurrentVersion)
                                updates.Add(line);
                        }
                        else
                        {
                            Console.WriteLine("Problem reading line from updatelist: {0}", line);
                            return null;
                        }
					}
				}
			}
			catch (FileNotFoundException ex)
			{
				throw ex;
			}
			
			// Return strings
			return updates.ToArray();
		}
		

		public int Update()
		{
            string[] updates = IsLocalUpdate ? GetLocalUpdates() : GetNewUpdates();
			
			if (updates == null)
			{
				Console.WriteLine("Update failed! Could not retrieve a proper updatelist from server. \nIf you have several mirrors configured, launcher will automatically try the next one.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
				return 2;
			}
			else if (updates.Length <= 0)
			{
				Console.WriteLine("No new updates found.");
				
				return 0;
			}
			else
			{
                Console.WriteLine("Getting updates...");

				if (updates.Length > 1)
				{
					string output = "";
					foreach (string update in updates)
						output += update + ", ";
					
					Console.WriteLine("Found updates: {0}", output);
				}
				else
					Console.WriteLine("Found update: {0}", updates[0]);
			}
			
			if (IsLocalUpdate)
			{
				Console.WriteLine("Running local update. Assuming all files are present.");
			}
			else
			{
				foreach (string update in updates)
				{
					string fileName = "fsbuild_" + update + ".zip";
					if (File.Exists(Path.Combine(ParentPath, fileName)))
					{
						// TODO: Add file size check
						Console.WriteLine("File already exists, skipping");
						continue;
					}
					
					Downloader.Get(UpdatesUrl + fileName, Path.Combine(ParentPath, fileName), true);

                    try
                    {
                        using (ZipFile zip = ZipFile.Read(Path.Combine(ParentPath, fileName)))
                        {
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogInfo(ex);
                        Console.WriteLine("Downloaded file was corrupt. Try running update again.");
                        File.Delete(Path.Combine(ParentPath, fileName));
                        return 1;
                    }
				}
			}
			
			DateTime start = DateTime.Now;

		    try
		    {
                Updater.Process(this, updates);
		    }
		    catch (Exception ex)
		    {
                Console.WriteLine(ex.Message);
		        return 1;
		    }
			
			
			SetCurrentVersion(updates[updates.Length - 1]);
			
			DateTime end = DateTime.Now;
			
			TimeSpan time = end - start;
			
			
			Console.WriteLine("Updater took {0} minutes and {1} seconds to run", time.Minutes, time.Seconds);

		    return 0;

		}
		
	}
}

