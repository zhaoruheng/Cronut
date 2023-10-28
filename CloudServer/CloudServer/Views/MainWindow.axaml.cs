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
    private readonly string tag;
    private readonly string connectionString;
    private readonly ServiceManager serviceManager;
    public static ListBox lb;
    private MainViewModel mvm;
    private WindowNotificationManager? _manager;

    public MainWindow()
    {
        InitializeComponent();
        tag = ConfigurationManager.AppSettings["initTag"].ToString();
        connectionString = ConfigurationManager.ConnectionStrings["FirstConnection"].ToString();
        int listenPort = int.Parse(ConfigurationManager.AppSettings["Port"].ToString());
        serviceManager = new ServiceManager(listenPort, connectionString);
        serviceManager.RealTimeItemAdded += OnRealTimeInfoItemAdded;
        lb = this.FindControl<ListBox>("DatailedParameter");
        mvm = new();
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

    public void Initialize_Click(object sender, RoutedEventArgs e) //初始化
    {
        serviceManager.InitProcess();

        SetAppSettingConf("initTag", "1");
        InitializeButton.IsEnabled = false;
        InitializeButton.Content = "Initialized";
    }

    public void Start_Click(object sender, RoutedEventArgs e)   //启动
    {
        serviceManager.Start();
        _ = RealTimeInfo.Items.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  服务器启动成功");

        StartButton.IsEnabled = false;
        StartButton.Content = "Started";
    }

    private void RefreshUserInfo_Click(object sender, RoutedEventArgs e)
    {
        if (StartButton.IsEnabled==false)
        {
            mvm.RefreshUserInfo();
            uuserList.DataContext = mvm;
        }
        else
        {
            _manager?.Show(new Notification("错误!", "请先启动服务器!", NotificationType.Error));
        }
    }

    private void RefreshFileInfo_Click(object sender, RoutedEventArgs e)
    {
        if (StartButton.IsEnabled == false)
        {
            mvm.RefreshFileInfo();
            ffileList.DataContext = mvm;
        }
        else
        {
            _manager?.Show(new Notification("错误!", "请先启动服务器!", NotificationType.Error));
        }
    }
}
