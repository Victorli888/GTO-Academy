using GTOTrainerApp.Models;
using Microsoft.Extensions.Logging;

namespace GTOTrainerApp.Services;

/// <summary>
/// Simple player decision service that always calls/checks.
/// This is a placeholder for testing until Claude API is integrated.
/// </summary>
public class SimplePlayerDecisionService : IPlayerDecisionService
{
    private readonly ILogger<SimplePlayerDecisionService> _logger;
    
    public SimplePlayerDecisionService(ILogger<SimplePlayerDecisionService> logger)
    {
        _logger = logger;
    }
    
    public Task<(PlayerAction action, int raiseAmount)> DecideActionAsync(PlayerDecisionContext context)
    {
        _logger.LogDebug("SimplePlayerDecisionService: {PlayerName} deciding action. AmountToCall: {Amount}, Chips: {Chips}", 
            context.Player.Name, context.AmountToCall, context.RemainingChips);
        
        // Simple strategy: Always call/check (never fold, never raise)
        // This ensures we get to showdown for testing
        
        if (context.AmountToCall == 0)
        {
            // No bet to call, so check
            _logger.LogDebug("SimplePlayerDecisionService: {PlayerName} checking", context.Player.Name);
            return Task.FromResult((PlayerAction.Check, 0));
        }
        else if (context.AmountToCall >= context.RemainingChips)
        {
            // Can't afford to call, go all-in
            _logger.LogDebug("SimplePlayerDecisionService: {PlayerName} going all-in (can't afford call)", context.Player.Name);
            return Task.FromResult((PlayerAction.AllIn, 0));
        }
        else
        {
            // Call the current bet
            _logger.LogDebug("SimplePlayerDecisionService: {PlayerName} calling ${Amount}", context.Player.Name, context.AmountToCall);
            return Task.FromResult((PlayerAction.Call, 0));
        }
    }
}

