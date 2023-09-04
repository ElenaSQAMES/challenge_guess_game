using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace ConsoleApp_Game
{
    class Program
    {
        private const string connectionString =
         "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\elena\\source\\repos\\challenge_guess_game\\DataBase_Game\\SampleDatabase.mdf";

        static void Main(string[] args)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Check if the "Scores" table already exists before creating it
                    using (var checkTableCommand = new SqlCommand("IF OBJECT_ID('Scores', 'U') IS NULL CREATE TABLE Scores (PlayerName NVARCHAR(50), Attempts INT)", connection))
                    {
                        checkTableCommand.ExecuteNonQuery();

                        ExportScoresToCSV();

                        Console.WriteLine("Press any key to start...");
                        Console.ReadKey();
                    }


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Table creation failed: " + ex.Message);
            }


            int maxAttempts = 10; // Set your desired maximum number of attempts

            bool playAgain = true;
            while (playAgain)
            {
                Console.WriteLine("Welcome to the Number Guessing Game!");
                Console.Write("Please, enter your name: ");
                string playerName = Console.ReadLine();

                int[] computerNumber = GenerateComputerNumber();
                int attempts = 0;

                Console.WriteLine("Try to guess the 4-digit number!");

                while (attempts < maxAttempts)
                {
                    Console.Write("Enter your guess (or 'exit' to escape): ");
                    string input = Console.ReadLine();

                    if (input.ToLower() == "exit")
                    {
                        Console.WriteLine("You've chosen to escape. The number was: " + string.Join("", computerNumber));
                        break;
                    }

                    if (input.Length != 4 || !int.TryParse(input, out int guess))
                    {
                        Console.WriteLine("Invalid input. Please enter a 4-digit number or 'exit' to escape.");
                        continue;
                    }

                    attempts++;

                    if (GuessNumber(computerNumber, guess))
                    {
                        Console.WriteLine("Congratulations, you guessed the number!");
                        Console.WriteLine($"It took you {attempts} attempts.");
                        SaveScore(playerName, attempts);
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"Attempts remaining: {maxAttempts - attempts}");
                    }
                }

                if (attempts >= maxAttempts)
                {
                    Console.WriteLine("Sorry, you've used all your attempts. The number was: " + string.Join("", computerNumber));
                }

                Console.Write("Do you want to play again? (yes/no): ");
                playAgain = Console.ReadLine().ToLower() == "yes";
            }
        }
        static int[] GenerateComputerNumber()
        {
            List<int> digits = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            int[] computerNumber = new int[4];
            Random random = new Random();

            for (int i = 0; i < 4; i++)
            {
                int index = random.Next(0, digits.Count);
                computerNumber[i] = digits[index];
                digits.RemoveAt(index);
            }

            return computerNumber;

        }
        static bool GuessNumber(int[] computerNumber, int guess)
        {
            int[] computerCopy = (int[])computerNumber.Clone();
            int[] guessDigits = new int[4];

            for (int i = 3; i >= 0; i--)
            {
                guessDigits[i] = guess % 10;
                guess /= 10;
            }

            int plusCount = 0;
            int minusCount = 0;

            for (int i = 0; i < 4; i++)
            {
                if (computerCopy[i] == guessDigits[i])
                {
                    plusCount++;
                    computerCopy[i] = -1;
                }
            }

            for (int i = 0; i < 4; i++)
            {
                if (computerCopy[i] != -1)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        if (j != i && computerCopy[j] == guessDigits[i])
                        {
                            minusCount++;
                            computerCopy[j] = -1;
                            break;
                        }
                    }
                }
            }
            Console.WriteLine("The computer generated number is: " + string.Join("", computerNumber));
            Console.WriteLine($"{new string('+', plusCount)}{new string('-', minusCount)}");
            return plusCount == 4;
        }

        static void SaveScore(string playerName, int attempts)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand("INSERT INTO Scores (PlayerName, Attempts) VALUES (@playerName, @attempts)", connection))
                    {
                        command.Parameters.AddWithValue("@playerName", playerName);
                        command.Parameters.AddWithValue("@attempts", attempts);
                        command.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"Saving {playerName}'s score: {attempts} attempts.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Score saving failed: " + ex.Message);
            }
        }
        static void ExportScoresToCSV()
        {
            try
            {
                // Define the directory path on the C drive
                string directoryPath = @"C:\GameScores";

                // Check if the directory exists, and create it if it doesn't
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Define the full path for the CSV file
                string csvFilePath = Path.Combine(directoryPath, "Game_scores.csv");

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand("SELECT PlayerName, Attempts FROM Scores", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            DataTable dataTable = new DataTable();
                            dataTable.Load(reader);

                            using (StreamWriter writer = new StreamWriter(csvFilePath))
                            {
                                // Write the column headers to the CSV file
                                foreach (DataColumn column in dataTable.Columns)
                                {
                                    writer.Write(column.ColumnName);
                                    if (column != dataTable.Columns[dataTable.Columns.Count - 1])
                                    {
                                        writer.Write(",");
                                    }
                                }
                                writer.WriteLine();

                                // Write the data rows to the CSV file
                                foreach (DataRow row in dataTable.Rows)
                                {
                                    for (int i = 0; i < dataTable.Columns.Count; i++)
                                    {
                                        writer.Write(row[i].ToString());
                                        if (i != dataTable.Columns.Count - 1)
                                        {
                                            writer.Write(",");
                                        }
                                    }
                                    writer.WriteLine();
                                }
                            }

                            Console.WriteLine("Scores exported to Game_scores.csv");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exporting scores to CSV failed: " + ex.Message);
            }
        }
        //static void ExportScoresToCSV()
        //{
        //    try
        //    {
        //        using (var connection = new SqlConnection(connectionString))
        //        {
        //            connection.Open();

        //            using (var command = new SqlCommand("SELECT PlayerName, Attempts FROM Scores", connection))
        //            {
        //                using (var reader = command.ExecuteReader())
        //                {
        //                    DataTable dataTable = new DataTable();
        //                    dataTable.Load(reader);

        //                    // Define the your path for the CSV file
        //                    string csvFilePath = @"C:\Game_scores.csv";

        //                    using (StreamWriter writer = new StreamWriter(csvFilePath))
        //                    {
        //                        // Write the column headers to the CSV file
        //                        foreach (DataColumn column in dataTable.Columns)
        //                        {
        //                            writer.Write(column.ColumnName);
        //                            if (column != dataTable.Columns[dataTable.Columns.Count - 1])
        //                            {
        //                                writer.Write(",");
        //                            }
        //                        }
        //                        writer.WriteLine();

        //                        // Write the data rows to the CSV file
        //                        foreach (DataRow row in dataTable.Rows)
        //                        {
        //                            for (int i = 0; i < dataTable.Columns.Count; i++)
        //                            {
        //                                writer.Write(row[i].ToString());
        //                                if (i != dataTable.Columns.Count - 1)
        //                                {
        //                                    writer.Write(",");
        //                                }
        //                            }
        //                            writer.WriteLine();
        //                        }
        //                    }

        //                    Console.WriteLine("Scores exported to scores.csv");
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Exporting scores to CSV failed: " + ex.Message);
        //    }
    }


}