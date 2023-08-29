﻿using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace EnvyUpdate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private string[] arguments = null;

        public MainWindow()
        {
            InitializeComponent();

            // Try to get command line arguments
            try
            {
                arguments = Environment.GetCommandLineArgs();
            }
            catch (IndexOutOfRangeException)
            {
                // This is necessary, since .NET throws an exception if you check for a non-existant arg.
            }

            // Check if Debug file exists
            if (File.Exists(Path.Combine(GlobalVars.exedirectory, "envyupdate.log")))
            {
                Debug.isVerbose = true;
                Debug.LogToFile("------");
                Debug.LogToFile("INFO Found log file, will start logging to this.");
            }

            Debug.LogToFile("INFO Starting EnvyUpdate, version " + System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion);

            // Check if running on supported Windows version.
            if (Environment.OSVersion.Version.Major < 10)
            {
                Debug.LogToFile("FATAL Unsupported OS version, terminating.");
                MessageBox.Show(Properties.Resources.unsupported_os);
                Environment.Exit(1);
            }

            // Check if EnvyUpdate is already running
            if (Util.IsInstanceOpen("EnvyUpdate"))
            {
                Debug.LogToFile("FATAL Found another instance, terminating.");

                MessageBox.Show(Properties.Resources.instance_already_running);
                Environment.Exit(1);
            }

            // Check dark theme
            AdjustTheme();
            SystemEvents.UserPreferenceChanged += AdjustTheme;

            // Delete installed legacy versions, required for people upgrading from very old versions.
            if (Directory.Exists(GlobalVars.appdata))
            {
                Debug.LogToFile("INFO Found old appdata installation, uninstalling.");
                Util.UninstallAll();
            }

            // Allow for running using a fake graphics card if no nvidia card is present.
            if (arguments.Contains("/fake"))
            {
                Debug.isFake = true;
                Debug.LogToFile("WARN Faking GPU with debug info.");
            }
            else if (!Util.IsNvidia())
            {
                Debug.LogToFile("FATAL No supported GPU found, terminating.");
                MessageBox.Show(Properties.Resources.no_compatible_gpu);
                Environment.Exit(255);
            }

            //Check if launched as miminized with arg
            if (arguments.Contains("/minimize"))
            {
                Debug.LogToFile("INFO Launching minimized.");
                WindowState = WindowState.Minimized;
                Hide();
            }

            GlobalVars.isMobile = Util.IsMobile();
            Debug.LogToFile("INFO Mobile: " + GlobalVars.isMobile);
        }
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Debug.LogToFile("INFO Window was minimized, closing to tray.");
                Hide();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Debug.LogToFile("INFO Uninstalling notifications and shutting down.");
            ToastNotificationManagerCompat.Uninstall(); // Uninstall notifications to prevent issues with the app being portable.
            Application.Current.Shutdown();
        }

        private void UiWindow_Activated(object sender, EventArgs e)
        {
            // This is a workaround for a bug (?) in the UI library, which causes the nav to not load the first item on startup.
            // Without this, the nav attempts to navigate before the window is shown, which doesn't work.
            try
            {
                var test = RootNavigation.Current.PageType;
            }
            catch (NullReferenceException)
            {
                RootNavigation.Navigate(0);
            }
        }

        private void AdjustTheme(object sender = null, UserPreferenceChangedEventArgs e = null)
        {
            if (Util.IsDarkTheme())
                Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Dark, Wpf.Ui.Appearance.BackgroundType.Mica);
            else
                Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Light, Wpf.Ui.Appearance.BackgroundType.Mica);
        }
    }
}