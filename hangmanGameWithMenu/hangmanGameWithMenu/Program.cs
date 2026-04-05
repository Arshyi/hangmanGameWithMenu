using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ConsoleHangmanMenu
{
    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            int wordLength = 6;
            int maxWrong = 6;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== HANGMAN ===");
                Console.WriteLine("Menu:");
                Console.WriteLine("  (n) New game");
                Console.WriteLine("  (l) Change word length");
                Console.WriteLine("  (m) Change max wrong guesses");
                Console.WriteLine("  (g) Get current settings");
                Console.WriteLine("  (q) Quit");
                Console.Write("\nYour choice: ");

                string choice = (Console.ReadLine() ?? "").Trim().ToLower();

                if (choice.StartsWith("q"))
                {
                    return;
                }
                else if (choice.StartsWith("g"))
                {
                    Console.WriteLine($"\nCurrent settings:");
                    Console.WriteLine($"  word length = {wordLength}");
                    Console.WriteLine($"  max wrong guesses = {maxWrong}");
                    Pause();
                }
                else if (choice.StartsWith("l"))
                {
                    Console.Write("\nWord length (3 to 12)? ");
                    if (TryGetInt(3, 12, out int newLen, out string err))
                    {
                        wordLength = newLen;
                        Console.WriteLine("Updated word length.");
                    }
                    else
                    {
                        Console.WriteLine(err);
                    }
                    Pause();
                }
                else if (choice.StartsWith("m"))
                {
                    Console.Write("\nMax wrong guesses (1 to 8)? ");
                    if (TryGetInt(1, 8, out int newMax, out string err))
                    {
                        maxWrong = newMax;
                        Console.WriteLine("Updated max wrong guesses.");
                    }
                    else
                    {
                        Console.WriteLine(err);
                    }
                    Pause();
                }
                else if (choice.StartsWith("n"))
                {
                    HangmanGame game = new HangmanGame(wordLength, maxWrong);
                    game.Run();
                    Pause();
                }
                else
                {
                    Console.WriteLine("\nUnknown option.");
                    Pause();
                }
            }
        }

        private static void Pause()
        {
            Console.Write("\nPress Enter to continue...");
            Console.ReadLine();
        }

        private static bool TryGetInt(int min, int max, out int value, out string err)
        {
            value = 0;
            err = "";

            string raw = (Console.ReadLine() ?? "").Trim();
            if (!int.TryParse(raw, out value))
            {
                err = "Not an acceptable number.";
                return false;
            }
            if (value < min || value > max)
            {
                err = "Number outside range.";
                return false;
            }
            return true;
        }
    }

    class HangmanGame
    {
        private readonly int wordLength;
        private readonly int maxWrong;

        private string answer = "";
        private char[] masked;
        private bool[] revealed;

        private readonly HashSet<char> guessed = new HashSet<char>();
        private readonly List<char> wrongGuesses = new List<char>();

        // 2D drawing canvas
        private readonly char[,] canvas;

        public HangmanGame(int wordLength, int maxWrong)
        {
            this.wordLength = wordLength;
            this.maxWrong = maxWrong;

            // A little "screen" for the drawing (2D array)
            canvas = new char[9, 13];
        }

        public void Run()
        {
            answer = WordSource.GetRandomWord(wordLength);
            if (string.IsNullOrWhiteSpace(answer))
            {
                Console.WriteLine("\nCould not load a word.");
                return;
            }

            masked = Enumerable.Repeat('_', answer.Length).ToArray();
            revealed = new bool[answer.Length];

            int wrongCount = 0;

            while (true)
            {
                Console.Clear();

                DrawHangman(wrongCount);
                Render(wrongCount);

                if (IsWin())
                {
                    Console.WriteLine("\n You win!");
                    Console.WriteLine($"Word: {answer.ToUpper()}");
                    return;
                }

                if (wrongCount >= maxWrong)
                {
                    Console.WriteLine("\n Game over!");
                    Console.WriteLine($"Word was: {answer.ToUpper()}");
                    return;
                }

                Console.Write("\nGuess a letter (or type !quit): ");
                string input = (Console.ReadLine() ?? "").Trim().ToLower();

                if (input == "!quit")
                    return;

                if (input.Length != 1 || input[0] < 'a' || input[0] > 'z')
                {
                    Console.WriteLine("Please enter a single letter (a-z).");
                    Pause();
                    continue;
                }

                char guess = input[0];

                if (guessed.Contains(guess))
                {
                    Console.WriteLine("You already guessed that.");
                    Pause();
                    continue;
                }

                guessed.Add(guess);

                bool hit = false;
                for (int i = 0; i < answer.Length; i++)
                {
                    if (answer[i] == guess)
                    {
                        revealed[i] = true;
                        masked[i] = char.ToUpper(guess);
                        hit = true;
                    }
                }

                if (!hit)
                {
                    wrongGuesses.Add(char.ToUpper(guess));
                    wrongCount++;
                }
            }
        }

        private void Render(int wrongCount)
        {
            PrintCanvas();

            Console.WriteLine();

            Console.Write("Word: ");
            for (int i = 0; i < masked.Length; i++)
            {
                Console.Write(masked[i]);
                Console.Write(' ');
            }
            Console.WriteLine();

            Console.WriteLine($"Guessed: {string.Join(" ", guessed.OrderBy(ch => ch).Select(ch => char.ToUpper(ch)))}");
            Console.WriteLine($"Wrong ({wrongGuesses.Count}/{maxWrong}): {string.Join(" ", wrongGuesses)}");
        }

        private bool IsWin()
        {
            for (int i = 0; i < revealed.Length; i++)
                if (!revealed[i]) return false;
            return true;
        }

        private void Pause()
        {
            Console.Write("Press Enter to continue...");
            Console.ReadLine();
        }

        // ===== 2D DRAWING =====

        private void ClearCanvas()
        {
            for (int r = 0; r < canvas.GetLength(0); r++)
                for (int c = 0; c < canvas.GetLength(1); c++)
                    canvas[r, c] = ' ';
        }

        private void DrawHangman(int wrongCount)
        {
            ClearCanvas();

            // scaffold
            PutString(0, 0, "+-----+");
            PutChar(1, 0, '|');
            PutChar(2, 0, '|');
            PutChar(3, 0, '|');
            PutChar(4, 0, '|');
            PutChar(5, 0, '|');
            PutString(6, 0, "+--------");
            PutString(7, 0, "===========");

            // rope
            PutChar(1, 6, '|');

            // body stages (up to 6 by default)
            // 1 head
            if (wrongCount >= 1) PutChar(2, 6, 'O');
            // 2 torso
            if (wrongCount >= 2) PutChar(3, 6, '|');
            // 3 left arm
            if (wrongCount >= 3) PutChar(3, 5, '/');
            // 4 right arm
            if (wrongCount >= 4) PutChar(3, 7, '\\');
            // 5 left leg
            if (wrongCount >= 5) PutChar(4, 5, '/');
            // 6 right leg
            if (wrongCount >= 6) PutChar(4, 7, '\\');

            // If maxWrong > 6, add “extras”
            if (wrongCount >= 7) PutChar(2, 5, '(');  // extra flair
            if (wrongCount >= 8) PutChar(2, 7, ')');  // extra flair
        }

        private void PutChar(int row, int col, char ch)
        {
            if (row < 0 || col < 0 || row >= canvas.GetLength(0) || col >= canvas.GetLength(1))
                return;
            canvas[row, col] = ch;
        }

        private void PutString(int row, int col, string s)
        {
            for (int i = 0; i < s.Length; i++)
                PutChar(row, col + i, s[i]);
        }

        private void PrintCanvas()
        {
            int rows = canvas.GetLength(0);
            int cols = canvas.GetLength(1);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                    Console.Write(canvas[r, c]);
                Console.WriteLine();
            }
        }
    }

    static class WordSource
    {
        private static readonly HttpClient client = new HttpClient();

        public static string GetRandomWord(int length)
        {
            // fallback if web fails
            string[] fallback =
            {
                "banana", "silver", "planet", "castle", "guitar",
                "rocket", "little", "forest", "window", "puzzle",
                "magnet", "butter", "person", "coffee", "kitten"
            };

            try
            {
                string url = "https://raw.githubusercontent.com/dwyl/english-words/master/words_alpha.txt";
                string content = Get(url);

                List<string> candidates = new List<string>();
                foreach (string line in content.Split('\n'))
                {
                    string w = line.Trim().ToLower();
                    if (w.Length == length && w.All(ch => ch >= 'a' && ch <= 'z'))
                        candidates.Add(w);
                }

                Random rnd = new Random();
                if (candidates.Count == 0)
                    return fallback[rnd.Next(fallback.Length)];

                return candidates[rnd.Next(candidates.Count)];
            }
            catch
            {
                return fallback[new Random().Next(fallback.Length)];
            }
        }

        private static string Get(string url) => GetAsync(url).Result;

        private static async Task<string> GetAsync(string url)
        {
            return await client.GetStringAsync(url);
        }
    }
}
