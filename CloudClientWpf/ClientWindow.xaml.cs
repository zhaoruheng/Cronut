using System.Windows;
using System.Configuration;
using System.IO;
using System.Threading;
using System;
using WinForms = System.Windows.Forms;
using System.Collections.Generic;
using System.Windows.Input;

namespace Cloud
{
    /// <summary>
    /// ClientWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ClientWindow : Window
    {
        private string workPath = string.Empty;
        private string userName = string.Empty;
        private ClientManager clientManager;
        private FileWatcher fw;

        public ClientWindow(ClientManager clientManager,string uName)
        {
            InitializeComponent();
            button3_f.Visibility= Visibility.Hidden;

            //textBox1.ReadOnly = true; //设置只读属性
            workPath = ConfigurationManager.AppSettings["TargetDir"];
            userName = ConfigurationManager.AppSettings["UserName"];
            if(uName!=userName)
            {
                workPath=string.Empty;
                SetAppSettingConf("TargetDir", "");
                SetAppSettingConf("UserName",uName);
            }
            textBox1.Text = "";
            this.clientManager = clientManager;

            if (!string.IsNullOrEmpty(workPath) && Directory.Exists(workPath))
            {
                textBox1.Text = workPath;
                textBox1.IsEnabled = false;
                button2.IsEnabled = false;
                button3.IsEnabled = false;
                button2.Visibility = Visibility.Hidden;
                button3.Visibility = Visibility.Hidden;
                button3_f.Visibility = Visibility.Visible;

                Thread th = new Thread(SyncTh);
                th.IsBackground = true;
                th.Start();

                fw = new FileWatcher(workPath, "*.*");
                fw.SendEvent += new FileWatcher.DelegateEventHander(clientManager.AnalysesEvent);
                fw.Start();
            }

            UpdateFileList2();
            clientManager.ReturnMsg += new ClientManager.DelegateEventHander(UpdateFileList2);
        }

        private void SyncTh()
        {
            clientManager.SyncProcess();
        }

        private void SetAppSettingConf(string key, string value)
        {
            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfa.AppSettings.Settings[key].Value = value;
            cfa.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }

        //specific
        private void UploadTh(object fp)
        {
            string filePath = fp as string;
            clientManager.UploadFileProcess(filePath);
        }

        private void DownloadTh(object obj)
        {
            string fileName = obj as string;
            byte res = clientManager.DownloadFileProcess(fileName);
            if (res == NetPublic.DefindedCode.ERROR)
                MessageBox.Show("下载失败");
            else
                MessageBox.Show("下载完成");
            return;
        }

        //auto
        private void button2_Click(object sender, EventArgs e)  //指定工作目录
        {
            //选择目标目录
            WinForms.FolderBrowserDialog dialog = new WinForms.FolderBrowserDialog
            {
                Description = "请选择目标目录"
            };

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                workPath = dialog.SelectedPath;
                textBox1.Text = workPath.Trim();
            }
        }

        private void button3_Click(object sender, EventArgs e)  //启动客户端
        {
            if (string.IsNullOrEmpty(workPath))
            {
                MessageBox.Show("未指定目标文件夹");
                return;
            }
            clientManager.workPath = workPath;
            SetAppSettingConf("TargetDir", workPath);

            clientManager.SyncProcess();
            //修改配置文件
            fw = new FileWatcher(workPath, "*.*");
            fw.SendEvent += new FileWatcher.DelegateEventHander(clientManager.AnalysesEvent);
            fw.Start();
            button3.IsEnabled = false;
            button2.IsEnabled = false;
            button2.Visibility = Visibility.Hidden;
            button3.Visibility = Visibility.Hidden;
            button3_f.Visibility = Visibility.Visible;
        }


        private void button5_Click(object sender, EventArgs e)  //刷新列表
        {
            UpdateFileList2();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            var items = listView1.SelectedItems;
            string downloadFile = items[0].ToString();
            Thread th = new Thread(DownloadTh);
            th.IsBackground = true;
            th.Start(downloadFile);
        }

        private void UpdateFileList2()
        {
            List<string> fileList = clientManager.GetFileNameList();
            if (fileList == null)
            {
                MessageBox.Show("获取文件列表失败");
                return;
            }
            ClearListView("");
            foreach (string f in fileList)
            {
                UpdateListView(f);
            }
        }

        private delegate void Delegate(string value);

        private void ClearListView(string arg)
        {
            if (!CheckAccess())
            {
                Delegate d = new Delegate(ClearListView);
                listView1.Dispatcher.Invoke(d, new object[] { arg });
            }
            else
            {
                listView1.Items.Clear();
            }
        }

        private void UpdateListView(string value)
        {
            if (!CheckAccess())
            {
                Delegate d = new Delegate(UpdateListView);
                listView1.Dispatcher.Invoke(d, new object[] { value });
            }
            else
                listView1.Items.Add(value);
        }

        private void Client_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            clientManager.LogoutProcess();
            //SetAppSettingConf("TargetDir", "");
            clientManager = null;
        }

        private void Window_LeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.DragMove();
            }
            catch { }
        }

        private void Click_Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
