using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CloudServer.ViewModels;

public class MainViewModel : ViewModelBase
{
    public string Greeting => "Welcome to Avalonia!";

    public ISeries[] Series { get; set; }
        = new ISeries[]
        {
            new LineSeries<double>
            {
                Values=new double[]{2,1,3,5,3,4,6},
                Fill=null
            }
        };

    
    public ObservableCollection<User> UserList { get; }
    public ObservableCollection<File> FileList { get; }

    public MainViewModel()
    {
        var userList = new List<User>
        {
            new User(1,"admin"),
            new User(2,"uheng"),
            new User(3,"guest"),
            new User(2,"uheng"),
            new User(2,"uheng"),
            new User(2,"uheng"),
            new User(2,"uheng"),
            new User(2,"uheng"),
            new User(2,"uheng"),
            new User(2,"uheng"),
            new User(2,"uheng"),
            new User(2,"uheng"),
            new User(2,"uheng"),
            new User(2,"uheng"),
            new User(2,"uheng"),

        };
        UserList = new ObservableCollection<User>(userList);

        var fileList = new List<File>
        {
            new File(1,"text.txt","3ibdekvjfesj",123,"_server/3ibdekvjfesj"),
            new File(1,"text.txt","3ibdekvjfesj",123,"_server/3ibdekvjfesj"),
            new File(1,"text.txt","3ibdekvjfesj",123,"_server/3ibdekvjfesj"),
            new File(1,"text.txt","3ibdekvjfesj",123,"_server/3ibdekvjfesj"),
            new File(1,"text.txt","3ibdekvjfesj",123,"_server/3ibdekvjfesj"),

        };
        FileList = new ObservableCollection<File>(fileList);
    }
}
