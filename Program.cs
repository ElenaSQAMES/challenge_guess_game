using System;
using System.Collections.Generic;
using System.Data.SqlClient;


namespace ConsoleApp_Game
{
    class Program
    {
        private const string connectionString =
         "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\elena\\Documents\\SampleDatabase.mdf";

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
            Console.WriteLine("My hint for computer number is: " + string.Join("", computerNumber));
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
        
    }
}