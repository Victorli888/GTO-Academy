# AI Player Decision System Architecture

## Overview

This project uses a pluggable architecture for AI player decision-making. The current implementation uses a simple "always call" strategy for testing, but it's designed to easily swap in Claude API or other AI services.

## Architecture Components

### 1. `IPlayerDecisionService` Interface
**Location:** `Services/IPlayerDecisionService.cs`

This is the contract that all AI decision services must implement:
```csharp
Task<(PlayerAction action, int raiseAmount)> DecideActionAsync(PlayerDecisionContext context);
```

### 2. `PlayerDecisionContext` Model
**Location:** `Models/PlayerDecisionContext.cs`

Contains all the information an AI needs to make a decision:
- Player's hole cards
- Community cards
- Current bet amounts
- Pot size
- Game phase
- Other players' actions
- Position at table

### 3. Current Implementation: `SimplePlayerDecisionService`
**Location:** `Services/SimplePlayerDecisionService.cs`

A simple implementation that:
- Always checks if no bet to call
- Always calls if there's a bet
- Goes all-in if can't afford to call

This ensures hands reach showdown for testing.

### 4. Integration Points

**PokerGameService:**
- `ProcessAIPlayerActionAsync()` - Processes AI player actions
- `BuildDecisionContext()` - Creates context from game state

**PokerTable Component:**
- `ProcessAIPlayersUntilHumanTurn()` - Automatically processes AI turns
- Called after human actions and when starting a hand

## How to Add Claude API

### Step 1: Create Claude Implementation

Create a new file: `Services/ClaudePlayerDecisionService.cs`

```csharp
using GTOTrainerApp.Models;
using Microsoft.Extensions.Logging;

namespace GTOTrainerApp.Services;

public class ClaudePlayerDecisionService : IPlayerDecisionService
{
    private readonly ILogger<ClaudePlayerDecisionService> _logger;
    private readonly IClaudeApiClient _claudeClient; // You'll need to create this
    
    public ClaudePlayerDecisionService(
        ILogger<ClaudePlayerDecisionService> logger,
        IClaudeApiClient claudeClient)
    {
        _logger = logger;
        _claudeClient = claudeClient;
    }
    
    public async Task<(PlayerAction action, int raiseAmount)> DecideActionAsync(
        PlayerDecisionContext context)
    {
        // Build prompt with game context
        var prompt = BuildPrompt(context);
        
        // Call Claude API
        var response = await _claudeClient.GetDecisionAsync(prompt);
        
        // Parse response and return action
        return ParseClaudeResponse(response);
    }
    
    private string BuildPrompt(PlayerDecisionContext context)
    {
        // Build a detailed prompt with:
        // - Hole cards
        // - Community cards
        // - Betting history
        // - Pot odds
        // - Position
        // etc.
    }
    
    private (PlayerAction, int) ParseClaudeResponse(string response)
    {
        // Parse Claude's response into action and raise amount
    }
}
```

### Step 2: Register in Program.cs

Replace the SimplePlayerDecisionService registration:

```csharp
// Old:
builder.Services.AddScoped<IPlayerDecisionService, SimplePlayerDecisionService>();

// New:
builder.Services.AddScoped<IPlayerDecisionService, ClaudePlayerDecisionService>();
builder.Services.AddScoped<IClaudeApiClient, ClaudeApiClient>();
```

### Step 3: That's It!

The rest of the code will automatically use Claude API because it depends on `IPlayerDecisionService`, not the concrete implementation.

## Decision Context Information

The `PlayerDecisionContext` provides:

- **HoleCards**: Player's private cards
- **CommunityCards**: Shared cards on the table
- **AmountToCall**: How much to call current bet
- **MinRaise**: Minimum raise amount
- **RemainingChips**: Player's chip stack
- **Pot**: Current pot size
- **Phase**: PreFlop, Flop, Turn, River
- **Position**: Seat position (0-7)
- **ActivePlayerCount**: Number of active players
- **OtherPlayersActions**: History of other players' actions

## Testing

The current `SimplePlayerDecisionService` ensures:
- All players call/check to showdown
- Hands complete for testing poker mechanics
- Easy to verify game flow works correctly

## Future Enhancements

1. **Multiple AI Strategies**: Create different implementations for different player styles
2. **Caching**: Cache Claude API responses for similar situations
3. **Rate Limiting**: Implement rate limiting for API calls
4. **Error Handling**: Better fallback strategies if API fails
5. **Cost Optimization**: Batch decisions or use cheaper models for simple decisions

