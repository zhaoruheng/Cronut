using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
namespace CloudClient.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ObservableCollection<Person> People { get; }

    public MainViewModel()
    {
        var people = new List<Person>
        {
            new Person("Neil", "Armstrong"),
                new Person("Buzz", "Lightyear"),
                new Person("James", "Kirk")
            };
        People = new ObservableCollection<Person>(people);
    }
}
