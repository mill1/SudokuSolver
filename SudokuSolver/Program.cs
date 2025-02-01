namespace SudokuSolver
{
    public class Program
    {
        static void Main(string[] args)
        {
            // 5 star page 0
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

            // 4 star page 12
            string[] data12 =
            [
                " 2  5 47 ",
                "    31   ",
                "  1   3 6",
                "   1 8   ",
                "  69  7 8",
                "     29  ",
                "     6 9 ",
                "4     6 7",
                "9 72  8  ",
            ];

            // 5 star page 13
            string[] data13 =
            [
                "     19  ",
                "   546 7 ",
                "   9  12 ",
                "  8      ",
                " 4  5   2",
                "6  7 9   ",
                " 69  4  8",
                " 7 2  4 9",
                "38       ",
            ];

            // 5 star page 15
            string[] data15 =
            [
                "4    9   ",
                "      3  ",
                "5  83 96 ",
                " 5   8 9 ",
                " 7  5    ",
                "6   432 7",
                "7       6",
                "8   64   ",
                "3 52  4 8",
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

            // https://www.livesudoku.com/en/sudoku/evil/

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

            new Sudoku().Solve(dataB);
        }
    }
}
