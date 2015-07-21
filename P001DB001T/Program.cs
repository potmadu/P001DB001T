﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Data.SqlClient;

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
                    command.CommandText = String.Format("SELECT * FROM {0}", table_name);

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
            Console.Write("Enter Video Name :");
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
                    command.Parameters.AddWithValue("@video_name", video_name);

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

        private static void play_video(string name)
        {



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
                        list_video();
                        Console.WriteLine("Input video name :");
                        string video_name = Console.ReadLine();
                        play_video(video_name);
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
