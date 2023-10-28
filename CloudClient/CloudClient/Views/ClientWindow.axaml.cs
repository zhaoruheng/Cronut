using Avalonia.Controls;
using Avalonia.Interactivity;
using Cloud;
using Avalonia;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using System.Linq;
using ExCSS;

namespace CloudClient.Views
{
    public partial class ClientWindow : Window
    {
        private string workPath=string.Empty;
        private string userName=string.Empty;
        private ClientManager clientManager;
        private FileWatcher fw;
        public static ListBox lb;

        public ClientWindow()
        {
            InitializeComponent();
        }

        public ClientWindow(ClientManager clientManager)
        {
            InitializeComponent();

            lb = this.FindControl<ListBox>("UploadingFileList");
            UploadingFileList.Items.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": 用户登陆成功");

            workPath = ConfigurationManager.AppSettings["TargetDir"];
            userName = ConfigurationManager.AppSettings["UserName"];
            this.clientManager = clientManager;

            if (!string.IsNullOrEmpty(workPath) && Directory.Exists(workPath) && !string.IsNullOrEmpty(userName) && string.Equals(userName, clientManager.getusername()))
            {
                UploadingFileList.Items.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": 用户已选择文件夹");

                ShowFilePath.Watermark = workPath;
                ShowFilePath.IsEnabled = false;
                ChooseButton.IsEnabled = false;
                ConfirmButton.IsEnabled = false;

                UploadingFileList.Items.Add("云端文件同步中...");

                Thread th = new Thread(SyncTh);
                th.IsBackground = true;
                th.Start();

                UploadingFileList.Items.Add("进入文件夹监控...");
                fw = new FileWatcher(workPath, "*.*");
                fw.SendEvent += new FileWatcher.DelegateEventHander(clientManager.AnalysesEvent);
                fw.Start();
            }
        }

        private void CloseButton_Click(object sender,RoutedEventArgs args)
        {
            clientManager.LogoutProcess();
            clientManager = null;
            this.Close();
        }

        private void SyncTh()
        {
            clientManager.SyncProcess();
        }

        private async void OpenFileButton_Click(object sender, RoutedEventArgs args)
        {
            var dialog = new OpenFolderDialog() { Title = "选择文件夹" };
            workPath = await dialog.ShowAsync(this);
            if (workPath != null)
            {
                // result是选中的文件夹路径
                ShowFilePath.Watermark = workPath;
                ChooseButton.IsEnabled = false;
                ChooseButton.Content = "Chosen";
            }
        }

        private void SetAppSettingConf(string key, string value)
        {
            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfa.AppSettings.Settings[key].Value = value;
            cfa.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void ConfirmButton_Click(object sender,RoutedEventArgs args)
        {
            if (string.IsNullOrEmpty(workPath))
            {
                return;
            }

            ConfirmButton.IsEnabled = false;

            UploadingFileList.Items.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": 用户已选择文件路径");

            clientManager.workPath = workPath;
            SetAppSettingConf("TargetDir", workPath);
            SetAppSettingConf("UserName", clientManager.getusername());

            //上传和下载文件进程
            clientManager.SyncProcess();

            fw = new FileWatcher(workPath, "*.*");
            fw.SendEvent += new FileWatcher.DelegateEventHander(clientManager.AnalysesEvent);
            fw.Start();


        }
    }
}
