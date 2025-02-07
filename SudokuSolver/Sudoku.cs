using SudokuSolver.Extensions;

namespace SudokuSolver
{    
    public class Sudoku
    {
        private readonly Field[,] _fields2D;
        private readonly List<Field> _fields = [];

        public Sudoku() : this(false)
        {                
        }

        public Sudoku(bool debug)
        {
            Settings.Debug = debug;

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

        public bool Solve(string[] data)
        {
            try
            {
                Initialize(data);
                var solved = SolveSudoku(); 
                PrintResult(solved);

                return solved;
            }
            catch (Exception e)
            {
                WriteLine(e, ConsoleColor.Red);
                return false;
            }
        }

        public bool SolveSudoku()
        {
            bool useAdvancedStrategies = true;

            while (!SudokuIsSolved())
            {
                if (TryBasicCandidateElimination() > 0) continue;
                if (TryNakedSingles() > 0) continue;
                if (TryHiddenSingles() > 0) continue;
                if (TryNakedSubsets() > 0) continue;
                if (TryHiddenSubsets() > 0) continue;

                if (useAdvancedStrategies)
                {
                    if (TryLockedCandidates() > 0) continue;

                    if (TryXWing() > 0) continue;
                    if (TryXYZWing() > 0) continue;
                    if (TrySwordfish() > 0) continue;
                    if (TryYWing() > 0) continue;

                    //if (TryJellyfish() > 0) continue;
                    //if (TryFinnedXWing() > 0) continue;
                    //if (TryForcingChains() > 0) continue;
                    //if (TryAlternatingInferenceChains() > 0) continue;
                    //if (TryUniqueRectangles() > 0) continue;
                }

                break; // No more eliminations possible
            }

            return SudokuIsSolved();
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
                    totalCandidatesRemoved += field.RemoveValueFromCandidates(value);
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

                if (Settings.Debug)
                    WriteLine($"Found solution (naked singles): {field}", ConsoleColor.Yellow, ConsoleColor.DarkBlue);

                // Remove this value from candidates in the same row, column, and block
                field.OtherRowFields().RemoveValueFromCandidates(value);
                field.OtherColumnFields().RemoveValueFromCandidates(value);
                field.OtherBlockFields().RemoveValueFromCandidates(value);
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

        private static int FindHiddenSinglesInSegment(IEnumerable<Field> segment)
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

                        if (Settings.Debug)
                            WriteLine($"Found solution (hidden singles): {field}", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
                    }

                    // Remove this value from candidates in the same row, column, and block
                    field.OtherRowFields().RemoveValueFromCandidates(value);
                    field.OtherColumnFields().RemoveValueFromCandidates(value);
                    field.OtherBlockFields().RemoveValueFromCandidates(value);
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
                    nrOfCandidatesRemoved += CheckFieldsWithSimilarCandidates(_fields.Blocks(i), candidateCount);
                    nrOfCandidatesRemoved += CheckFieldsWithSimilarCandidates(_fields.Rows(i), candidateCount);
                    nrOfCandidatesRemoved += CheckFieldsWithSimilarCandidates(_fields.Columns(i), candidateCount);
                }
            }

            return nrOfCandidatesRemoved;
        }

        private static int CheckFieldsWithSimilarCandidates(IEnumerable<Field> fields, int candidateCount)
        {
            int nrOfCandidatesRemoved = 0;

            // Generate candidate combinations based on the input candidateCount
            var candidateCombinations = GetCombinations(Enumerable.Range(1, 9).ToList(), candidateCount);

            foreach (var combination in candidateCombinations)
            {
                var matchingFields = fields.Where(f => combination.All(c => f.Candidates.Contains(c))).ToList();

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
                                    field.RemoveValueFromCandidates(candidate);
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
                var fieldsWithSubsetCandidates = segment.Where(f => subset.Any(c => f.Candidates.Contains(c))).ToList();

                // If exactly 'subsetSize' fields contain these candidates, we have a hidden subset
                if (fieldsWithSubsetCandidates.Count == subsetSize &&
                    subset.All(c => fieldsWithSubsetCandidates.Count(f => f.Candidates.Contains(c)) >= 1))
                {
                    foreach (var field in fieldsWithSubsetCandidates)
                    {
                        // Remove any candidates that are not part of the hidden subset
                        var extraCandidates = field.Candidates.Except(subset).ToList();
                        foreach (var candidate in extraCandidates)
                        {
                            nrOfCandidatesRemoved += field.RemoveValueFromCandidates(candidate);
                        }
                    }
                }
            }

            return nrOfCandidatesRemoved;
        }

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

        // Helper: Get locked row if all candidate fields are in a single row
        private int? GetLockedRow(List<Field> candidateFields)
        {
            if (!candidateFields.Any()) return null;
            int row = candidateFields.First().Row;
            return candidateFields.All(f => f.Row == row) ? row : (int?)null;
        }

        // Helper: Get locked column if all candidate fields are in a single column
        private int? GetLockedColumn(List<Field> candidateFields)
        {
            if (!candidateFields.Any()) return null;
            int column = candidateFields.First().Column;
            return candidateFields.All(f => f.Column == column) ? column : (int?)null;
        }

        // Helper: Get locked block if all candidate fields are in a single block
        private int? GetLockedBlock(List<Field> candidateFields)
        {
            if (!candidateFields.Any()) return null;
            int block = candidateFields.First().Block;
            return candidateFields.All(f => f.Block == block) ? block : (int?)null;
        }

        // TODO: had ik hier geen ext method voor?
        // Eliminate candidate from row outside the block (Pointing)
        private int EliminateFromRowOutsideBlock(int block, int row, int value)
        {
            var fieldsOutsideBlock = _fields.Rows(row).Where(f => f.Block != block);
            return fieldsOutsideBlock.RemoveValueFromCandidates(value);
        }

        // Eliminate candidate from column outside the block (Pointing)
        private int EliminateFromColumnOutsideBlock(int block, int column, int value)
        {
            var fieldsOutsideBlock = _fields.Columns(column).Where(f => f.Block != block);
            return fieldsOutsideBlock.RemoveValueFromCandidates(value);
        }

        // Eliminate candidate from block outside the row/column (Claiming)
        private int EliminateFromBlockOutsideLine(IEnumerable<Field> blockFields, List<Field> lineFields, int value)
        {
            var fieldsOutsideLine = blockFields.Except(lineFields);
            return fieldsOutsideLine.RemoveValueFromCandidates(value);
        }

        private void PrintResult(bool solved)
        {
            if (solved)
            {
                CheckValiditySolution();
                WriteLine("Solved:", ConsoleColor.Green);
            }
            else
            {
                PrintDebugInformation(true);
                WriteLine("Not solved:", ConsoleColor.DarkMagenta);
            }
                

            WriteLine(this, ConsoleColor.Yellow);
        }

        // Google: Sudoku X-Wing strategy explained
        private int TryXWing()
        {
            int totalCandidatesRemoved = 0;

            // Loop through all possible candidates (1 to 9)
            for (int candidate = 1; candidate <= 9; candidate++)
            {
                // Check for X-Wing patterns in both rows and columns
                totalCandidatesRemoved += FindXWing(candidate, isRowBased: true);
                totalCandidatesRemoved += FindXWing(candidate, isRowBased: false);
            }

            return totalCandidatesRemoved;
        }

        private int FindXWing(int candidate, bool isRowBased)
        {
            int candidatesRemoved = 0;
            var lineCandidates = new Dictionary<int, List<int>>();  // Line index -> List of positions where candidate exists

            // Step 1: Collect lines (rows or columns) with exactly two candidates
            for (int line = 0; line < 9; line++)
            {
                var positionsWithCandidate = new List<int>();

                for (int pos = 0; pos < 9; pos++)
                {
                    var field = isRowBased ? _fields2D[line, pos] : _fields2D[pos, line];
                    if (field.Candidates.Contains(candidate))
                    {
                        positionsWithCandidate.Add(pos);
                    }
                }

                if (positionsWithCandidate.Count == 2)
                {
                    lineCandidates[line] = positionsWithCandidate;
                }
            }

            // Step 2: Identify X-Wing patterns and eliminate candidates
            var lines = lineCandidates.Keys.ToList();

            for (int i = 0; i < lines.Count - 1; i++)
            {
                for (int j = i + 1; j < lines.Count; j++)
                {
                    var line1 = lines[i];
                    var line2 = lines[j];

                    // Check if positions match to form an X-Wing
                    if (lineCandidates[line1].SequenceEqual(lineCandidates[line2]))
                    {
                        var commonPositions = lineCandidates[line1];

                        // Eliminate candidate from other lines in the matched positions
                        foreach (var pos in commonPositions)
                        {
                            for (int otherLine = 0; otherLine < 9; otherLine++)
                            {
                                if (otherLine != line1 && otherLine != line2)
                                {
                                    var field = isRowBased ? _fields2D[otherLine, pos] : _fields2D[pos, otherLine];
                                    candidatesRemoved += field.RemoveValueFromCandidates(candidate);
                                }
                            }
                        }
                    }
                }
            }
            return candidatesRemoved;
        }

        // Google: Sudoku Y-Wing or XY-Wing strategy explained. A pivot has two pincers.
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
            return _fields
                .Except(new[] { pivot, pincer1, pincer2 })
                .Where(f => f.Candidates.Contains(candidateToRemove) && pincer1.IntersectsWith(f) && pincer2.IntersectsWith(f))
                .Select(f =>
                {
                    f.RemoveValueFromCandidates(candidateToRemove);
                    return 1;
                })
                .Sum();  // Sum up the number of candidates removed
        }


        private IEnumerable<Field> GetPincerPossibilities(Field pivot, int candidate, IEnumerable<Field> fieldsWithTwoCandidates)
        {
            return fieldsWithTwoCandidates
                .Where(f => f != pivot && f.Candidates.Contains(candidate) && pivot.IntersectsWith(f));
        }

        private int TryXYZWing()
        {
            int nrOfCandidatesRemoved = 0;

            // Find all cells with exactly 3 candidates (potential pivots)
            var pivotFields = _fields.WithNumberOfCandidates(3);

            foreach (var pivot in pivotFields)
            {
                var pivotCandidates = pivot.Candidates;

                // Find cells that are peers of the pivot (share row, column, or block) and have exactly 2 of the pivot's candidates
                var potentialPincers = pivot.OtherPeers()
                                            .WithNumberOfCandidates(2)
                                            .Where(p => p.Candidates.All(c => pivotCandidates.Contains(c)))
                                            .ToList();

                // Search for combinations of pincers that cover all 3 candidates in the pivot
                for (int i = 0; i < potentialPincers.Count; i++)
                {
                    for (int j = i + 1; j < potentialPincers.Count; j++)
                    {
                        var pincer1 = potentialPincers[i];
                        var pincer2 = potentialPincers[j];

                        // Check if combined pincers cover all 3 candidates of the pivot
                        var combinedCandidates = pincer1.Candidates.Union(pincer2.Candidates).ToList();
                        if (combinedCandidates.Count == 3 && combinedCandidates.All(c => pivotCandidates.Contains(c)))
                        {
                            // Identify the common candidate between the two pincers (this is the candidate to eliminate)
                            var candidateToEliminate = pincer1.Candidates.Intersect(pincer2.Candidates).FirstOrDefault();

                            // Eliminate candidate from cells that see both pincers
                            nrOfCandidatesRemoved += EliminateXYZWingCandidates(pincer1, pincer2, candidateToEliminate);
                        }
                    }
                }
            }

            return nrOfCandidatesRemoved;
        }

        // Helper: Eliminate candidates from fields that intersect with both pincers
        private int EliminateXYZWingCandidates(Field pincer1, Field pincer2, int candidateToEliminate)
        {
            int nrOfCandidatesRemoved = 0;

            var commonPeers = pincer1.OtherPeers()
                                     .Intersect(pincer2.OtherPeers())
                                     .Where(f => f.Candidates.Contains(candidateToEliminate))
                                     .ToList();

            foreach (var field in commonPeers)
            {
                nrOfCandidatesRemoved += field.RemoveValueFromCandidates(candidateToEliminate);
            }

            return nrOfCandidatesRemoved;
        }

        // TODO test voor maken
        private int TrySwordfish()
        {
            int nrOfCandidatesRemoved = 0;

            for (int candidate = 1; candidate <= 9; candidate++)
            {
                // Check rows for Swordfish pattern
                nrOfCandidatesRemoved += FindSwordfish(candidate, isRowBased: true);

                // Check columns for Swordfish pattern
                nrOfCandidatesRemoved += FindSwordfish(candidate, isRowBased: false);
            }

            return nrOfCandidatesRemoved;
        }

        private int FindSwordfish(int candidate, bool isRowBased)
        {
            int nrOfCandidatesRemoved = 0;

            // Collect rows or columns where the candidate appears in 2 or 3 positions
            var linesWithCandidate = new List<(int LineIndex, List<int> Positions)>();

            for (int i = 1; i <= 9; i++)
            {
                var fields = isRowBased ? _fields.Rows(i) : _fields.Columns(i);
                var positions = fields.Where(f => f.Candidates.Contains(candidate))
                                      .Select(f => isRowBased ? f.Column : f.Row)
                                      .ToList();

                if (positions.Count >= 2 && positions.Count <= 3)
                {
                    linesWithCandidate.Add((i, positions));
                }
            }

            // Check combinations of 3 lines for Swordfish pattern
            var lineCombinations = GetCombinations(linesWithCandidate, 3);

            foreach (var combination in lineCombinations)
            {
                var allPositions = combination.SelectMany(c => c.Positions).Distinct().ToList();

                if (allPositions.Count == 3)
                {
                    // Swordfish pattern found
                    foreach (var pos in allPositions)
                    {
                        // Eliminate candidate from other lines
                        for (int i = 1; i <= 9; i++)
                        {
                            if (!combination.Any(c => c.LineIndex == i))
                            {
                                var fieldsToEliminate = isRowBased ? _fields.Columns(pos).Where(f => f.Row == i)
                                                                   : _fields.Rows(pos).Where(f => f.Column == i);

                                nrOfCandidatesRemoved += fieldsToEliminate.RemoveValueFromCandidates(candidate);
                            }
                        }
                    }
                }
            }

            return nrOfCandidatesRemoved;
        }

        // Helper method to generate combinations
        private List<List<T>> GetCombinations<T>(List<T> list, int length)
        {
            if (length == 0) return new List<List<T>> { new List<T>() };

            if (list.Count == 0) return new List<List<T>>();

            var result = new List<List<T>>();

            T head = list[0];
            List<T> tail = list.Skip(1).ToList();

            foreach (var combination in GetCombinations(tail, length - 1))
            {
                combination.Insert(0, head);
                result.Add(combination);
            }

            result.AddRange(GetCombinations(tail, length));

            return result;
        }


        private int CheckXYZWing(Field pivot, Field pincer1, Field pincer2, int candidateToRemove)
        {
            return _fields
                .Except(new[] { pivot, pincer1, pincer2 })
                .Where(f => f.Candidates.Contains(candidateToRemove) &&
                            pivot.IntersectsWith(f) &&
                            pincer1.IntersectsWith(f) &&
                            pincer2.IntersectsWith(f))
                .Select(f =>
                {
                    f.RemoveValueFromCandidates(candidateToRemove);
                    return 1;
                })
                .Sum();
        }

        private int RemoveCandidatesOutsideBlock(int block, int value, List<Field> fieldsInBlockWithValueInCandidates, bool isRow)
        {
            IEnumerable<Field> fieldsOutsideBlock = _fields.Where(f => f.Block != block);

            fieldsOutsideBlock = isRow ?
                fieldsOutsideBlock.Rows(fieldsInBlockWithValueInCandidates[0].Row) :
                fieldsOutsideBlock.Columns(fieldsInBlockWithValueInCandidates[0].Column);

            return fieldsOutsideBlock.RemoveValueFromCandidates(value);
        }

        private static int RemoveCandidates(List<int> candidates, IEnumerable<Field> fields)
        {
            int nrOfCandidatesRemoved = 0;

            foreach (int candidate in candidates)
                nrOfCandidatesRemoved += fields.RemoveValueFromCandidates(candidate);

            return nrOfCandidatesRemoved;
        }

        // Helper method to generate combinations of a specific length
        public static List<List<int>> GetCombinations(List<int> list, int length)
        {
            if (length == 0) return new List<List<int>> { new List<int>() };

            return list.SelectMany((e, i) =>
                GetCombinations(list.Skip(i + 1).ToList(), length - 1)
                    .Select(c => (new List<int> { e }).Concat(c).ToList())
            ).ToList();
        }


        // Initialize the puzzle
        private void Initialize(string[] data)
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (data[row][col] != ' ')
                    {
                        var value = int.Parse(data[row][col].ToString());
                        _fields2D[row, col].Value = value;
                        _fields2D[row, col].Candidates = [value];
                    }
                }
            }
        }

        private bool SudokuIsSolved()
        {
            return _fields.TrueForAll(f => f.Value.HasValue);
        }

        private void CheckValiditySolution()
        {
            List<int> actual;

            for (int i = 1; i <= 9; i++)
            {
                actual = _fields.Rows(i).Select(f => (int)f.Value).ToList();
                CheckValidity(actual, i, "Row");

                actual = _fields.Columns(i).Select(f => (int)f.Value).ToList();
                CheckValidity(actual, i, "Column");

                actual = _fields.Blocks(i).Select(f => (int)f.Value).ToList();
                CheckValidity(actual, i, "Block");
            }
        }

        private void CheckValidity(List<int> actual, int i, string segment)
        {
            var expected = Enumerable.Range(1, 9).ToList();
            var result = expected.Except(actual);

            if (result.Any())
            {
                WriteLine(this, ConsoleColor.Magenta);
                throw new InvalidOperationException($"Invalid solution! {segment} {i}, missing value: {result.First()}");
            }
        }

        public override string ToString()
        {
            var output = string.Empty;

            for (int row = 0; row < 9; row++)
            {
                if(row % 3 == 0)
                    output += row == 0 ? "╔═══╦═══╦═══╗\r\n" : "╠═══╬═══╬═══╣\r\n";

                for (int col = 0; col < 9; col++)
                {
                    if (col % 3 == 0)
                        output += "║";

                    output += _fields2D[row, col].Value == null ? " " : _fields2D[row, col].Value.ToString();
                }
                output += "║\r\n";
            }
            output += "╚═══╩═══╩═══╝";

            return output;
        }

        private static void WriteLine(object obj, ConsoleColor foregroundColor = ConsoleColor.White, ConsoleColor backgroundColor = ConsoleColor.Black)
        {
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;
            Console.WriteLine(obj);
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        private void PrintDebugInformation(bool perRow)
        {
            WriteLine("################# DEBUG START ###################", ConsoleColor.Cyan);

            if (perRow)
            {
                for (int row = 0; row < 9; row++)
                    for (int col = 0; col < 9; col++)
                        WriteLine(_fields2D[row, col], ConsoleColor.Cyan);
            }
            else
            {
                for (int col = 0; col < 9; col++)
                    for (int row = 0; row < 9; row++)
                        WriteLine(_fields2D[row, col], ConsoleColor.Cyan);
            }

            WriteLine("################## DEBUG END ####################", ConsoleColor.Cyan);
        }

        private class BlockData
        {
            public int Value { get; set; }
            public required List<Field> Block { get; set; }
            public required IEnumerable<IGrouping<int, Field>> RowsContainingValue { get; set; }
            public required IEnumerable<IGrouping<int, Field>> ColumnsContainingValue { get; set; }
        }
    }
}
