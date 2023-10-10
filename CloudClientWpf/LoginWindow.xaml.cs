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
using System.Windows.Shapes;

namespace Cloud
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        int maxInput;
        string ipString;
        int port;

        public ClientManager ClientManager;

        public LoginWindow()
        {
            InitializeComponent();

            label3.Content = "";
            maxInput = 10;
            ipString = ConfigurationManager.AppSettings["ServerIP"].ToString();
            port = int.Parse(ConfigurationManager.AppSettings["Port"].ToString());
        }

        //登录界面
        private void button1_Click(object sender, EventArgs e)
        {
            label3.Content = "正在连接...";

            ClientManager clientManager = new ClientManager(ipString, port);

            //处理用户名和密码 前导or尾部 空白字符
            string userName = textBox1.Text.Trim();
            string userPass = textBox2.Password.ToString().Trim();
            byte status = CheckInput(userName, userPass);

            if (status == NetPublic.DefindedCode.OK)
            {
                try
                {
                    status = clientManager.LoginProcess(userName, userPass);
                }
                catch
                {
                    label3.Content = "⚠请检查网络连接";
                    clientManager = null;
                    return;
                }

            switch (status)
                {
                    case NetPublic.DefindedCode.LOGSUCCESS:
                        //DialogResult = DialogResult.OK;
                        label3.Content = "登录成功 正在同步...";

                        ClientWindow clientWindow = new ClientWindow(clientManager);
                        this.Close();
                        clientWindow.Show();
                        break;

                    case NetPublic.DefindedCode.PASSERROR:
                        label3.Content = "⚠密码错误";
                        break;

                    case NetPublic.DefindedCode.USERMISS:
                        label3.Content = "⚠用户不存在";
                        break;

                    default:
                        break;
                }
            }
            else if (status == NetPublic.DefindedCode.TOOLONG)
                label3.Content = "⚠输入过长";
        }

        //检查输入的用户名和密码的长度
        private byte CheckInput(string userName, string userPass)
        {
            if (userName.Length > maxInput || userPass.Length > maxInput)
                return NetPublic.DefindedCode.TOOLONG;
            return NetPublic.DefindedCode.OK;
        }

        //鼠标左键按下触发事件
        private void Window_LeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.DragMove();
            }
            catch { }
        }

        //输入完按Enter键调用Login按钮
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    button1_Click(button1, null);
                    break;
                default:
                    break;
            }
        }

        //关闭窗口
        private void Click_Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
