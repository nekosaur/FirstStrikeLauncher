using System;
using System.IO;

namespace BF2Utils
{
    enum ExitCode : int
    {
        Success = 0,
        Error = 1
    }

	class MainClass
	{
		public static bool silentUpdate;
		public static bool localUpdate;
	    private static ExitCode _exitCode = ExitCode.Error;
		
		public static int Main (string[] args)
		{
            // Delete previous log file
		    try
		    {
		        File.Delete("errors.log");
		    }
		    catch (Exception)
		    {
		    
		    }
            
			MainClass mc = new MainClass();
			
			if (args.Length < 3)
			{
				Console.WriteLine("usage: ModUpdater.exe <options> mod_name mods_folder_path updates_url");
			    return (int) _exitCode;
			}
			
			int optionsOffset = 0;
			
			foreach (string argument in args)
			{
				if (argument.StartsWith("-"))
				{
					SetOption(argument);
					optionsOffset++;
				}
				
			}
			
			Console.Write("Running update on {0} located in {1} with updates from {2}\n", args[optionsOffset], args[optionsOffset+1], args[optionsOffset+2]);

		    
            mc.Run(args[optionsOffset], args[optionsOffset + 1], args[optionsOffset + 2]);

		    if (!silentUpdate)
            {
                if (_exitCode != 0)
                    Console.WriteLine("\n\nSomething went wrong during the update.");

                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }

		    return (int) _exitCode;

		}
		
		private static void SetOption(string option)
		{
			if (option == "-silentUpdate")
				silentUpdate = true;
			else if (option == "-localUpdate")
				localUpdate = true;
		}
		
		public void Run(string modName, string parentPath, string updatesUrl)
		{
			BF2Utils.Mod mod = new BF2Utils.Mod(modName, parentPath, updatesUrl);

            if (mod.CurrentVersion < 0)
            {
                Console.WriteLine("Exiting early. Could not read current mod version");
                _exitCode = (ExitCode)1;
                return;
            }
			
			if (localUpdate)
				mod.IsLocalUpdate = true;
			
			_exitCode = (ExitCode)mod.Update();
			
		}
	}
}

