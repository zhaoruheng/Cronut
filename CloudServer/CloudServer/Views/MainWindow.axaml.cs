using Avalonia.Controls;
using System;
using System.Configuration;
using System.Linq;
using Cloud;
using Avalonia.Interactivity;

namespace CloudServer.Views;

public partial class MainWindow : Window
{
    private string tag;
    private string connectionString;
    private ServiceManager serviceManager;
    
    public MainWindow()
    {
        InitializeComponent();
        tag = ConfigurationManager.AppSettings["initTag"].ToString();
        connectionString = ConfigurationManager.ConnectionStrings["FirstConnection"].ToString();
        int listenPort = int.Parse(ConfigurationManager.AppSettings["Port"].ToString());
        serviceManager = new ServiceManager(listenPort, connectionString);
        serviceManager.RealTimeItemAdded += OnRealTimeInfoItemAdded;
        
        foreach (var para in new string[] { "alpha:13y9r5y42", "file tag: wca3uiewbcfk", "MHT Num:7", "Challenge MHT Index: 4", "Challenge Leaf Index: 0" }.OrderBy(x => x))
        {
            DatailedParameter.Items.Add(para);
        }
    }

    private void OnRealTimeInfoItemAdded(string item)
    {
        RealTimeInfo.Items.Add(item);
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
        RealTimeInfo.Items.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  服务器启动成功");

        StartButton.IsEnabled = false;
        StartButton.Content = "Started";
    }
}
