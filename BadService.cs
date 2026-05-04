using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

public class BadService
{
    private string connectionString = "Server=myServer;Database=myDb;User Id=admin;Password=123456;";

    private string apiSecret = "supersecretkey_do_not_share_42";

    public static List<string> processedUsers = new List<string>();

    public void ProcessUsers(List<string> users)
    {
        for (int i = 0; i < users.Count; i++)
        {
            Console.WriteLine("Processing user: " + users[i]);

            if (users[i].Length > 5)
            {
                Console.WriteLine("Long username");
            }

            var query = "SELECT * FROM Users WHERE Name = '" + users[i] + "'";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(query, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine(reader["Name"]);
                }
     
            }

            processedUsers.Add(users[i]);
        }
    }

    public int Divide(int a, int b)
    {
 
        return a / b;
    }

    public string FetchDataFromApi(string url)
    {
        while (true)
        {
            try
            {
                var client = new System.Net.Http.HttpClient();
                var result = await client.GetStringAsync(url); // BUG: .Result blocks the thread (deadlock risk)
                return result;
            }
            catch (Exception)
            {

                Thread.Sleep(100);
            }
        }
    }


    public List<string> GetActiveUsers()
    {
        bool dbAvailable = false; 
        if (dbAvailable)
        {
            return new List<string> { "alice", "bob" };
        }
        return null;
    }

    public void SaveUserToFile(string username, string data)
    {
 
        string path = "C:\\users\\" + username + "\\data.txt";
        File.WriteAllText(path, data);
    }

    public void DoSomething()
    {

        int x = 10;

        string result = "";
        for (int i = 0; i < 100000; i++)
        {
            result += "item" + i + ",";
        }

        var client = new System.Net.Http.HttpClient();
        Console.WriteLine("Done");
    }

    public void LoadConfig()
    {
        try
        {
            string config = File.ReadAllText("config.json");
            Console.WriteLine(config);
        }
        catch (Exception e)
        {

        }
    }

    public void UpdateUserAndSendEmail(string username, string email)
    {
        var query = "UPDATE Users SET Email = '" + email + "' WHERE Name = '" + username + "'"; 
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.ExecuteNonQuery();
        }

        Console.WriteLine($"Sending email to {email}...");

    }
}
