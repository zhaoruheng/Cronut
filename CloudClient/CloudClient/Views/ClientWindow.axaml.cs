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

        public ClientWindow()
        {
            InitializeComponent();

            foreach (var uploadingFile in new string[] { "111.txt�����ϴ�", "111.txt�ϴ���...", "111.txt�ϴ��ɹ�", "heihei.jpg", "111.txt�ϴ���...", "111.txt�ϴ���...", "111.txt�ϴ���...", "111.txt�ϴ���...", "111.txt�ϴ���...", "111.txt�ϴ���...", "111.txt�ϴ���...", "111.txt�ϴ���...", "111.txt�ϴ���...", "111.txt�ϴ���...", "111.txt�ϴ���...", "111.txt�ϴ���...", "111.txt�ϴ���...", "111.txt�ϴ���...", "111.txt�ϴ���...", "111.txt�ϴ���..." }.OrderBy(x => x))
            {
                UploadingFileList.Items.Add(uploadingFile);
            }
        }

        public ClientWindow(ClientManager clientManager)
        {
            InitializeComponent();

            workPath = ConfigurationManager.AppSettings["TargetDir"];
            userName = ConfigurationManager.AppSettings["UserName"];
            this.clientManager = clientManager;

            if (!string.IsNullOrEmpty(workPath) && Directory.Exists(workPath) && !string.IsNullOrEmpty(userName) && string.Equals(userName, clientManager.getusername()))
            {
                ShowFilePath.Watermark = workPath;
                ShowFilePath.IsEnabled = false;
                ChooseButton.IsEnabled = false;
                ConfirmButton.IsEnabled = false;

                Thread th = new Thread(SyncTh);
                th.IsBackground = true;
                th.Start();

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
            var dialog = new OpenFolderDialog() { Title = "ѡ���ļ���" };
            workPath = await dialog.ShowAsync(this);
            if (workPath != null)
            {
                // result��ѡ�е��ļ���·��
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

            clientManager.workPath = workPath;
            SetAppSettingConf("TargetDir", workPath);
            SetAppSettingConf("UserName", clientManager.getusername());

            //�ϴ��������ļ�����
            clientManager.SyncProcess();
            //MessageBox.Show("�ϴ������ļ����̽����������ļ����");

            fw = new FileWatcher(workPath, "*.*");
            fw.SendEvent += new FileWatcher.DelegateEventHander(clientManager.AnalysesEvent);
            fw.Start();


        }
    }
}
