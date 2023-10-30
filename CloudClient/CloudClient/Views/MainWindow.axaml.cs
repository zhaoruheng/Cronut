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

    //检查输入的长短是否合法
    private byte CheckInput(string userName, string userPass)
    {
        if (userName.Length > maxInput || userPass.Length > maxInput)
        {
            return NetPublic.DefindedCode.TOOLONG;
        }
        else if (userName.Length == 0 || userPass.Length == 0)
        {
            return NetPublic.DefindedCode.ERROR;
        }
        return NetPublic.DefindedCode.OK;
    }

    //登录按钮
    public void LoginButton_Click(object sender,RoutedEventArgs e)
    {
        LabelProcess.Content = "正在连接...";

        ClientManager clientManager = new ClientManager(ipString, port);

        //处理用户名和密码
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
        {
            LabelProcess.Content = "⚠输入过长";
        }
        else if(status == NetPublic.DefindedCode.ERROR)
        {
            LabelProcess.Content = "⚠用户名或密码为空";
        }
    }

    //注册按钮
    private void SignUpButton_Click(object sender, RoutedEventArgs e)
    {
        ClientManager clientManager = new ClientManager(ipString, port);
        string userName = UserNameBox.Text;
        string userPass = PasswordBox.Text;

        byte status = CheckInput(userName, userPass);

        if (status == NetPublic.DefindedCode.OK)
        {
            try
            {
                status = clientManager.SignUpProcess(userName, userPass);
            }
            catch
            {
                LabelProcess.Content = "⚠请检查网络连接";
                clientManager = null;
                return;
            }

            switch (status)
            {
                case NetPublic.DefindedCode.SIGNUPSUCCESS:
                    LabelProcess.Content = "注册成功!";
                    break;

                case NetPublic.DefindedCode.ERROR:
                    LabelProcess.Content = "⚠用户已存在";
                    break;

                default:
                    LabelProcess.Content = "⚠注册失败";
                    Debug.WriteLine("收到" + status);
                    break;
            }
        }
        else if(status==NetPublic.DefindedCode.TOOLONG)
        {
            LabelProcess.Content = "⚠输入的密码过长";
        }

        UserNameBox.Clear();    //将用户名和密码的textbox清空
        PasswordBox.Clear();
    }

    //按下Enter键自动登录
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                LoginButton_Click(LoginButton, null);
                break;
            default:
                break;
        }
    }

    //关闭
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    //最小化
    private void MinButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }
}
