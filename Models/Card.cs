namespace GTOTrainerApp.Models;

public class Card
{
    public string Suit { get; set; } = string.Empty;
    public string Rank { get; set; } = string.Empty;
    
    public int NumericValue
    {
        get
        {
            return Rank switch
            {
                "A" => 14,
                "K" => 13,
                "Q" => 12,
                "J" => 11,
                "10" => 10,
                "9" => 9,
                "8" => 8,
                "7" => 7,
                "6" => 6,
                "5" => 5,
                "4" => 4,
                "3" => 3,
                "2" => 2,
                _ => 0
            };
        }
    }
}
