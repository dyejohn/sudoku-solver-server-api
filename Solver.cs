namespace sudokubackendsolver;

public class Solver
{
    private List<Cell> _initialCellList = new();
    private List<SolverCell> _solvingList = new();
    // rows, columns, and boxes are really just cell groups - and can be treated as such.
    private List<CellGroup> _cellGroups = new();
    
    public void Initialize(List<SolverRequestItem> items)
    {
        ProcessCellList(items);
        
        // do all the rows.
        for (int i = 1; i < 10; i++)
        {
            var cellGroupRow = new CellGroup() { Name = $"Row {i}" };
            // get all the cells with that Y for the row.
            cellGroupRow.Cells = _solvingList.Where(cell => cell.Y == i).ToList();
            _cellGroups.Add(cellGroupRow);

            var cellGroupColumn = new CellGroup() { Name = $"Column {i}" };
            cellGroupColumn.Cells = _solvingList.Where(cell => cell.X == i).ToList();
            _cellGroups.Add(cellGroupColumn);
        }
        
        // boxes are a little weirder.
        _cellGroups.Add(MakeBox("UpperLeftBox", 1, 3, 1, 3));
        _cellGroups.Add(MakeBox("UpperCenterBox", 4, 6, 1, 3));
        _cellGroups.Add(MakeBox("UpperRightBox", 7, 9, 1, 3));

        _cellGroups.Add(MakeBox("CenterLeftBox", 1, 3, 4, 6));
        _cellGroups.Add(MakeBox("CenterCenterBox", 4, 6, 4, 6));
        _cellGroups.Add(MakeBox("CenterRightBox", 7, 9, 4, 6));

        _cellGroups.Add(MakeBox("LowerLeftBox", 1, 3, 7, 9));
        _cellGroups.Add(MakeBox("LowerCenterBox", 4, 6, 7, 9));
        _cellGroups.Add(MakeBox("LowerRightBox", 7, 9, 7, 9));

        // go through all groups and add the cross reference back to the cell. 
        foreach (var cellgroup in _cellGroups)
        {
            foreach (var cell in cellgroup.Cells)
            {
                cell.AssociatedGroups.Add(cellgroup);
            }
        }
    }

    private void ProcessCellList(List<SolverRequestItem> items)
    {
        foreach (var solverRequestItem in items)
        {
            // parse the coords from the name. 
            var coords = ParseCoordinateNameIntoCoordinate(solverRequestItem.Square);
            // I do not want these to be references to the same list.
            _initialCellList.Add(new Cell() { X = coords.X, Y = coords.Y, Value = ParseCellValue(solverRequestItem.Value)});
            _solvingList.Add(new SolverCell(){ X = coords.X, Y = coords.Y, Value = ParseCellValue(solverRequestItem.Value)});
        }
    }

    public List<SolverRequestItem> ReturnCellDifferences()
    {
        List<SolverRequestItem> solvedCells = new List<SolverRequestItem>();
        foreach (var solvedCell in _solvingList)
        {
            if (_initialCellList.Where(cell => cell.X == solvedCell.X && cell.Y == solvedCell.Y).Single().Value !=
                solvedCell.Value)
            {
                solvedCells.Add(new SolverRequestItem(){ Square = ParseCoordinateIntoCoordinateName(solvedCell.X, solvedCell.Y), Value = solvedCell.Value.ToString()});    
            }
        }

        return solvedCells;
    }

    // this will probably change to a complex result.
    public bool Solve()
    {
        bool solved = false;
        int unsolvedCellCount = _solvingList.Count(cell => cell.Value == 0);
        int[] previousUnsolvedCellCounts = new int[3] {0,0,0};
        
        // all solver blocks set up. Try to solve by elimination.

        // do this until we have a bad case or all values are found. stop if the number found doesn't fall for 3 passes.
        while (unsolvedCellCount > 0)
        {
            // make sure we're not stuck.
            if (previousUnsolvedCellCounts[0] == previousUnsolvedCellCounts[1] &&
                previousUnsolvedCellCounts[1] == previousUnsolvedCellCounts[2])
            {
                if (previousUnsolvedCellCounts[0] == unsolvedCellCount)
                {
                    // we're stuck
                    throw new ApplicationException("Unable to solve. 3 iterations at same value.");
                }
            }
            foreach (var cell in _solvingList.Where(cell => cell.Value == 0))
            {
                var possibleNumbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                // remove all the possible numbers from any that exist in all the connected groups. 
                // if zero exist, right now that's an error. eventually I'll add guessing and it won't be.
                foreach (var group in cell.AssociatedGroups)
                {
                    foreach (var groupedCell in group.Cells.Where(cell => cell.Value > 0))
                    {
                        // what happens if I try to remove a value I've already removed?
                        possibleNumbers.Remove(groupedCell.Value);
                    }
                }

                // if after all that, possible numbers is only 1 value.. we have a winner.
                if (possibleNumbers.Count() == 1)
                {
                    cell.Value = possibleNumbers[0];
                }
            }

            previousUnsolvedCellCounts[2] = previousUnsolvedCellCounts[1];
            previousUnsolvedCellCounts[1] = previousUnsolvedCellCounts[0];
            previousUnsolvedCellCounts[0] = unsolvedCellCount;
            unsolvedCellCount =  _solvingList.Count(cell => cell.Value == 0);

            if (previousUnsolvedCellCounts[0] == unsolvedCellCount)
            {
                // we're at risk of failing. go to phase 2. 
                // take a set, take a number, and see if only one cell can have that number.
                foreach (var group in _cellGroups)
                {
                    // we only have to check the numbers possible in the group.
                    var availableNumbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                    foreach (var usedNumber in group.Cells.Where(cell => cell.Value > 0).Select(cell => cell.Value).ToList())
                    {
                        availableNumbers.Remove(usedNumber);
                    }
                    
                    // now check every remaining cell and see if it can be the only one that can have that value.
                    var unfilledCells = group.Cells.Where(cell => cell.Value == 0).ToList();
                    List<SolverCell> matchedCells = new(); 
                    foreach (var availableNumber in availableNumbers)
                    {
                        matchedCells.Clear();
                        // for each unfilled cell
                        bool foundConflict = false;
                        foreach (var unfilledCell in unfilledCells)
                        {
                            if (CanCellAllowValue(unfilledCell, availableNumber))
                            {
                                matchedCells.Add(unfilledCell);
                            }
                        }

                        if (matchedCells.Count() == 1)
                        {
                            // winner!
                            matchedCells.Single().Value = availableNumber;
                        }
                    }
                }
            }
            unsolvedCellCount =  _solvingList.Count(cell => cell.Value == 0);
        }

        return true;
    }

    private bool CanCellAllowValue(SolverCell cell, int number)
    {
        // look in the other groups tied to it.
        foreach (var groupedToCell in cell.AssociatedGroups)
        {
            // and look at the values in that group.
            if (groupedToCell.Cells.Select(cell => cell.Value).ToList()
                .Contains(number))
            {
                return false;
            }
        }

        return true;
    }
    
    private CellGroup MakeBox(string boxName, int xLowerLimit, int xUpperLimit, int ylowerLimit, int yUpperLimit)
    {
        var box = new CellGroup()
        {
            Name = boxName, Cells =
                _solvingList.Where(cell =>
                        cell.X >= xLowerLimit && cell.X <= xUpperLimit && cell.Y >= ylowerLimit &&
                        cell.Y <= yUpperLimit)
                    .ToList()
        };

        return box;
    }
    
    private int ParseCellValue(string cellValue)
    {
        if (string.IsNullOrWhiteSpace(cellValue))
        {
            return 0;
        }

        int initialParse = 0;
        if (!int.TryParse(cellValue, out initialParse))
        {
            throw new ApplicationException("Cell value is non-numeric");
        }
        
        if (initialParse < 0 || initialParse > 9)
        {
            throw new ApplicationException($"Cell Value is outside allowed range. Parsed {initialParse}");
        }

        return initialParse;
    }
    
    private (int X, int Y) ParseCoordinateNameIntoCoordinate(string coordinateName)
    {
        // make sure input is valid.
        if (coordinateName.ToLower()[0] != 'x' || coordinateName.ToLower()[4] != 'y')
        {
            throw new ApplicationException("coordinate names appear invalid, expected in x-#-y-# format");
        }

        var x = int.Parse(coordinateName.Substring(2, 1));
        var y = int.Parse(coordinateName.Substring(6, 1));
        if (x < 1 || x > 9 || y <1 || y > 9)
        {
            throw new ApplicationException($"parsed x as {x} and y as {y} which are outside bounds ");
        }

        return (x, y);
    }

    private string ParseCoordinateIntoCoordinateName(int X, int Y)
    {
        return $"x-{X}-y-{Y}";
    }

}