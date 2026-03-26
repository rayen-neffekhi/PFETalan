using System;
using System.Collections.Generic;
using System.Data.SqlClient;

public class BadService
{
    private string connectionString = "Server=myServer;Database=myDb;User Id=admin;Password=123456;";

    public void ProcessUsers(List<string> users)
    {
        for (int i = 0; i < users.Count; i++)
        {
            Console.WriteLine("Processing user: " + users[i]);

            // BUG: possible null reference
            if (users[i].Length > 5)
            {
                Console.WriteLine("Long username");
            }

            // BAD PRACTICE: SQL Injection
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
        }
    }

    public int Divide(int a, int b)
    {
        // BUG: division by zero not handled
        return a / b;
    }

    public void DoSomething()
    {
        // CODE SMELL: unused variable
        int x = 10;

        // PERFORMANCE: inefficient loop
        for (int i = 0; i < 1000000; i++)
        {
            string s = "test" + i;
        }
    }
}