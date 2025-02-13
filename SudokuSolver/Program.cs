using System.Net.Http.Headers;
using System.Text.Json;

namespace SudokuSolverClient
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
                        var result = SolveSoduku(args[0]);
                        PrintSudoku(result);
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
                        var result = SolveSoduku(puzzle);
                        PrintSudoku(result);
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

            var request = client.GetAsync("Sudoku");
            List<string> rows = Request(request);

            return rows;
        }

        private static List<string> SolveSoduku(string content)
        {
            using HttpClient client = GetHttpClient();

            var request = client.GetAsync($"Sudoku/Solve?puzzle={content}");
            List<string> rows = Request(request);

            return rows;
        }

        private static void PrintSudoku(List<string> rows)
        {
            foreach (var row in rows)
                WriteLine($"\"{row}\"");

            var oneLiner = string.Join("", rows);
            WriteLine("Sudoku as oneliner:");
            WriteLine($"\"{oneLiner.Replace(" ", "0")}\"");            
        }

        private static List<string> Request(Task<HttpResponseMessage> message)
        {
            var tmp = message.Result;

            var content = message.Result.Content;
            var sudoku = content.ReadAsStringAsync().Result;

            List<string> rows = JsonSerializer.Deserialize<List<string>>(sudoku);

            return rows ?? [];
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
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET application");
            // https://stackoverflow.com/questions/23438416/why-is-httpclient-baseaddress-not-working
            client.BaseAddress = new Uri("https://localhost:44310/");

            return client;
        }
    }
}
