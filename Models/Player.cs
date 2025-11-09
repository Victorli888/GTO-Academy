namespace GTOTrainerApp.Models;

public class Player
{
    public string Name { get; set; } = string.Empty;
    public int Chips { get; set; }
    public int CurrentBet { get; set; }
    public int TotalBetThisRound { get; set; }
    public List<Card> HoleCards { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public bool IsDealer { get; set; }
    public bool IsHuman { get; set; }
    public bool IsAllIn { get; set; }
    public bool HasFolded { get; set; }
    public bool HasActed { get; set; }
    public PlayerStyle Style { get; set; } = PlayerStyle.Balanced;
    public PlayerAction LastAction { get; set; } = PlayerAction.None;
    public int LastBetAmount { get; set; }
    public HandRanking BestHand { get; set; } = new();
    public int HandStrength { get; set; }
}

public enum PlayerStyle
{
    Aggressive,
    Nit,
    Balanced
}

public enum PlayerAction
{
    None,
    Fold,
    Check,
    Call,
    Raise,
    AllIn
}

public class HandRanking
{
    public HandType Type { get; set; }
    public int Value { get; set; }
    public List<Card> Kickers { get; set; } = new();
    public string Description { get; set; } = "";
}

public enum HandType
{
    HighCard = 1,
    Pair = 2,
    TwoPair = 3,
    ThreeOfAKind = 4,
    Straight = 5,
    Flush = 6,
    FullHouse = 7,
    FourOfAKind = 8,
    StraightFlush = 9,
    RoyalFlush = 10
}
