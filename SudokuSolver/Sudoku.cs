using SudokuSolver.Extensions;

namespace SudokuSolver
{
    public class Sudoku
    {
        private readonly Field[,] _fields2D;
        private readonly List<Field> _fields = [];
        private int iteration = 0;

        public Sudoku()
        {
            // Initialize fields
            _fields2D = new Field[9, 9];

            for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                {
                    var field = new Field(row + 1, col + 1);
                    _fields2D[row, col] = field;
                    _fields.Add(field);
                }
        }

        public bool Solve(string[] data)
        {
            Initialize(data);
            
            int solutionsCount = 0;
            bool foundSolutionsBySlashing = true;
            bool foundSolutionsByElimination = false;
            bool removedCandidates = false;

            while (foundSolutionsBySlashing || foundSolutionsByElimination || removedCandidates)
            {
                iteration++;
                solutionsCount = _fields.Where(f => f.Value != null).Count();
                Console.WriteLine($"\nIteration {iteration}, solved: {solutionsCount}"); Console.WriteLine(this);

                if ( solutionsCount == _fields.Count )
                    break;

                foundSolutionsBySlashing = CheckSolutionsBySlashing();
                foundSolutionsByElimination = CheckSolutionsByElimination();                

                if (!foundSolutionsBySlashing && !foundSolutionsByElimination)
                {
                    // Complexity > 4 star puzzles. Time to bring out the big guns!
                    removedCandidates = false;

                    // TODO later aanzetten
                    //if (CheckRemoveCandidatesInOutsideBlocks())
                    //{
                    //    removedCandidates = true;
                    //    FindSolutionByResolvingSingleCandidate();
                    //    FindSolutionBasedOnOtherSegments();
                    //}

                    if (CheckTwoOptionsWithinBlockGroups())
                    {
                        removedCandidates = true;
                        FindSolutionByResolvingSingleCandidate();
                        FindSolutionBasedOnOtherSegments();
                    }

                    if (CheckFieldsWithSimilarCandidates())
                    {
                        removedCandidates = true;
                        FindSolutionByResolvingSingleCandidate();
                        FindSolutionBasedOnOtherSegments();
                    }
                }
            }

            var solved = solutionsCount == _fields.Count;

            Console.ForegroundColor = solved ? ConsoleColor.Green : ConsoleColor.Red;

            if (solved) 
            {
                CheckValiditySolution();
                Console.WriteLine("Solved:");
            }
            else
                Console.WriteLine("Not solved:");

            Console.ForegroundColor = ConsoleColor.White; 
            Console.WriteLine(this);

            return solved;
        }

        // Per value remove candidates from other fields within the same segment (row, column or block).
        // Solution is found by asserting that all other fields do not contain the value as a candidate in any segment.
        private bool CheckSolutionsBySlashing()
        {
            TryToRemoveCandidatesBySlashing();
            return FindSolutionBasedOnOtherSegments();
        }

        private void TryToRemoveCandidatesBySlashing()
        {
            foreach (var field in _fields)
            {
                if (field.Value != null)
                {
                    field.OtherRowFields(_fields).RemoveValueFromCandidates((int)field.Value);
                    field.OtherColumnFields(_fields).RemoveValueFromCandidates((int)field.Value);
                    field.OtherBlockFields(_fields).RemoveValueFromCandidates((int)field.Value);
                }
            }
        }

        private bool FindSolutionBasedOnOtherSegments()
        {
            bool solutionsFound = false;

            foreach (var field in _fields)
            {
                for (int value = 1; value <= 9; value++)
                {
                    if (field.Value == null)
                    {
                        if (!field.OtherRowFields(_fields).CandidatesContainsValue(value) ||
                            !field.OtherColumnFields(_fields).CandidatesContainsValue(value) ||
                            !field.OtherBlockFields(_fields).CandidatesContainsValue(value))
                        {
                            if (!field.Candidates.Contains(value))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine(this);
                                throw new ArgumentException($"Value not found in candidates: {field}");
                            }

                            field.Value = value;
                            field.Candidates = [value];
                            solutionsFound = true;
                        }
                    }
                }
            }
            return solutionsFound;
        }

        // Per field remove candidates in other fields within the same segment (row, column or block).
        // Solution is found by asserting that only one candidate is left.
        private bool CheckSolutionsByElimination()
        {
            TryToRemoveCandidatesByElmination();
            return FindSolutionByResolvingSingleCandidate();
        }

        private void TryToRemoveCandidatesByElmination()
        {
            foreach (var field in _fields)
            {
                if (field.Value == null)
                {
                    for (int value = 1; value <= 9; value++)
                    {
                        if (field.OtherRowFields(_fields).ContainsValue(value))
                            field.RemoveValueFromCandidates(value);

                        if (field.OtherColumnFields(_fields).ContainsValue(value))
                            field.RemoveValueFromCandidates(value);

                        if (field.OtherBlockFields(_fields).ContainsValue(value))
                            field.RemoveValueFromCandidates(value);
                    }
                }
            }
        }

        private bool FindSolutionByResolvingSingleCandidate()
        {
            bool solutionsFound = false;

            foreach (var field in _fields.Where(f => f.Candidates.Count == 1))
            {
                if (field.Value == null)
                {
                    if (field.OtherRowFields(_fields).ContainsValue(field.Candidates[0]) ||
                        field.OtherColumnFields(_fields).ContainsValue(field.Candidates[0]) ||
                        field.OtherBlockFields(_fields).ContainsValue(field.Candidates[0]))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(this);
                        throw new ArgumentException($"Invalid single candidate: {field.Candidates[0]}! Field: {field}");
                    }

                    field.Value = field.Candidates[0];
                    solutionsFound = true;
                }
            }
            return solutionsFound;
        }

        private bool CheckTwoOptionsWithinBlockGroups()
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
            var removedCandidatesRows = CheckTwoOptionsWithinBlockGroups(blockGroups, horizontal:true);

            // Vertical
            blockGroups = new List<List<Field>>
            {
                _fields.Where(f => new[] { 1, 4, 7 }.Contains(f.Block)).ToList(),
                _fields.Where(f => new[] { 2, 5, 8 }.Contains(f.Block)).ToList(),
                _fields.Where(f => new[] { 3, 6, 9 }.Contains(f.Block)).ToList()
            };
            var removedCandidatesColumns = CheckTwoOptionsWithinBlockGroups(blockGroups, horizontal: false);

            return removedCandidatesRows || removedCandidatesColumns;
        }

        private bool CheckTwoOptionsWithinBlockGroups(List<List<Field>> blockGroups, bool horizontal)
        {
            bool removedCandidates = false;

            foreach (var blockGroup in blockGroups)
            {
                for (int value = 1; value <= 9; value++)
                {
                    var blockGroupData = GetBlockData(value, blockGroup);

                    if (horizontal)
                    {
                        if (CheckValueCanFitInTwoRowsInTwoBlocksOnly(value, blockGroupData))
                            removedCandidates = true;
                    }
                    else // vertical
                    {
                        if(CheckValueCanFitInTwoColumnsInTwoBlocksOnly(value, blockGroupData))
                            removedCandidates = true;
                    }
                }
            }
            return removedCandidates;
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

        private bool CheckValueCanFitInTwoRowsInTwoBlocksOnly(int value, List<BlockData> blockData)
        {
            var candidatesRemoved = false;

            var counts = blockData.Select(r => r.RowsContainingValue.Count());
            if (!(counts.Count(c => c == 2) == 2 && counts.Contains(3)))
                return false;

            // Resolve the row id's of the block where the value will fit in all three rows. 
            var resultTwoRows = blockData.Where(r => r.RowsContainingValue.Count() == 2).First();
            var rowsTwo = resultTwoRows.RowsContainingValue.SelectMany(g => g).Select(f => f.Row).Distinct();
            var resultThreeRows = blockData.Where(r => r.RowsContainingValue.Count() == 3).First();
            var rowsThree = resultThreeRows.RowsContainingValue.SelectMany(g => g).Select(f => f.Row).Distinct();

            // Remove candidates from the block regarding the rowsTwo rows
            foreach (var row in rowsTwo)
            {
                var targetFields = resultThreeRows.Block.Where(f => f.Row == row);
                if (RemoveValueFromTargetFieldsCandidates(value, targetFields))
                    candidatesRemoved = true;
            }
            return candidatesRemoved;
        }

        private static bool CheckValueCanFitInTwoColumnsInTwoBlocksOnly(int value, List<BlockData> blockData)
        {
            var candidatesRemoved = false;

            var counts = blockData.Select(r => r.ColumnsContainingValue.Count());
            if (!(counts.Count(c => c == 2) == 2 && counts.Contains(3)))
                return false;

            // Resolve the column id's of the block where the value will fit in all three columns. 
            var resultTwoColumns = blockData.Where(r => r.ColumnsContainingValue.Count() == 2).First();
            var columnsTwo = resultTwoColumns.ColumnsContainingValue.SelectMany(g => g).Select(f => f.Column).Distinct();
            var resultThreeColumns = blockData.Where(r => r.ColumnsContainingValue.Count() == 3).First();
            var columnsThree = resultThreeColumns.ColumnsContainingValue.SelectMany(g => g).Select(f => f.Column).Distinct();

            // Remove candidates from the block regarding the columnsTwo columns
            foreach (var column in columnsTwo)
            {
                var targetFields = resultThreeColumns.Block.Where(f => f.Column == column);
                if (RemoveValueFromTargetFieldsCandidates(value, targetFields))
                    candidatesRemoved = true;
            }
            return candidatesRemoved;
        }

        private static bool RemoveValueFromTargetFieldsCandidates(int value, IEnumerable<Field> targetFields)
        {
            int candidatesCountBefore = GetSumOfCandidatesCounts(targetFields);
            targetFields.RemoveValueFromCandidates(value);
            int candidatesCountAfter = GetSumOfCandidatesCounts(targetFields);

            return candidatesCountAfter != candidatesCountBefore;
        }

        private static int GetSumOfCandidatesCounts(IEnumerable<Field> targetFields)
        {
            return targetFields.Select(f => f.Candidates.Count).Aggregate((a, b) => a + b);
        }

        private static IEnumerable<IGrouping<int, Field>> ResolveRowsContainingValue(int value, List<Field> block)
        {
            return block.Where(f => f.Candidates.Contains(value)).GroupBy(f => f.Row);
        }

        private static IEnumerable<IGrouping<int, Field>> ResolveColumnsContainingValue(int value, List<Field> block)
        {
            return block.Where(f => f.Candidates.Contains(value)).GroupBy(f => f.Column);
        }

        private bool CheckFieldsWithSimilarCandidates()
        {            
            // Identify situations where, regarding a segment, two fields contain only two different possible values (or three in the case of three fields, etc.)
            // For example, in block 1, fields 1 and 3 contain candidates with the possible values 4 and 8.
            // Block 1, field 1; Candidates: 4 5 7 8
            // Block 1, field 3; Candidates: 4 5 8
            // In such cases, remove the other candidates (5 and 7) from fields 1 and 3.
            // This applies to blocks, rows, and columns.

            bool removedCandidates3 = false;

            for (int candidateCount = 2; candidateCount <= 4; candidateCount++)
            {
                for (int i = 1; i <= 9; i++)
                {
                    if (CheckFieldsWithSimilarCandidates(_fields.Where(f => f.Block == i), "Block", candidateCount))
                        removedCandidates3 = true;

                    if (CheckFieldsWithSimilarCandidates(_fields.Where(f => f.Row == i), "Row", candidateCount))
                        removedCandidates3 = true;

                    if (CheckFieldsWithSimilarCandidates(_fields.Where(f => f.Column == i), "Column", candidateCount))
                        removedCandidates3 = true;
                }
            }

            return removedCandidates3;
        }
        
        // Per block try to find situations where a value can only exist in one row or column (e.g. two fields in block 2 on the same row where only a 7 can go).
        // In those cases eliminate 7 as a candidate of fields on that row in the other horizontal blocks (= block 1 and 3) and see what happens.
        private bool CheckRemoveCandidatesInOutsideBlocks()
        {
            var removedCandidate = false;

            for (int block = 1; block <= 9; block++)
            {
                for (int value = 1; value <= 9; value++)
                {
                    var fieldsInBlockWithValueInCandidates = _fields.Where(f => f.Block == block && f.Candidates.Contains(value)).ToList();

                    if (fieldsInBlockWithValueInCandidates.Count <= 3)
                    {
                        // In same row?
                        if (fieldsInBlockWithValueInCandidates.GroupBy(f => f.Row).Count() == 1)
                        {
                            var result = RemoveCandidatesOutsideBlock(block, value, fieldsInBlockWithValueInCandidates, true);

                            if (result)
                                removedCandidate = true;
                        }

                        // In same column?
                        if (fieldsInBlockWithValueInCandidates.GroupBy(f => f.Column).Count() == 1)
                        {
                            var result = RemoveCandidatesOutsideBlock(block, value, fieldsInBlockWithValueInCandidates, false);

                            if (result)
                                removedCandidate = true;
                        }
                    }
                }
            }
            return removedCandidate;
        }

        private bool RemoveCandidatesOutsideBlock(int block, int value, List<Field> fieldsInBlockWithValueInCandidates, bool isRow)
        {
            var eliminatedOptionsFound = false;

            var fieldsOutsideBlock = _fields.Where(f => f.Block != block);
            fieldsOutsideBlock = isRow ?
                fieldsOutsideBlock.Where(f => f.Row == fieldsInBlockWithValueInCandidates[0].Row) :
                fieldsOutsideBlock.Where(f => f.Column == fieldsInBlockWithValueInCandidates[0].Column);

            eliminatedOptionsFound = fieldsOutsideBlock.CandidatesContainsValue(value);

            fieldsOutsideBlock.RemoveValueFromCandidates(value);
            return eliminatedOptionsFound;
        }

        private static bool CheckFieldsWithSimilarCandidates(IEnumerable<Field> fields, string segment, int candidateCount)
        {
            bool candidateRemoved = false;

            // Generate candidate combinations based on the input candidateCount
            var candidateNumbers = Enumerable.Range(1, 9).ToList();
            var candidateCombinations = GetCombinations(candidateNumbers, candidateCount);

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
                                    // Console.WriteLine($"Check {segment}: {candidateCount}-candidate combination: {string.Join(" ", combination)}, Removing candidate {candidate}"); Console.WriteLine(field.ToString());
                                    field.RemoveValueFromCandidates(candidate);
                                    candidateRemoved = true;
                                }
                            }
                        }
                    }
                }
            }
            return candidateRemoved;
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
                actual = _fields.Where(f => f.Row == i).Select(f => (int)f.Value).ToList();
                CheckValidity(actual, i, "Row");

                actual = _fields.Where(f => f.Column == i).Select(f => (int)f.Value).ToList();
                CheckValidity(actual, i, "Column");

                actual = _fields.Where(f => f.Block == i).Select(f => (int)f.Value).ToList();
                CheckValidity(actual, i, "Block");
            }
        }

        private void CheckValidity(List<int> actual, int i, string segment)
        {
            var expected = Enumerable.Range(1, 9).ToList();
            var result = expected.Except(actual);

            if (result.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(this);
                throw new InvalidOperationException($"Invalid solution! {segment} {i}, missing value: {result.First()} iteration {iteration}");
            };
        }

        public void PrintCandidatesPerField()
        {
            Console.WriteLine("################# DUMP START ###################");
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    Console.WriteLine(_fields2D[row, col]);
                }
            }
            Console.WriteLine("################## DUMP END ####################");
        }


        public override string ToString()
        {
            var output = string.Empty;

            for (int row = 0; row < 9; row++)
            {
                if(row % 3 == 0)
                    output += "+-----------+\n";

                for (int col = 0; col < 9; col++)
                {
                    if (col % 3 == 0)
                        output += "|";

                    output += _fields2D[row, col].Value == null ? " " : _fields2D[row, col].Value.ToString();
                }
                output += "|\n";
            }
            output += "+-----------+";

            return output;
        }

        private class BlockData
        {
            public int Value { get; set; }
            public List<Field> Block { get; set; }
            public IEnumerable<IGrouping<int, Field>> RowsContainingValue { get; set; }
            public IEnumerable<IGrouping<int, Field>> ColumnsContainingValue { get; set; }
        }
    }
}
