using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Cloud;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Input;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CloudClient.Views;

public partial class MainWindow : Window
{
    int maxInput;
    string ipString;
    int port;

    public MainWindow()
    {
        InitializeComponent();

        LabelProcess.Content = "";

        maxInput = 10;
        ipString = ConfigurationManager.AppSettings["ServerIP"].ToString();
        port = int.Parse(ConfigurationManager.AppSettings["Port"].ToString());
    }

    public void LoginButton_Click(object sender,RoutedEventArgs e)
    {
        LabelProcess.Content = "正在连接...";

        ClientManager clientManager = new ClientManager(ipString, port);

        //处理用户名和密码 前导or尾部 空白字符
        string userName = UserNameBox.Text;
        string userPass = PasswordBox.Text;
        byte status = CheckInput(userName, userPass);

        if(status == NetPublic.DefindedCode.OK)
        {
            try
            {
                status = clientManager.LoginProcess(userName, userPass);
            }
            catch
            {
                LabelProcess.Content= "⚠请检查网络连接";
                clientManager = null;
                return;
            }

            switch (status)
            {
                case NetPublic.DefindedCode.LOGSUCCESS:
                    LabelProcess.Content = "登录成功 正在同步...";

                    ClientWindow clientWindow = new ClientWindow(clientManager);
                    clientWindow.Show();
                    this.Close();
                    break;

                case NetPublic.DefindedCode.PASSERROR:
                    LabelProcess.Content = "⚠密码错误";
                    break;

                case NetPublic.DefindedCode.USERMISS:
                    LabelProcess.Content = "⚠用户不存在";
                    break;

                default:
                    break;
            }
        }
        else if (status == NetPublic.DefindedCode.TOOLONG)
            LabelProcess.Content = "⚠输入过长";
    }

    private byte CheckInput(string userName, string userPass)
    {
        if (userName.Length > maxInput || userPass.Length > maxInput)
            return NetPublic.DefindedCode.TOOLONG;
        return NetPublic.DefindedCode.OK;
    }
}
