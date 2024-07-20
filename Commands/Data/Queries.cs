using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Commands.Models;

namespace Commands.Data
{
    internal class Queries
    {
        public static List<Gallery> GetGalleriesBySeries(string series)
        {
            // Hardcoded connection string
            string connectionString = "Server=localhost; Database=Kalilo.Services; Integrated Security=true;";

            // SQL Query
            string query =
                @"
            SELECT [GalleryId], [Name]
            FROM [Kalilo.Services].[dbo].[Galleries]
            WHERE Series LIKE @series";

            List<Gallery> galleries = new List<Gallery>();

            // Create and open a connection
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Create a SqlCommand object
                SqlCommand command = new SqlCommand(query, connection);

                // Add the parameter to the SqlCommand object
                command.Parameters.AddWithValue("@series", series);

                try
                {
                    // Open the connection
                    connection.Open();

                    // Execute the SQL command
                    SqlDataReader reader = command.ExecuteReader();

                    // Read the data
                    while (reader.Read())
                    {
                        Gallery gallery = new Gallery()
                        {
                            GalleryId = reader["GalleryId"].ToString(),
                            Name = reader["Name"].ToString()
                        };
                        galleries.Add(gallery);
                    }

                    // Close the reader
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                return galleries;
            }
        }
    }
}
