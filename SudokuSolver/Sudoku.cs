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
                var solved = TrySolvePuzzle();                
                PrintResult(solved);

                return solved;
            }
            catch (Exception e)
            {
                WriteLine(e, ConsoleColor.Red);
                return false;
            }
        }

        private bool TrySolvePuzzle()
        {
            int iteration = 0;
            int foundSolutions = 1;
            int nrOfCandidatesRemoved = 0;

            while (foundSolutions > 0 || nrOfCandidatesRemoved > 0)
            {
                iteration++;

                if (Settings.Debug)
                {
                    //WriteLine($"Iteration: {iteration}", ConsoleColor.Blue);
                    //WriteLine(this, ConsoleColor.Cyan);
                    //PrintDebugInformation(true);
                }

                nrOfCandidatesRemoved = TrySlashing();
                foundSolutions = CheckHiddenSingles();

                nrOfCandidatesRemoved += TryEliminationByValuesInSegments();
                foundSolutions += CheckNakedSingles();

                if (_fields.Count(f => f.Value != null) == _fields.Count)
                    break;

                if (foundSolutions == 0)
                    nrOfCandidatesRemoved = ApplyAdvancedStrategies();
            }

            return _fields.Count(f => f.Value != null) == _fields.Count;
        }

        private void PrintResult(bool solved)
        {
            if (solved)
            {
                CheckValiditySolution();
                WriteLine("Solved:", ConsoleColor.Green);
            }
            else
                WriteLine("Not solved:", ConsoleColor.DarkMagenta);

            WriteLine(this, ConsoleColor.Yellow);
        }

        // Advanced strategies. 'TryStrategyX' means 'try to eliminate candidates by applying strategy X'.
        private int ApplyAdvancedStrategies()
        {
            int nrOfCandidatesRemoved = TryStripClothedCandidates();

            nrOfCandidatesRemoved += TryNakedCombinations();

            if (nrOfCandidatesRemoved == 0)
                nrOfCandidatesRemoved = TryAdvancedSlashing();

            if (nrOfCandidatesRemoved == 0)
                nrOfCandidatesRemoved = TryTwoOptionsWithinBlockGroups();

            if (nrOfCandidatesRemoved == 0)
                nrOfCandidatesRemoved = TryLockedCandidates();                    

            if (nrOfCandidatesRemoved == 0)
                nrOfCandidatesRemoved = TryXWing();

            if (nrOfCandidatesRemoved == 0)
                nrOfCandidatesRemoved = TryYWing();

            if (nrOfCandidatesRemoved == 0)
                nrOfCandidatesRemoved = TryXYZWing();

            return nrOfCandidatesRemoved;
        }

        // Per value try to remove candidates from other fields within the same segment (row, column or block).
        private int TrySlashing()
        {
            int nrOfCandidatesRemoved = 0;

            foreach (var field in _fields)
            {
                if (field.Value != null)
                {
                    nrOfCandidatesRemoved += field.OtherRowFields().RemoveValueFromCandidates((int)field.Value);
                    nrOfCandidatesRemoved += field.OtherColumnFields().RemoveValueFromCandidates((int)field.Value);
                    nrOfCandidatesRemoved += field.OtherBlockFields().RemoveValueFromCandidates((int)field.Value);
                }
            }

            return nrOfCandidatesRemoved;
        }

        // Try to find a solution by asserting that all other fields do not contain the value as a candidate in any segment.
        private int CheckHiddenSingles()
        {
            int nrOfSolutionsFound = 0;

            for (int value = 1; value <= 9; value++)
            {
                foreach (var field in _fields)
                {
                    if (field.Value == null)
                    {
                        if (!field.OtherRowFields().CandidatesContainsValue(value) ||
                            !field.OtherColumnFields().CandidatesContainsValue(value) ||
                            !field.OtherBlockFields().CandidatesContainsValue(value))
                        {
                            if (!field.Candidates.Contains(value))
                                throw new ArgumentException($"Solution value not found in candidates: {field}");

                            field.Value = value;
                            field.Candidates = [value];
                            nrOfSolutionsFound++;

                            if (Settings.Debug)
                                WriteLine($"Found solution (Check hidden singles): {field}", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
                        }
                    }
                }
            }
            return nrOfSolutionsFound;
        }

        // Per field try to eliminate candidates based on the values in the segments the field is part of.
        private int TryEliminationByValuesInSegments()
        {
            int nrOfCandidatesRemoved = 0;

            foreach (var field in _fields)
            {
                if (field.Value == null)
                {
                    for (int value = 1; value <= 9; value++)
                    {
                        if (field.OtherRowFields().ContainsValue(value))
                            nrOfCandidatesRemoved += field.RemoveValueFromCandidates(value);

                        if (field.OtherColumnFields().ContainsValue(value))
                            nrOfCandidatesRemoved += field.RemoveValueFromCandidates(value);

                        if (field.OtherBlockFields().ContainsValue(value))
                            nrOfCandidatesRemoved += field.RemoveValueFromCandidates(value);
                    }
                }
            }
            return nrOfCandidatesRemoved;
        }

        // Try to find a solution by asserting per field that only one candidate is left.
        private int CheckNakedSingles()
        {
            int solutionsFound = 0;

            foreach (var field in _fields.Where(f => f.Candidates.Count == 1))
            {
                if (field.Value == null)
                {
                    if (field.OtherRowFields().ContainsValue(field.Candidates[0]) ||
                        field.OtherColumnFields().ContainsValue(field.Candidates[0]) ||
                        field.OtherBlockFields().ContainsValue(field.Candidates[0]))
                        throw new ArgumentException($"Invalid single candidate: {field.Candidates[0]}! Field: {field}");

                    field.Value = field.Candidates[0];
                    solutionsFound++;

                    if (Settings.Debug)
                        WriteLine($"Found solution (Check naked singles): {field}", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
                }
            }
            return solutionsFound;
        }

        private int TryTwoOptionsWithinBlockGroups()
        {
            // There are six 'block groups'; three horizontal and three vertical;
            // For each block group check the following for values 1-9 (example: horizontal): 
            // Check if a value will only fit in the same TWO rows regarding two blocks.
            // If so then the OTHER row w.r. to the OTHER block cannot contain that value.
            // The same is true regarding vertical block groups.

            // Horizontal            
            var blockGroups = new List<List<Field>>
            {
                _fields.Where(f => new[] { 1, 2, 3 }.Contains(f.Block)).ToList(),
                _fields.Where(f => new[] { 4, 5, 6 }.Contains(f.Block)).ToList(),
                _fields.Where(f => new[] { 7, 8, 9 }.Contains(f.Block)).ToList(),
            };
            int nrOfCandidatesRemovedRows = CheckTwoOptionsWithinBlockGroups(blockGroups, horizontal:true);

            // Vertical
            blockGroups = new List<List<Field>>
            {
                _fields.Where(f => new[] { 1, 4, 7 }.Contains(f.Block)).ToList(),
                _fields.Where(f => new[] { 2, 5, 8 }.Contains(f.Block)).ToList(),
                _fields.Where(f => new[] { 3, 6, 9 }.Contains(f.Block)).ToList()
            };
            int nrOfCandidatesRemovedColumns = CheckTwoOptionsWithinBlockGroups(blockGroups, horizontal: false);

            return nrOfCandidatesRemovedRows + nrOfCandidatesRemovedColumns;
        }

        private int CheckTwoOptionsWithinBlockGroups(List<List<Field>> blockGroups, bool horizontal)
        {
            int nrOfCandidatesRemoved = 0;

            foreach (var blockGroup in blockGroups)
            {
                for (int value = 1; value <= 9; value++)
                {
                    var blockGroupData = GetBlockData(value, blockGroup);

                    if (horizontal)
                        nrOfCandidatesRemoved += CheckValueCanFitInTwoRowsInTwoBlocksOnly(value, blockGroupData);                            
                    else // vertical
                        nrOfCandidatesRemoved += CheckValueCanFitInTwoColumnsInTwoBlocksOnly(value, blockGroupData);
                }
            }
            return nrOfCandidatesRemoved;
        }

        private List<BlockData> GetBlockData(int value, List<Field> blockGroup)
        {
            return new List<BlockData>()
            {
                 ResolveBlockData(value, blockGroup.Where(b => b.Block == blockGroup[0].Block).ToList()),
                 ResolveBlockData(value, blockGroup.Where(b => b.Block == blockGroup[3].Block).ToList()),
                 ResolveBlockData(value, blockGroup.Where(b => b.Block == blockGroup[6].Block).ToList()),
            };
        }

        private static BlockData ResolveBlockData(int value, List<Field> block)
        {
            return new() 
            { 
                Value = value, 
                Block = block, 
                RowsContainingValue = ResolveRowsContainingValue(value, block), 
                ColumnsContainingValue = ResolveColumnsContainingValue(value, block) 
            };
        }

        private int CheckValueCanFitInTwoRowsInTwoBlocksOnly(int value, List<BlockData> blockData)
        {
            int nrOfCandidatesRemoved = 0;

            var counts = blockData.Select(r => r.RowsContainingValue.Count());
            if (!(counts.Count(c => c == 2) == 2 && counts.Contains(3)))
                return 0;

            // Check if the two rows where the value can fit are identical for both
            var resultsTwoRows = blockData.Where(r => r.RowsContainingValue.Count() == 2).ToList();
            var rowsTwoFirst = resultsTwoRows[0].RowsContainingValue.SelectMany(g => g).Select(f => f.Row).Distinct().ToList();
            var rowsTwoSecond = resultsTwoRows[1].RowsContainingValue.SelectMany(g => g).Select(f => f.Row).Distinct().ToList();

            if (!(rowsTwoFirst[0] == rowsTwoSecond[0] && rowsTwoFirst[1] == rowsTwoSecond[1]))
                return 0;

            // Resolve the row id's of the block where the value will fit in all three rows. 
            var rowsTwo = resultsTwoRows[0].RowsContainingValue.SelectMany(g => g).Select(f => f.Row).Distinct();
            var resultThreeRows = blockData.Where(r => r.RowsContainingValue.Count() == 3).First();
            var rowsThree = resultThreeRows.RowsContainingValue.SelectMany(g => g).Select(f => f.Row).Distinct();

            // Remove candidates from the block regarding the rowsTwo rows
            foreach (var row in rowsTwo)
            {
                var targetFields = resultThreeRows.Block.Where(f => f.Row == row);
                nrOfCandidatesRemoved += targetFields.RemoveValueFromCandidates(value);
            }
            return nrOfCandidatesRemoved;
        }

        private static int CheckValueCanFitInTwoColumnsInTwoBlocksOnly(int value, List<BlockData> blockData)
        {
            int nrOfCandidatesRemoved = 0;

            var counts = blockData.Select(r => r.ColumnsContainingValue.Count());
            if (!(counts.Count(c => c == 2) == 2 && counts.Contains(3)))
                return 0;

            // Check if the two columns where the value can fit are identical for both
            var resultsTwoColumns = blockData.Where(r => r.ColumnsContainingValue.Count() == 2).ToList();
            var columnsTwoFirst = resultsTwoColumns[0].ColumnsContainingValue.SelectMany(g => g).Select(f => f.Column).Distinct().ToList();
            var columnsTwoSecond = resultsTwoColumns[1].ColumnsContainingValue.SelectMany(g => g).Select(f => f.Column).Distinct().ToList();

            if (!(columnsTwoFirst[0] == columnsTwoSecond[0] && columnsTwoFirst[1] == columnsTwoSecond[1]))
                return 0;

            // Resolve the column id's of the block where the value will fit in all three columns. 
            var resultTwoColumns = blockData.Where(r => r.ColumnsContainingValue.Count() == 2).First();
            var columnsTwo = resultTwoColumns.ColumnsContainingValue.SelectMany(g => g).Select(f => f.Column).Distinct();
            var resultThreeColumns = blockData.Where(r => r.ColumnsContainingValue.Count() == 3).First();
            var columnsThree = resultThreeColumns.ColumnsContainingValue.SelectMany(g => g).Select(f => f.Column).Distinct();

            // Remove candidates from the block regarding the columnsTwo columns
            foreach (var column in columnsTwo)
            {
                var targetFields = resultThreeColumns.Block.Where(f => f.Column == column);
                nrOfCandidatesRemoved += targetFields.RemoveValueFromCandidates(value);
            }
            return nrOfCandidatesRemoved;
        }

        private static IEnumerable<IGrouping<int, Field>> ResolveRowsContainingValue(int value, List<Field> block)
        {
            return block.Where(f => f.Candidates.Contains(value)).GroupBy(f => f.Row);
        }

        private static IEnumerable<IGrouping<int, Field>> ResolveColumnsContainingValue(int value, List<Field> block)
        {
            return block.Where(f => f.Candidates.Contains(value)).GroupBy(f => f.Column);
        }

        // Identify situations where, regarding a segment, two fields contain only two different possible values (or three in the case of three fields, etc.)
        // For example, in block 1, fields 1 and 3 contain candidates with the possible values 4 and 8.
        // Block 1, field 1; Candidates: 4 5 7 8
        // Block 1, field 3; Candidates: 4 5 8
        // In such cases, remove the other candidates (5 and 7) from fields 1 and 3. This applies to all segments ( blocks, rows, and columns).
        private int TryStripClothedCandidates()
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

        // Try to look 'naked pairs'; if in a segment two field contain the same two candidates then those values can only go in those two fields.
        // As a consequence those candidates maybe stripped from the other fields in that segment (row, column or block)
        private int TryNakedCombinations()
        {
            int nrOfCandidatesRemoved = 0;

            IEnumerable<Field> fields;

            for (int i = 1; i <= 9; i++)
            {
                fields = _fields.Blocks(i);
                nrOfCandidatesRemoved += CheckNakedPairs(fields);
                nrOfCandidatesRemoved += CheckNakedTriplets(fields);

                fields = _fields.Rows(i);
                nrOfCandidatesRemoved += CheckNakedPairs(fields);
                nrOfCandidatesRemoved += CheckNakedTriplets(fields);

                fields = _fields.Columns(i);
                nrOfCandidatesRemoved += CheckNakedPairs(fields);
                nrOfCandidatesRemoved += CheckNakedTriplets(fields);
            }

            return nrOfCandidatesRemoved;
        }

        private static int CheckNakedPairs(IEnumerable<Field> fields)
        {
            int nrOfCandidatesRemoved = 0;
            var fieldsWithTwoCandidates = fields.Where(f => f.Candidates.Count == 2);

            if (fieldsWithTwoCandidates.Count() < 2)            
                return 0;

            foreach (var combination in GetCombinations(Enumerable.Range(1, 9).ToList(), 2))
            {
                var fieldsWithTwoIdenticalCandidates = fieldsWithTwoCandidates.Where(f => f.Candidates[0] == combination[0] && f.Candidates[1] == combination[1]).ToList();

                if(fieldsWithTwoIdenticalCandidates.Count() == 2)
                {
                    // Remove the two candidates from the other fields in the segement
                    var otherFields = fields.Except(fieldsWithTwoIdenticalCandidates);
                    nrOfCandidatesRemoved += RemoveCandidates(combination, otherFields);
                    break;
                }
            }
            return nrOfCandidatesRemoved;
        }

        private static int CheckNakedTriplets(IEnumerable<Field> fields)
        {
            int nrOfCandidatesRemoved = 0;
            var fieldsWithTwoCandidates = fields.Where(f => f.Candidates.Count == 2); // 2 is correct

            if (fieldsWithTwoCandidates.Count() < 3)
                return 0;

            foreach (var combination in GetCombinations(Enumerable.Range(1, 9).ToList(), 3))
            {
                var fieldsWithFirstNakedTriplet = fieldsWithTwoCandidates.Where(f =>  f.Candidates[0] == combination[0] && f.Candidates[1] == combination[1]);
                var fieldsWithSecondNakedTriplet = fieldsWithTwoCandidates.Where(f => f.Candidates[0] == combination[0] && f.Candidates[1] == combination[2]);
                var fieldsWithThirdNakedTriplet = fieldsWithTwoCandidates.Where(f =>  f.Candidates[0] == combination[1] && f.Candidates[1] == combination[2]);

                if (!(fieldsWithFirstNakedTriplet.Any() && fieldsWithSecondNakedTriplet.Any() && fieldsWithThirdNakedTriplet.Any()))
                    continue;

                if (!(fieldsWithFirstNakedTriplet.Count() == 1 && fieldsWithSecondNakedTriplet.Count() == 1 && fieldsWithThirdNakedTriplet.Count() == 1))
                    continue;

                // Remove the three candidates from the other fields in the segement
                var otherFields = fields.Except(fieldsWithFirstNakedTriplet)
                                        .Except(fieldsWithSecondNakedTriplet)
                                        .Except(fieldsWithThirdNakedTriplet);

                nrOfCandidatesRemoved += RemoveCandidates(combination, otherFields);

            }
            return nrOfCandidatesRemoved;
        }

        private int TryLockedCandidates()
        {
            int nrOfCandidatesRemoved = 0;

            for (int i = 1; i <= 9; i++)
            {
                var block = _fields.Blocks(i).ToList();

                // Per block check per row/column if a value can only fit in that row/column and not in 
                // the field in the outside blocks.
                nrOfCandidatesRemoved += CheckLockedCandidatesPerBlockRow(block);
                nrOfCandidatesRemoved += CheckLockedCandidatesPerBlockColumn(block);
            }
            return nrOfCandidatesRemoved;
        }

        private int CheckLockedCandidatesPerBlockRow(List<Field> block)
        {
            int nrOfCandidatesRemoved = 0;

            for (int row = block[0].Row; row <= block[0].Row + 2; row++)
            {
                // First get the list of values present in the row of the block.
                var rowFields = _fields.Where(f => f.Block == block[0].Block && f.Row == row).ToList();
                List<int> rowFieldValues = ResolveAllCandidatesBlockFields(rowFields);
                // Then get the list of values present in the row outside the block.
                var otherFields = _fields.Where(f => f.Block != block[0].Block && f.Row == row).ToList();
                List<int> otherFieldValues = ResolveAllCandidatesOtherBlockFields(otherFields);

                var candidates = rowFieldValues.Except(otherFieldValues).ToList();
                var otherRowFields = block.Except(rowFields);

                nrOfCandidatesRemoved += RemoveCandidates(candidates, otherRowFields);

            }
            return nrOfCandidatesRemoved;
        }

        private int CheckLockedCandidatesPerBlockColumn(List<Field> block)
        {
            int nrOfCandidatesRemoved = 0;

            for (int column = block[0].Column; column <= block[0].Column + 2; column++)
            {
                // First get the list of values present in the column of the block.
                var columnFields = _fields.Where(f => f.Block == block[0].Block && f.Column == column).ToList();
                List<int> columnFieldValues = ResolveAllCandidatesBlockFields(columnFields);
                // Then get the list of values present in the column outside the block.
                var otherFields = _fields.Where(f => f.Block != block[0].Block && f.Column == column).ToList();
                List<int> otherFieldValues = ResolveAllCandidatesOtherBlockFields(otherFields);

                var candidates = columnFieldValues.Except(otherFieldValues).ToList();
                var otherColumnFields = block.Except(columnFields);

                nrOfCandidatesRemoved += RemoveCandidates(candidates, otherColumnFields);
            }

            return nrOfCandidatesRemoved;
        }

        private static List<int> ResolveAllCandidatesBlockFields(List<Field> fields)
        {
            return [.. fields[0].Candidates
                .Union(fields[1].Candidates)
                .Union(fields[2].Candidates)
                .OrderBy(x => x)];
        }

        private static List<int> ResolveAllCandidatesOtherBlockFields(List<Field> fields)
        {
            return [.. fields[0].Candidates
                .Union(fields[1].Candidates)
                .Union(fields[2].Candidates)
                .Union(fields[3].Candidates)
                .Union(fields[4].Candidates)
                .Union(fields[5].Candidates)
                .OrderBy(x => x)];
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
            int totalCandidatesRemoved = 0;

            // Step 1: Identify all pivot fields with exactly 3 candidates
            var pivotFields = _fields.WithNumberOfCandidates(3);

            foreach (var pivot in pivotFields)
            {
                var candidates = pivot.Candidates;

                // Step 2: Find possible pincers that share candidates with the pivot
                var pincerCandidates = _fields.WithNumberOfCandidates(2)
                    .Where(p => pivot.IntersectsWith(p))
                    .ToList();

                // Step 3: Look for two pincers that match XYZ-Wing conditions
                foreach (var pincer1 in pincerCandidates)
                {
                    foreach (var pincer2 in pincerCandidates.Except(new[] { pincer1 }))
                    {
                        // Check if pincers and pivot together form an XYZ-Wing
                        var allCandidates = pivot.Candidates
                            .Union(pincer1.Candidates)
                            .Union(pincer2.Candidates)
                            .ToList();

                        if (allCandidates.Count == 3 &&
                            pincer1.Candidates.Intersect(pivot.Candidates).Any() &&
                            pincer2.Candidates.Intersect(pivot.Candidates).Any())
                        {
                            // Step 4: Determine the candidate to remove (present in pivot but not in pincers)
                            var candidateToRemove = pivot.Candidates
                                .Except(pincer1.Candidates)
                                .Except(pincer2.Candidates)
                                .FirstOrDefault();

                            if (candidateToRemove != 0)
                            {
                                totalCandidatesRemoved += CheckXYZWing(pivot, pincer1, pincer2, candidateToRemove);
                                if (totalCandidatesRemoved > 0) return totalCandidatesRemoved;  // Early exit if candidates removed
                            }
                        }
                    }
                }
            }

            return totalCandidatesRemoved;
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

        // Per block try to find situations where a value can only exist in one row or column (e.g. two fields in block 2 on the same row where only a 7 can go).
        // In those cases eliminate 7 as a candidate of fields on that row in the other horizontal blocks (= block 1 and 3) and see what happens.
        private int TryAdvancedSlashing()
        {
            int nrOfCandidatesRemoved = 0;

            for (int block = 1; block <= 9; block++)
            {
                for (int value = 1; value <= 9; value++)
                {
                    var fieldsInBlockWithValueInCandidates = _fields.Blocks(block).Where(f => f.Candidates.Contains(value)).ToList();

                    if (fieldsInBlockWithValueInCandidates.Count <= 3)
                    {
                        // In same row?
                        if (fieldsInBlockWithValueInCandidates.GroupBy(f => f.Row).Count() == 1)
                            nrOfCandidatesRemoved += RemoveCandidatesOutsideBlock(block, value, fieldsInBlockWithValueInCandidates, true);

                        // In same column?
                        if (fieldsInBlockWithValueInCandidates.GroupBy(f => f.Column).Count() == 1)
                            nrOfCandidatesRemoved += RemoveCandidatesOutsideBlock(block, value, fieldsInBlockWithValueInCandidates, false);
                    }
                }
            }
            return nrOfCandidatesRemoved;
        }

        private int RemoveCandidatesOutsideBlock(int block, int value, List<Field> fieldsInBlockWithValueInCandidates, bool isRow)
        {
            IEnumerable<Field> fieldsOutsideBlock = _fields.Where(f => f.Block != block);

            fieldsOutsideBlock = isRow ?
                fieldsOutsideBlock.Rows(fieldsInBlockWithValueInCandidates[0].Row) :
                fieldsOutsideBlock.Columns(fieldsInBlockWithValueInCandidates[0].Column);

            return fieldsOutsideBlock.RemoveValueFromCandidates(value);
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

        private static int RemoveCandidates(List<int> candidates, IEnumerable<Field> fields)
        {
            int nrOfCandidatesRemoved = 0;

            foreach (int candidate in candidates)
                nrOfCandidatesRemoved += fields.RemoveValueFromCandidates(candidate);

            return nrOfCandidatesRemoved;
        }

        // Helper method to generate combinations of a specific length
        private static IEnumerable<List<int>> GetCombinations(List<int> list, int length)
        {
            if (length == 0)
                return new List<List<int>> { new List<int>() };

            return list.SelectMany((item, index) =>
                GetCombinations(list.Skip(index + 1).ToList(), length - 1),
                (item, items) => new List<int> { item }.Concat(items).ToList());
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
            };
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
