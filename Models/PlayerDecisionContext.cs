namespace GTOTrainerApp.Models;

/// <summary>
/// Context information provided to AI players for making decisions.
/// This contains all the information a player needs to make an informed decision.
/// </summary>
public class PlayerDecisionContext
{
    /// <summary>
    /// The player making the decision
    /// </summary>
    public Player Player { get; set; } = null!;
    
    /// <summary>
    /// Current game state
    /// </summary>
    public GameState GameState { get; set; } = null!;
    
    /// <summary>
    /// The player's hole cards (only visible to this player)
    /// </summary>
    public List<Card> HoleCards => Player.HoleCards;
    
    /// <summary>
    /// Community cards visible to all players
    /// </summary>
    public List<Card> CommunityCards => GameState.CommunityCards;
    
    /// <summary>
    /// Current betting amount required to call
    /// </summary>
    public int AmountToCall => GameState.CurrentBet - Player.CurrentBet;
    
    /// <summary>
    /// Minimum raise amount
    /// </summary>
    public int MinRaise => GameState.MinRaise;
    
    /// <summary>
    /// Player's remaining chips
    /// </summary>
    public int RemainingChips => Player.Chips;
    
    /// <summary>
    /// Current pot size
    /// </summary>
    public int Pot => GameState.Pot;
    
    /// <summary>
    /// Current game phase (PreFlop, Flop, Turn, River)
    /// </summary>
    public GamePhase Phase => GameState.Phase;
    
    /// <summary>
    /// Player's position at the table (0-7)
    /// </summary>
    public int Position { get; set; }
    
    /// <summary>
    /// Number of active players (not folded, not all-in)
    /// </summary>
    public int ActivePlayerCount => GameState.Players.Count(p => !p.HasFolded && !p.IsAllIn);
    
    /// <summary>
    /// List of other players' actions this round (for context)
    /// </summary>
    public List<PlayerActionInfo> OtherPlayersActions { get; set; } = new();
}

/// <summary>
/// Information about another player's action
/// </summary>
public class PlayerActionInfo
{
    public string PlayerName { get; set; } = string.Empty;
    public PlayerAction Action { get; set; }
    public int Amount { get; set; }
    public bool IsAllIn { get; set; }
}

