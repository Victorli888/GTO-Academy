namespace GTOTrainerApp.Models;

public class Player
{
    public string Name { get; set; } = string.Empty;
    public int Chips { get; set; }
    public int CurrentBet { get; set; }
    public List<Card> HoleCards { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public bool IsDealer { get; set; }
    public bool IsHuman { get; set; }
    public PlayerStyle Style { get; set; } = PlayerStyle.Balanced;
}

public enum PlayerStyle
{
    Aggressive,
    Nit,
    Balanced
}
