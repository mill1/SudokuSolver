
using SudokuSolver.Api.Exceptions;
using SudokuSolver.Api.Extensions;
using SudokuSolver.Api.Interfaces;
using SudokuSolver.Api.Models;

namespace SudokuSolver.Api.Services
{
    public class SudokuService : ISudokuService
    {
        private readonly Field[,] _fields2D;
        private readonly List<Field> _fields = [];
        private readonly ILogger<SudokuService> _logger;

        public SudokuService(ILogger<SudokuService> logger)
        {
            _logger = logger;

            // Initialize fields
            _fields2D = new Field[9, 9];

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    var field = new Field(row + 1, col + 1, _fields);
                    _fields2D[row, col] = field;
                    _fields.Add(field);
                }
            }
        }

        public string GetSudoku()
        {
            string[] sudokus =
            [
                "000001030231090000065003100678924300103050006000136700009360570006019843300000000",
                "400009000000000300500830960050008090070050000600043207700000006800064000305200408",
                "020004700008200000900600000000008306506300004090500170000000900640001000000000018",
                "904600000000000018020050460500001040400002000000090000080000700051008300000500006",
                "500960004002000080000000003000000207000002000040750006000409000400001302000028005"
            ];

            return sudokus[Random.Shared.Next(0, 5)];
        }

        public int[,] Solve(string puzzle)
        {
            try
            {
                _logger.LogInformation("Solve START", puzzle);

                Initialize(puzzle);
                ValidateInput();
                SolveSudoku();

                var grid = ConvertFieldsToGrid();

                _logger.LogInformation("Solve END", grid);

                return grid;
            }
            catch (Exception e)
            {
                _logger.LogError("e.Message",e);
                throw;
            }
        }

        private void SolveSudoku()
        {
            int _iteration = 0;

            while (!SudokuIsSolved())
            {
                _iteration++;

                _logger.LogDebug($"Iteration: {_iteration}");
                _logger.LogDebug($"{this}");

                if (TryBasicCandidateElimination() > 0) continue;
                if (TryNakedSingles() > 0) continue;
                if (TryHiddenSingles() > 0) continue;
                if (TryNakedSubsets() > 0) continue;
                if (TryHiddenSubsets() > 0) continue;
                if (TryLockedCandidates() > 0) continue;
                if (TryXWing() > 0) continue;
                if (TryYWing() > 0) continue;

                break; // No more eliminations possible
            }
        }

        // 1. Per value try to remove candidates from other fields within the same segment (row, column or block).
        private int TryBasicCandidateElimination()
        {
            int totalCandidatesRemoved = 0;

            foreach (var field in _fields.Where(f => f.Value == null))
            {
                var existingValues = field.OtherRowFields().Select(f => f.Value)
                    .Concat(field.OtherColumnFields().Select(f => f.Value))
                    .Concat(field.OtherBlockFields().Select(f => f.Value))
                    .Where(v => v.HasValue)
                    .Select(v => v.Value)
                    .Distinct()
                    .ToList();

                foreach (var value in existingValues)                
                    totalCandidatesRemoved += RemoveCandidateFromField(field, value, "Strategy 1");                                    
            }

            return totalCandidatesRemoved;
        }

        // 2. When a field has only one candidate left.
        private int TryNakedSingles()
        {
            int nrOfValuesSet = 0;

            foreach (var field in _fields.Where(f => f.Candidates.Count == 1 && !f.Value.HasValue))
            {
                int value = field.Candidates.First();
                field.Value = value;
                field.Candidates.Clear();
                nrOfValuesSet++;

                _logger.LogDebug($"Found solution (naked singles): {field}");

                // Remove this value from candidates in the same row, column, and block
                RemoveCandidateFromFields(field.OtherPeers(), value, "Strategy 2");

            }

            return nrOfValuesSet;
        }

        // 3. When a candidate appears only once in a segment, even if other candidates exist.
        private int TryHiddenSingles()
        {
            int nrOfValuesSet = 0;

            for (int i = 1; i <= 9; i++)
            {
                nrOfValuesSet += FindHiddenSinglesInSegment(_fields.Rows(i));
                nrOfValuesSet += FindHiddenSinglesInSegment(_fields.Columns(i));
                nrOfValuesSet += FindHiddenSinglesInSegment(_fields.Blocks(i));
            }

            return nrOfValuesSet;
        }

        private int FindHiddenSinglesInSegment(IEnumerable<Field> segment)
        {
            int nrOfValuesSet = 0;

            for (int value = 1; value <= 9; value++)
            {
                var candidateFields = segment.Where(f => f.Candidates.Contains(value)).ToList();

                // If the candidate appears in exactly one field, it's a Hidden Single
                if (candidateFields.Count == 1)
                {
                    var field = candidateFields.First();

                    if (!field.Value.HasValue)
                    {
                        field.Value = value;
                        field.Candidates.Clear();
                        nrOfValuesSet++;

                        _logger.LogDebug($"Found solution (hidden singles): {field}");
                    }

                    // Remove this value from candidates in the same row, column, and block
                    RemoveCandidateFromFields(field.OtherPeers(), value, "Strategy 3");
                }
            }

            return nrOfValuesSet;
        }

        // 4. Remove shared candidates from other cells when multiple cells have the exact same candidates.
        private int TryNakedSubsets()
        {
            int nrOfCandidatesRemoved = 0;

            for (int candidateCount = 2; candidateCount <= 4; candidateCount++)
            {
                for (int i = 1; i <= 9; i++)
                {
                    nrOfCandidatesRemoved += FindFieldsWithSimilarCandidates(_fields.Blocks(i), candidateCount);
                    nrOfCandidatesRemoved += FindFieldsWithSimilarCandidates(_fields.Rows(i), candidateCount);
                    nrOfCandidatesRemoved += FindFieldsWithSimilarCandidates(_fields.Columns(i), candidateCount);
                }
            }

            return nrOfCandidatesRemoved;
        }

        private int FindFieldsWithSimilarCandidates(IEnumerable<Field> fields, int candidateCount)
        {
            int nrOfCandidatesRemoved = 0;

            // Generate candidate combinations based on the input candidateCount
            var candidateCombinations = GetCombinations(Enumerable.Range(1, 9).ToList(), candidateCount);

            foreach (var combination in candidateCombinations)
            {
                var matchingFields = fields.Where(f => combination.TrueForAll(c => f.Candidates.Contains(c))).ToList();

                // If exactly candidateCount fields match the combination, check further
                if (matchingFields.Count == candidateCount)
                {
                    // Ensure that each candidate appears exactly candidateCount times
                    bool validCombination = true;
                    foreach (var candidate in combination)
                    {
                        if (fields.Count(f => f.Candidates.Contains(candidate)) != candidateCount)
                        {
                            validCombination = false;
                            break;
                        }
                    }

                    if (validCombination)
                    {
                        // Remove other candidates from the matching fields
                        foreach (var field in matchingFields)
                        {
                            var candidates = field.Candidates.ToList();
                            foreach (int candidate in candidates)
                            {
                                if (!combination.Contains(candidate))
                                {
                                    
                                    RemoveCandidateFromField(field, candidate, "Strategy 4");
                                    nrOfCandidatesRemoved += 1;
                                }
                            }
                        }
                    }
                }
            }
            return nrOfCandidatesRemoved;
        }

        // 5. When candidates appear in exactly the same number of cells, remove other candidates from those cells
        private int TryHiddenSubsets()
        {
            int nrOfCandidatesRemoved = 0;

            for (int subsetSize = 2; subsetSize <= 4; subsetSize++)
            {
                nrOfCandidatesRemoved += TryHiddenSubset(subsetSize);
            }

            return nrOfCandidatesRemoved;
        }

        private int TryHiddenSubset(int subsetSize)
        {
            int nrOfCandidatesRemoved = 0;

            for (int i = 1; i <= 9; i++)
            {
                nrOfCandidatesRemoved += FindHiddenSubsetsInSegment(_fields.Rows(i), subsetSize);
                nrOfCandidatesRemoved += FindHiddenSubsetsInSegment(_fields.Columns(i), subsetSize);
                nrOfCandidatesRemoved += FindHiddenSubsetsInSegment(_fields.Blocks(i), subsetSize);
            }

            return nrOfCandidatesRemoved;
        }

        private int FindHiddenSubsetsInSegment(IEnumerable<Field> segment, int subsetSize)
        {
            int nrOfCandidatesRemoved = 0;

            // Generate all combinations of candidates of the given subset size
            var candidateSubsets = GetCombinations(Enumerable.Range(1, 9).ToList(), subsetSize);

            foreach (var subset in candidateSubsets)
            {
                // Find all fields that contain at least one candidate from the subset
                var fieldsWithSubsetCandidates = segment.Where(f => subset.Exists(c => f.Candidates.Contains(c))).ToList();

                // If exactly 'subsetSize' fields contain these candidates, we have a hidden subset
                if (fieldsWithSubsetCandidates.Count == subsetSize &&
                    subset.All(c => fieldsWithSubsetCandidates.Exists(f => f.Candidates.Contains(c))))
                {
                    foreach (var field in fieldsWithSubsetCandidates)
                    {
                        // Remove any candidates that are not part of the hidden subset
                        var extraCandidates = field.Candidates.Except(subset).ToList();
                        foreach (var candidate in extraCandidates)
                        {
                            nrOfCandidatesRemoved += RemoveCandidateFromField(field, candidate, "Strategy 5");
                        }
                    }
                }
            }

            return nrOfCandidatesRemoved;
        }

        // 6. When candidates are restricted to a block within a row/column, remove them from the block outside the row/column.
        private int TryLockedCandidates()
        {
            int nrOfCandidatesRemoved = 0;

            // Apply Pointing for each block
            for (int block = 1; block <= 9; block++)
            {
                var fieldsInBlock = _fields.Blocks(block);

                for (int value = 1; value <= 9; value++)
                {
                    var candidateFields = fieldsInBlock.Where(f => f.Candidates.Contains(value)).ToList();

                    // POINTING: Check if candidates are in a single row or column within the block
                    var lockedRow = GetLockedRow(candidateFields);
                    if (lockedRow.HasValue)
                        nrOfCandidatesRemoved += EliminateFromRowOutsideBlock(block, lockedRow.Value, value);

                    var lockedColumn = GetLockedColumn(candidateFields);
                    if (lockedColumn.HasValue)
                        nrOfCandidatesRemoved += EliminateFromColumnOutsideBlock(block, lockedColumn.Value, value);
                }
            }

            // Apply Claiming for each row and column
            for (int i = 1; i <= 9; i++)
            {
                for (int value = 1; value <= 9; value++)
                {
                    var rowFields = _fields.Rows(i).Where(f => f.Candidates.Contains(value)).ToList();
                    var lockedBlockRow = GetLockedBlock(rowFields);
                    if (lockedBlockRow.HasValue)
                        nrOfCandidatesRemoved += EliminateFromBlockOutsideLine(_fields.Blocks(lockedBlockRow.Value), rowFields, value);

                    var colFields = _fields.Columns(i).Where(f => f.Candidates.Contains(value)).ToList();
                    var lockedBlockCol = GetLockedBlock(colFields);
                    if (lockedBlockCol.HasValue)
                        nrOfCandidatesRemoved += EliminateFromBlockOutsideLine(_fields.Blocks(lockedBlockCol.Value), colFields, value);
                }
            }

            return nrOfCandidatesRemoved;
        }

        private int? GetLockedRow(List<Field> candidateFields)
        {
            if (!candidateFields.Any()) return null;
            int row = candidateFields[0].Row;
            return candidateFields.TrueForAll(f => f.Row == row) ? row : null;
        }

        private int? GetLockedColumn(List<Field> candidateFields)
        {
            if (!candidateFields.Any()) return null;
            int column = candidateFields[0].Column;
            return candidateFields.TrueForAll(f => f.Column == column) ? column : null;
        }

        private int? GetLockedBlock(List<Field> candidateFields)
        {
            if (!candidateFields.Any()) return null;
            int block = candidateFields[0].Block;
            return candidateFields.TrueForAll(f => f.Block == block) ? block : null;
        }

        // Eliminate candidate from row outside the block (Pointing)
        private int EliminateFromRowOutsideBlock(int block, int row, int value)
        {
            var fieldsOutsideBlock = _fields.Rows(row).Where(f => f.Block != block);
            return RemoveCandidateFromFields(fieldsOutsideBlock, value, "Strategy 6");
        }

        // Eliminate candidate from column outside the block (Pointing)
        private int EliminateFromColumnOutsideBlock(int block, int column, int value)
        {
            var fieldsOutsideBlock = _fields.Columns(column).Where(f => f.Block != block);
            return RemoveCandidateFromFields(fieldsOutsideBlock, value, "Strategy 6");
        }

        // Eliminate candidate from block outside the row/column (Claiming)
        private int EliminateFromBlockOutsideLine(IEnumerable<Field> blockFields, List<Field> lineFields, int value)
        {
            var fieldsOutsideLine = blockFields.Except(lineFields);
            return RemoveCandidateFromFields(fieldsOutsideLine, value, "Strategy 6");
        }

        // 7. Google: Sudoku X-Wing strategy explained
        private int TryXWing()
        {
            int totalRemoved = 0;
            for (int candidate = 1; candidate <= 9; candidate++)
            {
                totalRemoved += FindXWingPatterns(candidate, isRowBased: true);
                totalRemoved += FindXWingPatterns(candidate, isRowBased: false);
            }
            return totalRemoved;
        }

        private int FindXWingPatterns(int candidate, bool isRowBased)
        {
            var linePatterns = Enumerable.Range(0, 9)
                .Select(line => new { Line = line, Positions = GetCandidatePositions(line, candidate, isRowBased) })
                .Where(p => p.Positions.Count == 2)
                .ToList();

            int removed = 0;
            for (int i = 0; i < linePatterns.Count - 1; i++)
            {
                for (int j = i + 1; j < linePatterns.Count; j++)
                {
                    if (linePatterns[i].Positions.SequenceEqual(linePatterns[j].Positions))
                    {
                        removed += EliminateXWingCandidates(linePatterns[i].Positions, linePatterns[i].Line, linePatterns[j].Line, candidate, isRowBased);
                    }
                }
            }
            return removed;
        }

        private List<int> GetCandidatePositions(int line, int candidate, bool isRowBased)
        {
            return Enumerable.Range(0, 9)
                .Where(pos => (isRowBased ? _fields2D[line, pos] : _fields2D[pos, line]).Candidates.Contains(candidate))
                .ToList();
        }

        private int EliminateXWingCandidates(List<int> positions, int line1, int line2, int candidate, bool isRowBased)
        {
            int removed = 0;
            foreach (var pos in positions)
            {
                for (int otherLine = 0; otherLine < 9; otherLine++)
                {
                    if (otherLine != line1 && otherLine != line2)
                    {
                        var field = isRowBased ? _fields2D[otherLine, pos] : _fields2D[pos, otherLine];
                        removed += RemoveCandidateFromField(field, candidate, "Strategy 7");
                    }
                }
            }
            return removed;
        }

        // 8. Google: Sudoku Y-Wing or XY-Wing strategy explained. A pivot has two pincers.
        private int TryYWing()
        {
            int totalCandidatesRemoved = 0;

            // Get all fields with exactly 2 candidates
            var fieldsWithTwoCandidates = _fields.WithNumberOfCandidates(2);

            foreach (var pivot in fieldsWithTwoCandidates)
            {
                var (x, y) = (pivot.Candidates[0], pivot.Candidates[1]);

                // Find potential pincers for both candidates
                var pincer1Candidates = GetPincerPossibilities(pivot, x, fieldsWithTwoCandidates);
                var pincer2Candidates = GetPincerPossibilities(pivot, y, fieldsWithTwoCandidates);

                // Identify the third candidate (z) that both pincers share
                var potentialThirdValues = Enumerable.Range(1, 9).Except(new[] { x, y });

                foreach (var z in potentialThirdValues)
                {
                    var pincer1s = pincer1Candidates.Where(f => f.Candidates.Contains(z));
                    var pincer2s = pincer2Candidates.Where(f => f.Candidates.Contains(z));

                    // If valid pincers are found, attempt to remove z from intersecting fields
                    if (pincer1s.Any() && pincer2s.Any())
                    {
                        foreach (var pincer1 in pincer1s)
                        {
                            foreach (var pincer2 in pincer2s)
                            {
                                totalCandidatesRemoved += CheckYWing(pivot, pincer1, pincer2, z);
                                if (totalCandidatesRemoved > 0) return totalCandidatesRemoved;  // Early exit on success
                            }
                        }
                    }
                }
            }

            return totalCandidatesRemoved;
        }

        private int CheckYWing(Field pivot, Field pincer1, Field pincer2, int candidateToRemove)
        {
            int nrOfCandidatesRemoved = 0;

            // Find fields that intersect with both pincers and remove the z candidate
            var fieldsToCheck = _fields.Except([pivot, pincer1, pincer2]).Where(f => f.Candidates.Contains(candidateToRemove));

            foreach (var field in fieldsToCheck)
            {
                if (pincer1.IntersectsWith(field) && pincer2.IntersectsWith(field))
                {
                    RemoveCandidateFromField(field, candidateToRemove, "Strategy 8");
                    nrOfCandidatesRemoved++;
                }
            }
            return nrOfCandidatesRemoved;
        }

        private IEnumerable<Field> GetPincerPossibilities(Field pivot, int candidate, IEnumerable<Field> fieldsWithTwoCandidates)
        {
            return fieldsWithTwoCandidates
                .Where(f => f != pivot && f.Candidates.Contains(candidate) && pivot.IntersectsWith(f));
        }

        // Helper method to generate combinations of a specific length
        public static List<List<int>> GetCombinations(List<int> list, int length)
        {
            return (length == 0)
                ? new List<List<int>> { new List<int>() }
                : list.SelectMany((e, i) =>
                    GetCombinations(list.Skip(i + 1).ToList(), length - 1)
                        .Select(c => new List<int> { e }.Concat(c).ToList())
                  ).ToList();
        }

        // Initialize the puzzle
        private void Initialize(string puzzle)
        {
            var data = puzzle.SplitStringByLength(9).ToArray();

            // Check if the input has exactly 9 rows and 9 columns
            if (data.Length != 9 || data.Any(row => row.Length != 9))
                throw new InvalidPuzzleException("Expected 9x9 grid.");

            // Ensure all characters are digits 1-9 or spaces (indicating empty cells)
            if (data.Any(row => row.Any(ch => !char.IsDigit(ch))))
                throw new InvalidPuzzleException("Allowed characters: 0-9");

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (data[row][col] != '0')
                    {
                        var value = int.Parse(data[row][col].ToString());
                        _fields2D[row, col].Value = value;
                        _fields2D[row, col].Candidates = [];
                    }
                }
            }
        }

        private void ValidateInput()
        {
            // Check if the puzzle has at least 17 clues
            if (_fields.Count(f => f.Value.HasValue) < 17)
                throw new InvalidPuzzleException("Minimum number of clues: 17");

            // Validate rows, columns, and blocks
            ValidateSegments();
        }

        private void ValidateSegments()
        {
            for (int i = 1; i <= 9; i++)
            {
                if (!HasUniqueNumbers(_fields.Rows(i)))
                    throw new InvalidPuzzleException($"Row {i}: duplicate values found.");
                if (!HasUniqueNumbers(_fields.Columns(i)))
                    throw new InvalidPuzzleException($"Column {i}: duplicate values found.");
                if (!HasUniqueNumbers(_fields.Blocks(i)))
                    throw new InvalidPuzzleException($"Block {i}: duplicate values found.");
            }
        }

        private bool HasUniqueNumbers(IEnumerable<Field> fields)
        {
            var numbers = fields.Where(f => f.Value.HasValue && f.Value != 0)
                                 .Select(f => f.Value.Value)
                                 .ToList();

            return numbers.Distinct().Count() == numbers.Count;
        }


        private bool SudokuIsSolved()
        {
            return _fields.TrueForAll(f => f.Value.HasValue);
        }

        private int[,] ConvertFieldsToGrid()
        {
            int[,] grid = new int[9, 9];

            foreach (var field in _fields)
            {
                int rowIndex = field.Row - 1;
                int colIndex = field.Column - 1;

                grid[rowIndex, colIndex] = field.Value ?? 0;
            }

            return grid;
        }

        private int RemoveCandidateFromFields(IEnumerable<Field> fields, int candidate, string strategie)
        {
            int nrOfCandidatesRemoved = 0;

            foreach (var field in fields)
                nrOfCandidatesRemoved += RemoveCandidateFromField(field, candidate, strategie);

            return nrOfCandidatesRemoved;
        }

        private int RemoveCandidateFromField(Field field, int candidate, string strategie)
        {
            if (field.Candidates.Contains(candidate))
            {
                _logger.LogDebug($"{strategie}: remove candidate {candidate} from {field}");
                var remove = field.Candidates.Single(c => c == candidate);
                field.Candidates.Remove(remove);

                if (field.Candidates.Count == 0)
                    throw new InvalidOperationException($"No candidates left after removing value {candidate}. Field: {field}");

                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}