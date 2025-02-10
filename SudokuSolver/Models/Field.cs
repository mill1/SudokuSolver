namespace SudokuSolverClient.Models
{
    internal class Field(int row, int column, List<Field> fields)
    {
        public int Row { get; } = row;
        public int Column { get; } = column;
        public int Block { get; } = ResolveBlock(row, column);
        public int? Value { get; set; } = null;
        public List<int> Candidates { get; set; } = Enumerable.Range(1, 9).ToList();

        public List<Field> Fields = fields;

        private static int ResolveBlock(int row, int column)
        {
            return row switch
            {
                1 or 2 or 3 => column switch
                {
                    1 or 2 or 3 => 1,
                    4 or 5 or 6 => 2,
                    7 or 8 or 9 => 3,
                    _ => 0,
                },
                4 or 5 or 6 => column switch
                {
                    1 or 2 or 3 => 4,
                    4 or 5 or 6 => 5,
                    7 or 8 or 9 => 6,
                    _ => 0,
                },
                7 or 8 or 9 => column switch
                {
                    1 or 2 or 3 => 7,
                    4 or 5 or 6 => 8,
                    7 or 8 or 9 => 9,
                    _ => 0,
                },
                _ => 0,
            };
        }

        public override string ToString()
        {
            return $"Row: {Row} Col: {Column} Block: {Block} Val: {Value} Cand.: {string.Join(' ', Candidates)}";
        }
    }
}
