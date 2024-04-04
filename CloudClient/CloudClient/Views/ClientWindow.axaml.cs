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
using log4net;
using Avalonia.Threading;
using Avalonia.Media;

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
        private static ProgressBar progressBar;
        private static ILog log = LogManager.GetLogger("Log");
        private SplitView sv1, sv2;
        private ScrollViewer ufViewer1;
        private TextBlock fuTextBlock;
        public static int processValue = 0;
        bool isFileUploadProcessVisible = false;

        private bool close = true;

        public ClientWindow(ClientManager clientManager)
        {
            InitializeComponent();

            progressBar = this.Find<ProgressBar>("progress");

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
                log.Info("�û���ѡ���ļ���");

                ShowFilePath.Watermark = workPath;
                //ShowFilePath.IsEnabled = false;
                ChooseButton.IsEnabled = false;
                ConfirmButton.IsEnabled = false;

                UploadingFileList.Items.Add("�ƶ��ļ�ͬ����...");
                log.Info("�ƶ��ļ�ͬ��");

                Thread th = new Thread(SyncTh);
                th.IsBackground = true;
                th.Start();

                UploadingFileList.Items.Add("�����ļ��м��...");
                log.Info("�����ļ��м��");
                fw = new FileWatcher(workPath, "*.*");
                fw.SendEvent += new FileWatcher.DelegateEventHander(clientManager.AnalysesEvent);
                fw.Start();
            }

            UpdateFileList();
        }

        private void FoldButton_Click(object sender, RoutedEventArgs e)
        {
            if (isFileUploadProcessVisible)
            {
                uploadFileProcessGrid.IsVisible = false;
                uploadFileBorder.Width = 713;
                chooseFilePathTextBox.Width = 462;
                ShowFilePath.Width = 462;
                FileWrapPanel.Width = 680;
                uploadedFileViewer.Width = 680;
            }
            else
            {
                uploadFileProcessGrid.IsVisible = true;
                uploadFileBorder.Width = 531;
                chooseFilePathTextBox.Width = 280;
                ShowFilePath.Width = 280;
                FileWrapPanel.Width = 510;
                uploadedFileViewer.Width = 510;
            }

            isFileUploadProcessVisible = !isFileUploadProcessVisible;
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            sv1 = this.Find<SplitView>("splitView1");
            sv2 = this.Find<SplitView>("splitView2");
            ufViewer1 = this.Find<ScrollViewer>("uploadedFileViewer");
            fuTextBlock = this.Find<TextBlock>("fileUploadTextBlock");

            if (close)
            {
                sv1.OpenPaneLength = 230;
                sv2.OpenPaneLength = 230;
                fuTextBlock.IsVisible = true;
                ufViewer1.Width = 570;
                close = false;
            }
            else
            {
                sv1.OpenPaneLength = 60;
                sv2.OpenPaneLength = 60;
                fuTextBlock.IsVisible = false;
                ufViewer1.Width = 890;
                close = true;
            }
        }

        //���½�������ֵ
        public static void UpdateProgressBar(int value)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                progressBar.Value = value;
            });
        }

        private void UpdateProgressBar()
        {
            // ʹ��Dispatcher��UI�߳��ϸ���ProgressBar��Value����
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // ������Ը�����Ҫ���½�������ֵ
                progressBar.Value += 1;
                

                // ���Value�ﵽ���ֵ������ֹͣ��ʱ����������Value��ֵ
                if (progressBar.Value >= progressBar.Maximum)
                {
                    // ֹͣ��ʱ��
                    //timer.Stop();

                    // ��������Value��ֵ
                    progressBar.Value = progressBar.Minimum;
                }
            });
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
                ChooseButton.Background = new SolidColorBrush(Colors.Red);
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
            progressBar.Value = 10;
            UploadingFileList.Items.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": �û���ѡ���ļ�·��");
            progressBar.Value = 25;

            clientManager.workPath = workPath;
            SetAppSettingConf("TargetDir", workPath);
            SetAppSettingConf("UserName", clientManager.getusername());
            progressBar.Value = 50;

            //�ϴ��������ļ�����
            //clientManager.SyncProcess();
            Thread th = new Thread(SyncTh);
            th.IsBackground = true;
            th.Start();
            progressBar.Value = 100;

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
    }
}
