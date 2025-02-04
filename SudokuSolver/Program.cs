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
            string[] data =
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

            new Sudoku().Solve(data);
        }
    }
}
