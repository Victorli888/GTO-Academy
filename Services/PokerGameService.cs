using GTOTrainerApp.Models;
using Microsoft.Extensions.Logging;

namespace GTOTrainerApp.Services;

public class PokerGameService
{
    private readonly HandEvaluationService _handEvaluator;
    private readonly IPlayerDecisionService _playerDecisionService;
    private readonly ILogger<PokerGameService> _logger;
    
    public PokerGameService(
        HandEvaluationService handEvaluator, 
        IPlayerDecisionService playerDecisionService,
        ILogger<PokerGameService> logger)
    {
        _handEvaluator = handEvaluator;
        _playerDecisionService = playerDecisionService;
        _logger = logger;
    }
    
    public void StartNewGame(GameState gameState)
    {
        gameState.Players.Clear();
        gameState.CommunityCards.Clear();
        gameState.Pot = 0;
        gameState.CurrentBet = 0;
        gameState.DealerPosition = 0;
        gameState.Phase = GamePhase.PreFlop;
        gameState.IsBettingRoundActive = false;
        gameState.IsGameActive = true;
        gameState.GameMessage = "New game started! Click 'Start Hand' to begin.";
        gameState.Winners.Clear();
        
        // Initialize 8 players
        for (int i = 0; i < 8; i++)
        {
            gameState.Players.Add(new Player
            {
                Name = i == 0 ? "You" : $"Player {i + 1}",
                Chips = 1000,
                IsActive = true,
                IsHuman = i == 0,
                Style = (PlayerStyle)(i % 3),
                IsDealer = i == 0
            });
        }
    }
    
    public void StartHand(GameState gameState)
    {
        _logger.LogInformation("StartHand called. IsGameActive: {IsActive}, Players: {Count}", 
            gameState.IsGameActive, gameState.Players.Count);
        
        if (!gameState.IsGameActive) 
        {
            _logger.LogWarning("StartHand called but game is not active");
            gameState.GameMessage = "Game is not active!";
            return;
        }
        
        if (gameState.Players.Count == 0)
        {
            _logger.LogError("StartHand called but no players exist");
            gameState.GameMessage = "No players in game! Please start a new game first.";
            return;
        }
        
        try
        {
            gameState.GameMessage = "Resetting hand state...";
            _logger.LogDebug("Resetting hand state");
            
            // Reset hand state
            foreach (var player in gameState.Players)
            {
                player.HoleCards.Clear();
                player.CurrentBet = 0;
                player.TotalBetThisRound = 0;
                player.HasFolded = false;
                player.HasActed = false;
                player.IsAllIn = false;
                player.LastAction = PlayerAction.None;
                player.BestHand = new HandRanking();
            }
            
            gameState.CommunityCards.Clear();
            gameState.Pot = 0;
            gameState.CurrentBet = 0;
            gameState.Phase = GamePhase.PreFlop;
            gameState.IsBettingRoundActive = false;
            gameState.Winners.Clear();
            
            gameState.GameMessage = "Posting blinds...";
            _logger.LogDebug("Posting blinds");
            
            // Post blinds
            PostBlinds(gameState);
            
            gameState.GameMessage = "Dealing hole cards...";
            _logger.LogDebug("Dealing hole cards");
            
            // Deal hole cards
            DealHoleCards(gameState);
            
            var totalCardsDealt = gameState.Players.Sum(p => p.HoleCards.Count);
            _logger.LogDebug("Dealt {CardCount} hole cards to {PlayerCount} players", 
                totalCardsDealt, gameState.Players.Count);
            
            gameState.GameMessage = "Starting betting round...";
            _logger.LogDebug("Starting betting round");
            
            // Start pre-flop betting
            StartBettingRound(gameState);
            
            _logger.LogInformation("Hand started successfully. Phase: {Phase}, CurrentPlayer: {PlayerIndex}, Message: {Message}",
                gameState.Phase, gameState.CurrentPlayerIndex, gameState.GameMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in StartHand");
            gameState.GameMessage = $"Error starting hand: {ex.Message}";
            throw;
        }
    }
    
    private void PostBlinds(GameState gameState)
    {
        var smallBlindIndex = (gameState.DealerPosition + 1) % 8;
        var bigBlindIndex = (gameState.DealerPosition + 2) % 8;
        
        var smallBlindPlayer = gameState.Players[smallBlindIndex];
        var bigBlindPlayer = gameState.Players[bigBlindIndex];
        
        // Post small blind
        var smallBlindAmount = Math.Min(gameState.SmallBlind, smallBlindPlayer.Chips);
        smallBlindPlayer.Chips -= smallBlindAmount;
        smallBlindPlayer.CurrentBet = smallBlindAmount;
        smallBlindPlayer.TotalBetThisRound = smallBlindAmount;
        gameState.Pot += smallBlindAmount;
        
        // Post big blind
        var bigBlindAmount = Math.Min(gameState.BigBlind, bigBlindPlayer.Chips);
        bigBlindPlayer.Chips -= bigBlindAmount;
        bigBlindPlayer.CurrentBet = bigBlindAmount;
        bigBlindPlayer.TotalBetThisRound = bigBlindAmount;
        gameState.Pot += bigBlindAmount;
        
        gameState.CurrentBet = bigBlindAmount;
        gameState.MinRaise = bigBlindAmount;
        gameState.LastRaiseAmount = bigBlindAmount;
        
        gameState.GameMessage = $"{smallBlindPlayer.Name} posts small blind (${smallBlindAmount}), {bigBlindPlayer.Name} posts big blind (${bigBlindAmount})";
    }
    
    private void DealHoleCards(GameState gameState)
    {
        gameState.Deck = CreateDeck();
        _logger.LogDebug("Created deck with {CardCount} cards", gameState.Deck.Count);
        
        if (gameState.Deck.Count < gameState.Players.Count * 2)
        {
            _logger.LogError("Deck has insufficient cards: {CardCount} for {PlayerCount} players", 
                gameState.Deck.Count, gameState.Players.Count);
            throw new InvalidOperationException($"Deck has insufficient cards: {gameState.Deck.Count}");
        }
        
        var random = new Random();
        
        // Deal 2 cards to each player
        for (int i = 0; i < 2; i++)
        {
            foreach (var player in gameState.Players)
            {
                if (gameState.Deck.Count > 0)
                {
                    var cardIndex = random.Next(gameState.Deck.Count);
                    player.HoleCards.Add(gameState.Deck[cardIndex]);
                    gameState.Deck.RemoveAt(cardIndex);
                }
                else
                {
                    _logger.LogError("Ran out of cards while dealing to {PlayerName}", player.Name);
                    throw new InvalidOperationException("Ran out of cards while dealing");
                }
            }
        }
        
        _logger.LogDebug("Successfully dealt cards. Remaining deck: {CardCount}", gameState.Deck.Count);
    }
    
    private void StartBettingRound(GameState gameState)
    {
        gameState.IsBettingRoundActive = true;
        
        // Find first player to act
        var firstToAct = (gameState.DealerPosition + 3) % 8; // UTG
        if (gameState.Phase == GamePhase.PreFlop)
        {
            firstToAct = (gameState.DealerPosition + 3) % 8; // UTG
        }
        else
        {
            firstToAct = (gameState.DealerPosition + 1) % 8; // Small blind
        }
        
        gameState.CurrentPlayerIndex = firstToAct;
        gameState.GameMessage = $"{gameState.Players[gameState.CurrentPlayerIndex].Name}'s turn to act";
    }
    
    public void MakePlayerAction(GameState gameState, PlayerAction action, int amount = 0)
    {
        var player = gameState.Players[gameState.CurrentPlayerIndex];
        
        switch (action)
        {
            case PlayerAction.Fold:
                player.HasFolded = true;
                player.LastAction = PlayerAction.Fold;
                gameState.GameMessage = $"{player.Name} folds";
                break;
                
            case PlayerAction.Check:
                player.LastAction = PlayerAction.Check;
                gameState.GameMessage = $"{player.Name} checks";
                break;
                
            case PlayerAction.Call:
                var callAmount = Math.Min(gameState.CurrentBet - player.CurrentBet, player.Chips);
                player.Chips -= callAmount;
                player.CurrentBet += callAmount;
                player.TotalBetThisRound += callAmount;
                gameState.Pot += callAmount;
                player.LastAction = PlayerAction.Call;
                player.LastBetAmount = callAmount;
                gameState.GameMessage = $"{player.Name} calls ${callAmount}";
                break;
                
            case PlayerAction.Raise:
                var raiseAmount = Math.Max(amount, gameState.MinRaise);
                var totalBet = gameState.CurrentBet + raiseAmount;
                var actualRaise = Math.Min(totalBet - player.CurrentBet, player.Chips);
                
                player.Chips -= actualRaise;
                player.CurrentBet += actualRaise;
                player.TotalBetThisRound += actualRaise;
                gameState.Pot += actualRaise;
                gameState.CurrentBet = player.CurrentBet;
                gameState.MinRaise = actualRaise;
                gameState.LastRaiseAmount = actualRaise;
                player.LastAction = PlayerAction.Raise;
                player.LastBetAmount = actualRaise;
                gameState.GameMessage = $"{player.Name} raises to ${gameState.CurrentBet}";
                break;
                
            case PlayerAction.AllIn:
                var allInAmount = player.Chips;
                player.Chips = 0;
                player.CurrentBet += allInAmount;
                player.TotalBetThisRound += allInAmount;
                gameState.Pot += allInAmount;
                player.IsAllIn = true;
                player.LastAction = PlayerAction.AllIn;
                player.LastBetAmount = allInAmount;
                gameState.GameMessage = $"{player.Name} goes all in for ${allInAmount}";
                
                if (player.CurrentBet > gameState.CurrentBet)
                {
                    gameState.CurrentBet = player.CurrentBet;
                    gameState.MinRaise = allInAmount;
                }
                break;
        }
        
        player.HasActed = true;
        
        // Check if betting round is complete
        if (IsBettingRoundComplete(gameState))
        {
            EndBettingRound(gameState);
        }
        else
        {
            MoveToNextPlayer(gameState);
            // Note: AI player actions will be processed by the component
            // component after state update
        }
    }
    
    private bool IsBettingRoundComplete(GameState gameState)
    {
        var activePlayers = gameState.Players.Where(p => !p.HasFolded && !p.IsAllIn).ToList();
        if (activePlayers.Count <= 1) return true;
        
        var playersWhoCanAct = activePlayers.Where(p => !p.HasActed || p.CurrentBet < gameState.CurrentBet).ToList();
        return playersWhoCanAct.Count == 0;
    }
    
    private void EndBettingRound(GameState gameState)
    {
        gameState.IsBettingRoundActive = false;
        
        // Reset round betting
        foreach (var player in gameState.Players)
        {
            player.CurrentBet = 0;
            player.HasActed = false;
        }
        gameState.CurrentBet = 0;
        
        // Move to next phase
        if (gameState.Phase == GamePhase.PreFlop)
        {
            gameState.Phase = GamePhase.Flop;
            DealCommunityCards(gameState, 3);
        }
        else if (gameState.Phase == GamePhase.Flop)
        {
            gameState.Phase = GamePhase.Turn;
            DealCommunityCards(gameState, 1);
        }
        else if (gameState.Phase == GamePhase.Turn)
        {
            gameState.Phase = GamePhase.River;
            DealCommunityCards(gameState, 1);
        }
        else if (gameState.Phase == GamePhase.River)
        {
            gameState.Phase = GamePhase.Showdown;
            Showdown(gameState);
            return;
        }
        
        // Start next betting round
        if (gameState.Phase != GamePhase.Showdown)
        {
            StartBettingRound(gameState);
        }
    }
    
    private void DealCommunityCards(GameState gameState, int count)
    {
        var random = new Random();
        for (int i = 0; i < count; i++)
        {
            if (gameState.Deck.Count > 0)
            {
                var cardIndex = random.Next(gameState.Deck.Count);
                gameState.CommunityCards.Add(gameState.Deck[cardIndex]);
                gameState.Deck.RemoveAt(cardIndex);
            }
        }
    }
    
    private void Showdown(GameState gameState)
    {
        var activePlayers = gameState.Players.Where(p => !p.HasFolded).ToList();
        
        if (activePlayers.Count == 1)
        {
            var winner = activePlayers[0];
            winner.Chips += gameState.Pot;
            gameState.Winners.Add(winner);
            gameState.GameMessage = $"{winner.Name} wins ${gameState.Pot}!";
        }
        else
        {
            // Evaluate all hands
            foreach (var player in activePlayers)
            {
                player.BestHand = _handEvaluator.EvaluateHand(player.HoleCards, gameState.CommunityCards);
            }
            
            // Find winners
            var sortedPlayers = activePlayers.OrderByDescending(p => p.BestHand, new HandRankingComparer(_handEvaluator)).ToList();
            var winningHand = sortedPlayers[0].BestHand;
            
            var winners = sortedPlayers.Where(p => _handEvaluator.CompareHands(p.BestHand, winningHand) == 0).ToList();
            
            // Distribute pot
            var potPerWinner = gameState.Pot / winners.Count;
            var remainder = gameState.Pot % winners.Count;
            
            for (int i = 0; i < winners.Count; i++)
            {
                var amount = potPerWinner + (i < remainder ? 1 : 0);
                winners[i].Chips += amount;
                gameState.Winners.Add(winners[i]);
            }
            
            var winnerNames = string.Join(", ", winners.Select(w => w.Name));
            var handDescription = winners[0].BestHand.Description;
            gameState.GameMessage = $"{winnerNames} win(s) ${gameState.Pot} with {handDescription}!";
        }
        
        gameState.Pot = 0;
        gameState.DealerPosition = (gameState.DealerPosition + 1) % 8;
        gameState.Phase = GamePhase.PreFlop;
    }
    
    private void MoveToNextPlayer(GameState gameState)
    {
        do
        {
            gameState.CurrentPlayerIndex = (gameState.CurrentPlayerIndex + 1) % 8;
        } while (gameState.Players[gameState.CurrentPlayerIndex].HasFolded || 
                 gameState.Players[gameState.CurrentPlayerIndex].IsAllIn);
        
        gameState.GameMessage = $"{gameState.Players[gameState.CurrentPlayerIndex].Name}'s turn to act";
    }
    
    /// <summary>
    /// Processes an AI player's action automatically.
    /// Returns true if the action was processed, false if the player is human.
    /// </summary>
    public async Task<bool> ProcessAIPlayerActionAsync(GameState gameState)
    {
        if (!gameState.IsBettingRoundActive)
            return false;
            
        var currentPlayer = gameState.Players[gameState.CurrentPlayerIndex];
        
        // Only process AI players
        if (currentPlayer.IsHuman)
            return false;
        
        // Skip if player has already acted or can't act
        if (currentPlayer.HasFolded || currentPlayer.IsAllIn)
            return false;
        
        _logger.LogDebug("Processing AI player action for {PlayerName}", currentPlayer.Name);
        
        try
        {
            // Build decision context
            var context = BuildDecisionContext(gameState, currentPlayer);
            
            // Get decision from AI service
            var (action, raiseAmount) = await _playerDecisionService.DecideActionAsync(context);
            
            _logger.LogInformation("AI Player {PlayerName} decided: {Action} (raise: {RaiseAmount})", 
                currentPlayer.Name, action, raiseAmount);
            
            // Execute the decision
            MakePlayerAction(gameState, action, raiseAmount);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI player action for {PlayerName}", currentPlayer.Name);
            // Default to fold on error
            MakePlayerAction(gameState, PlayerAction.Fold, 0);
            return true;
        }
    }
    
    /// <summary>
    /// Builds a decision context for a player
    /// </summary>
    private PlayerDecisionContext BuildDecisionContext(GameState gameState, Player player)
    {
        var context = new PlayerDecisionContext
        {
            Player = player,
            GameState = gameState,
            Position = gameState.Players.IndexOf(player)
        };
        
        // Build list of other players' actions this round
        foreach (var otherPlayer in gameState.Players)
        {
            if (otherPlayer != player && otherPlayer.LastAction != PlayerAction.None)
            {
                context.OtherPlayersActions.Add(new PlayerActionInfo
                {
                    PlayerName = otherPlayer.Name,
                    Action = otherPlayer.LastAction,
                    Amount = otherPlayer.LastBetAmount,
                    IsAllIn = otherPlayer.IsAllIn
                });
            }
        }
        
        return context;
    }
    
    private List<Card> CreateDeck()
    {
        var deck = new List<Card>();
        var suits = new[] { "Hearts", "Diamonds", "Clubs", "Spades" };
        var ranks = new[] { "A", "K", "Q", "J", "10", "9", "8", "7", "6", "5", "4", "3", "2" };
        
        foreach (var suit in suits)
        {
            foreach (var rank in ranks)
            {
                deck.Add(new Card { Suit = suit, Rank = rank });
            }
        }
        
        return deck;
    }
}

public class HandRankingComparer : IComparer<HandRanking>
{
    private readonly HandEvaluationService _handEvaluator;
    
    public HandRankingComparer(HandEvaluationService handEvaluator)
    {
        _handEvaluator = handEvaluator;
    }
    
    public int Compare(HandRanking x, HandRanking y)
    {
        return _handEvaluator.CompareHands(x, y);
    }
}
