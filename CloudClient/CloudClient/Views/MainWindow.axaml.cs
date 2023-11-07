using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Cloud;
using Avalonia.Skia;
using Avalonia.Media.Imaging;
using System;
using System.Configuration;
using System.Diagnostics;
using Avalonia.Markup.Xaml;
using Avalonia;
using SkiaSharp.Extended;
using System.IO;
using DevExpress.Utils.Svg;
using log4net;
using DevExpress.Utils.About;

namespace CloudClient.Views;

public partial class MainWindow : Window
{
    int maxInput;
    string ipString;
    int port;
    private bool isDragging = false;
    private Point startPosition;
    private TextBox passwordBox;
    private Button seePasswordButton;
    private bool isPasswordVisible = false;
    private bool isPasswordVisible1 = false;
    private bool isPasswordVisible2 = false;

    private static ILog log = LogManager.GetLogger("Test");


    public MainWindow()
    {
        InitializeComponent();
        NewStackPanel.IsVisible = false;
        DragDrop.SetAllowDrop(this, true); // 启用拖放

        // 订阅拖放事件
        this.PointerPressed += OnPointerPressed;
        this.PointerMoved += OnPointerMoved;
        this.PointerReleased += OnPointerReleased;

        passwordBox = this.Find<TextBox>("PasswordBox");
        seePasswordButton = this.Find<Button>("SeePasswordButton");
       // AttachDevTools();

        LabelProcess.Content = "";
        LabelProcessSignUp.Content = "";
        maxInput = 10;

        ipString = ConfigurationManager.AppSettings["ServerIP"].ToString();
        port = int.Parse(ConfigurationManager.AppSettings["Port"].ToString());
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

        if (userName == null || userPass == null)
        {
            LabelProcess.Content = "⚠用户名或密码为空";
            log.Error("用户名或密码为空");
            return;
        }

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
                log.Error("网络连接失败");
                clientManager = null;
                return;
            }

            switch (status)
            {
                case NetPublic.DefindedCode.LOGSUCCESS:
                    LabelProcess.Content = "登录成功 正在同步...";

                    log.Info("登录成功");

                    ClientWindow clientWindow = new ClientWindow(clientManager);
                    clientWindow.Show();
                    this.Close();
                    break;

                case NetPublic.DefindedCode.PASSERROR:
                    LabelProcess.Content = "⚠密码错误";
                    log.Error("密码错误");
                    break;

                case NetPublic.DefindedCode.USERMISS:
                    LabelProcess.Content = "⚠用户不存在";
                    log.Error("用户不存在");
                    break;

                case NetPublic.DefindedCode.ERROR:
                    LabelProcess.Content = "⚠用户重复登录";
                    log.Error("用户重复登录");
                    break;

                default:
                    break;
            }
        }
        else if (status == NetPublic.DefindedCode.TOOLONG)
        {
            LabelProcess.Content = "⚠输入过长";
            log.Error("输入过长");
        }
        else if(status == NetPublic.DefindedCode.ERROR)
        {
            LabelProcess.Content = "⚠用户名或密码为空";
            log.Error("用户名或密码为空");
        }
    }

    //注册按钮
    private void SignUpButton_Click(object sender, RoutedEventArgs e)
    {
        
        BigStackPanel.IsVisible = false;
        LabelProcess.IsVisible = false;
        NewStackPanel.IsVisible = true;
    }
    
    //确认注册
    private void SignUpConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        ClientManager clientManager = new ClientManager(ipString, port);
        string str1 = PasswordBoxSignUp.Text;
        string str2 = PasswordBoxSignUpConfirm.Text;

        if(str1!=str2)
        {
            LabelProcessSignUp.Content = "⚠密码不一致";
            PasswordBoxSignUpConfirm.Clear();
            return;
        }

        string userName = UserNameBoxSignUp.Text;
        string userPass = PasswordBoxSignUp.Text;

        if(userName==null || userPass==null)
        {
            LabelProcessSignUp.Content = "⚠用户名或密码为空";
            return;
        }

        byte status = CheckInput(userName, userPass);

        if (status == NetPublic.DefindedCode.OK)
        {
            try
            {
                status = clientManager.SignUpProcess(userName, userPass);
            }
            catch
            {
                LabelProcessSignUp.Content = "⚠请检查网络连接";
                clientManager = null;
                return;
            }

            switch (status)
            {
                case NetPublic.DefindedCode.SIGNUPSUCCESS:
                    LabelProcessSignUp.Content = "注册成功!";
                    break;

                case NetPublic.DefindedCode.ERROR:
                    LabelProcessSignUp.Content = "⚠用户已存在";
                    break;

                default:
                    LabelProcessSignUp.Content = "⚠注册失败";
                    break;
            }
        }
        else if(status==NetPublic.DefindedCode.TOOLONG)
        {
            LabelProcessSignUp.Content = "⚠输入的密码过长";
        }

        UserNameBoxSignUp.Clear();    //将用户名和密码的textbox清空
        PasswordBoxSignUp.Clear();
        PasswordBoxSignUpConfirm.Clear();
    }

    private void GoBack_Click(object sender, RoutedEventArgs e)
    {
        NewStackPanel.IsVisible = false;
        BigStackPanel.IsVisible = true;
        LabelProcess.IsVisible = true;
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

    private void TogglePasswordVisibility(object sender, RoutedEventArgs e)
    {
        if (isPasswordVisible)
        {
            // 如果密码可见，将密码字符设置为 ●
            passwordBox.PasswordChar = '●';
            seePasswordButton.IsVisible = true;
            UnseePasswordButton.IsVisible = false;
        }
        else
        {
            // 如果密码不可见，将密码字符设置为无
            passwordBox.PasswordChar = '\0';
            seePasswordButton.IsVisible = false;
            UnseePasswordButton.IsVisible = true;
        }

        isPasswordVisible = !isPasswordVisible;
    }

    private void TogglePasswordVisibility1(object sender, RoutedEventArgs e)
    {
        if (isPasswordVisible1)
        {
            // 如果密码可见，将密码字符设置为 ●
            PasswordBoxSignUp.PasswordChar = '●';
            SeePasswordButtonSignUp.IsVisible = true;
            UnseePasswordButtonSignUp.IsVisible = false;
        }
        else
        {
            // 如果密码不可见，将密码字符设置为无
            PasswordBoxSignUp.PasswordChar = '\0';
            SeePasswordButtonSignUp.IsVisible = false;
            UnseePasswordButtonSignUp.IsVisible = true;
        }

        isPasswordVisible1 = !isPasswordVisible1;
    }

    private void TogglePasswordVisibility2(object sender, RoutedEventArgs e)
    {
        if (isPasswordVisible2)
        {
            // 如果密码可见，将密码字符设置为 ●
            PasswordBoxSignUpConfirm.PasswordChar = '●';
            SeePasswordButtonSignUpConfirm.IsVisible = true;
            UnseePasswordButtonSignUpConfirm.IsVisible = false;
        }
        else
        {
            // 如果密码不可见，将密码字符设置为无
            PasswordBoxSignUpConfirm.PasswordChar = '\0';
            SeePasswordButtonSignUpConfirm.IsVisible = false;
            UnseePasswordButtonSignUpConfirm.IsVisible = true;
        }

        isPasswordVisible2 = !isPasswordVisible2;
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

    //最大化
    private void MaxButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Maximized;
        MaxButton.IsVisible = false;
        NormButton.IsVisible = true;
    }

    //恢复
    private void NormButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Normal;
        NormButton.IsVisible = false;
        MaxButton.IsVisible = true;
    }
}
