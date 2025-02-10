using SudokuSolverClient.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SudokuSolverClient
{
    public static class Program
    {
        static void Main2(string[] args)
        {
            bool debug = false;
            if(args.Length > 0) 
                debug = true;
            
            string[] puzzle =
            [
                "     1 3 ",
                "231 9    ",
                " 65  31  ",
                "6789243  ",
                "1 3 5   6",
                "   1367  ",
                "  936 57 ",
                "  6 19843",
                "3        ",
            ];

            new SudokuSolver(debug).Solve(puzzle);
        }

        static async Task Main()
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            var repositories = await ProcessRepositoriesAsync(client);

            foreach (var repo in repositories)
            {
                Console.WriteLine($"Name: {repo.Name}");
                Console.WriteLine($"Homepage: {repo.Homepage}");
                Console.WriteLine($"GitHub: {repo.GitHubHomeUrl}");
                Console.WriteLine($"Description: {repo.Description}");
                Console.WriteLine($"Watchers: {repo.Watchers:#,0}");
                Console.WriteLine($"{repo.LastPush}");
                Console.WriteLine();
            }
        }

        static async Task<List<Repository>> ProcessRepositoriesAsync(HttpClient client)
        {
            await using Stream stream =
                await client.GetStreamAsync("https://api.github.com/orgs/dotnet/repos");
            var repositories =
                await JsonSerializer.DeserializeAsync<List<Repository>>(stream);
            return repositories ?? new();
        }
    }
}
