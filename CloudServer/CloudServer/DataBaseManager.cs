﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Security.Cryptography;
using NetPublic;

namespace Cloud
{
    class DataBaseManager
    {
        private string connectionString;
        private string queryString;
        private string useString;
        private string dbName;
        private string defalutConnectionString = "Data Source=.;Initial Catalog=master;Integrated Security=True";

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

        public void InitialUser(string userName, string passwd)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] tmp = Encoding.Default.GetBytes(passwd);
            byte[] passMD5 = md5.ComputeHash(tmp);

            string passString = string.Empty;

            foreach (var i in passMD5)
            {
                passString += i.ToString("x2"); //16进制
            }

            string queryString = useString +
                "INSERT INTO UserTable VALUES (" +
                "'" + userName + "'," +
                "'" + passString + "'," +
                "NULL" +
                ");";
            CreateCommand(queryString);
        }

        /*改了**********************/
        public byte CreateUser(string userName, string passwd)
        {
            string queryString = useString +
                "SELECT COUNT(*) FROM UserTable " +
                "WHERE USER_NAME = '" + userName + "';";
            int res = ExecuteScalar(queryString);

            if (res != 0)
            {
                return DefindedCode.ERROR;
            }
            else
            {
                queryString = useString +
                "INSERT INTO UserTable VALUES (" +
                "'" + userName + "'," +
                "'" + passwd + "'," +
                "NULL" +
                ");";
                CreateCommand(queryString);
                return DefindedCode.OK;
            }
        }

        //检查数据库是否存在，若存在则删除所有表并新建空表
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
                queryString = useString + " If exists(SELECT * FROM INFORMATION_SCHEMA.TABLES where table_name = 'UserTable') DROP TABLE UserTable; " +
                    "If exists(SELECT * FROM INFORMATION_SCHEMA.TABLES where table_name = 'UpFileTable') DROP TABLE UpFileTable; " +
                    " If exists(SELECT * FROM INFORMATION_SCHEMA.TABLES where table_name = 'FileTable') DROP TABLE FileTable; " +
                    " If exists(SELECT * FROM INFORMATION_SCHEMA.TABLES where table_name = 'MHTTable') DROP TABLE MHTTable; ";
                CreateCommand(queryString);
            }
            else
            {
                //建数据库 
                queryString = "CREATE DATABASE " + dbName + ";";

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
                "FileTag VARCHAR(512) NOT NULL," +                     //Hash值 SHA1(F)
                "FILE_SIZE BIGINT NOT NULL," +                       //文件大小
                "PHYSICAL_ADD NVARCHAR(MAX) NOT NULL, " +            //物理地址
                "MHT_Num BIGINT NOT NULL," +
                "ENKEY NVARCHAR(512) NOT NULL" +
                ");";
            CreateCommand(queryString);

            //建表 UpFileTable
            queryString = useString +
               "CREATE TABLE UpFileTable (" +
               "FILE_ID INT," +                                    //文件标识
               "USER_ID  INT NOT NULL," +                          //用户标识    
               "FILE_NAME NVARCHAR(256) NOT NULL," +                //用户上传文件名
               "USER_NAME NVARCHAR(50) NOT NULL," +                //用户登录名
               "UPLOAD_TIME DATETIME NOT NULL, " +                 // 用户上传文件时间
               ");";
            CreateCommand(queryString);

            //建表MHTTable
            queryString = useString +
                "CREATE TABLE MHTTable (" +
                "FILE_ID INT," +
                "MHT_ID  INT NOT NULL," +
                "Salt NVARCHAR(100) NOT NULL," +
                "RootNode NVARCHAR(512) NOT NULL," +
                "PRIMARY KEY(FILE_ID,MHT_ID)" +
                ");";
            CreateCommand(queryString);

            InitialUser("admin", "123456");
            InitialUser("uheng", "123456");
            InitialUser("guest", "111111");
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

        //查询该用户某个文件的物理地址
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
        public string GetEnKey(string userName, string fileName)
        {
            queryString = useString +
                "SELECT * FROM UpFileTable " +
                "Where USER_NAME='" + userName + "' " +
               "AND FILE_NAME='" + fileName + "';";
            int fileID = ExecuteScalar(queryString);
            if (fileID <= 0)
                return "";
            queryString = useString +
                "SELECT ENKEY FROM FileTable " +
                "Where FILE_ID='" + fileID + "';";
            string enKey;
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

        public int RemoveFile(string userName, string fileName)
        {
            //删除UpFileTable记录
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

        public int GetFileCountByTag(string fileTag)
        {
            string queryString = $"SELECT FILE_ID FROM FileTable WHERE FileTag = '{fileTag}'";
            return ExecuteScalar(queryString);
        }
        public void InsertFileTable(ref int fileID, string fileName, string fileTag, int MHTNum, long fileSize, string serAdd, string enKey)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // 创建 SQL 查询字符串，使用参数占位符
                string queryString = "INSERT INTO FileTable VALUES(@FileTag, @FileSize, @SerAdd, @MHTNum, @EnKey)";

                // 创建一个 SqlCommand 对象，并将查询字符串和连接对象关联
                using (SqlCommand command = new SqlCommand(queryString, connection))
                {
                    // 添加参数，替代参数占位符
                    command.Parameters.AddWithValue("@FileTag", fileTag);
                    command.Parameters.AddWithValue("@FileSize", fileSize);
                    command.Parameters.AddWithValue("@SerAdd", serAdd);
                    command.Parameters.AddWithValue("@MHTNum", MHTNum);
                    command.Parameters.AddWithValue("@EnKey", enKey);

                    // 打开数据库连接
                    connection.Open();

                    // 执行查询
                    int rowsAffected = command.ExecuteNonQuery();

                    // 处理查询结果（如果需要）
                }
            }

            queryString = string.Format(useString +
                               "SELECT * FROM FileTable WHERE FileTag='{0}';", fileTag);
            fileID = ExecuteScalar(queryString);
        }

        public void InsertMHTTable(int fileID, int MHTID, string salt, string rootNode)
        {
            queryString = string.Format(useString +
                                                "INSERT INTO MHTTable VALUES('{0}', '{1}', '{2}', '{3}');", fileID, MHTID, salt, rootNode);
            ExecuteScalar(queryString);
        }

        public void FindUpFileTable(int fileID, string fileName, string uploadDateTime, string username)
        {
            queryString = string.Format(useString +
                                                             "SELECT * FROM UpFileTable WHERE FILE_NAME='{0}' AND USER_NAME='{1}';", fileName, username);
            int res = ExecuteScalar(queryString);
            if (res == 0)
                InsertUpFileTable(fileID, fileName, uploadDateTime, username);
            else
                UpdateUpFileTable(fileID, fileName, uploadDateTime, username);
        }
        public void InsertUpFileTable(int fileID, string fileName, string uploadDateTime, string username)
        {
            int userid = -1;
            queryString = string.Format(useString +
                                              "SELECT USER_ID FROM UserTable WHERE USER_NAME='{0}';", username);
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                reader.Read();
                userid = (int)reader[0];
                reader.Close();
            }

            queryString = string.Format(useString +
                                         "INSERT INTO UpFileTable VALUES('{0}', '{1}', '{2}', '{3}','{4}');", fileID, userid, fileName, username, uploadDateTime);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                command.ExecuteNonQuery();
            }


        }
        public void UpdateUpFileTable(int fileID, string fileName, string uploadDateTime, string username)
        {
            int userid = -1;
            queryString = string.Format(useString +
                                                             "SELECT USER_ID FROM UserTable WHERE USER_NAME='{0}';", username);
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                reader.Read();
                userid = (int)reader[0];
                reader.Close();
            }

            queryString = string.Format(useString +
                                                        "UPDATE UpFileTable SET FILE_ID = '{0}', UPLOAD_TIME = '{1}'" +
                                                                                                "WHERE FILE_NAME = '{2}' AND USER_NAME = '{3}';", fileID, uploadDateTime, fileName, username);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public int GetMHTNum(string fileTag)
        {
            string queryString = $"SELECT MHT_Num FROM FileTable WHERE FileTag = '{fileTag}'";
            return ExecuteScalar(queryString);
        }

        public string GetSalt(int fileID, int MHTID)
        {
            string queryString = $"SELECT Salt FROM MHTTable WHERE FILE_ID = '{fileID}' AND MHT_ID = '{MHTID}'";
            string salt = string.Empty;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                reader.Read();
                salt = (string)reader[0];
                reader.Close();
            }
            return salt;
        }

        public string GetRootNode(int fileID, int MHTID)
        {
            string queryString = $"SELECT RootNode FROM MHTTABLE WHERE FILE_ID = '{fileID}' AND MHT_ID='{MHTID}'";
            string rootNode = string.Empty;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                reader.Read();
                rootNode = (string)reader[0];
                reader.Close();
            }
            return rootNode;
        }

        public int GetUpfileNum()
        {
            //获取upfileTable的总行数
            int upfileNum = 0;
            queryString = useString +
                "SELECT COUNT(*) FROM UpFileTable;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                upfileNum = Convert.ToInt32(command.ExecuteScalar());

            }
            return upfileNum;
        }
        public int GetFileNum()
        {
            //获取fileTable的总行数
            int fileNum = 0;
            queryString = useString +
                "SELECT COUNT(*) FROM FileTable;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                fileNum = Convert.ToInt32(command.ExecuteScalar());
            }
            return fileNum;
        }
        public List<string> GetUserInfo()
        {
            //获取usertable的id及name，存储到List
            List<string> userInfo = new List<string>();
            queryString = useString +
                "SELECT USER_ID, USER_NAME FROM UserTable;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    userInfo.Add(((int)reader[0]).ToString() + "," + (string)reader[1]);
                }
                reader.Close();
            }
            return userInfo;
        }
        public List<string> GetFileInfo()
        {
            //获取filetable的id,tag,size,seradd，利用id查询upfiletable的name，存储到List
            List<string> fileInfo = new List<string>();
            queryString = useString +
                "SELECT FILE_ID, FileTag, FILE_SIZE, PHYSICAL_ADD FROM FileTable;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int fileID = (int)reader[0];
                    string fileTag = (string)reader[1];
                    long fileSize = (long)reader[2];
                    string serAdd = (string)reader[3];
                    string fileName = string.Empty;
                    string userName = string.Empty;
                    string uploadTime = string.Empty;
                    queryString = useString +
                        "SELECT FILE_NAME, USER_NAME, UPLOAD_TIME FROM UpFileTable " +
                        "Where FILE_ID='" + fileID + "';";
                    using (SqlConnection connection1 = new SqlConnection(connectionString))
                    {
                        SqlCommand command1 = new SqlCommand(queryString, connection1);
                        connection1.Open();
                        SqlDataReader reader1 = command1.ExecuteReader();
                        reader1.Read();
                        fileName = (string)reader1[0];
                        userName = (string)reader1[1];
                        uploadTime = ((DateTime)reader1[2]).ToString();
                        reader1.Close();
                    }
                    fileInfo.Add(fileID.ToString() + "," + fileTag + "," + fileSize.ToString() + "," + serAdd + "," + fileName + "," + userName + "," + uploadTime);
                }
                reader.Close();
            }
            return fileInfo;
        }
    }
}

