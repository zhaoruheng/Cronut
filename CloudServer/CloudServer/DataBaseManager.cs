using System;
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
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Connection.Open();
                count = Convert.ToInt32(command.ExecuteScalar());
            }
            return count;
        }

        public string GetEnMd5(string userName, string fileName)
        {
            string queryString = useString + "SELECT ENMD5 FROM UpFileTable WHERE USER_NAME = @UserName AND FILE_NAME = @FileName;";
            string enMd5 = string.Empty;

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Parameters.AddWithValue("@UserName", userName);
                command.Parameters.AddWithValue("@FileName", fileName);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    enMd5 = (string)reader["ENMD5"];
                }
                reader.Close();
            }
            return enMd5;
        }

        public int LoginAuthentication(string userName, string passwd)
        {
            int result = 0;
            string query1 = useString + "SELECT * FROM UserTable WHERE USER_NAME = @UserName1;";

            using (SqlConnection connection = new(connectionString))
            {
                using (SqlCommand command = new(query1, connection))
                {
                    command.Parameters.AddWithValue("@UserName1", userName);
                    connection.Open();
                    object tmp = command.ExecuteScalar();
                    if (tmp == null)
                    {
                        result = 0;
                    }
                    else
                    {
                        result = (int)tmp;
                    }
                }
            }

            if (result == 0)
            {
                return -1;
            }

            string query2 = useString + "SELECT * FROM UserTable WHERE USER_NAME = @UserName2 AND PASSWORD = @Password;";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query2, connection))
                {
                    command.Parameters.AddWithValue("@UserName2", userName);
                    command.Parameters.AddWithValue("@Password", passwd);
                    connection.Open();
                    object tmp= command.ExecuteScalar();
                    if (tmp == null)
                    {
                        return 0;
                    }
                    else
                    {
                        result = (int)tmp;
                    }
                }
            }
            return result;
        }


        public FileInfoList GetFileList(string userName)
        {
            queryString = useString +
                "SELECT FILE_NAME, UPLOAD_TIME FROM UpFileTable " +
                "Where USER_NAME=@UserName;";

            FileInfoList fil = new();

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Parameters.AddWithValue("@UserName", userName); // 使用参数化查询

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
                "Where USER_NAME=@UserName " +
                "AND FILE_NAME=@FileName;";

            int fileID;
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Parameters.AddWithValue("@UserName", userName); // 使用参数化查询
                command.Parameters.AddWithValue("@FileName", fileName); // 使用参数化查询

                connection.Open();
                object tmp = command.ExecuteScalar();
                if (tmp == null)
                {
                    fileID = 0;
                }
                else
                {
                    fileID = (int)tmp;
                }
            }

            if (fileID <= 0)
                return "";

            queryString = useString +
                "SELECT PHYSICAL_ADD FROM FileTable " +
                "Where FILE_ID=@FileID;";

            string physicalAdd;
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Parameters.AddWithValue("@FileID", fileID); // 使用参数化查询

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
                "Where USER_NAME=@UserName " +
               "AND FILE_NAME=@FileName;";

            int fileID;
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Parameters.AddWithValue("@UserName", userName); // 使用参数化查询
                command.Parameters.AddWithValue("@FileName", fileName); // 使用参数化查询

                connection.Open();
                object tmp = command.ExecuteScalar();
                if (tmp == null)
                {
                    fileID = 0;
                }
                else
                {
                    fileID = (int)tmp;
                }
            }

            if (fileID <= 0)
                return "";

            queryString = useString +
                "SELECT ENKEY FROM FileTable " +
                "Where FILE_ID=@FileID;";

            string enKey;
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Parameters.AddWithValue("@FileID", fileID); // 使用参数化查询

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
            string fileId = string.Empty;
            queryString = useString +
                "SELECT FILE_ID FROM UpFileTable " +
                "Where USER_NAME=@UserName " +
                "AND FILE_NAME=@FileName;";
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Parameters.AddWithValue("@UserName", userName); // 使用参数化查询
                command.Parameters.AddWithValue("@FileName", fileName); // 使用参数化查询

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    fileId = ((int)reader[0]).ToString();
                }
                reader.Close();
            }
            //删除UpFileTable记录
            queryString = useString +
                "DELETE FROM UpFileTable " +
                "Where USER_NAME=@UserName " +
                "AND FILE_NAME=@FileName;";
            int reVal = 0;
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Parameters.AddWithValue("@UserName", userName); // 使用参数化查询
                command.Parameters.AddWithValue("@FileName", fileName); // 使用参数化查询

                connection.Open();
                reVal = command.ExecuteNonQuery();
            }


            //如果upfiletable中没有该文件，删除FileTable记录，删除物理文件
            queryString = useString +
                "SELECT * FROM UpFileTable " +
                "Where FILE_NAME=@FileName;";
            string physicalAdd = string.Empty;
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Parameters.AddWithValue("@FileName", fileName); // 使用参数化查询

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    reader.Close();
                    queryString = useString +
                        "SELECT PHYSICAL_ADD FROM FileTable " +
                        "Where FILE_ID=@FileID;";
                    using (SqlConnection connection1 = new(connectionString))
                    {
                        SqlCommand command1 = new(queryString, connection1);
                        command1.Parameters.AddWithValue("@FileID", fileId); // 使用参数化查询

                        connection1.Open();
                        SqlDataReader reader1 = command1.ExecuteReader();
                        if (reader1.Read())
                        {
                            physicalAdd = (string)reader1[0];
                        }
                        reader1.Close();
                    }
                    queryString = useString +
                        "DELETE FROM FileTable " +
                        "Where FILE_ID=@FileID;";
                    using (SqlConnection connection1 = new(connectionString))
                    {
                        SqlCommand command1 = new(queryString, connection1);
                        command1.Parameters.AddWithValue("@FileID", fileId); // 使用参数化查询

                        connection1.Open();
                        command1.ExecuteNonQuery();
                    }
                    System.IO.File.Delete(physicalAdd);
                }
                reader.Close();
            }
            return reVal;
        }


        public int RenameFile(string userName, string oldFileName, string fileName)
        {
            // 更新UpFileTable记录
            queryString = useString +
                "UPDATE UpFileTable " +
                "SET FILE_NAME=@NewFileName " +
                "Where USER_NAME=@UserName " +
                "AND FILE_NAME=@OldFileName;";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@NewFileName", fileName);
                command.Parameters.AddWithValue("@UserName", userName); 
                command.Parameters.AddWithValue("@OldFileName", oldFileName); 

                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

        public List<string> GetCloudFiles()
        {
            List<string> cloudFiles = new();
            queryString = useString +
                "SELECT * FROM FileTable;";
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
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
            string queryString = "SELECT FILE_ID FROM FileTable WHERE FileTag = @FileTag";

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Parameters.AddWithValue("@FileTag", fileTag);

                connection.Open();
                object tmp = command.ExecuteScalar();
                if (tmp == null)
                {
                    return 0;
                }
                else
                {
                    return (int)tmp;
                }
            }
        }

        public void InsertFileTable(ref int fileID, string fileName, string fileTag, int MHTNum, long fileSize, string serAdd, string enKey)
        {
            using (SqlConnection connection = new(connectionString))
            {
                string queryString = "INSERT INTO FileTable VALUES(@FileTag, @FileSize, @SerAdd, @MHTNum, @EnKey)";

                using (SqlCommand command = new(queryString, connection))
                {
                    command.Parameters.AddWithValue("@FileTag", fileTag);
                    command.Parameters.AddWithValue("@FileSize", fileSize);
                    command.Parameters.AddWithValue("@SerAdd", serAdd);
                    command.Parameters.AddWithValue("@MHTNum", MHTNum);
                    command.Parameters.AddWithValue("@EnKey", enKey);

                    connection.Open();

                    int rowsAffected = command.ExecuteNonQuery();
                }
            }

            queryString = string.Format(useString +
                               "SELECT * FROM FileTable WHERE FileTag='{0}';", fileTag);
            fileID = ExecuteScalar(queryString);
        }

        public void InsertMHTTable(int fileID, int MHTID, string salt, string rootNode)
        {
            queryString = useString +
                "INSERT INTO MHTTable VALUES(@FileID, @MHTID, @Salt, @RootNode);";

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Parameters.AddWithValue("@FileID", fileID);
                command.Parameters.AddWithValue("@MHTID", MHTID);
                command.Parameters.AddWithValue("@Salt", salt);
                command.Parameters.AddWithValue("@RootNode", rootNode);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public void FindUpFileTable(int fileID, string fileName, string uploadDateTime, string username)
        {
            queryString = useString + "SELECT * FROM UpFileTable WHERE FILE_NAME=@FileName AND USER_NAME=@UserName;";

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Parameters.AddWithValue("@FileName", fileName);
                command.Parameters.AddWithValue("@UserName", username);

                connection.Open();

                object tmp = command.ExecuteScalar();
                int res;
                if (tmp == null)
                {
                    res = 0;
                }
                else
                {
                    res = (int)tmp;
                }

                if (res == 0)
                {
                    InsertUpFileTable(fileID, fileName, uploadDateTime, username);
                }
                else
                {
                    UpdateUpFileTable(fileID, fileName, uploadDateTime, username);
                }
            }
        }

        public void InsertUpFileTable(int fileID, string fileName, string uploadDateTime, string username)
        {
            int userid = -1;
            string selectQueryString = useString + "SELECT USER_ID FROM UserTable WHERE USER_NAME=@UserName;";

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand selectCommand = new(selectQueryString, connection);
                selectCommand.Parameters.AddWithValue("@UserName", username);

                connection.Open();
                SqlDataReader reader = selectCommand.ExecuteReader();

                if (reader.Read())
                {
                    userid = (int)reader[0];
                }

                reader.Close();
            }

            //按照filename，username查询，如果有重复的则删除
            string deleteQueryString = useString + "DELETE FROM UpFileTable WHERE FILE_NAME=@FileName AND USER_NAME=@UserName;";
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand deleteCommand = new(deleteQueryString, connection);
                deleteCommand.Parameters.AddWithValue("@FileName", fileName);
                deleteCommand.Parameters.AddWithValue("@UserName", username);

                connection.Open();
                deleteCommand.ExecuteNonQuery();
            }

            string insertQueryString = useString + "INSERT INTO UpFileTable VALUES(@FileID, @UserID, @FileName, @UserName, @UploadDateTime);";

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand insertCommand = new(insertQueryString, connection);
                insertCommand.Parameters.AddWithValue("@FileID", fileID);
                insertCommand.Parameters.AddWithValue("@UserID", userid);
                insertCommand.Parameters.AddWithValue("@FileName", fileName);
                insertCommand.Parameters.AddWithValue("@UserName", username);
                insertCommand.Parameters.AddWithValue("@UploadDateTime", uploadDateTime);

                connection.Open();
                insertCommand.ExecuteNonQuery();
            }
        }

        public void UpdateUpFileTable(int fileID, string fileName, string uploadDateTime, string username)
        {
            int userid = -1;
            string selectQueryString = string.Format(useString + "SELECT USER_ID FROM UserTable WHERE USER_NAME = @UserName;");

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand selectCommand = new(selectQueryString, connection);
                selectCommand.Parameters.AddWithValue("@UserName", username);

                connection.Open();
                SqlDataReader reader = selectCommand.ExecuteReader();

                if (reader.Read())
                {
                    userid = (int)reader[0];
                }

                reader.Close();
            }

            string updateQueryString = string.Format(useString + "UPDATE UpFileTable SET FILE_ID = @FileID, UPLOAD_TIME = @UploadTime " +
                                                      "WHERE FILE_NAME = @FileName AND USER_NAME = @UserName;");

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand updateCommand = new(updateQueryString, connection);
                updateCommand.Parameters.AddWithValue("@FileID", fileID);
                updateCommand.Parameters.AddWithValue("@UploadTime", uploadDateTime);
                updateCommand.Parameters.AddWithValue("@FileName", fileName);
                updateCommand.Parameters.AddWithValue("@UserName", username);

                connection.Open();
                updateCommand.ExecuteNonQuery();
            }
        }

        public int GetMHTNum(string fileTag)
        {
            string queryString = "SELECT MHT_Num FROM FileTable WHERE FileTag = @FileTag";

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Parameters.AddWithValue("@FileTag", fileTag);
                connection.Open();
                //return (int)command.ExecuteScalar();
                object tmp = command.ExecuteScalar();
                if (tmp == null)
                {
                    return 0;
                }
                else
                {
                    return (int)(long)tmp;
                }
            }
        }


        public string GetSalt(int fileID, int MHTID)
        {
            string queryString = "SELECT Salt FROM MHTTable WHERE FILE_ID = @FileID AND MHT_ID = @MHTID";
            string salt = string.Empty;

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Parameters.AddWithValue("@FileID", fileID);
                command.Parameters.AddWithValue("@MHTID", MHTID);

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
            string queryString = "SELECT RootNode FROM MHTTABLE WHERE FILE_ID = @FileID AND MHT_ID = @MHTID";
            string rootNode = string.Empty;

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                command.Parameters.AddWithValue("@FileID", fileID);
                command.Parameters.AddWithValue("@MHTID", MHTID);

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
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
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
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
                connection.Open();
                fileNum = Convert.ToInt32(command.ExecuteScalar());
            }
            return fileNum;
        }

        public List<string> GetUserInfo()
        {
            //获取usertable的id及name，存储到List
            List<string> userInfo = new();
            queryString = useString +
                "SELECT USER_ID, USER_NAME FROM UserTable;";
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
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
            // 获取filetable的id, tag, size, seradd，利用id查询upfiletable的name，存储到List
            List<string> fileInfo = new();
            queryString = useString +
                "SELECT FILE_ID, FileTag, FILE_SIZE, PHYSICAL_ADD FROM FileTable;";
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(queryString, connection);
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
                        "Where FILE_ID=@FileID;";
                    using (SqlConnection connection1 = new(connectionString))
                    {
                        SqlCommand command1 = new(queryString, connection1);
                        command1.Parameters.AddWithValue("@FileID", fileID); // 添加参数
                        connection1.Open();
                        SqlDataReader reader1 = command1.ExecuteReader();
                        reader1.Read();
                        if(reader1.HasRows)
                        {
                            fileName = (string)reader1[0];
                            userName = (string)reader1[1];
                            uploadTime = ((DateTime)reader1[2]).ToString();
                        }
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

