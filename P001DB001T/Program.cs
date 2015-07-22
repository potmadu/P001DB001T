using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Diagnostics;

namespace Database_Communication
{
    class Program
    {
        private static string server_name, database_name, table_name;

        private static bool test_connection()
        {

            bool result = false;
            SqlConnection SqlConnection = connect_database();
            if (SqlConnection != null)
            {
                result = true;
            }
            return result;
        }

        private static SqlConnection connect_database()
        {

            SqlConnection sqlConnection1 = new SqlConnection(@"Data Source=" + server_name + ";Initial Catalog = " + database_name + "; Integrated Security = True");
            try
            {
                sqlConnection1.Open();
            }
            catch (SqlException e)
            {
                Console.WriteLine("Cannot connect to Database Server, {0}", e);
                Console.Write("Press <ENTER> to exit "); Console.Read();
                Environment.Exit(0);
            }
            return sqlConnection1;

        }

        private static void disconnect_database(SqlConnection sqlconnection)
        {

            if (sqlconnection != null)
            {
                sqlconnection.Close();
            }

        }

        private static void list_video()
        {

            using (TransactionScope transactionScope = new TransactionScope())
            {
                SqlConnection sqlConnection = connect_database();

                using (SqlCommand command = new SqlCommand())
                {

                    command.Connection = sqlConnection;
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = String.Format("SELECT PkId,Id,Description FROM {0}", table_name);

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        Console.WriteLine("\n List Video in Database");
                        Console.WriteLine("==============================");
                        while (reader.Read())
                        {
                            Console.WriteLine("ID: {0}, Name : {1}, Identifier : {2}", reader.GetValue(0), reader.GetValue(2), reader.GetValue(1));
                        }
                        Console.WriteLine("============================== \n");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{0}", e);
                    }
                }
                disconnect_database(sqlConnection);
                transactionScope.Complete();
            }
        }

        private static void save_video()
        {

            string path, video_name;

            Console.WriteLine("Save Video =========== \n\n");
            Console.Write("Enter Video File path :");
            path = Console.ReadLine();
            Console.Write("Enter Video Name in Database :");
            video_name = Console.ReadLine();
            Console.Write("Saving Video ...");

            using (TransactionScope transactionScope = new TransactionScope())
            {
                SqlConnection sqlConnection = connect_database();
                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = sqlConnection;
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "Insert Into VideoTable([Description],[FileData]) Values(@video_name, (select* from openrowset(bulk '" + path + "', single_blob) as varbinary(max)))";
                    FileInfo f = new FileInfo(path);
                    command.Parameters.AddWithValue("@video_name", video_name+f.Extension);

                    try
                    {
                        int recordsAffected = command.ExecuteNonQuery();
                        Console.WriteLine("Saving success, {0} row(s) affected", recordsAffected);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{0}", e);
                    }

                    disconnect_database(sqlConnection);
                    transactionScope.Complete();
                }
            }

            list_video();

        }

        private static void play_video()
        {

            string video_name;

            Console.WriteLine("Play Video ==========");

            list_video();
            Console.Write("Insert Video Name : ");
            video_name = Console.ReadLine();
            Console.Write("Retrieving Video ...");
            using (TransactionScope transactionScope = new TransactionScope())
            {
                SqlConnection sqlConnection = connect_database();
                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = sqlConnection;
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = String.Format("SELECT FileData.PathName() As Path, GET_FILESTREAM_TRANSACTION_CONTEXT() As TransactionContext FROM {0} WHERE description=@video_name", table_name);
                    command.Parameters.AddWithValue("@video_name", video_name);

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read()) {
                            string filePath = (string) reader["Path"];
                            byte[] transactionContext = (byte[]) reader["TransactionContext"];
                            SqlFileStream sqlFileStream = new SqlFileStream(filePath,transactionContext,FileAccess.Read);
                            byte[] video_data = new byte[sqlFileStream.Length];
                            sqlFileStream.Read(video_data,0,Convert.ToInt32(sqlFileStream.Length));
                            sqlFileStream.Close();

                            string filename = @"D:\Project\Smart Motion Detection\Database Systems\Program\temp\"+video_name+".wmv";
                            FileStream fs = new FileStream(filename,FileMode.Create,FileAccess.Write,FileShare.Write);
                            fs.Write(video_data,0,video_data.Length);
                            fs.Flush();
                            fs.Close();
                            Console.WriteLine("Successfull");

                            Console.Write("Playing Video ...");
                            Process process = new Process();
                            process.StartInfo.FileName = filename;
                            process.Start();

                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{0}", e);
                    }
                    disconnect_database(sqlConnection);
                    transactionScope.Complete();
                }
            }

        }

        private static void show_menu()
        {

            string command = "-1";

            while (command != "0")
            {

                Console.WriteLine("Please Input Specific Command...");
                Console.WriteLine("(1) List Video in Database");
                Console.WriteLine("(2) Save Video to Database");
                Console.WriteLine("(3) Play Video from Database");
                Console.WriteLine("(0) Exit Systems");

                Console.Write("Command : ");
                command = Console.ReadLine();
                Console.Clear();

                switch (command)
                {
                    case "1":
                        list_video();
                        break;
                    case "2":
                        save_video();
                        break;
                    case "3":
                        play_video();
                        break;
                }
            }
        }


        static void Main(string[] args)
        {
            Console.Write("Enter Database Server Name :");
            server_name = Console.ReadLine();
            Console.Write("Enter Database Name :");
            database_name = Console.ReadLine();
            Console.Write("Enter Table Name :");
            table_name = Console.ReadLine();
            Console.WriteLine("Establishing connection with database {0} on server {1} ...", database_name, server_name);
            if (test_connection())
            {
                Console.WriteLine("Connection successfully established. \n");
                show_menu();
                Environment.Exit(0);
            }

        }
    }
}
