namespace GTOTrainerApp.Models;

public class GameState
{
    public List<Player> Players { get; set; } = new();
    public List<Card> CommunityCards { get; set; } = new();
    public int Pot { get; set; }
    public int CurrentBet { get; set; }
    public int DealerPosition { get; set; }
    public int CurrentPlayerIndex { get; set; }
    public int SmallBlind { get; set; } = 10;
    public int BigBlind { get; set; } = 20;
    public int MinRaise { get; set; }
    public GamePhase Phase { get; set; } = GamePhase.PreFlop;
    public bool IsBettingRoundActive { get; set; }
    public int LastRaiseAmount { get; set; }
    public List<Card> Deck { get; set; } = new();
    public string GameMessage { get; set; } = "";
    public bool IsGameActive { get; set; } = false;
    public List<Player> Winners { get; set; } = new();
    public int SidePot { get; set; }
}

public enum GamePhase
{
    PreFlop,
    Flop,
    Turn,
    River,
    Showdown
}
