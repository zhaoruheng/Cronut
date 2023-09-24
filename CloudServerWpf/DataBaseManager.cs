using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Security.Cryptography;
using NetPublic;
using System.Windows;

namespace Cloud
{
    class DataBaseManager
    {
        string connectionString;
        string queryString;
        string useString;
        string dbName;
        string defalutConnectionString = "Data Source=.;Initial Catalog=master;Integrated Security=True";

        public DataBaseManager(string con)
        {
            connectionString = "Data Source=.;Initial Catalog=" + con + ";Integrated Security=True";
            useString = "USE " + con + ";";
            dbName = con;
        }

        private void CreateCommand(string queryString)
        {
            using (SqlConnection connection = new SqlConnection(defalutConnectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Connection.Open();
                
                command.ExecuteNonQuery();
            }
        }

        private void CreateUser(string userName, string passwd)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] tmp = Encoding.Default.GetBytes(passwd);
            byte[] passMD5 = md5.ComputeHash(tmp);
            string passString = string.Empty;

            foreach (var i in passMD5)
                passString += i.ToString("x2"); //16进制

            string queryString = useString +
                "INSERT INTO UserTable VALUES (" +
                "'" + userName + "'," +
                "'" + passString + "'," +
                "NULL" +
                ");";
            CreateCommand(queryString);
        }

        public void InitProcess()
        {
            queryString = string.Format("SELECT database_id FROM sys.databases WHERE Name = '{0}'", dbName);

            
            int count = 0;
            using (SqlConnection connection = new SqlConnection(defalutConnectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Connection.Open();
                count = Convert.ToInt32(command.ExecuteScalar());
                command.ExecuteNonQuery();
            }
            

            if (count != 0)
            {
                //删表
                queryString = useString+" If exists(SELECT * FROM INFORMATION_SCHEMA.TABLES where table_name = 'UserTable') DROP TABLE UserTable; " +
                    "If exists(SELECT * FROM INFORMATION_SCHEMA.TABLES where table_name = 'UpFileTable') DROP TABLE UpFileTable; " +
                    " If exists(SELECT * FROM INFORMATION_SCHEMA.TABLES where table_name = 'FileTable') DROP TABLE FileTable; " +
                    " If exists(SELECT * FROM INFORMATION_SCHEMA.TABLES where table_name = 'MHTTable') DROP TABLE MHTTable; ";
                CreateCommand(queryString);
            }
            else
            {
                //建数据库 
                queryString = "CREATE DATABASE " + dbName+";";
                
                CreateCommand(queryString);
            }

            //建表 UserTable
            queryString = useString +
                "CREATE TABLE UserTable (" +
                "USER_ID INT IDENTITY(1,1) PRIMARY KEY," +          //用户ID 自增IDENTITY(1,1)
                "USER_NAME NVARCHAR(50) NOT NULL," +                //用户名    NVARCHAR  unicode
                "PASSWORD NVARCHAR(50)," +                          //密码
                "NONE NCHAR(10)" +                                  //预留字段
                ");";
            CreateCommand(queryString);

            //建表 FileTable
            queryString = useString +
                "CREATE TABLE FileTable (" +
                "FILE_ID INT IDENTITY(1,1) PRIMARY KEY," +          //文件ID
                "FileTag VARCHAR(50) NOT NULL," +                     //Hash值 SHA1(F)
                "FILE_SIZE BIGINT NOT NULL," +                       //文件大小
                "PHYSICAL_ADD NVARCHAR(MAX) NOT NULL, " +            //物理地址
                "MHT_Num BIGINT NOT NULL,"+
                ");";
            CreateCommand(queryString);

            //建表 UpFileTable
            queryString = useString +
                "CREATE TABLE UpFileTable (" +
                "FILE_ID INT," +                                    //文件标识
                "USER_ID  INT NOT NULL," +                          //用户标识    
                "FILE_NAME NVARCHAR(50) NOT NULL," +                //用户上传文件名
                "USER_NAME NVARCHAR(50) NOT NULL," +                //用户登录名
                "UPLOAD_TIME DATETIME NOT NULL, " +                 // 用户上传文件时间
                "ENMD5 VARCHAR(70) NOT NULL" +                   //加密后的文件MD5
                "PRIMARY KEY(USER_ID,FILE_ID)" +
                ");";
            CreateCommand(queryString);

            //建表MHTTable
            queryString = useString +
                "CREATE TABLE MHTTable (" +
                "FILE_ID INT," +                                   
                "MHT_ID  INT NOT NULL," +                             
                "Salt NVARCHAR(50) NOT NULL," +               
                "RootNode NVARCHAR(50) NOT NULL," + 
                "PRIMARY KEY(FILE_ID,MHT_ID)" +
                ");";
            CreateCommand(queryString);

            CreateUser("admin", "123456");
            CreateUser("uheng", "123456");
            CreateUser("guest", "111111");
        }

        private int ExecuteScalar(string queryString)
        {
            int count = 0;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Connection.Open();
                count = Convert.ToInt32(command.ExecuteScalar());
            }
            return count;
        }

        public string GetEnKey(string userName, string fileName)
        {
            queryString = string.Format(useString +
                "SELECT FILE_ID FROM UpFileTable WHERE USER_NAME = '{0}' AND FILE_NAME = '{1}';", userName, fileName);
            int fileID = ExecuteScalar(queryString);
            queryString = string.Format(useString +
                "SELECT ENKEY FROM FileTable WHERE FILE_ID = '{0}';", fileID);
            string enKey = string.Empty;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                reader.Read();
                enKey = (string)reader[0];
                reader.Close();
            }
            return enKey;
        }
        public string GetEnMd5(string userName, string fileName)
        {
            queryString = string.Format(useString +
                "SELECT ENMD5 FROM UpFileTable WHERE USER_NAME = '{0}' AND FILE_NAME = '{1}';", userName, fileName);
            string enMd5 = string.Empty;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                reader.Read();
                enMd5 = (string)reader[0];
                reader.Close();
            }
            return enMd5;
        }
        public int LoginAuthentication(string userName, string passwd)
        {
            int result = 0;
            queryString = useString +
                "SELECT * FROM UserTable " +
                "Where USER_NAME='" + userName + "';";
            result = ExecuteScalar(queryString);
            if (result == 0)
            {
                return -1;
            }

            queryString = useString +
                "SELECT * FROM UserTable " +
                "Where USER_NAME='" + userName + "'" +
               "AND PASSWORD='" + passwd + "';";
            return ExecuteScalar(queryString);
        }

        public FileInfoList GetFileList(string userName)
        {
            queryString = useString +
                "SELECT FILE_NAME, UPLOAD_TIME FROM UpFileTable " +
                "Where USER_NAME='" + userName + "';";

            FileInfoList fil = new FileInfoList();

            using (SqlConnection connection =
                       new SqlConnection(connectionString))
            {
                SqlCommand command =
                    new SqlCommand(queryString, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    fil.nameList.Add((string)reader[0]);
                    string tmp = ((DateTime)reader[1]).ToString();
                    fil.upTimeList.Add(tmp);
                }
                reader.Close();
            }
            return fil;
        }
        public int InsertFile(string userFile, long fileSize, string userName, string enMd5, string sha1, string uploadTime)
        {
            int status = 0; //文件重复
            int fileID;
            queryString = useString +
                "SELECT * FROM FileTable " +
                "Where FileTag='" + sha1 + "';";
            fileID = ExecuteScalar(queryString);

            if (fileID == 0)
                //文件不重复
            {
                string physicalAdd = "./ServerFiles/" + sha1;   //物理地址./ServerFiles/+sha1
                queryString = string.Format(useString +
                    "INSERT INTO FileTable VALUES('{0}', '{1}', '{2}');", sha1, fileSize, physicalAdd);
                ExecuteScalar(queryString);

                /*逻辑可能有问题！！！！！！！！！！！！！！！！！！！！！！*/
                queryString = string.Format(useString +
                    "SELECT * FROM FileTable WHERE FileTag='{0}';", sha1);
                fileID = ExecuteScalar(queryString);
                status = 1;  //云端不存在，需要上传
            }

            //判断同一用户重名文件
            queryString = useString +
                "SELECT * FROM UpFileTable " +
                "WHERE USER_NAME='" + userName + "' " +
                "AND FILE_NAME='" + userFile + "';";
            int res = ExecuteScalar(queryString);
            if (res != 0)   //同一用户只更新
            {
                queryString = string.Format(useString +
                    "UPDATE UpFileTable SET FILE_ID = '{0}', UPLOAD_TIME = '{1}', ENMD5 = '{2}'" +
                    "WHERE FILE_NAME = '{3}' AND USER_NAME = '{4}';", fileID, uploadTime, enMd5, userFile, userName);
                ExecuteScalar(queryString);
            }
            else
            {
                //获取用户ID
                queryString = useString +
                    "SELECT * FROM UserTable " +
                    "Where USER_NAME='" + userName + "';";
                int userID = ExecuteScalar(queryString);
                queryString = string.Format(useString +
                    "INSERT INTO UpFileTable VALUES ({0}, {1}, '{2}', '{3}', '{4}', '{5}');",
                    fileID, userID, userFile, userName, uploadTime, enMd5);
                ExecuteScalar(queryString);
            }
            return status;
        }
        public string GetFilePath(string userName, string fileName)
        {
            queryString = useString +
                "SELECT * FROM UpFileTable " +
                "Where USER_NAME='" + userName + "' " +
               "AND FILE_NAME='" + fileName + "';";
            int fileID = ExecuteScalar(queryString);
            if (fileID <= 0)
                return "";
            queryString = useString +
                "SELECT PHYSICAL_ADD FROM FileTable " +
                "Where FILE_ID='" + fileID + "';";
            string physicalAdd;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                reader.Read();
                physicalAdd = (string)reader[0];
                reader.Close();
            }
            return physicalAdd;
        }
        public int RemoveFile(string userName, string fileName)
        {
            queryString = useString +
                "DELETE  FROM UpFileTable " +
                "Where USER_NAME='" + userName + "' " +
                "AND FILE_NAME='" + fileName + "';";
            return ExecuteScalar(queryString);
        }
        public int RenameFile(string userName, string oldFileName, string fileName)
        {
            queryString = useString +
                "UPDATE UpFileTable " +
                "SET FILE_NAME='" + fileName + "' " +
                "Where USER_NAME='" + userName + "' " +
                "AND FILE_NAME='" + oldFileName + "';";
            return ExecuteScalar(queryString);
        }

        public List<string> GetCloudFiles()
        {
            List<string> cloudFiles = new List<string>();
            queryString = useString +
                "SELECT * FROM FileTable;";
            using (SqlConnection connection =
                    new SqlConnection(connectionString))
            {
                SqlCommand command =
                    new SqlCommand(queryString, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    cloudFiles.Add((string)reader[1] + "       " + ((long)reader[2]).ToString() + "B");
                }
                reader.Close();
            }
            return cloudFiles;
        }
    }
}

