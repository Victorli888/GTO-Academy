using GTOTrainerApp.Models;

namespace GTOTrainerApp.Services;

/// <summary>
/// Interface for player decision-making services.
/// This allows plugging in different AI strategies (simple rules, Claude API, etc.)
/// </summary>
public interface IPlayerDecisionService
{
    /// <summary>
    /// Determines what action a player should take given the current game context.
    /// </summary>
    /// <param name="context">The decision context containing all relevant game information</param>
    /// <returns>A tuple containing the action and optional raise amount (if raising)</returns>
    Task<(PlayerAction action, int raiseAmount)> DecideActionAsync(PlayerDecisionContext context);
}

