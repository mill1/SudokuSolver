namespace SudokuSolver
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Template
            string[] data0 =
            [
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
            ];


            // https://www.sudoku9x9.com/expert/            
            string[] dataA =
            [
                "      3 9",
                "    4  6 ",
                "    26  8",
                "5 1  4 2 ",
                " 7  18   ",
                "  6      ",
                "8     6  ",
                "    95  7",
                "4  3   1 ",
            ];

            // https://www.livesudoku.com/en/sudoku/evil/ = random
            string[] dataB =
            [
                " 8     5 ",
                "    3   8",
                "  491   2",
                "23  7 1  ",
                "4  6  2 5",
                " 7      6",
                "  81 6  9",
                "941      ",
                "         ",
            ];

            string[] data =
            [
                "   5 3624",
                "324  7 16",
                "    24 3 ",
                "1     3  ",
                "2 6 719 4",
                "  9     1",
                "   6834  ",
                "4 271   3",
                " 834 21  ",
            ];

            new Sudoku(true).Solve(data);
        }
    }
}
