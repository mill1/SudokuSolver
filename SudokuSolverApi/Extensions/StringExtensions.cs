namespace SudokuSolverApi.Extensions
{
    public static class StringExtensions
    {
        public static List<string> SplitStringByLength(this string input, int chunkSize)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < input.Length; i += chunkSize)
            {
                result.Add(input.Substring(i, Math.Min(chunkSize, input.Length - i)));
            }
            return result;
        }
    }
}
