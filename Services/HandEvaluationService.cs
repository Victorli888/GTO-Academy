using GTOTrainerApp.Models;

namespace GTOTrainerApp.Services;

public class HandEvaluationService
{
    public HandRanking EvaluateHand(List<Card> holeCards, List<Card> communityCards)
    {
        var allCards = holeCards.Concat(communityCards).ToList();
        
        // Check for straight flush first (includes royal flush)
        var straightFlush = CheckStraightFlush(allCards);
        if (straightFlush != null) return straightFlush;
        
        // Check for four of a kind
        var fourOfAKind = CheckFourOfAKind(allCards);
        if (fourOfAKind != null) return fourOfAKind;
        
        // Check for full house
        var fullHouse = CheckFullHouse(allCards);
        if (fullHouse != null) return fullHouse;
        
        // Check for flush
        var flush = CheckFlush(allCards);
        if (flush != null) return flush;
        
        // Check for straight
        var straight = CheckStraight(allCards);
        if (straight != null) return straight;
        
        // Check for three of a kind
        var threeOfAKind = CheckThreeOfAKind(allCards);
        if (threeOfAKind != null) return threeOfAKind;
        
        // Check for two pair
        var twoPair = CheckTwoPair(allCards);
        if (twoPair != null) return twoPair;
        
        // Check for pair
        var pair = CheckPair(allCards);
        if (pair != null) return pair;
        
        // High card
        return CheckHighCard(allCards);
    }
    
    private HandRanking CheckStraightFlush(List<Card> cards)
    {
        var suits = cards.GroupBy(c => c.Suit);
        
        foreach (var suit in suits)
        {
            if (suit.Count() >= 5)
            {
                var suitCards = suit.OrderByDescending(c => c.NumericValue).ToList();
                var straight = FindStraight(suitCards);
                
                if (straight != null)
                {
                    var isRoyal = straight.All(c => c.NumericValue >= 10) && straight.Any(c => c.Rank == "A");
                    return new HandRanking
                    {
                        Type = isRoyal ? HandType.RoyalFlush : HandType.StraightFlush,
                        Value = isRoyal ? 14 : straight.Max(c => c.NumericValue),
                        Kickers = straight.Take(5).ToList(),
                        Description = isRoyal ? "Royal Flush" : $"Straight Flush, {GetHighCardDescription(straight.Max(c => c.NumericValue))} high"
                    };
                }
            }
        }
        
        return null;
    }
    
    private HandRanking CheckFourOfAKind(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.NumericValue);
        var fourOfAKind = groups.FirstOrDefault(g => g.Count() == 4);
        
        if (fourOfAKind != null)
        {
            var kicker = groups.Where(g => g.Key != fourOfAKind.Key)
                              .OrderByDescending(g => g.Key)
                              .FirstOrDefault()?.First();
            
            return new HandRanking
            {
                Type = HandType.FourOfAKind,
                Value = fourOfAKind.Key,
                Kickers = kicker != null ? new List<Card> { kicker } : new List<Card>(),
                Description = $"Four {GetRankDescription(fourOfAKind.Key)}s"
            };
        }
        
        return null;
    }
    
    private HandRanking CheckFullHouse(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.NumericValue).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();
        
        var threeOfAKind = groups.FirstOrDefault(g => g.Count() >= 3);
        var pair = groups.FirstOrDefault(g => g.Count() >= 2 && g.Key != threeOfAKind?.Key);
        
        if (threeOfAKind != null && pair != null)
        {
            return new HandRanking
            {
                Type = HandType.FullHouse,
                Value = threeOfAKind.Key,
                Kickers = new List<Card> { pair.First() },
                Description = $"{GetRankDescription(threeOfAKind.Key)}s full of {GetRankDescription(pair.Key)}s"
            };
        }
        
        return null;
    }
    
    private HandRanking CheckFlush(List<Card> cards)
    {
        var suits = cards.GroupBy(c => c.Suit);
        var flushSuit = suits.FirstOrDefault(s => s.Count() >= 5);
        
        if (flushSuit != null)
        {
            var flushCards = flushSuit.OrderByDescending(c => c.NumericValue).Take(5).ToList();
            return new HandRanking
            {
                Type = HandType.Flush,
                Value = flushCards[0].NumericValue,
                Kickers = flushCards,
                Description = $"Flush, {GetHighCardDescription(flushCards[0].NumericValue)} high"
            };
        }
        
        return null;
    }
    
    private HandRanking CheckStraight(List<Card> cards)
    {
        var straight = FindStraight(cards);
        if (straight != null)
        {
            return new HandRanking
            {
                Type = HandType.Straight,
                Value = straight.Max(c => c.NumericValue),
                Kickers = straight.Take(5).ToList(),
                Description = $"Straight, {GetHighCardDescription(straight.Max(c => c.NumericValue))} high"
            };
        }
        
        return null;
    }
    
    private List<Card> FindStraight(List<Card> cards)
    {
        var uniqueValues = cards.Select(c => c.NumericValue).Distinct().OrderByDescending(v => v).ToList();
        
        // Check for regular straight
        for (int i = 0; i <= uniqueValues.Count - 5; i++)
        {
            var straight = new List<int>();
            for (int j = 0; j < 5; j++)
            {
                if (i + j < uniqueValues.Count && 
                    (j == 0 || uniqueValues[i + j] == uniqueValues[i + j - 1] - 1))
                {
                    straight.Add(uniqueValues[i + j]);
                }
            }
            
            if (straight.Count == 5)
            {
                return straight.Select(v => cards.First(c => c.NumericValue == v)).ToList();
            }
        }
        
        // Check for A-2-3-4-5 straight
        if (uniqueValues.Contains(14) && uniqueValues.Contains(5) && uniqueValues.Contains(4) && 
            uniqueValues.Contains(3) && uniqueValues.Contains(2))
        {
            return new List<Card>
            {
                cards.First(c => c.NumericValue == 5),
                cards.First(c => c.NumericValue == 4),
                cards.First(c => c.NumericValue == 3),
                cards.First(c => c.NumericValue == 2),
                cards.First(c => c.NumericValue == 14)
            };
        }
        
        return null;
    }
    
    private HandRanking CheckThreeOfAKind(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.NumericValue);
        var threeOfAKind = groups.FirstOrDefault(g => g.Count() == 3);
        
        if (threeOfAKind != null)
        {
            var kickers = groups.Where(g => g.Key != threeOfAKind.Key)
                               .OrderByDescending(g => g.Key)
                               .Take(2)
                               .SelectMany(g => g.Take(1))
                               .ToList();
            
            return new HandRanking
            {
                Type = HandType.ThreeOfAKind,
                Value = threeOfAKind.Key,
                Kickers = kickers,
                Description = $"Three {GetRankDescription(threeOfAKind.Key)}s"
            };
        }
        
        return null;
    }
    
    private HandRanking CheckTwoPair(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.NumericValue).Where(g => g.Count() >= 2).OrderByDescending(g => g.Key).ToList();
        
        if (groups.Count >= 2)
        {
            var firstPair = groups[0];
            var secondPair = groups[1];
            var kicker = groups.Skip(2).FirstOrDefault()?.First() ?? 
                        groups.Where(g => g.Key != firstPair.Key && g.Key != secondPair.Key)
                              .OrderByDescending(g => g.Key)
                              .FirstOrDefault()?.First();
            
            return new HandRanking
            {
                Type = HandType.TwoPair,
                Value = firstPair.Key,
                Kickers = new List<Card> { secondPair.First(), kicker }.Where(c => c != null).ToList(),
                Description = $"{GetRankDescription(firstPair.Key)}s and {GetRankDescription(secondPair.Key)}s"
            };
        }
        
        return null;
    }
    
    private HandRanking CheckPair(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.NumericValue);
        var pair = groups.FirstOrDefault(g => g.Count() == 2);
        
        if (pair != null)
        {
            var kickers = groups.Where(g => g.Key != pair.Key)
                               .OrderByDescending(g => g.Key)
                               .Take(3)
                               .SelectMany(g => g.Take(1))
                               .ToList();
            
            return new HandRanking
            {
                Type = HandType.Pair,
                Value = pair.Key,
                Kickers = kickers,
                Description = $"Pair of {GetRankDescription(pair.Key)}s"
            };
        }
        
        return null;
    }
    
    private HandRanking CheckHighCard(List<Card> cards)
    {
        var highCards = cards.OrderByDescending(c => c.NumericValue).Take(5).ToList();
        
        return new HandRanking
        {
            Type = HandType.HighCard,
            Value = highCards[0].NumericValue,
            Kickers = highCards,
            Description = $"{GetHighCardDescription(highCards[0].NumericValue)} high"
        };
    }
    
    private string GetRankDescription(int value)
    {
        return value switch
        {
            14 => "Ace",
            13 => "King",
            12 => "Queen",
            11 => "Jack",
            10 => "Ten",
            _ => value.ToString()
        };
    }
    
    private string GetHighCardDescription(int value)
    {
        return value switch
        {
            14 => "Ace",
            13 => "King",
            12 => "Queen",
            11 => "Jack",
            10 => "Ten",
            _ => value.ToString()
        };
    }
    
    public int CompareHands(HandRanking hand1, HandRanking hand2)
    {
        // Compare hand types first
        if (hand1.Type != hand2.Type)
        {
            return hand1.Type.CompareTo(hand2.Type);
        }
        
        // Same hand type, compare values
        if (hand1.Value != hand2.Value)
        {
            return hand1.Value.CompareTo(hand2.Value);
        }
        
        // Same values, compare kickers
        for (int i = 0; i < Math.Min(hand1.Kickers.Count, hand2.Kickers.Count); i++)
        {
            if (hand1.Kickers[i].NumericValue != hand2.Kickers[i].NumericValue)
            {
                return hand1.Kickers[i].NumericValue.CompareTo(hand2.Kickers[i].NumericValue);
            }
        }
        
        return 0; // Hands are equal
    }
}
