namespace sudokubackendsolver;

public class SolverResponse
{
    public List<SolverRequestItem> UpdatedValues { get; set; } = new List<SolverRequestItem>();
    public bool Solved { get; set; } = false;
}