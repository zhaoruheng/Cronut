using Cloud;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using System;
using SkiaSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using Avalonia.Threading;
using DynamicData;
using System.Linq;

namespace CloudServer.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly DataBaseManager dm;
    public ObservableCollection<ObservableValue> MainChartValues1 = new();
    public ObservableCollection<ObservableValue> MainChartValues2 = new();
    public ObservableCollection<ObservableValue> MainChartValues3 = new();
    private ObservableCollection<User> _userList;
    private ObservableCollection<UpFile> _upFileList;

    private Timer timer1;
    private Timer timer2;
    private Timer timer3;

    public ISeries[] SumFileSeries { get; set; }
    public ISeries[] SumResFileSeries { get; set; }
    public ISeries[] UserSeries { get; set; }

    public MainViewModel()
    {
        string con = ConfigurationManager.ConnectionStrings["FirstConnection"].ToString();
        dm = new DataBaseManager(con);
        UserList = new ObservableCollection<User>();
        UpFileList = new ObservableCollection<UpFile>();

        MainChartValues1.AddRange(Enumerable.Range(0, 30).Select(_ => new ObservableValue(0)));
        MainChartValues2.AddRange(Enumerable.Range(0, 30).Select(_ => new ObservableValue(0)));
        MainChartValues3.AddRange(Enumerable.Range(0, 30).Select(_ => new ObservableValue(0)));

        SumFileSeries = new ISeries[]
        {
            new LineSeries<ObservableValue>
            {
                Values = MainChartValues1,
                GeometrySize = 0,
                GeometryStroke = null,
                Fill = new SolidColorPaint(new SKColor(181,212,233)),
                Stroke = new SolidColorPaint(new SKColor(1,111,174)){StrokeThickness=2},
            }
        };

        SumResFileSeries = new ISeries[]
        {
            new LineSeries<ObservableValue>
            {
                Values = MainChartValues2,
                GeometrySize = 0,
                GeometryStroke = null,
                Fill = new SolidColorPaint(new SKColor(181,212,233)),
                Stroke = new SolidColorPaint(new SKColor(1,111,174)){StrokeThickness=2},
            }
        };

        UserSeries = new ISeries[]
        {
            new LineSeries<ObservableValue>
            {
                Values = MainChartValues3,
                GeometrySize = 0,
                GeometryStroke = null,
                Fill = new SolidColorPaint(new SKColor(181,212,233)),
                Stroke = new SolidColorPaint(new SKColor(1,111,174)){StrokeThickness=2},
            }
        };

        timer1 = new Timer(GetRealTimeFileNum, null, 0, 400); //每隔1s触发一次回调函数
        timer2 = new Timer(GetRealTimeResFileNum, null, 0, 400); //每隔1s触发一次回调函数
        timer3 = new Timer(GetRealTimeUserNum, null, 0, 400); //每隔1s触发一次回调函数
    }

    private static int GetFileMaxYAxis()
    {
        return 20;
    }

    private static int GetUserMaxYAxis()
    {
        return 5;
    }

    //文件数的纵坐标
    public List<Axis> YFileAxis { get; set; } = new List<Axis>
    {
        new Axis
        {
            MinStep=4,
            ForceStepToMin=true,
            MaxLimit = GetFileMaxYAxis(),
            MinLimit=0,
            TextSize=12
        }
    };

    //用户数的纵坐标
    public List<Axis> YUserAxis { get; set; } = new List<Axis>
    {
        new Axis
        {
            MinStep=1,
            ForceStepToMin=true,
            MaxLimit = GetUserMaxYAxis(),
            MinLimit=0,
            TextSize=12
        }
    };

    //横坐标
    public List<Axis> XAxis { get; set; } = new List<Axis>
    {
        new Axis
        {
            IsVisible=false
        }
    };

    public ObservableCollection<User> UserList
    {
        get => _userList;
        set
        {
            if (_userList != value)
            {
                _userList = value;
                OnPropertyChanged(nameof(UserList));
            }
        }
    }

    public ObservableCollection<UpFile> UpFileList
    {
        get => _upFileList;
        set
        {
            if (_upFileList != value)
            {
                _upFileList = value;
                OnPropertyChanged(nameof(UpFileList));
            }
        }
    }

    private void GetRealTimeFileNum(object state)
    {
        string con = ConfigurationManager.ConnectionStrings["FirstConnection"].ToString();
        DataBaseManager dm = new DataBaseManager(con);
        int realTimeFileNum = dm.GetUpfileNum();

        Dispatcher.UIThread.Invoke(() =>
        {
            MainChartValues1.RemoveAt(0);
            MainChartValues1.Add(new ObservableValue(realTimeFileNum));
        });
    }

    private void GetRealTimeResFileNum(object state)
    {
        string con = ConfigurationManager.ConnectionStrings["FirstConnection"].ToString();
        DataBaseManager dm = new DataBaseManager(con);
        int realTimeResFileNum = dm.GetFileNum();
        Dispatcher.UIThread.Invoke(() =>
        {
            MainChartValues2.RemoveAt(0);
            MainChartValues2.Add(new ObservableValue(realTimeResFileNum));
        });
    }

    private void GetRealTimeUserNum(object state)
    {
        int realTimeUser = ServiceManager.onlineUser.Count;

        Dispatcher.UIThread.Invoke(() =>
        {
            MainChartValues3.RemoveAt(0);
            MainChartValues3.Add(new ObservableValue(realTimeUser));
        });
    }

    public void RefreshUserInfo()
    {
        List<string> userInfoList = dm.GetUserInfo();
        List<User> userList = new();
        foreach (string ul in userInfoList)
        {
            string[] parts = ul.Split(',');
            if (parts.Length == 2)
            {
                string userID = parts[0];
                string userName = parts[1];

                User uu = new(userID, userName);
                userList.Add(uu);
            }
        }
        UserList = new ObservableCollection<User>(userList);
    }

    public void RefreshFileInfo()
    {
        List<string> fileInfoList = dm.GetFileInfo();
        List<UpFile> fileList = new();
        foreach (string fl in fileInfoList)
        {
            string[] parts = fl.Split(',');
            if (parts.Length == 7)
            {
                string fileID = parts[0];
                string fileTag = parts[1];
                string fileSize = parts[2];
                string serAdd = parts[3];
                string fileName = parts[4];
                string userName = parts[5];
                string uploadTime = parts[6];

                if (!String.IsNullOrEmpty(fileName))
                {
                    UpFile ff = new(fileID, fileName, userName, fileTag, fileSize, serAdd, uploadTime);
                    fileList.Add(ff);
                }
                else Debug.WriteLine("检测到空");
            }
        }
        UpFileList = new ObservableCollection<UpFile>(fileList);
    }
}
