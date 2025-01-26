using SudokuSolver.Extensions;

namespace SudokuSolver
{
    public class Sudoku
    {
        private readonly Field[,] _fields2D;
        private readonly List<Field> _fields = [];

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
            bool removedCandidates1 = false;
            bool removedCandidates2 = false;
            bool removedCandidates3 = false;

            while (foundSolutionsBySlashing || foundSolutionsByElimination || removedCandidates1 || removedCandidates2 || removedCandidates3)
            {
                solutionsCount = _fields.Where(f => f.Value != null).Count();
                Console.WriteLine($"Solved: {solutionsCount}");

                if ( solutionsCount == _fields.Count )
                    break;

                foundSolutionsBySlashing = FindSolutionsBySlashing();
                foundSolutionsByElimination = FindSolutionsByElimination();                

                if (!foundSolutionsBySlashing && !foundSolutionsByElimination)
                {
                    // Complexity > 4 star puzzles. Time to bring out the big guns!                    
                    removedCandidates1 = TryToRemoveCandidatesInOutsideBlocks();
                    removedCandidates2 = CheckRowsAndColumnsWithinBlockGroup();
                    removedCandidates3 = CheckFieldsWithSimilarCandidates();
                }
            }

            var solved = solutionsCount == _fields.Count;

            Console.ForegroundColor = solved ? ConsoleColor.Green : ConsoleColor.Red;

            if (solved)
                Console.WriteLine("Solved:");
            else
                Console.WriteLine("Not solved:");

            Console.ResetColor();
            Console.WriteLine(this);

            return solved;
        }

        // Per value remove candidates from other fields within the same segment (row, column or block).
        // Solution is found by asserting that all other fields do not contain the value as a candidate in any segment.
        private bool FindSolutionsBySlashing()
        {
            bool solutionsFound = false;
            
            TryToRemoveCandidatesBySlashing();

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
                            field.Value = value;
                            field.Candidates = [value];
                            solutionsFound = true;
                        }
                    }
                }
            }
            return solutionsFound;
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

        // Per field remove candidates in other fields within the same segment (row, column or block).
        // Solution is found by asserting that only one candidate is left.
        private bool FindSolutionsByElimination()
        {
            bool solutionsFound = false;
            TryToRemoveCandidatesByElmination();

            foreach (var field in _fields.Where(f => f.Candidates.Count == 1))
            {
                if (field.Value == null)
                {
                    field.Value = field.Candidates[0];
                    solutionsFound = true;
                }
            }
            return solutionsFound;
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

        private bool CheckRowsAndColumnsWithinBlockGroup()
        {
            // There are six 'block groups'; three horizontal and three vertical;
            // First horizontal block group: block 1, block 2 and block 3
            // Last vertical block group: block 3, block 6 and block 9
            // For each block group check the following for values 1-9 (example: horizontal): 
            // Check if a value will only fit in the same TWO rows regarding two blocks.
            // If so then the OTHER row w.r. to the OTHER block cannot contain that value.
            // The same is true regarding vertical block groups.

            var blockGroups = new List<List<Field>>
            {
                // Horizontal
                _fields.Where(f => new[] { 1, 2, 3 }.Contains(f.Block)).ToList(),
                _fields.Where(f => new[] { 4, 5, 6 }.Contains(f.Block)).ToList(),
                _fields.Where(f => new[] { 7, 8, 9 }.Contains(f.Block)).ToList(),
                // Vertical
                // TODO
                //_fields.Where(f => new[] { 1, 4, 7 }.Contains(f.Block)).ToList(),
                //_fields.Where(f => new[] { 2, 5, 8 }.Contains(f.Block)).ToList(),
                //_fields.Where(f => new[] { 3, 6, 9 }.Contains(f.Block)).ToList()
            };

            foreach (var blockGroup in blockGroups)
            {
                for (int value = 1; value <=9 ; value++)
                {
                    var blockGroupData = GetBlockData(value, blockGroup);

                    CheckValueCanFitInTwoRowsInTwoBlocksOnly(value, blockGroup);
                }
            }

            // TODO
            return false;
        }

        private List<BlockData> GetBlockData(int value, List<Field> blockGroup)
        {
            var blockA = blockGroup.Where(b => b.Block == blockGroup[0].Block).ToList();
            var blockB = blockGroup.Where(b => b.Block == blockGroup[3].Block).ToList();
            var blockC = blockGroup.Where(b => b.Block == blockGroup[6].Block).ToList();

            var results = new List<BlockData>()
            {
                 new() { Block = blockA, RowsContainingValue = ResolveRowsContainingValue(value, blockA), ColumnsContainingValue = ResolveColumnsContainingValue(value, blockA) },
                 new() { Block = blockB, RowsContainingValue = ResolveRowsContainingValue(value, blockB), ColumnsContainingValue = ResolveColumnsContainingValue(value, blockB) },
                 new() { Block = blockC, RowsContainingValue = ResolveRowsContainingValue(value, blockC), ColumnsContainingValue = ResolveColumnsContainingValue(value, blockC) },
            };

            return results;
        }

        private class BlockData
        {
            int value {  get; set; }
            public List<Field> Block { get; set; }
            public IEnumerable<IGrouping<int, Field>> RowsContainingValue { get; set; }
            public IEnumerable<IGrouping<int, Field>> ColumnsContainingValue { get; set; }
        }

        private void CheckValueCanFitInTwoRowsInTwoBlocksOnly(int value, List<Field> blockGroup)
        {
            var blockA = blockGroup.Where(b => b.Block == blockGroup[0].Block).ToList();
            var blockARowsContainingValue = ResolveRowsContainingValue(value, blockA);
            var blockB = blockGroup.Where(b => b.Block == blockGroup[3].Block).ToList();
            var blockBRowsContainingValue = ResolveRowsContainingValue(value, blockB);
            var blockC = blockGroup.Where(b => b.Block == blockGroup[6].Block).ToList();
            var blockCRowsContainingValue = ResolveRowsContainingValue(value, blockC);
            
            var results = new List<BlockData>()
            {
                 new() { Block = blockA, RowsContainingValue = blockARowsContainingValue },
                 new() { Block = blockB, RowsContainingValue = blockBRowsContainingValue },
                 new() { Block = blockC, RowsContainingValue = blockCRowsContainingValue }
            };

            var counts = results.Select(r => r.RowsContainingValue.Count());
            if (!(counts.Count(c => c == 2) == 2 && counts.Contains(3)))
                return;

            // Resolve the row id's of the block where the value will fit in all three rows. 
            var resultTwoRows = results.Where(r => r.RowsContainingValue.Count() == 2).First();
            var rowsTwo =  resultTwoRows.RowsContainingValue.SelectMany(g => g).Select(f => f.Row).Distinct();
            var resultThreeRows = results.Where(r => r.RowsContainingValue.Count() == 3).First();
            var rowsThree = resultThreeRows.RowsContainingValue.SelectMany(g => g).Select(f => f.Row).Distinct();

            // Remove candidates from the block from the 'other' row.
            var targetRow = rowsThree.Except(rowsTwo).First();

            var targetFields = resultThreeRows.Block.Where(f => f.Row == targetRow);

            targetFields.RemoveValueFromCandidates(value);
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
        private bool TryToRemoveCandidatesInOutsideBlocks()
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
            var combinations = new List<ValueCombination>();
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
                                    //Console.WriteLine($"Check {segment}: {candidateCount}-candidate combination: {string.Join(" ", combination)}, Removing candidate {candidate}"); Console.WriteLine(field.ToString());
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

        public override string ToString()
        {
            var output = string.Empty;

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    output += _fields2D[row, col].Value == null ? " " : _fields2D[row, col].Value.ToString();
                    //output += $"{_fields2D[row, col]}\r\n";
                }
                output += "\r\n";
            }

            return output;
        }

        private class ValueCombination
        {
            public List<int> Values { get; set; }
            public List<Field> Fields { get; set; }
        }
    }
}
