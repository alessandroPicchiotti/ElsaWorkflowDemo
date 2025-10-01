/*
Usa Event + PublishEvent - è il sostituto ufficiale di SignalReceived in Elsa 3.5.1 e funziona esattamente allo stesso modo!
*/

using Elsa.Workflows;
using Elsa.Workflows.Management.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ElsaWorkflowDemo.Api.Controllers;
[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    private readonly IActivityRegistry _activityRegistry;

    public DebugController(IActivityRegistry activityRegistry)
    {
        _activityRegistry = activityRegistry;
    }

    [HttpGet("activities")]
    public IActionResult GetActivities()
    {
        try
        {
            var activities = _activityRegistry.ListAll();
            var activityList = activities
                .OrderBy(a => a.Category)
                .ThenBy(a => a.Name)
                .Select(a => new
                {
                    //Type = a.Type,
                    Name = a.Name,
                    DisplayName = a.DisplayName ?? a.Name,
                    Category = a.Category ?? "Uncategorized",
                    Description = a.Description
                }).ToList();

            return Ok(new
            {
                TotalActivities = activityList.Count,
                Activities = activityList
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
        }
    }

    [HttpGet("check-signal")]
    public IActionResult CheckSignal()
    {
        try
        {
            // Verifica se il tipo SignalReceived esiste
            var signalType = Type.GetType("Elsa.Workflows.Activities.SignalReceived, Elsa.Workflows.Core");
            var exists = signalType != null;

            return Ok(new
            {
                SignalReceivedExists = exists,
                SignalType = signalType?.FullName,
                Message = exists ? "SignalReceived è disponibile" : "SignalReceived NON trovato"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
    [HttpGet("find-signal")]
    public IActionResult FindSignal()
    {
        var possibleTypes = new[]
        {
        "Elsa.Workflows.Activities.SignalReceived, Elsa.Workflows.Core",
        "Elsa.Workflows.Activities.SignalReceived, Elsa.Core",
        "Elsa.Workflows.Activities.SignalReceived, Elsa",
        "Elsa.Activities.SignalReceived, Elsa.Workflows.Core",
        "Elsa.Signals.Activities.SignalReceived, Elsa.Signals"
    };

        var results = new List<object>();

        foreach (var typeName in possibleTypes)
        {
            try
            {
                var type = Type.GetType(typeName);
                results.Add(new
                {
                    TypeName = typeName,
                    Exists = type != null,
                    FullName = type?.FullName
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    TypeName = typeName,
                    Exists = false,
                    Error = ex.Message
                });
            }
        }

        return Ok(results);
    }

}
