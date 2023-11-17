namespace CloudServer.ViewModels
{
    public class User
    {
        public string userID { get; set; }
        public string userName { get; set; }
        public string userGroup { get; set; }
        public string registerTime { get; set; }
        public string lastLoginTime { get; set; }
        public string userState { get; set; }


        public User(string id, string name, string userGroup, string registerTime, string lastLoginTime, string userState)
        {
            userID = id;
            userName = name;
            this.userGroup = userGroup;
            this.registerTime = registerTime;
            this.lastLoginTime = lastLoginTime;
            this.userState = userState;
        }
    }
}
