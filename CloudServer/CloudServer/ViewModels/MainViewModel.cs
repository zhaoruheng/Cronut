using Cloud;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;

namespace CloudServer.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly DataBaseManager dm;

    public ISeries[] Series { get; set; }
        = new ISeries[]
        {
            new LineSeries<double>
            {
                Values=new double[]{2,1,3,5,3,4,6},
                Fill=null
            }
        };

    private ObservableCollection<User> _userList;
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

    private ObservableCollection<UpFile> _upFileList;
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

    public MainViewModel()
    {
        string con = ConfigurationManager.ConnectionStrings["FirstConnection"].ToString();
        dm = new DataBaseManager(con);
        UserList = new ObservableCollection<User>();
        UpFileList = new ObservableCollection<UpFile>();
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

                UpFile ff = new(fileID, fileName, userName, fileTag, fileSize, serAdd, uploadTime);
                fileList.Add(ff);
            }
        }
        UpFileList = new ObservableCollection<UpFile>(fileList);
    }
}
