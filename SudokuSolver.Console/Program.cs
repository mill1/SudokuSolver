using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;

namespace SudokuSolver.ConsoleApp
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                    RunUi();
                else
                {
                    if (args.Length == 1)
                        SolveSoduku(args[0]);
                    else
                        throw new ArgumentException($"Expected args.Length: 1, encountered: {args.Length}");
                }
            }
            catch (Exception e)
            {
                WriteLine(e, ConsoleColor.Red);
            }
        }

        private static void RunUi()
        {
            bool running = true;

            do
            {
                PrintOptions();

                var answer = Console.ReadLine()?.ToLower().Trim();

                switch (answer)
                {
                    case "g":
                        var sudoku = GetSudoku();
                        PrintSudoku(sudoku);
                        break;
                    case "s":
                        WriteLine("Enter sudoku to solve:", ConsoleColor.Cyan);
                        WriteLine("E.g.: 000001030231090000065003100678924300103050006000136700009360570006019843300000000", ConsoleColor.Cyan);
                        var puzzle = Console.ReadLine();
                        SolveSoduku(puzzle);
                        break;
                    case "h":
                        WriteLine("Enter the option of choice and select Enter.", ConsoleColor.Cyan);
                        WriteLine("Tip: use 'Get sudoku' for input for 'Solve sudoku'.", ConsoleColor.Cyan);
                        break;
                    case "q":
                        running = false;
                        break;
                    default:
                        WriteLine("Invalid option.");
                        break;
                }
            } while (running);
        }

        private static string GetSudoku()
        {
            using HttpClient client = GetHttpClient();
            
            var content = client.GetAsync("Sudoku").Result.Content;
            var result = content.ReadAsStringAsync().Result;

            return JsonConvert.DeserializeObject<string>(result);
        }

        private static void PrintSudoku(string sudoku)
        {
            var array = ConvertStringToSudokuArray(sudoku);

            WriteLine(Gridify(array), ConsoleColor.Cyan);
            WriteLine("As one line:", ConsoleColor.Cyan);
            WriteLine(sudoku, ConsoleColor.Cyan);
        }

        private static void SolveSoduku(string sudoku)
        {
            using HttpClient client = GetHttpClient();

            var response = client.GetAsync($"Sudoku/Solve?sudoku={sudoku}").Result;

            if (response.IsSuccessStatusCode)
            {                
                var result = JsonConvert.DeserializeObject<int[,]>(response.Content.ReadAsStringAsync().Result);
                var solved = SudokuIsSolved(result);

                WriteLine(solved ? "Solved." : "Not solved. This is how far I got:", solved ? ConsoleColor.Green : ConsoleColor.Magenta);
                WriteLine(Gridify(result), solved ? ConsoleColor.Green : ConsoleColor.Magenta);
            }
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var message = response.Content.ReadAsStringAsync().Result;
                WriteLine($"Bad request: {message}", ConsoleColor.Red);
            }
            if(response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.BadRequest) 
                WriteLine($"Status code: {(int)response.StatusCode} ({response.StatusCode})", ConsoleColor.Red);
        }

        private static bool SudokuIsSolved(int[,] sudoku)
        {
            var flattened = sudoku.Cast<int>().ToList();
            return flattened.TrueForAll(f => f != 0); 
        }

        public static string Gridify(int[,] array)
        {
            var output = string.Empty;

            for (int row = 0; row < 9; row++)
            {
                if (row % 3 == 0)
                    output += row == 0 ? "╔═══╦═══╦═══╗\r\n" : "╠═══╬═══╬═══╣\r\n";

                for (int col = 0; col < 9; col++)
                {
                    if (col % 3 == 0)
                        output += "║";

                    output += array[row, col] == 0 ? " " : array[row, col].ToString();
                }
                output += "║\r\n";
            }
            output += "╚═══╩═══╩═══╝";

            return output;
        }

        private static void PrintOptions()
        {
            WriteLine(new string('-', 25), ConsoleColor.Yellow);
            WriteLine("Make a choice:", ConsoleColor.Yellow);
            WriteLine("g: Get sudoku", ConsoleColor.Yellow);
            WriteLine("s: Solve sudoku", ConsoleColor.Yellow);
            WriteLine("h: Help", ConsoleColor.Yellow);
            WriteLine("q: Quit", ConsoleColor.Yellow);
            WriteLine(new string('-', 25), ConsoleColor.Yellow);
        }

        private static void WriteLine(object obj, ConsoleColor foregroundColor = ConsoleColor.White)
        {
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine(obj);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static int[,] ConvertStringToSudokuArray(string input)
        {
            int[,] grid = new int[9, 9];

            for (int i = 0; i < 81; i++)
                grid[i / 9, i % 9] = input[i] - '0'; // Convert char to int

            return grid;
        }

        private static HttpClient GetHttpClient()
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET application");            
            client.BaseAddress = new Uri("https://localhost:7111/");

            return client;
        }
    }
}