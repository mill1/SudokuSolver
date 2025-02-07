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


            string[] data =
            [
                "4 8  5   ",
                "    7    ",
                "   6 1   ",
                "3        ",
                " 9  1 24 ",
                " 5  846  ",
                "  6     3",
                "1    2 6 ",
                " 7    8 9",
            ];


            new Sudoku(true).Solve(data);
        }
    }
}
