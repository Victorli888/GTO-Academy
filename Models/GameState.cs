namespace GTOTrainerApp.Models;

public class GameState
{
    public List<Player> Players { get; set; } = new();
    public List<Card> CommunityCards { get; set; } = new();
    public int Pot { get; set; }
    public int CurrentBet { get; set; }
    public int DealerPosition { get; set; }
    public GamePhase Phase { get; set; } = GamePhase.PreFlop;
}

public enum GamePhase
{
    PreFlop,
    Flop,
    Turn,
    River,
    Showdown
}
