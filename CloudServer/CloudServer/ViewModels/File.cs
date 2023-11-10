using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudServer.ViewModels
{
    public class File
    {
        public int fileID { get; set; }
        public string fileName { get; set; }
        public string fileTag { get; set; }
        public int fileSize { get; set; }
        public string serAdd { get; set; }

        public File(int id,string name,string tag,int size,string add)
        {
            fileID = id;
            fileName = name;
            fileTag = tag;
            fileSize = size;
            serAdd = add;
        }
    }
}
