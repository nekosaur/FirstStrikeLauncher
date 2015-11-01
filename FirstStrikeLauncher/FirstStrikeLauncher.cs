using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using FirstStrikeLauncher.Properties;
using Microsoft.Win32;
using NAppUpdate.Framework;
using Vestris.ResourceLib;
using Ionic.Zip;
using System.Security.Principal;
using System.Security.Permissions;

namespace FirstStrikeLauncher
{

    public partial class FirstStrikeLauncher : Form
    {
        private Dictionary<string, string> _args;
        private Config _config;
        private string _configName = "config.xml";
        private string _appDataFolder = "";
        private bool _stayMinimized = true;
        private bool _browserIsLoaded = false;
        private int _currentMirror = 0;

        public FirstStrikeLauncher()
        {
            InitializeComponent();
        }

        private void FirstStrikeLauncherLoad(object sender, EventArgs e)
        {
            this.Icon = Resources.FirstStrikeIcon;

            // The folder for the roaming current user 
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // Combine the base folder with your specific folder....
            _appDataFolder = Path.Combine(appData, "FirstStrikeLauncher");

            if (File.Exists(Path.Combine(_appDataFolder, _configName)) && !Utils.HaveWritePermissionsForFileOrFolder(Path.Combine(_appDataFolder, _configName), Utils.GetCurrentSID()))
            {
                MessageBox.Show("A recent update to the launcher has required that the old config file be removed. A new one will now be generated.\n\nMake sure to set your options in the Preferences menu again. We apologize for the inconvenience.\n\nMake sure to click Yes if you get a UAC prompt!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ProcessStartInfo procStartInfo = new ProcessStartInfo()
                {
                    //RedirectStandardError = true,
                    //RedirectStandardOutput = true,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = "cmd.exe",
                    Arguments = "/C del /Q \"" + Path.Combine(_appDataFolder, _configName) + "\"",
                    //Verb = "runas"
                };

                using (Process proc = new Process())
                {
                    proc.StartInfo = procStartInfo;
                    try
                    {
                        proc.Start();
                    }
                    catch (Exception ex)
                    {
                        Log.LogInfo(ex);
                        throw;
                    }
                }

            }
            

            // Check if folder exists and if not, create it
            if (!Directory.Exists(_appDataFolder))
                Directory.CreateDirectory(_appDataFolder);

            Log.SetOutputFolder(_appDataFolder);

            CheckBattlefieldVersion();

            InitializeApplication();

            LoadTabControl();

        }

        private void CheckBattlefieldVersion()
        {
            string version = GetRegistryKey("SOFTWARE\\Electronic Arts\\EA Games\\Battlefield 2142", "Version");

            if (version == null)
            {
                DialogResult result = MessageBox.Show(this,
                                "It seems like you do not currently have Battlefield 2142 1.51 installed. Is this correct?\nClicking Yes will close the launcher and allow you to install the latest version of Battlefield 2142.",
                                "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error);

                if (result == DialogResult.Yes)
                    Application.Exit();
                else
                    return;
            }
            else if (float.Parse(version.Replace('.', ',')) < 1.51f)
            {
                MessageBox.Show(this,
                                string.Format("You need version 1.51 of Battlefield 2142 to be able to play FirstStrike.\nYour current version is {0}", version),
                                "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                Application.Exit();
            }
        }

        private void LoadTabControl()
        {
            BackgroundWorker worker = new BackgroundWorker();

            worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
                {
                    if (e.Error != null)
                        MessageBox.Show(this, e.Error.ToString(), "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                };
            worker.DoWork += delegate(object s, DoWorkEventArgs e)
                {
                    // TODO: Is there a better way to distinguish between pub and tester?
                    if (_config.UpdatesUrl.Contains("pub"))
                        treeViewChangelog.Nodes.AddRange(Changelog.GetPublicLog(_config.UpdatesUrl + "changelog.txt"));
                    else
                        treeViewChangelog.Nodes.AddRange(Changelog.GetTesterLog(100, 10));
                };

            worker.RunWorkerAsync();
        }

        private void InitializeApplication()
        {
            // Setup systray icon
            systrayIcon.Icon = Resources.FirstStrikeIcon;
            systrayIcon.Visible = false;

            ContextMenuStrip strip = new ContextMenuStrip();
            strip.Items.Add("Launch", null, new EventHandler(OnSystrayStripLaunchClick));
            strip.Items.Add("-");
            strip.Items.Add("Open", null, new EventHandler(OnSystrayStripOpenClick));
            strip.Items.Add("Exit", null, new EventHandler(OnSystrayStripExitClick));
            systrayIcon.ContextMenuStrip = strip;

            GetArguments();

            // Skip this for now, UAC bullshit is too much
            /*
            // If config file doesn't exist, try to do a cleanup
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), _configName)))
            {
                CleanupOldFiles();
            }
            */

            LoadConfig();
            SanityCheck();

            cbNoUpdate.Checked = !_config.UpdateBeforeLaunch;

            this.BringToFront();

            UpdateManager manager = UpdateManager.Instance;

            manager.UpdateFeedReader = new NAppUpdate.Framework.FeedReaders.NauXmlFeedReader();
            manager.UpdateSource =
                new NAppUpdate.Framework.Sources.SimpleWebSource(_config.ApplicationUpdatesFeed);
            manager.Config.TempFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FirstStrikeLauncher\\Updates");
            manager.Config.BackupFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FirstStrikeLauncher\\Backup");

            manager.ReinstateIfRestarted();

            CheckForAppUpdates();
            
        }

        /// <summary>
        /// Checks if everything is in place for launcher to work properly
        /// </summary>
        private void SanityCheck()
        {
            if (!Directory.Exists(Path.Combine(_config.ParentPath, _config.Name))) 
            {
                MessageBox.Show(String.Format("Could not find mod {0} in {1}. Application will now exit.", _config.Name, _config.ParentPath), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            // Adding check for widescreen argument here. Best place for now
            if (_config.Arguments.ContainsKey("+szx") && !_config.Arguments.ContainsKey("+widescreen"))
            {
                int width = int.Parse(_config.Arguments["+szx"]);
                int height = int.Parse(_config.Arguments["+szy"]);

                float aspect = width / height;

                if (aspect < 1.3f || aspect > 1.4f)
                {
                    _config.Arguments["+widescreen"] = "1";

                    _config.Serialize(Path.Combine(_appDataFolder, _configName));
                }
            }

        }

        private void CleanupOldFiles()
        {
            string[] files = Resources.OldLauncherFiles.Split(new string[] {"\r\n"}, StringSplitOptions.None);

            foreach (string name in files)
            {
                try
                {
                    if (!name.Contains("."))
                    {
                        Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), name), true);
                    }
                    else
                    {
                        File.Delete(Path.Combine(Directory.GetCurrentDirectory(), name));
                    }

                }
                catch (FileNotFoundException)
                {
                }
                catch (DirectoryNotFoundException)
                {
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                    Log.LogInfo(ex);
                    throw;
                }
            }

        }

        private void OnWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            if (!CheckServerStatus("http://www.fsmod.com/"))
                return;

            try
            {
                RSS rss = new RSS("http://www.fsmod.com/bothanspy/news.xml", 5);
                e.Result = rss.GetHtml();
            }
            catch (Exception)
            {
                e.Result = "<html><head></head><body>There was a problem getting the news.</body></htmL>";
            }
        }

        private void OnWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null) MessageBox.Show(e.Error.ToString());

            webBrowserNews.DocumentText = (string) e.Result;
        }

        private void OnSystrayStripOpenClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void OnSystrayStripExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void OnSystrayStripLaunchClick(object sender, EventArgs e)
        {
            StartGame();
        }

        private string GetRegistryKey(string path, string value)
        {
            RegistryKey start = Registry.LocalMachine;

            RegistryKey key = start.OpenSubKey(path);

            if (key != null)
            {
                return (string)key.GetValue(value);
            }
            else
                return null;
        }

        /// <summary>
        /// Populates _args with any and all supplied command line arguments
        /// </summary>
        private void GetArguments()
        {
            _args = new Dictionary<string, string>();

            string[] args = Environment.GetCommandLineArgs();

            foreach (string arg in args)
            {
                if (arg.EndsWith(".exe"))
                    continue;

                _args.Add(arg.Split('=')[0].Replace("--", ""), arg.Split('=')[1]);
            }
        }

        /// <summary>
        /// Loads the config, either supplied through command line argument, the default config.xml, or creates a new one if none exists
        /// </summary>
        private void LoadConfig()
        {
            try
            {
                if (_args.ContainsKey("config") && _args["config"] != null)
                    _configName = _args["config"];

                _config = Config.Deserialize(Path.Combine(_appDataFolder, _configName));
            }
            catch (Exception)
            {
                _config = CreateDefaultConfig();

                try
                {
                    _config.Serialize(Path.Combine(_appDataFolder, _configName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There's a problem creating a new config file.\n\n" + ex.Message + "\n\nShutting down application.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Log.LogInfo(ex);
                    Application.Exit();
                }
            }

            if (string.IsNullOrEmpty(_config.ParentPath))
                _config.ParentPath = GetParentPath();

            if (_config == null)
                Application.Exit();
            
        }

        /// <summary>
        /// Checks for application updates
        /// </summary>
        private void CheckForAppUpdates()
        {           
            if (!CheckServerStatus(_config.ApplicationUpdatesFeed))
                return;

            UpdateManager updManager = UpdateManager.Instance;           

            if (updManager.State == UpdateManager.UpdateProcessState.AfterRestart)
            {
                Log.LogInfo("Launcher has been restarted and updated");
                updManager.CleanUp();
                return;
            }
            
            if (updManager.State != UpdateManager.UpdateProcessState.NotChecked)
            {
                MessageBox.Show("Update process has already initialized; current state: " + updManager.State.ToString());
                return;
            }

            try
            {
                // Check for updates - returns true if relevant updates are found (after processing all the tasks and conditions)
                // Throws exceptions in case of bad arguments or unexpected results
                updManager.CheckForUpdates();

                if (updManager.UpdatesAvailable > 0)
                {
                    List<string> changes = new List<string>();

                    MessageBox.Show(string.Format("Updates are available ({0} updates). They will now be installed. Please wait. Installation can take a minute or two. The launcher will automatically restart.\n\nMake sure to click Yes if you get a UAC prompt!", updManager.UpdatesAvailable), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    updManager.PrepareUpdates();

                    ApplyAppUpdates();
                }
            }
            catch (Exception ex)
            {
                updManager.CleanUp();
                Log.LogInfo("Something seems to have gone wrong while checking for application updates. Error: " + ex.Message);

                if (ex is NAppUpdateException)
                {
                    // This indicates a feed or network error
                    Log.LogInfo("Something seems to be wrong with the application update server, or your config file");
                }
                else if (ex is NullReferenceException)
                {
                    Log.LogInfo("Referenced null somewhere. " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Installs application updates
        /// </summary>
        private void ApplyAppUpdates()
        {
            UpdateManager updManager = UpdateManager.Instance;

            if (updManager.State != UpdateManager.UpdateProcessState.Prepared)
            {
                MessageBox.Show(this, "Cannot install updates at the current state, they need to be prepared first. Current state is " + updManager.State.ToString());
                return;
            }

            updManager.ApplyUpdates(true, true, false);

            if (updManager.State != UpdateManager.UpdateProcessState.AppliedSuccessfully)
            {
                MessageBox.Show(this, "Error while trying to install software updates", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Provides a FolderBrowserDialog to choose the parent path (mods folder)
        /// </summary>
        /// <returns>string value of selected folder path</returns>
        private string GetParentPath()
        {
            string path = null;

            path = GetRegistryKey("SOFTWARE\\Electronic Arts\\EA Games\\Battlefield 2142", "InstallDir");

            if (path != null)
            {
                path = Path.Combine(path, "mods");

                Log.LogInfo(string.Format("Battlefield 2142 installation found at {0}", path));

                return path;
            }

            FolderBrowserDialog fbDialog = new FolderBrowserDialog();

            fbDialog.Description = "Please select your Battlefield 2142 mods folder.";
            fbDialog.ShowNewFolderButton = false;

            DialogResult result = fbDialog.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                path = fbDialog.SelectedPath;

                if (!path.EndsWith("mods"))
                {
                    MessageBox.Show(this, "You did not select your mods folder. Try again.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    path = GetParentPath();
                }
            }
            else if (result == DialogResult.Cancel)
            {
                MessageBox.Show(this, "You need to select your Battlefield 2142 installation folder to continue.\nShutting down launcher.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            return path;
        }

        /// <summary>
        /// Creates a default config from static values
        /// </summary>
        /// <returns>A new Config object</returns>
        private Config CreateDefaultConfig()
        {
            Config config = Config.DeserializeFromString(Resources.config);

            return config;
        }

        private bool CheckServerStatus(string server)
        {
            string status = NetUtils.CheckInternetConnection(server, 2000);

            if (status != null)
            {
                Log.LogInfo(status);
                return false;
            }
            else
                return true;
        }

        /// <summary>
        /// Checks if updates are avaliable for current mod, and installs any and all found. Uses external application ModUpdater.exe
        /// </summary>
        /// <param name="closeWindowOnFinish">If true, closes external console automatically when it's done</param>
        /// <returns>Returns true if ModUpdater exits gracefully</returns>
        private bool CheckForUpdates(bool closeWindowOnFinish)
        {
            MessageBox.Show("The launcher will now check for updates.\n\nMake sure to click Yes if you get a UAC prompt", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            List<string> options = new List<string>();

            foreach (KeyValuePair<string, bool> kvp in _config.Options)
            {
                if (kvp.Value)
                    options.Add(kvp.Key);
            }

            if (closeWindowOnFinish)
                options.Add("-silentUpdate");

            string arguments = string.Join(" ", options.ToArray()) + " " + _config.Name + " \"" + _config.ParentPath + "\" " + _config.UpdatesUrl[_currentMirror];

            Process updateProcess = new Process();

            updateProcess.StartInfo.FileName = "ModUpdater.exe";
            updateProcess.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            updateProcess.StartInfo.Arguments = arguments;
            //updateProcess.StartInfo.Verb = "runas";
            updateProcess.StartInfo.UseShellExecute = true;

            try
            {
                updateProcess.Start();
                updateProcess.WaitForExit();
            }
            catch (Win32Exception ex)
            {
                Log.LogInfo(ex);
                throw;
            }

            if (updateProcess.ExitCode == 0)
                return true;
            
            return false;
        }

        /// <summary>
        /// Starts the currently loaded mod
        /// </summary>
        //[PrincipalPermissionAttribute(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
        private void StartGame()
        {
            List<string> options = new List<string>();

            options.Add("+modPath mods/" + _config.Name);

            foreach (KeyValuePair<string, string> kvp in _config.Arguments)
            {
                options.Add(kvp.Key + " " + kvp.Value);
            }

            string arguments = string.Join(" ", options.ToArray());

            Process gameProcess = new Process();

            string exeDirectory = Directory.GetParent(_config.ParentPath).FullName;
            string originalExe = "";
            string filePath = "";

            // Decide which exe to use when creating the FirstStrike exe
            if (File.Exists(Path.Combine(exeDirectory, "BF2142Pace.exe")))
                originalExe = "BF2142Pace.exe";
            else
                originalExe = "BF2142.exe";

            // Make sure FirstStrike.exe exists
            if (!File.Exists(Path.Combine(exeDirectory, "FirstStrike.exe")))
            {
                CreateGameExe(originalExe);
            }

            // If user is running EADM version, make sure FSLauncher.exe exists, and is used.
            if (File.Exists(Path.Combine(exeDirectory, "BF2142Launcher.exe")))
            {
                if (!File.Exists(Path.Combine(exeDirectory, "FSLauncher.exe")))
                {
                    using (ZipFile zip = new ZipFile("exe.data"))
                    {
                        zip["FSLauncher.exe"].Extract(exeDirectory);
                    }
                }

                filePath = Path.Combine(exeDirectory, "FSLauncher.exe");
            }
            else
                filePath = Path.Combine(exeDirectory, "FirstStrike.exe");

            gameProcess.StartInfo.FileName = filePath;
            gameProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(filePath);
            gameProcess.StartInfo.Arguments = arguments;
            gameProcess.StartInfo.UseShellExecute = true;

            gameProcess.EnableRaisingEvents = true;
            gameProcess.Exited += new EventHandler(OnGameProcessExited);

            gameProcess.Start();

            // if window is open, go to tray, and set _stayMinimized to make sure we return to open window
            if (WindowState == FormWindowState.Normal)
            {
                _stayMinimized = false;
                WindowState = FormWindowState.Minimized;
            }

        }

        private delegate void ShowFormCallback();

        private void OnGameProcessExited(object sender, EventArgs e)
        {
            // The game process might reside in another thread, so make sure we execute in the main thread
            if (!_stayMinimized)
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new ShowFormCallback(ShowForm));
                }
            }
        }

        private void ShowForm()
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        /// <summary>
        /// Dynamically creates a new FirstStrike.exe if none exists, using the vanilla BF2142 exe file.
        /// </summary>
        /// <param name="originalExe">Exe file to base new FirstStrike exe on</param>
        //[PrincipalPermissionAttribute(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
        private void CreateGameExe(string originalExe)
        {
            // TODO: Make sure this works with all retail versions (orig, deluxe, digital)
            // TODO: Make it so new exe is exactly the same size as original. Currently slightly bigger. Is it not overwriting old icons?
            
            string exeDirectory = Directory.GetParent(_config.ParentPath).FullName;

            File.Copy(Path.Combine(exeDirectory, originalExe), Path.Combine(exeDirectory, "FirstStrike.exe"));

            try
            {
                using (ZipFile zip = new ZipFile("exe.data"))
                {
                    zip["icon.ico"].Extract(Directory.GetCurrentDirectory(), ExtractExistingFileAction.OverwriteSilently);
                }

                IconFile icon = new IconFile("icon.ico");

                IconDirectoryResource resource = new IconDirectoryResource(icon);

                resource.Name = new ResourceId(101);

                for (int i = 0; i < 3; i++)
                {
                    resource.Icons[i].Id = (ushort)(i + 1);
                }

                resource.SaveTo(Path.Combine(exeDirectory, "FirstStrike.exe"));

                File.Delete("icon.ico");

            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show(this, string.Format("The file {0} is missing. Please find it.", ex.FileName), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CheckForUpdates(false);
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PreferencesForm preferences = new PreferencesForm(_config);

            DialogResult result = preferences.ShowDialog();

            if (result == DialogResult.OK)
            {
                _config = preferences.Config;

                _config.Serialize(Path.Combine(_appDataFolder, _configName));
            }

            preferences.Dispose();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseApplication();

            Application.Exit();
        }

        private void cbNoUpdate_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                _config.UpdateBeforeLaunch = !cbNoUpdate.Checked;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            if (_config.UpdateBeforeLaunch)
            {
                if (CheckServerStatus(_config.UpdatesUrl[_currentMirror]))
                {
                    if (!CheckForUpdates(true))
                        return;
                }
                else
                    MessageBox.Show("The update server is currently down. The game will still launch, but you might not be able to join servers if you are missing updates.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            StartGame();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox box = new AboutBox();

            box.ShowDialog();

            box.Dispose();
        }

        private void CloseApplication()
        {
            if (_config != null)
                _config.Serialize(Path.Combine(_appDataFolder, _configName));

            UpdateManager updateManager = UpdateManager.Instance;

            if (updateManager.State == UpdateManager.UpdateProcessState.Prepared)
            {
                updateManager.Abort();

                while (updateManager.IsWorking) ;

                updateManager.ApplyUpdates(false);

            }

            updateManager.CleanUp();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            CloseApplication();
        }

        private void FirstStrikeLauncherResize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                systrayIcon.Visible = true;
                Hide();
            }
            else if (WindowState == FormWindowState.Normal)
            {
                systrayIcon.Visible = false;
                _stayMinimized = true;
            }
        }

        private void systrayIconDoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void webBrowserNews_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url.AbsoluteUri.StartsWith("http://"))
            {
                e.Cancel = true;

                Process.Start(e.Url.AbsoluteUri);

            }
        }

        private void webBrowserNews_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            if (e.Url.AbsoluteUri == "about:blank" && !_browserIsLoaded)
            {
                _browserIsLoaded = true;

                BackgroundWorker worker = new BackgroundWorker();

                worker.DoWork += new DoWorkEventHandler(OnWorkerDoWork);
                worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnWorkerCompleted);

                worker.RunWorkerAsync();

                
            }
        }


    }
}
