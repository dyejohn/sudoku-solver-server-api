using Microsoft.AspNetCore.Mvc;

namespace sudokubackendsolver.Controllers;

[ApiController]
[Route("[controller]")]

public class SolverController : Controller
{
    // POST
    [HttpPost]
    public SolverResponse Post(List<SolverRequestItem> solverRequestItems)
    {
        var solver = new Solver();
        solver.Initialize(solverRequestItems);
        solver.Solve();
        var solvedCells = solver.ReturnCellDifferences();
        
        var response = new SolverResponse();
        response.UpdatedValues = solvedCells;
        return response;
    }
}