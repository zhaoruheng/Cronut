using Avalonia.Controls;
using System;
using System.Configuration;
using Cloud;
using Avalonia.Interactivity;
using CloudServer.ViewModels;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Notifications;

namespace CloudServer.Views;

public partial class MainWindow : Window
{
    private readonly string connectionString;
    private readonly ServiceManager serviceManager;
    public static ListBox lb;
    private WindowNotificationManager? _manager;

    public MainWindow()
    {
        InitializeComponent();

        connectionString = ConfigurationManager.ConnectionStrings["FirstConnection"].ToString();
        int listenPort = int.Parse(ConfigurationManager.AppSettings["Port"].ToString());
        serviceManager = new ServiceManager(listenPort, connectionString);

        serviceManager.RealTimeItemAdded += OnRealTimeInfoItemAdded;

        lb = this.FindControl<ListBox>("DatailedParameter");
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _manager = new WindowNotificationManager(this)
        {
            MaxItems = 3
        };
    }

    private void OnRealTimeInfoItemAdded(string item)
    {
        _ = RealTimeInfo.Items.Add(item);
    }

    public void SetAppSettingConf(string key, string value)
    {
        Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        cfa.AppSettings.Settings[key].Value = value;
        cfa.Save();
        ConfigurationManager.RefreshSection("appSettings");
    }

    //初始化按钮
    public void Initialize_Click(object sender, RoutedEventArgs e) 
    {
        serviceManager.InitProcess();

        SetAppSettingConf("initTag", "1");
        InitializeButton.IsEnabled = false;
        InitializeButton.Content = "Initialized";
    }

    //启动按钮
    public void Start_Click(object sender, RoutedEventArgs e)   
    {
        serviceManager.Start();
        _ = RealTimeInfo.Items.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  服务器启动成功");

        StartButton.IsEnabled = false;
        StartButton.Content = "Started";
    }

    //更新前端用户表
    private void RefreshUserInfo_Click(object sender, RoutedEventArgs e)
    {
        if (StartButton.IsEnabled == false)
        {
            MainViewModel mvm = new();
            mvm.RefreshUserInfo();
            uuserList.DataContext = mvm;
        }
        else
        {
            _manager?.Show(new Notification("错误!", "请先启动服务器!", NotificationType.Error));
        }
    }

    //更新前端文件表
    private void RefreshFileInfo_Click(object sender, RoutedEventArgs e)
    {
        if (StartButton.IsEnabled == false)
        {
            MainViewModel mvm = new();
            mvm.RefreshFileInfo();
            ffileList.DataContext = mvm;
        }
        else
        {
            _manager?.Show(new Notification("错误!", "请先启动服务器!", NotificationType.Error));
        }
    }

    //关闭
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    //最小化
    private void MinButton_Click(object sender,RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
}
