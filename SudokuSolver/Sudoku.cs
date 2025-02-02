using SudokuSolver.Extensions;
using System.Linq;

namespace SudokuSolver
{
    // Testen met examples in 'Diabolical Strategies' en 'Extreme Strategies': https://www.sudokuwiki.org/Finned_Swordfish, https://www.sudokuwiki.org/AIC_with_ALSs etc.
    public class Sudoku
    {
        private readonly Field[,] _fields2D;
        private readonly List<Field> _fields = [];

        // TODO lw beide
        private int iteration = 0;
        private bool printRemoveCandidateAndAddSolution = false;

        public Sudoku()
        {
            // Initialize fields
            _fields2D = new Field[9, 9];

            for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                {
                    var field = new Field(row + 1, col + 1, _fields);
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
            bool candidatesRemoved = false;

            while (foundSolutionsBySlashing || foundSolutionsByElimination || candidatesRemoved)
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
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Enter advanced algoritms");
                    //PrintCandidatesPerField(false);
                    Console.ForegroundColor = ConsoleColor.White;

                    // TODO lw
                    printRemoveCandidateAndAddSolution = true;

                    // Complexity > 4 star puzzles. Time to bring out the big guns!
                    candidatesRemoved = false;

                    if (CheckClothedCandidatesToStrip())
                        candidatesRemoved = true;

                    if (CheckNakedCombinations())
                        candidatesRemoved = true;   
                    
                    if(!candidatesRemoved)
                        if (CheckAdvancedSlashing())
                            candidatesRemoved = true;

                    if (!candidatesRemoved)
                        if (CheckTwoOptionsWithinBlockGroups())
                            candidatesRemoved = true;

                    if (!candidatesRemoved)
                        if (CheckLockedCandidates())
                            candidatesRemoved = true;

                    if (!candidatesRemoved)
                        if(CheckXWingRows() || CheckXWingColumns())
                            candidatesRemoved = true;

                    if (!candidatesRemoved)
                        if (CheckYWing())
                            candidatesRemoved = true;

                    // TODO lw
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Exit advanced algoritms");
                    Console.ForegroundColor = ConsoleColor.White;
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
            return CheckAbsentValuesInCandidatesOfOtherSegments();
        }

        private void TryToRemoveCandidatesBySlashing()
        {
            foreach (var field in _fields)
            {
                if (field.Value != null)
                {
                    field.OtherRowFields().RemoveValueFromCandidates((int)field.Value);
                    field.OtherColumnFields().RemoveValueFromCandidates((int)field.Value);
                    field.OtherBlockFields().RemoveValueFromCandidates((int)field.Value);
                }
            }
        }

        private bool CheckAbsentValuesInCandidatesOfOtherSegments()
        {
            bool solutionsFound = false;

            foreach (var field in _fields)
            {
                for (int value = 1; value <= 9; value++)
                {
                    if (field.Value == null)
                    {
                        if (!field.OtherRowFields().CandidatesContainsValue(value) ||
                            !field.OtherColumnFields().CandidatesContainsValue(value) ||
                            !field.OtherBlockFields().CandidatesContainsValue(value))
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

                            // TODO lw
                            if (printRemoveCandidateAndAddSolution)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Found solution (1): {field}");
                                Console.ForegroundColor = ConsoleColor.White;
                            }
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
            return CheckNakedSingles();
        }

        private void TryToRemoveCandidatesByElmination()
        {
            foreach (var field in _fields)
            {
                if (field.Value == null)
                {
                    for (int value = 1; value <= 9; value++)
                    {
                        if (field.OtherRowFields().ContainsValue(value))
                            field.RemoveValueFromCandidates(value);

                        if (field.OtherColumnFields().ContainsValue(value))
                            field.RemoveValueFromCandidates(value);

                        if (field.OtherBlockFields().ContainsValue(value))
                            field.RemoveValueFromCandidates(value);
                    }
                }
            }
        }

        private bool CheckNakedSingles()
        {
            bool solutionsFound = false;

            foreach (var field in _fields.Where(f => f.Candidates.Count == 1))
            {
                if (field.Value == null)
                {
                    if (field.OtherRowFields().ContainsValue(field.Candidates[0]) ||
                        field.OtherColumnFields().ContainsValue(field.Candidates[0]) ||
                        field.OtherBlockFields().ContainsValue(field.Candidates[0]))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(this);
                        throw new ArgumentException($"Invalid single candidate: {field.Candidates[0]}! Field: {field}");
                    }

                    field.Value = field.Candidates[0];
                    solutionsFound = true;


                    if (printRemoveCandidateAndAddSolution)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Found solution (2): {field}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
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
            var candidatesRemovedRows = CheckTwoOptionsWithinBlockGroups(blockGroups, horizontal:true);

            // Vertical
            blockGroups = new List<List<Field>>
            {
                _fields.Where(f => new[] { 1, 4, 7 }.Contains(f.Block)).ToList(),
                _fields.Where(f => new[] { 2, 5, 8 }.Contains(f.Block)).ToList(),
                _fields.Where(f => new[] { 3, 6, 9 }.Contains(f.Block)).ToList()
            };
            var candidatesRemovedColumns = CheckTwoOptionsWithinBlockGroups(blockGroups, horizontal: false);

            return candidatesRemovedRows || candidatesRemovedColumns;
        }

        private bool CheckTwoOptionsWithinBlockGroups(List<List<Field>> blockGroups, bool horizontal)
        {
            bool candidatesRemoved = false;

            foreach (var blockGroup in blockGroups)
            {
                for (int value = 1; value <= 9; value++)
                {
                    var blockGroupData = GetBlockData(value, blockGroup);

                    if (horizontal)
                    {
                        if (CheckValueCanFitInTwoRowsInTwoBlocksOnly(value, blockGroupData))
                            candidatesRemoved = true;
                    }
                    else // vertical
                    {
                        if(CheckValueCanFitInTwoColumnsInTwoBlocksOnly(value, blockGroupData))
                            candidatesRemoved = true;
                    }
                }
            }
            return candidatesRemoved;
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

            // Check if the two rows where the value can fit are identical for both
            var resultsTwoRows = blockData.Where(r => r.RowsContainingValue.Count() == 2).ToList();
            var rowsTwoFirst = resultsTwoRows[0].RowsContainingValue.SelectMany(g => g).Select(f => f.Row).Distinct().ToList();
            var rowsTwoSecond = resultsTwoRows[1].RowsContainingValue.SelectMany(g => g).Select(f => f.Row).Distinct().ToList();

            if (!(rowsTwoFirst[0] == rowsTwoSecond[0] && rowsTwoFirst[1] == rowsTwoSecond[1]))
                return false;

            // Resolve the row id's of the block where the value will fit in all three rows. 
            var rowsTwo = resultsTwoRows[0].RowsContainingValue.SelectMany(g => g).Select(f => f.Row).Distinct();
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

            // Check if the two columns where the value can fit are identical for both
            var resultsTwoColumns = blockData.Where(r => r.ColumnsContainingValue.Count() == 2).ToList();
            var columnsTwoFirst = resultsTwoColumns[0].ColumnsContainingValue.SelectMany(g => g).Select(f => f.Column).Distinct().ToList();
            var columnsTwoSecond = resultsTwoColumns[1].ColumnsContainingValue.SelectMany(g => g).Select(f => f.Column).Distinct().ToList();

            if (!(columnsTwoFirst[0] == columnsTwoSecond[0] && columnsTwoFirst[1] == columnsTwoSecond[1]))
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

        // Identify situations where, regarding a segment, two fields contain only two different possible values (or three in the case of three fields, etc.)
        // For example, in block 1, fields 1 and 3 contain candidates with the possible values 4 and 8.
        // Block 1, field 1; Candidates: 4 5 7 8
        // Block 1, field 3; Candidates: 4 5 8
        // In such cases, remove the other candidates (5 and 7) from fields 1 and 3. This applies to all segments ( blocks, rows, and columns).
        private bool CheckClothedCandidatesToStrip()
        {            
            bool candidatesRemoved = false;

            for (int candidateCount = 2; candidateCount <= 4; candidateCount++)
            {
                for (int i = 1; i <= 9; i++)
                {
                    if (CheckFieldsWithSimilarCandidates(_fields.Blocks(i), "Block", candidateCount))
                        candidatesRemoved = true;

                    if (CheckFieldsWithSimilarCandidates(_fields.Rows(i), "Row", candidateCount))
                        candidatesRemoved = true;

                    if (CheckFieldsWithSimilarCandidates(_fields.Columns(i), "Column", candidateCount))
                        candidatesRemoved = true;
                }
            }

            return candidatesRemoved;
        }

        // Try to look 'naked pairs'; if in a segment two field contain the same two candidates then those values can only go in those two fields.
        // As a consequence those candidates maybe stripped from the other fields in that segment (row, column or block)
        private bool CheckNakedCombinations()
        {
            bool candidatesRemoved = false;

            IEnumerable<Field> fields;

            for (int i = 1; i <= 9; i++)
            {
                fields = _fields.Blocks(i);
                if (CheckNakedPairs(fields) || CheckNakedTriplets(fields))
                    candidatesRemoved = true;

                fields = _fields.Rows(i);
                if (CheckNakedPairs(fields) || CheckNakedTriplets(fields))
                    candidatesRemoved = true;

                fields = _fields.Columns(i);
                if (CheckNakedPairs(fields) || CheckNakedTriplets(fields))
                    candidatesRemoved = true;
            }

            return candidatesRemoved;
        }

        private static bool CheckNakedPairs(IEnumerable<Field> fields)
        {
            bool candidatesRemoved = false;

            // Generate candidate combinations
            var candidateCombinations = GetCombinations(Enumerable.Range(1, 9).ToList(), 2);

            var fieldsWithTwoCandidates = fields.Where(f => f.Candidates.Count == 2);
            if (fieldsWithTwoCandidates.Count() < 2)            
                return false;

            foreach (var combination in candidateCombinations)
            {
                var fieldsWithTwoIdenticalCandidates = fieldsWithTwoCandidates.Where(f => f.Candidates[0] == combination[0] && f.Candidates[1] == combination[1]).ToList();

                if(fieldsWithTwoIdenticalCandidates.Count() == 2)
                {
                    // Remove the two candidates from the other fields in the segement
                    var otherFields = fields.Except(fieldsWithTwoIdenticalCandidates);
                    candidatesRemoved = RemoveCandidates(candidatesRemoved, combination, otherFields);
                    break;
                }
            }
            return candidatesRemoved;
        }

        private static bool CheckNakedTriplets(IEnumerable<Field> fields)
        {
            bool candidatesRemoved = false;

            // Generate candidate combinations based on the input candidateCount
            var candidateCombinations = GetCombinations(Enumerable.Range(1, 9).ToList(), 3);

            var fieldsWithTwoCandidates = fields.Where(f => f.Candidates.Count == 2); // 2 is correct
            if (fieldsWithTwoCandidates.Count() < 3)
                return false;

            foreach (var combination in candidateCombinations)
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

                candidatesRemoved = RemoveCandidates(candidatesRemoved, combination, otherFields);

            }
            return candidatesRemoved;
        }

        private bool CheckLockedCandidates()
        {
            var candidatesRemoved = false;

            for (int i = 1; i <= 9; i++)
            {
                var block = _fields.Blocks(i).ToList();

                // Per block check per row/column if a value can only fit in that row/column and not in 
                // the field in the outside blocks.
                if (CheckLockedCandidatesPerBlockRow(block) || CheckLockedCandidatesPerBlockColumn(block))
                    if (!candidatesRemoved)
                        candidatesRemoved = true;
            }
            return candidatesRemoved;
        }

        private bool CheckLockedCandidatesPerBlockRow(List<Field> block)
        {
            var candidatesRemoved = false;

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

                candidatesRemoved = RemoveCandidates(candidatesRemoved, candidates, otherRowFields);

            }
            return candidatesRemoved;
        }

        private bool CheckLockedCandidatesPerBlockColumn(List<Field> block)
        {
            var candidatesRemoved = false;

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

                candidatesRemoved = RemoveCandidates(candidatesRemoved, candidates, otherColumnFields);
            }

            return candidatesRemoved;
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
        private bool CheckXWingRows()
        {
            var candidatesRemoved = false;

            for (int value = 1; value <= 9; value++)
            {
                var xWingFields = new List<List<Field>>();

                for (int column = 1; column <= 9; column++)
                {
                    var columnFields = _fields.Columns(column);
                    var FieldsContainingCandidate = columnFields.Where(f => f.Candidates.Contains(value)).ToList();

                    if (FieldsContainingCandidate.Count == 2)
                        xWingFields.Add(FieldsContainingCandidate);
                }

                if (xWingFields.Count != 2)
                    continue;

                // Occurrence in identical rows regading both columns??
                if (xWingFields[0][0].Row == xWingFields[1][0].Row && xWingFields[0][1].Row == xWingFields[1][1].Row)
                {
                    for (int i = 0; i <= 1; i++)
                    {
                        var otherFieldsInRow = _fields.Rows(xWingFields[0][i].Row).Except(new List<Field> { xWingFields[0][i], xWingFields[1][i] });
                        var candidatesWereRemoved = RemoveValueFromCandidates(value, otherFieldsInRow);

                        if (!candidatesRemoved)
                            candidatesRemoved = candidatesWereRemoved;
                    }
                }
            }
            return candidatesRemoved;
        }

        // Google: Sudoku X-Wing strategy explained
        private bool CheckXWingColumns()
        {
            var candidatesRemoved = false;

            for (int value = 1; value <= 9; value++)
            {
                var xWingFields = new List<List<Field>>();

                for (int row = 1; row <= 9; row++)
                {
                    var rowFields = _fields.Rows(row);
                    var FieldsContainingCandidate = rowFields.Where(f => f.Candidates.Contains(value)).ToList();

                    if (FieldsContainingCandidate.Count == 2)
                        xWingFields.Add(FieldsContainingCandidate);
                }

                if (xWingFields.Count != 2)
                    continue;

                // Occurrence in identical columns regading both rows??
                if (xWingFields[0][0].Column == xWingFields[1][0].Column && xWingFields[0][1].Column == xWingFields[1][1].Column)
                {
                    for (int i = 0; i <= 1; i++)
                    {
                        var otherFieldsInColumn = _fields.Columns(xWingFields[0][i].Column).Except(new List<Field> {xWingFields[0][i], xWingFields[1][i]});
                        var candidatesWereRemoved = RemoveValueFromCandidates(value, otherFieldsInColumn);

                        if (!candidatesRemoved)
                            candidatesRemoved = candidatesWereRemoved;
                    }
                }
            }
            return candidatesRemoved;
        }

        // Google: Sudoku Y-Wing or XY-Wing strategy explained
        private bool CheckYWing()
        {
            // First try to locate three buddy fields;
            // xy = 'middle' field. F.i. candidates 1, 5
            // xz = 'wing 1' field. F.i. candidates 1, 2
            // yz = 'wing 2' field. F.i. candidates 2, 5

            var fields2CandidatesOk = _fields.WithNumberOfCandidates(2);

            var fields2Candidates = new List<Field>() { _fields2D[1, 1] };

            foreach (Field field in fields2Candidates)
            {
                var a = field.OtherRowFields().WithNumberOfCandidates(2);
            }

            return false;
        }


        // Per block try to find situations where a value can only exist in one row or column (e.g. two fields in block 2 on the same row where only a 7 can go).
        // In those cases eliminate 7 as a candidate of fields on that row in the other horizontal blocks (= block 1 and 3) and see what happens.
        private bool CheckAdvancedSlashing()
        {
            var candidatesRemoved = false;

            for (int block = 1; block <= 9; block++)
            {
                for (int value = 1; value <= 9; value++)
                {
                    var fieldsInBlockWithValueInCandidates = _fields.Blocks(block).Where(f => f.Candidates.Contains(value)).ToList();

                    if (fieldsInBlockWithValueInCandidates.Count <= 3)
                    {
                        // In same row?
                        if (fieldsInBlockWithValueInCandidates.GroupBy(f => f.Row).Count() == 1)
                        {
                            var result = RemoveCandidatesOutsideBlock(block, value, fieldsInBlockWithValueInCandidates, true);

                            if (result)
                                candidatesRemoved = true;
                        }

                        // In same column?
                        if (fieldsInBlockWithValueInCandidates.GroupBy(f => f.Column).Count() == 1)
                        {
                            var result = RemoveCandidatesOutsideBlock(block, value, fieldsInBlockWithValueInCandidates, false);

                            if (result)
                                candidatesRemoved = true;
                        }
                    }
                }
            }
            return candidatesRemoved;
        }

        private bool RemoveCandidatesOutsideBlock(int block, int value, List<Field> fieldsInBlockWithValueInCandidates, bool isRow)
        {
            IEnumerable<Field> fieldsOutsideBlock = _fields.Where(f => f.Block != block);

            fieldsOutsideBlock = isRow ?
                fieldsOutsideBlock.Rows(fieldsInBlockWithValueInCandidates[0].Row) :
                fieldsOutsideBlock.Columns(fieldsInBlockWithValueInCandidates[0].Column);

            return RemoveValueFromCandidates(value, fieldsOutsideBlock);
        }

        private static bool CheckFieldsWithSimilarCandidates(IEnumerable<Field> fields, string segment, int candidateCount)
        {
            bool candidatesRemoved = false;

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
                                    candidatesRemoved = true;
                                }
                            }
                        }
                    }
                }
            }
            return candidatesRemoved;
        }

        private static bool RemoveCandidates(bool candidatesRemoved, List<int> candidates, IEnumerable<Field> otherFields)
        {
            foreach (int candidate in candidates)
            {
                var candidatesWereRemoved = RemoveValueFromCandidates(candidate, otherFields);

                if (!candidatesRemoved)
                    candidatesRemoved = candidatesWereRemoved;
            }
            return candidatesRemoved;
        }

        private static bool RemoveValueFromCandidates(int candidate, IEnumerable<Field> fields)
        {
            int candidatesCountBefore = GetSumOfCandidatesCounts(fields);
            fields.RemoveValueFromCandidates(candidate);
            int candidatesCountAfter = GetSumOfCandidatesCounts(fields);

            return candidatesCountBefore != candidatesCountAfter;
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
            Console.ForegroundColor = ConsoleColor.White;

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
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(this);
                throw new InvalidOperationException($"Invalid solution! {segment} {i}, missing value: {result.First()} iteration {iteration}");
            };
        }

        public void PrintCandidatesPerField(bool perRow)
        {
            Console.WriteLine("################# PRINT START ###################");

            if (perRow)
            {
                for (int row = 0; row < 9; row++)
                {
                    for (int col = 0; col < 9; col++)
                    {
                        Console.WriteLine(_fields2D[row, col]);
                    }
                }
            }
            else
            {
                for (int col = 0; col < 9; col++)
                {
                    for (int row = 0; row < 9; row++)
                    {
                        Console.WriteLine(_fields2D[row, col]);
                    }
                }
            }

            Console.WriteLine("################## PRINT END ####################");
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

        private class BlockData
        {
            public int Value { get; set; }
            public List<Field> Block { get; set; }
            public IEnumerable<IGrouping<int, Field>> RowsContainingValue { get; set; }
            public IEnumerable<IGrouping<int, Field>> ColumnsContainingValue { get; set; }
        }
    }
}
