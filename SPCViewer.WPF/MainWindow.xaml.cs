﻿using Microsoft.Win32;
using SPCViewer.Core;
using SPCViewer.ViewModel;
using SPCViewer.WPF.Resources;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using ThemeCommons.Controls;
using ThemeCommons.Extension.Native;

namespace SPCViewer.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DefaultWindow
    {
        public static IntPtr WindowHandle { get; private set; }

        public MainViewModel ViewModel;

        public MainWindow()
        {
            Settings.Instance.Load($"{AppDomain.CurrentDomain.BaseDirectory}/settings.json");
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
            InitializeComponent();
            HandleArgs();

            Loaded += (sender, args) =>
            {
                WindowHandle =
                    new WindowInteropHelper(Application.Current.MainWindow ?? throw new InvalidOperationException())
                        .Handle;
                HwndSource.FromHwnd(WindowHandle)?.AddHook(new HwndSourceHook(HandleMessages));
            };
        }

        /// <summary>
        /// Handles stored command line args
        /// </summary>
        private void HandleArgs()
        {
            var app = Application.Current;
            if (app.Properties["args"] == null) return;
            var files = ((string[])app.Properties["args"])
                ?.Where(File.Exists);
            ViewModel.OpenFiles(files.ToArray());
        }

        /// <summary>
        /// Drag and Drop Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFileDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            ViewModel.OpenFiles(files);
        }

        /// <summary>
        /// Handles File Open by Dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = Constants.OpenFileFilter,
                Multiselect = true
            };
            if (ofd.ShowDialog(this) != true) return;
            ViewModel.OpenFiles(ofd.FileNames.Where(File.Exists).ToArray());
        }

        /// <summary>
        /// Handles Saving Files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog()
            {
                Filter = Constants.SaveFileFilter,
            };
            if (sfd.ShowDialog(this) != true) return;
            ViewModel.SaveFile(sfd.FileName);
        }

        /// <summary>
        /// Handles Message from other source
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="message"></param>
        /// <param name="wParameter"></param>
        /// <param name="lParameter"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private static IntPtr HandleMessages(IntPtr handle, int message, IntPtr wParameter, IntPtr lParameter,
            ref bool handled)
        {
            var data = NativeMethods.GetMessage(message, lParameter);
            if (data == null) return IntPtr.Zero;

            if (Application.Current.MainWindow == null)
                return IntPtr.Zero;
            if (Application.Current.MainWindow.WindowState == WindowState.Minimized)
                Application.Current.MainWindow.WindowState = WindowState.Normal;

            NativeMethods.SetForegroundWindow(WindowHandle);

            var args = data.Split(",");
            Application.Current.Properties["args"] = args;
            var mw = Application.Current.MainWindow;
            (mw as MainWindow)?.HandleArgs();

            handled = true;
            return IntPtr.Zero;
        }

        /// <summary>
        /// Creates document
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DocumentCreate_OnClick(object sender, RoutedEventArgs e) => ViewModel.CreateDocument();
    }
}
