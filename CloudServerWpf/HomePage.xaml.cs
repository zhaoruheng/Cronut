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
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class HomePage : Page
    {
        string tag;
        string connectionString;
        ServiceManager serviceManager;

        public HomePage()
        {
            InitializeComponent();

            tag = ConfigurationManager.AppSettings["initTag"].ToString();
            connectionString = ConfigurationManager.ConnectionStrings["FirstConnection"].ToString();
            int listenPort = int.Parse(ConfigurationManager.AppSettings["Port"].ToString());
            serviceManager = new ServiceManager(listenPort, connectionString);
            serviceManager.ReturnMsg += new ServiceManager.ReturnMsgDelegate(UpdateInfoDisp);
            button4_f.Visibility = Visibility.Hidden;
            button1_f.Visibility = Visibility.Hidden;
        }

        delegate void Delegate(string value);
        public void UpdateInfoDisp(string value)
        {
            if (!CheckAccess())
            {
                Delegate d = new Delegate(UpdateInfoDisp);
                listBox1.Dispatcher.Invoke(d, new object[] { value });
            }
            else
                listBox1.Items.Add(value);
        }

        private void SetAppSettingConf(string key, string value)
        {
            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfa.AppSettings.Settings[key].Value = value;
            cfa.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void button4_Click(object sender, EventArgs e) //初始化
        {
            serviceManager.InitProcess();
            SetAppSettingConf("initTag", "1");
            button4.Visibility = Visibility.Hidden;
            button4_f.Visibility = Visibility.Visible;
        }

        private void button1_Click(object sender, EventArgs e) //启动
        {
            serviceManager.Start();
            listBox1.Items.Add(DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss") + ": 服务器启动成功");
            button1.Visibility = Visibility.Hidden;
            button1_f.Visibility = Visibility.Visible;
        }
    }
}
