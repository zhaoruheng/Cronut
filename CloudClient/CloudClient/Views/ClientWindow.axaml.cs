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
using Avalonia.Input;
using System.Diagnostics;

namespace CloudClient.Views
{
    public partial class ClientWindow : Window
    {
        private string workPath=string.Empty;
        private string userName=string.Empty;
        private ClientManager clientManager;
        private FileWatcher fw;
        public static ListBox lb;
        private bool isDragging = false;
        private Point startPosition;
        private int selectorFileType = 0;   //0��ҳ 1ͼƬ 2�ĵ� 3��Ƶ��Ƶ 4����
        public ClientWindow(ClientManager clientManager)
        {
            InitializeComponent();

            DragDrop.SetAllowDrop(this, true); // �����Ϸ�

            // �����Ϸ��¼�
            this.PointerPressed += OnPointerPressed;
            this.PointerMoved += OnPointerMoved;
            this.PointerReleased += OnPointerReleased;

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

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                isDragging = true;
                startPosition = e.GetPosition(this);
            }
        }

        private void OnPointerMoved(object sender, PointerEventArgs e)
        {
            if (isDragging)
            {
                var currentPosition = e.GetPosition(this);
                var offset = currentPosition - startPosition;
                this.Position = new PixelPoint(this.Position.X + (int)offset.X, this.Position.Y + (int)offset.Y);
                startPosition = currentPosition;
            }
        }

        private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
            }
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

        //ɸѡȫ���ļ�
        private void SelectAllFile_Click(object sender, RoutedEventArgs e)
        {
            selectorFileType = 0;
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
        
        //ɸѡ�ı��ļ�
        private void SelectTextFile_Click(object sender, RoutedEventArgs e)
        {
            selectorFileType = 2;
            List<string> fileList = clientManager.GetFileNameList();

            if (fileList == null)
            {
                return;
            }

            FileWrapPanel.Children.Clear();
            foreach (string f in fileList)
            {
                string tmp = f.Substring(f.IndexOf('.') + 1, f.Length - (f.IndexOf('.') + 1));
                if (!tmp.Equals("docx") && !tmp.Equals("doc") && 
                    !tmp.Equals("txt") && !tmp.Equals("xls") && 
                    !tmp.Equals("xlsx") && !tmp.Equals("pdf")&&
                    !tmp.Equals("ppt") && !tmp.Equals("pptx")&&
                    !tmp.Equals("md")) continue;
                UpdateFileWrapPanel(f);
            }
        }

        //ɸѡͼƬ�ļ�
        private void SelectPicFile_Click(object sender, RoutedEventArgs e)
        {
            selectorFileType = 1;
            List<string> fileList = clientManager.GetFileNameList();

            if (fileList == null)
            {
                return;
            }

            FileWrapPanel.Children.Clear();
            foreach (string f in fileList)
            {
                string tmp = f.Substring(f.IndexOf('.') + 1, f.Length - (f.IndexOf('.') + 1));
                if (!tmp.Equals("png") && !tmp.Equals("jpg") &&
                    !tmp.Equals("jpeg") && !tmp.Equals("bmp") &&
                    !tmp.Equals("gif") && !tmp.Equals("eps") &&
                    !tmp.Equals("tif") && !tmp.Equals("svg")) continue;
                UpdateFileWrapPanel(f);
            }
        }

        //ɸѡ��Ƶ��Ƶ�ļ�
        private void SelectVideoFile_Click(object sender, RoutedEventArgs e)
        {
            selectorFileType = 3;
            List<string> fileList = clientManager.GetFileNameList();

            if (fileList == null)
            {
                return;
            }

            FileWrapPanel.Children.Clear();
            foreach (string f in fileList)
            {
                string tmp = f.Substring(f.IndexOf('.') + 1, f.Length - (f.IndexOf('.') + 1));
                if (!tmp.Equals("mp4") && !tmp.Equals("mp3") &&
                    !tmp.Equals("wav") && !tmp.Equals("avi")) continue;
                UpdateFileWrapPanel(f);
            }
        }

        //ɸѡ�����ļ�
        private void SelectOtherFile_Click(object sender, RoutedEventArgs e)
        {
            selectorFileType = 4;
            List<string> fileList = clientManager.GetFileNameList();

            if (fileList == null)
            {
                return;
            }

            FileWrapPanel.Children.Clear();
            foreach (string f in fileList)
            {
                string tmp = f.Substring(f.IndexOf('.') + 1, f.Length - (f.IndexOf('.') + 1));
                if (tmp.Equals("docx") || tmp.Equals("doc") ||
                    tmp.Equals("txt") || tmp.Equals("xls") ||
                    tmp.Equals("xlsx") || tmp.Equals("pdf") ||
                    tmp.Equals("ppt") || tmp.Equals("pptx") ||
                    tmp.Equals("md") || tmp.Equals("png") ||
                    tmp.Equals("jpg") || tmp.Equals("jpeg") ||
                    tmp.Equals("bmp") || tmp.Equals("gif") || tmp.Equals("eps") ||
                    tmp.Equals("tif") || tmp.Equals("svg") ||
                    tmp.Equals("mp4") || tmp.Equals("mp3") ||
                    tmp.Equals("wav") || tmp.Equals("avi")) continue;

                UpdateFileWrapPanel(f);
            }
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

            if (selectorFileType == 0)
            {
                //����ȫ���ļ�
                foreach (string f in fileList)
                {
                    UpdateFileWrapPanel(f);
                }
            }
            else if (selectorFileType == 1)
            {
                //����ͼƬ�ļ�
                foreach (string f in fileList)
                {
                    string tmp = f.Substring(f.IndexOf('.') + 1, f.Length - (f.IndexOf('.') + 1));
                    if (!tmp.Equals("png") && !tmp.Equals("jpg") &&
                        !tmp.Equals("jpeg") && !tmp.Equals("bmp") &&
                        !tmp.Equals("gif") && !tmp.Equals("eps") &&
                        !tmp.Equals("tif") && !tmp.Equals("svg")) continue;
                    UpdateFileWrapPanel(f);
                }
            }
            else if (selectorFileType == 2)
            {
                //�����ĵ��ļ�
                foreach (string f in fileList)
                {
                    string tmp = f.Substring(f.IndexOf('.') + 1, f.Length - (f.IndexOf('.') + 1));
                    if (!tmp.Equals("docx") && !tmp.Equals("doc") &&
                        !tmp.Equals("txt") && !tmp.Equals("xls") &&
                        !tmp.Equals("xlsx") && !tmp.Equals("pdf") &&
                        !tmp.Equals("ppt") && !tmp.Equals("pptx") &&
                        !tmp.Equals("md")) continue;
                    UpdateFileWrapPanel(f);
                }

            }
            else if (selectorFileType == 3)
            {
                //������Ƶ��Ƶ�ļ�
                foreach (string f in fileList)
                {
                    string tmp = f.Substring(f.IndexOf('.') + 1, f.Length - (f.IndexOf('.') + 1));
                    if (!tmp.Equals("mp4") && !tmp.Equals("mp3") &&
                        !tmp.Equals("wav") && !tmp.Equals("avi")) continue;
                    UpdateFileWrapPanel(f);
                }
            }
            else if (selectorFileType == 4)
            {
                //���������ļ�
                foreach (string f in fileList)
                {
                    string tmp = f.Substring(f.IndexOf('.') + 1, f.Length - (f.IndexOf('.') + 1));
                    if (tmp.Equals("docx") || tmp.Equals("doc") ||
                        tmp.Equals("txt") || tmp.Equals("xls") ||
                        tmp.Equals("xlsx") || tmp.Equals("pdf") ||
                        tmp.Equals("ppt") || tmp.Equals("pptx") ||
                        tmp.Equals("md") || tmp.Equals("png") ||
                        tmp.Equals("jpg") || tmp.Equals("jpeg") ||
                        tmp.Equals("bmp") || tmp.Equals("gif") || tmp.Equals("eps") ||
                        tmp.Equals("tif") || tmp.Equals("svg") ||
                        tmp.Equals("mp4") || tmp.Equals("mp3") ||
                        tmp.Equals("wav") || tmp.Equals("avi")) continue;

                    UpdateFileWrapPanel(f);
                }

            }
            else return;
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

        //��С������
        private void MinButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        //���
        private void MaxButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            MaxButton.IsVisible = false;
            NormButton.IsVisible = true;
        }

        //�ָ�
        private void NormButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            NormButton.IsVisible = false;
            MaxButton.IsVisible = true;
        }
    }
}
