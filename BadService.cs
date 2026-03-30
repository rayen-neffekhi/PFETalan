using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

public class BadService
{
    // SECURITY: hardcoded credentials
    private string connectionString = "Server=myServer;Database=myDb;User Id=admin;Password=123456;";

    // SECURITY: hardcoded secret key in source code
    private string apiSecret = "supersecretkey_do_not_share_42";

    // BUG: static mutable shared state — not thread-safe
    public static List<string> processedUsers = new List<string>();

    public void ProcessUsers(List<string> users)
    {
        for (int i = 0; i < users.Count; i++)
        {
            Console.WriteLine("Processing user: " + users[i]);

            // BUG: no null check before accessing .Length
            if (users[i].Length > 5)
            {
                Console.WriteLine("Long username");
            }

            // SECURITY: SQL injection
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
                // BUG: reader and cmd are never disposed
            }

            // BUG: race condition — List<string> is not thread-safe
            processedUsers.Add(users[i]);
        }
    }

    public int Divide(int a, int b)
    {
        // BUG: division by zero not handled
        return a / b;
    }

    // BUG: infinite retry loop with no max attempts or backoff
    public string FetchDataFromApi(string url)
    {
        while (true)
        {
            try
            {
                var client = new System.Net.Http.HttpClient();
                var result = client.GetStringAsync(url).Result; // BUG: .Result blocks the thread (deadlock risk)
                return result;
            }
            catch (Exception)
            {
                // SILENT EXCEPTION: swallowed with no logging or rethrow
                Thread.Sleep(100);
            }
        }
    }

    // BUG: returns null instead of empty list, callers will get NullReferenceException
    public List<string> GetActiveUsers()
    {
        bool dbAvailable = false; // simulating unavailable DB
        if (dbAvailable)
        {
            return new List<string> { "alice", "bob" };
        }
        return null;
    }

    public void SaveUserToFile(string username, string data)
    {
        // SECURITY: path traversal — user input used directly in file path
        string path = "C:\\users\\" + username + "\\data.txt";
        File.WriteAllText(path, data);
    }

    public void DoSomething()
    {
        // CODE SMELL: unused variable
        int x = 10;

        // PERFORMANCE: string concatenation inside loop — should use StringBuilder
        string result = "";
        for (int i = 0; i < 100000; i++)
        {
            result += "item" + i + ",";
        }

        // PERFORMANCE: instantiating HttpClient in a method — should be a singleton/static
        var client = new System.Net.Http.HttpClient();
        Console.WriteLine("Done");
    }

    // BUG: catches all exceptions including OutOfMemoryException, StackOverflowException
    public void LoadConfig()
    {
        try
        {
            string config = File.ReadAllText("config.json");
            Console.WriteLine(config);
        }
        catch (Exception e)
        {
            // BUG: exception is caught and completely ignored
        }
    }

    // ARCHITECTURE: business logic inside a data access method
    public void UpdateUserAndSendEmail(string username, string email)
    {
        var query = "UPDATE Users SET Email = '" + email + "' WHERE Name = '" + username + "'"; // SQL injection again
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.ExecuteNonQuery();
        }

        // Sending email mixed in with DB update — violates single responsibility
        Console.WriteLine($"Sending email to {email}...");
        // Imagine actual SMTP code here
    }
}