using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Cloud
{
    /// <summary>
    /// SecondPage.xaml 的交互逻辑
    /// </summary>
    public partial class SecondPage : Page
    {
        string tag;
        string connectionString;
        ServiceManager serviceManager;

        public SecondPage()
        {
            InitializeComponent();

            tag = ConfigurationManager.AppSettings["initTag"].ToString();
            connectionString = ConfigurationManager.ConnectionStrings["FirstConnection"].ToString();
            int listenPort = int.Parse(ConfigurationManager.AppSettings["Port"].ToString());
            //if (tag == "1")
            //	button4.Visible = false;
        }

        private void SetAppSettingConf(string key, string value)
        {
            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfa.AppSettings.Settings[key].Value = value;
            cfa.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void button2_Click(object sender, EventArgs e) //获取云端文件列表
        {
            listBox2.Items.Clear();
            listBox2.Items.Add("云端文件路径：./ServerFiles/");
            DataBaseManager dbm = new DataBaseManager(connectionString);
            List<string> cloudFiles = dbm.GetCloudFiles();
            foreach (var i in cloudFiles)
            {
                listBox2.Items.Add("./ServerFiles/" + i);
            }
        }
    }
}
