using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace SudokuSolver.AppConsole
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
                    {
                        SolveSoduku(args[0]);
                    }
                    else
                    {
                        throw new ArgumentException($"Expected args.Length: 1, encountered: {args.Length}");
                    }
                }
            }
            catch (Exception e)
            {
                WriteLine(e, ConsoleColor.Red);
            }
        }

        private static void RunUi()
        {
            PrintHeader();

            bool running = true;

            do
            {
                PrintOptions();

                var answer = Console.ReadLine()?.ToLower().Trim();

                switch (answer)
                {
                    case "g":
                        var sudoku = GetSoduku();
                        PrintSudoku(sudoku);
                        break;
                    case "s":
                        WriteLine("Enter sudoku to solve as oneliner:");
                        WriteLine("E.g.: 000001030231090000065003100678924300103050006000136700009360570006019843300000000");
                        var puzzle = Console.ReadLine();
                        SolveSoduku(puzzle);
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

        private static List<string> GetSoduku()
        {
            using HttpClient client = GetHttpClient();
            
            var content = client.GetAsync("Sudoku").Result.Content;
            var result = content.ReadAsStringAsync().Result;

            List<string> rows = JsonConvert.DeserializeObject<List<string>>(result);

            return rows ?? [];
        }

        private static void SolveSoduku(string puzzle)
        {
            using HttpClient client = GetHttpClient();

            var result = client.GetAsync($"Sudoku/Solve?puzzle={puzzle}").Result;

            if (result.IsSuccessStatusCode)
            {
                WriteLine("!"); // stel vast opgelost of niet (code heb je al?)

                var x = result.Content.ReadAsStringAsync().Result;

                var sudoku = JsonConvert.DeserializeObject<int[,]>(x);
                WriteLine(PrintIt(sudoku), ConsoleColor.Green);
            }
            if (result.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var message = result.Content.ReadAsStringAsync().Result;
                WriteLine($"Bad request: {message}", ConsoleColor.Red);
            }
            WriteLine($"Status code: {(int)result.StatusCode} ({result.StatusCode})", ConsoleColor.Red);
        }

        public static string PrintIt(int[,] _fields2D)
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

                    output += _fields2D[row, col] == null ? " " : _fields2D[row, col].ToString();
                }
                output += "║\r\n";
            }
            output += "╚═══╩═══╩═══╝";

            return output;
        }

        private static void PrintSudoku(List<string> rows)
        {
            foreach (var row in rows)
                WriteLine($"\"{row}\"");

            var oneLiner = string.Join("", rows);
            WriteLine("Sudoku as oneliner:");
            WriteLine($"\"{oneLiner.Replace(" ", "0")}\"");            
        }

        private static void PrintOptions()
        {
            WriteLine("Make a choice:", ConsoleColor.Blue);
            WriteLine("g: Get sudoku", ConsoleColor.Blue);
            WriteLine("s: Solve sudoku", ConsoleColor.Blue);
            WriteLine("q: Quit", ConsoleColor.Blue);
        }

        private static void PrintHeader()
        {
            WriteLine(new string('-', 25), ConsoleColor.Blue);
            WriteLine("Soduku solver", ConsoleColor.Blue);
            WriteLine(new string('-', 25), ConsoleColor.Blue);
        }

        private static void WriteLine(object obj, ConsoleColor foregroundColor = ConsoleColor.White, ConsoleColor backgroundColor = ConsoleColor.Black)
        {
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;
            Console.WriteLine(obj);
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        private static HttpClient GetHttpClient()
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET application");            
            client.BaseAddress = new Uri("https://localhost:44310/");

            return client;
        }
    }
}
