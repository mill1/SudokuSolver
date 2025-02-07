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
                var solved = SolveSudokuUsingSimpleStrategies();                
                PrintResult(solved);

                return solved;
            }
            catch (Exception e)
            {
                WriteLine(e, ConsoleColor.Red);
                return false;
            }
        }

        public bool SolveSudokuUsingSimpleStrategies()
        {
            bool progressMade;

            do
            {
                progressMade = false;

                // 1. Basic Candidate Elimination
                progressMade |= TryBasicCandidateElimination() > 0;

                // 2. Naked Singles
                progressMade |= TryNakedSingles() > 0;

                // 3. Hidden Singles
                progressMade |= TryHiddenSingles() > 0;

                // 4.Pointing Pairs / Triples
                progressMade |= TryPointingPairsTriples() > 0;

                // 5. Claiming Pairs/Triples
                progressMade |= TryClaimingPairsTriples() > 0;

                // 6. Naked Pairs/Triples/Quads
                progressMade |= TryNakedSubsets() > 0;

                // 7. Hidden Pairs/Triples/Quads
                 progressMade |= TryHiddenSubsets() > 0;

            } while (progressMade);

            // Check if the Sudoku is solved
            return IsSudokuSolved();
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
                    field.Value = value;
                    field.Candidates.Clear();
                    nrOfValuesSet++;

                    // Remove this value from candidates in the same row, column, and block
                    field.OtherRowFields().RemoveValueFromCandidates(value);
                    field.OtherColumnFields().RemoveValueFromCandidates(value);
                    field.OtherBlockFields().RemoveValueFromCandidates(value);
                }
            }

            return nrOfValuesSet;
        }

        // 4. When candidates are restricted to a row/column in a block, remove them from the same row/column outside the block.
        private int TryPointingPairsTriples()
        {
            int totalCandidatesRemoved = 0;

            for (int block = 1; block <= 9; block++)
            {
                for (int value = 1; value <= 9; value++)
                {
                    var candidateFields = _fields.Blocks(block).Where(f => f.Candidates.Contains(value)).ToList();

                    if (candidateFields.Count is >= 2 and <= 3)
                    {
                        // Check for pointing pairs/triples in rows or columns
                        totalCandidatesRemoved += RemovePointingCandidates(block, value, candidateFields, isRow: true);
                        totalCandidatesRemoved += RemovePointingCandidates(block, value, candidateFields, isRow: false);
                    }
                }
            }
            return totalCandidatesRemoved;
        }

        private int RemovePointingCandidates(int block, int value, List<Field> candidateFields, bool isRow)
        {
            // Extract the unique rows or columns
            var uniqueSegments = isRow
                ? candidateFields.Select(f => f.Row).Distinct().ToList()
                : candidateFields.Select(f => f.Column).Distinct().ToList();

            // Proceed only if all candidates are in the same row or column
            if (uniqueSegments.Count == 1)
            {
                int segment = uniqueSegments.First();

                // Select fields outside the block but in the same row/column
                var fieldsToCheck = _fields
                    .Where(f => f.Block != block && (isRow ? f.Row == segment : f.Column == segment) && f.Candidates.Contains(value))
                    .ToList();

                // Remove the candidate from these fields
                return fieldsToCheck.RemoveValueFromCandidates(value);
            }
            return 0;
        }

        // 5. When candidates are restricted to a block within a row/column, remove them from the block outside the row/column.
        private int TryClaimingPairsTriples()
        {
            int totalCandidatesRemoved = 0;

            for (int i = 1; i <= 9; i++)
            {
                totalCandidatesRemoved += FindClaimingCandidates(_fields.Rows(i));
                totalCandidatesRemoved += FindClaimingCandidates(_fields.Columns(i));
            }

            return totalCandidatesRemoved;
        }

        private int FindClaimingCandidates(IEnumerable<Field> segment)
        {
            int nrOfCandidatesRemoved = 0;

            for (int value = 1; value <= 9; value++)
            {
                var candidateFields = segment.Where(f => f.Candidates.Contains(value)).ToList();

                if (candidateFields.Count > 1)
                {
                    var uniqueBlocks = candidateFields.Select(f => f.Block).Distinct().ToList();

                    // If all candidates are in the same block, remove the candidate from the rest of the block
                    if (uniqueBlocks.Count == 1)
                    {
                        var block = uniqueBlocks.First();
                        var fieldsToUpdate = _fields.Blocks(block).Except(candidateFields);
                        nrOfCandidatesRemoved += fieldsToUpdate.RemoveValueFromCandidates(value);   
                    }
                }
            }

            return nrOfCandidatesRemoved;
        }

        // 6. Remove shared candidates from other cells when multiple cells have the exact same candidates.
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

        // 7. When candidates appear in exactly the same number of cells, remove other candidates from those cells
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



        private int FindHiddenPairsInSegmentVersion1(IEnumerable<Field> segment)
        {
            int nrOfCandidatesRemoved = 0;

            // Check all pairs of candidates
            for (int c1 = 1; c1 <= 8; c1++)
            {
                for (int c2 = c1 + 1; c2 <= 9; c2++)
                {
                    var fieldsWithC1 = segment.Where(f => f.Candidates.Contains(c1)).ToList();
                    var fieldsWithC2 = segment.Where(f => f.Candidates.Contains(c2)).ToList();

                    var sharedFields = fieldsWithC1.Intersect(fieldsWithC2).ToList();

                    // If both candidates appear in exactly two shared fields
                    if (sharedFields.Count == 2)
                    {
                        foreach (var field in sharedFields)
                        {
                            var extraCandidates = field.Candidates.Except(new[] { c1, c2 }).ToList();
                            foreach (var extra in extraCandidates)
                            {
                                nrOfCandidatesRemoved += field.RemoveValueFromCandidates(extra);
                                Console.WriteLine(666);
                            }
                        }
                    }
                }
            }

            return nrOfCandidatesRemoved;
        }


        //private bool TrySolvePuzzle()
        //{
        //    int iteration = 0;
        //    int foundSolutions = 1;
        //    int nrOfCandidatesRemoved = 0;

        //    while (foundSolutions > 0 || nrOfCandidatesRemoved > 0)
        //    {
        //        iteration++;

        //        if (Settings.Debug)
        //        {
        //            //WriteLine($"Iteration: {iteration}", ConsoleColor.Blue);
        //            //WriteLine(this, ConsoleColor.Cyan);
        //            //PrintDebugInformation(true);
        //        }

        //        nrOfCandidatesRemoved = TryBasicCandidateElimination();
        //        foundSolutions = CheckHiddenSingles();

        //        nrOfCandidatesRemoved += TryEliminationByValuesInSegments();
        //        foundSolutions += CheckNakedSingles();

        //        if (_fields.Count(f => f.Value != null) == _fields.Count)
        //            break;

        //        if (foundSolutions == 0)
        //            nrOfCandidatesRemoved = ApplyAdvancedStrategies();
        //    }

        //    return _fields.Count(f => f.Value != null) == _fields.Count;
        //}

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
            int nrOfCandidatesRemoved = TryNakedSubsets();

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

        private bool IsSudokuSolved()
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
