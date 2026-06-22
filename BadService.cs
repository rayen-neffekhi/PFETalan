using System;
using System.Collections.Generic;
using System.Data.SqlClient;

public class UserService
{

    private string connectionString = "Server=myServer;Database=myDb;User Id=admin;Password=123456;";

    public void ProcessUsers(List<string> users)
    {
        if (users == null) throw new ArgumentNullException(nameof(users));

        foreach (var user in users)
        {

            Console.WriteLine("Processing user: " + user);

            if (user.Length > 5)
            {
                Console.WriteLine("Long username");
            }

            var query = "SELECT * FROM Users WHERE Name = @name";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@name", user);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine(reader["Name"]);
                    }
                }
            }
        }
    }

    public int Divide(int a, int b)
    {
        return a / b;
    }

    public void DoSomething()
    {

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 1000000; i++)
        {
            sb.Append("test").Append(i);
            sb.Clear();
        }
    }
}
