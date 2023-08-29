﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace EnvyUpdate
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class DashboardPage
    {
        private string localDriv = null;
        private string onlineDriv = null;
        private string gpuURL = null;
        private string skippedVer = null;

        public DashboardPage()
        {
            InitializeComponent();

            if (Debug.isFake)
                localDriv = Debug.LocalDriv();
            else
                localDriv = Util.GetLocDriv();

            Debug.LogToFile("INFO Local driver version: " + localDriv);

            if (localDriv != null)
            {
                Debug.LogToFile("INFO Local driver version already known, updating info without reloading.");
                UpdateLocalVer(false);
            }

            Debug.LogToFile("INFO Detecting driver type.");

            if (Debug.isFake)
                textblockLocalType.Text = "DCH (Debug)";
            else if (Util.IsDCH())
                textblockLocalType.Text = "DCH";
            else
                textblockLocalType.Text = "Standard";

            Debug.LogToFile("INFO Done detecting driver type: " + textblockLocalType.Text);

            // Check for startup shortcut
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "EnvyUpdate.lnk")))
            {
                Debug.LogToFile("INFO Autostart is enabled.");
                switchAutostart.IsChecked = true;
                switchAutostart_Click(null, null); //Automatically recreate shortcut to account for moved EXE.
            }

            DispatcherTimer Dt = new DispatcherTimer();
            Dt.Tick += new EventHandler(Dt_Tick);
            // Check for new updates every 5 hours.
            Dt.Interval = new TimeSpan(5, 0, 0);
            Dt.Start();
            Debug.LogToFile("INFO Started check timer.");

            string watchDirPath = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramW6432%"), "NVIDIA Corporation\\Installer2\\InstallerCore");
            if (Directory.Exists(watchDirPath))
            {
                GlobalVars.monitoringInstall = true;

                var driverFileChangedWatcher = new FileSystemWatcher(watchDirPath);
                driverFileChangedWatcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Size;
                driverFileChangedWatcher.Changed += DriverFileChanged;

                driverFileChangedWatcher.Filter = "*.dll";
                driverFileChangedWatcher.IncludeSubdirectories = false;
                driverFileChangedWatcher.EnableRaisingEvents = true;
                Debug.LogToFile("INFO Started update file system watcher.");
            }
            else
                Debug.LogToFile("WARN Could not start update file system watcher. Path not found: " + watchDirPath);

            Load();
        }

        private void Dt_Tick(object sender, EventArgs e)
        {
            Load();
        }

        private void Load()
        {
            if (Util.GetDTID() == 18)
            {
                Debug.LogToFile("INFO Found studio driver.");
                switchStudioDriver.IsChecked = true;
            }
            else
            {
                Debug.LogToFile("INFO Found standard driver.");
                switchStudioDriver.IsChecked = false;
            }

            if (File.Exists(GlobalVars.exedirectory + "skip.envy"))
            {
                Debug.LogToFile("INFO Found version skip config.");
                skippedVer = File.ReadLines(GlobalVars.exedirectory + "skip.envy").First();
            }

            // This little bool check is necessary for debug fake mode. 
            if (Debug.isFake)
            {
                localDriv = Debug.LocalDriv();
                cardLocal.Header = localDriv;
                textblockGPUName.Text = Debug.GPUname();
            }

            try
            {
                Debug.LogToFile("INFO Trying to get GPU update URL.");
                gpuURL = Util.GetGpuUrl();
            }
            catch (ArgumentException)
            {
                Debug.LogToFile("WARN Could not get GPU update URL, trying again with non-studio driver.");
                try
                {
                    // disable SD and try with GRD
                    if (File.Exists(GlobalVars.exedirectory + "sd.envy"))
                    {
                        File.Delete(GlobalVars.exedirectory + "sd.envy");
                    }

                    gpuURL = Util.GetGpuUrl(); //try again with GRD
                    MessageBox.Show(Properties.Resources.ui_studionotsupported);
                    switchStudioDriver.IsChecked = false;
                }
                catch (ArgumentNullException)
                {
                    MessageBox.Show("ERROR: Could not get list of GPU models from Nvidia, please check your network connection.\nOtherwise, please report this issue on GitHub.");
                    Environment.Exit(11);
                }
                catch (ArgumentException e)
                {
                    // Now we have a problem.
                    Debug.LogToFile("FATAL Invalid API response from Nvidia. Attempted API call: " + e.Message);
                    MessageBox.Show("ERROR: Invalid API response from Nvidia. Please file an issue on GitHub.\nAttempted API call:\n" + e.Message);
                    Environment.Exit(10);
                }
            }

            using (var c = new WebClient())
            {
                Debug.LogToFile("INFO Trying to get newest driver version.");
                string pContent = c.DownloadString(gpuURL);
                var pattern = @"Windows\/\d{3}\.\d{2}";
                Regex rgx = new Regex(pattern);
                var matches = rgx.Matches(pContent);
                onlineDriv = Regex.Replace(Convert.ToString(matches[0]), "Windows/", "");
                cardOnline.Header = onlineDriv;
                Debug.LogToFile("INFO Got online driver version: " + onlineDriv);
            }

            try
            {
                if (float.Parse(localDriv) < float.Parse(onlineDriv))
                {
                    Debug.LogToFile("INFO Local version is older than online. Setting UI...");
                    SetInfoBar(false);
                    buttonDownload.Visibility = Visibility.Visible;
                    if (skippedVer == null)
                    {
                        buttonSkipVersion.ToolTip = Properties.Resources.ui_skipversion;
                        buttonSkipVersion.IsEnabled = true;
                        buttonSkipVersion.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        buttonSkipVersion.IsEnabled = true;
                        buttonSkipVersion.ToolTip = Properties.Resources.ui_skipped;
                    }

                    Debug.LogToFile("INFO UI set.");

                    if (skippedVer != onlineDriv)
                    {
                        Debug.LogToFile("INFO Showing update popup notification.");
                        Notify.ShowDrivUpdatePopup();
                    }
                }
                else
                {
                    Debug.LogToFile("INFO Local version is up to date.");
                    buttonSkipVersion.Visibility = Visibility.Collapsed;
                    SetInfoBar(true);
                }
            }
            catch (FormatException)
            {
                Debug.LogToFile("INFO Caught FormatException, assuming locale workaround is necessary.");
                //Thank you locales. Some languages need , instead of . for proper parsing
                string cLocalDriv = localDriv.Replace('.', ',');
                string cOnlineDriv = onlineDriv.Replace('.', ',');
                if (float.Parse(cLocalDriv) < float.Parse(cOnlineDriv))
                {
                    Debug.LogToFile("INFO Local version is older than online. Setting UI...");
                    SetInfoBar(false);
                    buttonDownload.Visibility = Visibility.Visible;
                    if (skippedVer == null)
                    {
                        buttonSkipVersion.IsEnabled = true;
                        buttonSkipVersion.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        buttonSkipVersion.IsEnabled = false;
                        buttonSkipVersion.ToolTip = Properties.Resources.ui_skipped;
                    }
                        
                    if (skippedVer != onlineDriv)
                    {
                        Debug.LogToFile("INFO Showing update popup notification.");
                        Notify.ShowDrivUpdatePopup();
                    }
                }
                else
                {
                    Debug.LogToFile("INFO Local version is up to date.");
                    buttonSkipVersion.Visibility = Visibility.Collapsed;
                    SetInfoBar(true);
                }
            }

            //Check for different version than skipped version
            if (skippedVer != null && skippedVer != onlineDriv)
            {
                Debug.LogToFile("INFO Skipped version is surpassed, deleting setting.");
                skippedVer = null;
                if (File.Exists(GlobalVars.exedirectory + "skip.envy"))
                    File.Delete(GlobalVars.exedirectory + "skip.envy");
                buttonSkipVersion.ToolTip = Properties.Resources.ui_skipversion;
                buttonSkipVersion.IsEnabled = true;
                buttonSkipVersion.Visibility = Visibility.Visible;
            }

            // Check if update file already exists and display install button instead
            if (File.Exists(Path.Combine(GlobalVars.exedirectory, onlineDriv + "-nvidia-installer.exe")))
            {
                Debug.LogToFile("INFO Found downloaded driver installer, no need to redownload.");
                buttonDownload.Visibility = Visibility.Collapsed;
                buttonInstall.Visibility = Visibility.Visible;
            }
        }

        private void switchStudioDriver_Unchecked(object sender, RoutedEventArgs e)
        {
            if (File.Exists(GlobalVars.exedirectory + "sd.envy"))
            {
                Debug.LogToFile("INFO Switching to game ready driver.");
                File.Delete(GlobalVars.exedirectory + "sd.envy");
                Load();
            }
        }

        private void switchStudioDriver_Checked(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(GlobalVars.exedirectory + "sd.envy"))
            {
                Debug.LogToFile("INFO Switching to studio driver.");
                File.Create(GlobalVars.exedirectory + "sd.envy").Close();
                Load();
            }
        }

        private void switchAutostart_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "EnvyUpdate.lnk")))
            {
                Debug.LogToFile("INFO Removing autostart entry.");
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "EnvyUpdate.lnk"));
            }
            if (switchAutostart.IsChecked == true)
            {
                Debug.LogToFile("INFO Creating autostart entry.");
                Util.CreateShortcut("EnvyUpdate", Environment.GetFolderPath(Environment.SpecialFolder.Startup), GlobalVars.exeloc, "NVidia Update Checker", "/minimize");
            }
        }

        private void buttonSkipVersion_Click(object sender, RoutedEventArgs e)
        {
            Debug.LogToFile("INFO Skipping version.");
            skippedVer = onlineDriv;
            File.WriteAllText(GlobalVars.exedirectory + "skip.envy", onlineDriv);
            buttonSkipVersion.IsEnabled = false;
            buttonSkipVersion.ToolTip = Properties.Resources.ui_skipped;
            MessageBox.Show(Properties.Resources.skip_confirm);
        }

        private void UpdateLocalVer(bool reloadLocalDriv = true)
        {
            Debug.LogToFile("INFO Updating local driver version in UI.");
            if (reloadLocalDriv)
            {
                Debug.LogToFile("INFO Reloading local driver version.");
                localDriv = Util.GetLocDriv();
            }
            cardLocal.Header = localDriv;
            if (GlobalVars.isMobile)
                textblockGPUName.Text = Util.GetGPUName(false) + " (mobile)";
            else
                textblockGPUName.Text = Util.GetGPUName(false);
        }

        void DriverFileChanged(object sender, FileSystemEventArgs e)
        {
            Debug.LogToFile("INFO Watched driver file changed! Reloading data.");
            System.Threading.Thread.Sleep(10000);
            Application.Current.Dispatcher.Invoke(delegate
            {
                UpdateLocalVer();
                Load();
            });
        }

        private void CardOnline_Click(object sender, RoutedEventArgs e)
        {
            Debug.LogToFile("INFO Opening download page.");
            Process.Start(gpuURL);
        }

        private void SetInfoBar (bool good)
        {
            if (good)
            {
                infoBarStatus.Severity = Wpf.Ui.Controls.InfoBarSeverity.Success;
                infoBarStatus.Title = Properties.Resources.ui_info_uptodate;
                infoBarStatus.Message = Properties.Resources.ui_message_good;
            }
            else
            {
                infoBarStatus.Severity = Wpf.Ui.Controls.InfoBarSeverity.Warning;
                infoBarStatus.Title = Properties.Resources.ui_info_outdated;
                infoBarStatus.Message = Properties.Resources.ui_message_update;
            }
        }

        private void buttonDownload_Click(object sender, RoutedEventArgs e)
        {
            progressbarDownload.Visibility = Visibility.Visible;
            buttonDownload.IsEnabled = false;

            if (File.Exists(Path.Combine(GlobalVars.exedirectory, onlineDriv + "-nvidia-installer.exe.downloading")))
            {
                Debug.LogToFile("WARN Found previous unfinished download, retrying.");
                File.Delete(Path.Combine(GlobalVars.exedirectory, onlineDriv + "-nvidia-installer.exe.downloading"));
            }
            Thread thread = new Thread(() => {
                WebClient client = new WebClient();
                client.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:115.0) Gecko/20100101 Firefox/115.0";
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                client.DownloadFileAsync(new Uri(Util.GetDirectDownload(gpuURL)), Path.Combine(GlobalVars.exedirectory, onlineDriv + "-nvidia-installer.exe.downloading"));
            });
            thread.Start();
            Debug.LogToFile("INFO Started installer download.");
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            Application.Current.Dispatcher.Invoke(new Action(() => {
                progressbarDownload.Value = int.Parse(Math.Truncate(percentage).ToString());
            }));
        }
        void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                buttonDownload.IsEnabled = true;
                progressbarDownload.Visibility = Visibility.Collapsed;
            }));
            if (e.Error == null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    showSnackbar(Wpf.Ui.Common.ControlAppearance.Success, Wpf.Ui.Common.SymbolRegular.CheckmarkCircle24, Properties.Resources.info_download_success, Properties.Resources.info_download_success_title);
                    buttonDownload.Visibility = Visibility.Collapsed;
                    buttonInstall.Visibility = Visibility.Visible;
                    Debug.LogToFile("INFO Download successful.");
                }));
                if (File.Exists(Path.Combine(GlobalVars.exedirectory, onlineDriv + "-nvidia-installer.exe")))
                    File.Delete(Path.Combine(GlobalVars.exedirectory, onlineDriv + "-nvidia-installer.exe"));
                File.Move(Path.Combine(GlobalVars.exedirectory, onlineDriv + "-nvidia-installer.exe.downloading"), Path.Combine(GlobalVars.exedirectory, onlineDriv + "-nvidia-installer.exe"));
            }
            else
            {
                File.Delete(Path.Combine(GlobalVars.exedirectory, onlineDriv + "-nvidia-installer.exe.downloading"));
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    showSnackbar(Wpf.Ui.Common.ControlAppearance.Danger, Wpf.Ui.Common.SymbolRegular.ErrorCircle24, Properties.Resources.info_download_error, Properties.Resources.info_download_error_title);
                    Debug.LogToFile("INFO Download NOT successful. Error: " + e.Error.ToString());
                }));
            }
        }
        private void buttonInstall_Click(object sender, RoutedEventArgs e)
        {

        }

        private void showSnackbar (Wpf.Ui.Common.ControlAppearance appearance, Wpf.Ui.Common.SymbolRegular icon, string message = "", string title = "")
        {
            snackbarInfo.Appearance = appearance;
            snackbarInfo.Icon = icon;
            snackbarInfo.Title = title;
            snackbarInfo.Message = message;
            snackbarInfo.Show();
        }
    }
}
