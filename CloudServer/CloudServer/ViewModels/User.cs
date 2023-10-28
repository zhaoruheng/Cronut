
namespace CloudServer.ViewModels
{
    public class User
    {
        public string userID { get; set; }
        public string userName { get; set; }

        public User(string id, string name)
        {
            userID = id;
            userName = name;
        }
    }
}
