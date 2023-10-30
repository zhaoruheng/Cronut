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
using static System.Net.Mime.MediaTypeNames;

namespace CloudClient.Views
{
    public partial class ClientWindow : Window
    {
        private string workPath=string.Empty;
        private string userName=string.Empty;
        private ClientManager clientManager;
        private FileWatcher fw;
        public static ListBox lb;

        public ClientWindow(ClientManager clientManager)
        {
            InitializeComponent();

            lb = this.FindControl<ListBox>("UploadingFileList");
            UploadingFileList.Items.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": �û���½�ɹ�");

            workPath = ConfigurationManager.AppSettings["TargetDir"];
            userName = ConfigurationManager.AppSettings["UserName"];

            this.clientManager = clientManager;

            if (!string.IsNullOrEmpty(workPath) && Directory.Exists(workPath) && !string.IsNullOrEmpty(userName) && string.Equals(userName, clientManager.getusername()))
            {
                UploadingFileList.Items.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": �û���ѡ���ļ���");

                ShowFilePath.Watermark = workPath;
                ShowFilePath.IsEnabled = false;
                ChooseButton.IsEnabled = false;
                ConfirmButton.IsEnabled = false;

                UploadingFileList.Items.Add("�ƶ��ļ�ͬ����...");

                Thread th = new Thread(SyncTh);
                th.IsBackground = true;
                th.Start();

                UploadingFileList.Items.Add("�����ļ��м��...");
                fw = new FileWatcher(workPath, "*.*");
                fw.SendEvent += new FileWatcher.DelegateEventHander(clientManager.AnalysesEvent);
                fw.Start();
            }

            UpdateFileList();
        }

        private void SetAppSettingConf(string key, string value)
        {
            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfa.AppSettings.Settings[key].Value = value;
            cfa.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }

        //�û��ǳ���ť
        private void CloseButton_Click(object sender,RoutedEventArgs args)
        {
            clientManager.LogoutProcess();
            clientManager = null;
            this.Close();
        }

        //��С������
        private void MinButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void SyncTh()
        {
            clientManager.SyncProcess();
        }

        //ѡ��ͬ���ļ���
        private async void OpenFileButton_Click(object sender, RoutedEventArgs args)
        {
            var dialog = new OpenFolderDialog() { Title = "ѡ���ļ���" };
            workPath = await dialog.ShowAsync(this);
            if (workPath != null)
            {
                ShowFilePath.Watermark = workPath;
                ChooseButton.IsEnabled = false;
                ChooseButton.Content = "Chosen";
            }
        }

        //ѡ���ļ�·��
        private void ConfirmButton_Click(object sender,RoutedEventArgs args)
        {
            if (string.IsNullOrEmpty(workPath))
            {
                return;
            }

            ConfirmButton.IsEnabled = false;

            UploadingFileList.Items.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": �û���ѡ���ļ�·��");

            clientManager.workPath = workPath;
            SetAppSettingConf("TargetDir", workPath);
            SetAppSettingConf("UserName", clientManager.getusername());

            //�ϴ��������ļ�����
            clientManager.SyncProcess();

            fw = new FileWatcher(workPath, "*.*");
            fw.SendEvent += new FileWatcher.DelegateEventHander(clientManager.AnalysesEvent);
            fw.Start();
        }

        //�����ļ��б�
        private void UpdateFileList()
        {
            List<string> fileList = clientManager.GetFileNameList();
            if (fileList == null)
            {
                return;
            }

            FileWrapPanel.Children.Clear();
            foreach (string f in fileList)
            {
                UpdateFileWrapPanel(f);
            }
        }

        //�����ļ�Wrap Panel
        private void UpdateFileWrapPanel(string f)
        {
            FileButton fileButton = new FileButton();
            TextBlock textBlock = fileButton.FindControl<TextBlock>("FileButtonTextBlock");
            textBlock.Text = f;

            FileWrapPanel.Children.Add(fileButton);
        }

        //�ļ��б���°�ť
        private void UpdateFile_Click(object sender, RoutedEventArgs e)
        {
            UpdateFileList();
        }
    }
}
