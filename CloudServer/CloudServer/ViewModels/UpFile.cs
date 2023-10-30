namespace CloudServer.ViewModels
{
    public class UpFile
    {
        public string fileID { get; set; }
        public string fileName { get; set; }
        public string initialUserName { get; set; }
        public string fileTag { get; set; }
        public string fileSize { get; set; }
        public string serAdd { get; set; }
        public string uploadTime { get; set; }

        public UpFile(string id, string name, string uName, string tag, string size, string add, string time)
        {
            fileID = id;
            fileName = name;
            initialUserName = uName;
            fileTag = tag;
            fileSize = size;
            serAdd = add;
            uploadTime = time;
        }
    }
}
