namespace SudokuSolver.Api.Interfaces
{
    public interface ISudokuService
    {
        public string GetSudoku();
        public int[,] Solve(string sudoku);
    }
}
