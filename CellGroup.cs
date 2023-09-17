namespace sudokubackendsolver;

public class CellGroup
{
    public string Name { get; set; }
    public List<SolverCell> Cells { get; set; } = new List<SolverCell>();
}