# BridgeIt - Contract Bridge Bidding Engine

## What This Project Is

BridgeIt is a contract bridge bidding engine and trainer. It models the Acol bidding system with a rule-based engine that can bid hands automatically, explain bids, and infer information about unseen hands from the auction. The UI lets you observe robot auctions, play as North, configure deals, and debug the engine's reasoning.

The long-term aim is to support multiple bidding systems (Standard American, etc.) by swapping rule sets, and to use the knowledge/inference system to make increasingly sophisticated bidding decisions.

## Architecture Overview

```
BridgeIt.Core          Core domain, bidding engine, rules, analysis
BridgeIt.Api           ASP.NET Core API + SignalR hub (real-time bidding)
BridgeIt.UI            React 19 + TypeScript + Vite + MUI frontend
BridgeIt.Dealer        Monte Carlo hand generation with constraints
BridgeIt.Tests         NUnit unit tests (697+ tests)
BridgeIt.TestHarness   System tests with full deal/auction sequences
BridgeIt.Analysis      PBN file parsing and analysis models
BridgeIt.CLI           Console app for local testing
BridgeIt.Systems       Bidding system loading (future)
BridgeIt.AI            ML experiments (future)
```

## Key Domain Concepts

- **Seat**: North=0, East=1, South=2, West=3. `GetNextSeat()` = `(seat+1)%4`. `GetPartner()` = `(seat+2)%4`. Auction proceeds N→E→S→W.
- **Hand**: 13 cards, `ToString()` format is `Spades/Hearts/Diamonds/Clubs` using rank chars (A K Q J T 9-2). Empty suits are valid (e.g. `K7652//862/KJ865`).
- **Suit**: Clubs=0, Diamonds=1, Hearts=2, Spades=3.
- **Rank**: Two=2 through Ace=14.
- **Bid**: Can be suit bid (level + suit), no-trumps bid (level), Pass, Double, or Redouble.
- **AuctionHistory**: Ordered list of `AuctionBid(Seat, Bid)`. Auction ends after 3 consecutive passes following an opening bid.

## BiddingEngine Design

### Rule System

Rules implement `IBiddingRule` via `BiddingRuleBase`. The engine iterates rules by priority (highest first), calling `CouldMakeBid()` then `Apply()`. First matching rule wins.

Each rule has **two directions**:
- **Forward** (bidding): `CouldMakeBid()` → `Apply()` → produces a bid
- **Backward** (inference): `CouldExplainBid()` → `GetConstraintForBid()` → extracts what we learn about a player from their bid

And a **forward constraint** for negative inference:
- `GetForwardConstraints()` → the full conjunction of conditions that must be true for this rule to fire. Used by `PlayerKnowledgeEvaluator` to infer what we learn when a player *doesn't* use a rule (passes through it).

### BuildConstraints Pattern

To avoid duplicating constraint definitions, rules use a shared `BuildConstraints()` method:

```csharp
private static CompositeConstraint BuildConstraints()
    => new() { Constraints = { new HcpConstraint(MinHcp, MaxHcp), new BalancedConstraint() } };

public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
    => BuildConstraints();

public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    => new(bid, BuildConstraints(), PartnershipBiddingState.ConstructiveSearch);
```

For bid-dependent constraints (e.g. suit openings), there's an overload `BuildConstraints(Suit suit)` for the backward case.

### Constraint Types

- `HcpConstraint(min, max)` — high card point range
- `SuitLengthConstraint(suit, expr)` — card count in a suit (e.g. ">=5")
- `BalancedConstraint()` — 4-3-3-3, 4-4-3-2, or 5-3-3-2 distribution
- `CompositeConstraint` — AND-conjunction of multiple constraints

### Knowledge System (TableKnowledge)

`TableKnowledge` holds a `PlayerKnowledge` for each seat, built by replaying the auction through `RuleLookupService` and `PlayerKnowledgeEvaluator`.

Key design decisions:
- **`Me`** = what I've communicated through my bids (purely bid-derived, NOT cross-table inferred). Rules can compare `Me` vs `HandEvaluation` to find hidden strength/length.
- **`Partner`** = what partner has shown + cross-table inference from opponents.
- **Cross-table inference** skips `_mySeat` so `Me` stays clean.
- **Negative inference**: When a player passes through a rule they could have used, `PlayerKnowledgeEvaluator.ResolveNegation` / `CollectConstraints` / `ApplyNegation` negates the forward constraint conjunction.
- **Knowledge rules guard**: Use `PartnerLastNonPassBid != null` (not `HasNarrowedRanges`) to confirm partner actively communicated, because opponent bids trigger cross-table inference that narrows partner ranges without partner saying anything.

### Rule Priority & Registration

Rules are registered in `Program.cs` with explicit priorities. Current rule set (Acol):

**Opening Bids** (NoBids context):
| Rule | Priority | Bid | Constraints |
|------|----------|-----|-------------|
| Acol2NTOpeningRule | 20 | 2NT | HCP 20-22, balanced |
| Acol1NTOpeningRule | 20 | 1NT | HCP 12-14, balanced |
| AcolStrongOpening | 19 | 2♣ | HCP 20-35 |
| Acol1SuitOpeningRule | 10 | 1x | HCP 12-19, 4+ suit |
| WeakOpeningRule | 9 | 3x/4x | HCP 6-9, 6+ suit |

**Conventions** (parameterized by `NTConventionContext`):
- `StandardStayman` / `StandardTransfer` / `StaymanResponse` / `CompleteTransfer`
- Three contexts: `After1NT` (level 2, 11+ HCP), `After2NT` (level 3, 4+ HCP), `After2C2D2NT` (level 3, 0+ HCP)

**Responses to 1-Suit** (Responder, round 1):
| Rule | Priority | Bid |
|------|----------|-----|
| AcolJacoby2NTOver1Major | 55 | 2NT (13+, 4+ fit) |
| AcolRaiseMajorOver1Suit | 50 | 2M/3M/4M |
| AcolNewSuitOver1Suit | 40 | New suit |
| AcolRaiseMinorOver1Suit | 35 | 2m/3m |
| Acol1NTResponseTo1Suit | 30 | 1NT (6-9) |

**Opener Rebids** (round 2):
| Rule | Priority |
|------|----------|
| AcolOpenerAfterJacoby2NT | 60 |
| AcolOpenerAfterNTInvite | 50 |
| AcolOpenerAfterMajorRaise | 45 |
| AcolRebidOwnSuit | 42 |
| AcolRebidNewSuit | 40 |
| AcolRebidRaiseSuit | 35 |
| AcolRebidBalanced | 25 |

**Knowledge Rules** (catch-all, any seat):
| Rule | Priority |
|------|----------|
| KnowledgeBidGameInSuit | 2 |
| KnowledgeInviteInSuit | 2 |
| KnowledgeBidGameInNT | 1 |
| KnowledgeSignOffInFit | 1 |
| KnowledgeSignOff (Pass) | 0 |

### Engine-Level Bid Validation

`BiddingEngine.ChooseBid()` validates every non-pass bid via `IBidValidityChecker.IsValid()` after `rule.Apply()`. Invalid bids (below current contract, illegal doubles) are logged and skipped.

## Frontend (BridgeIt.UI)

React 19 + TypeScript + Vite + Material-UI. Communicates via SignalR.

**Key pages**: Test page (`/test`) — green felt table with 4 hands, auction table, bidding board, debug panel.

**Key components**:
- `TestGameTable` — displays all 4 hands and central auction
- `TestInfoPanel` — mode toggle (observer/player), deal buttons, restart auction
- `TestDealDialog` — 3 tabs: Scenario (pre-built), Bespoke (custom constraints), Exact Hands (paste hand strings)
- `DebugPanel` — engine decision log
- `BiddingBoard` — interactive bid entry for human players
- `GameContext` — React context holding all SignalR state and functions

**SignalR Hub** (`GameHub`):
- `StartObserverGame` / `StartTestGame` — all-robot or play-as-North
- `DealScenario(key)` / `DealBespoke(dto)` / `DealExactHands(handText)` — deal types
- `RestartAuction` — re-run auction on current deal
- `MakeBid(string)` — human player bid submission

## Dealer (Monte Carlo)

`Dealer.GenerateConstrainedDeal()` shuffles a deck up to 100,000 times per deal, checking hand specifications for all 4 seats. `HandSpecification` has pre-built specs (Acol openings, responses, passing opponents) and a generic `Generator()` for custom constraints.

## Testing

- **Unit tests** (`BridgeIt.Tests`): 697+ tests covering rules, constraints, auction evaluation, knowledge inference. NUnit + Moq.
- **System tests** (`BridgeIt.TestHarness`): Full deal→auction sequences with generated hands. Tests in `SystemTests/Acol/` namespace. Uses `TestBridgeEnvironment` to wire up the full engine.
- **Pre-existing failures**: ~225 Board/PBN tests in TestHarness are known failures (Phase 3b, not yet addressed).

## Common Patterns

- **DecisionContext**: Bundles `BiddingContext` (hand, seat, auction), `HandEvaluation` (HCP, shape, balanced), `AuctionEvaluation` (seat role, partner bids, bidding round), `TableKnowledge`, and `PartnershipBiddingState`.
- **PartnershipBiddingState**: `Unknown`, `ConstructiveSearch`, `GameInvitational`, `FitEstablished`, `SlamExploration`, `SignOff`.
- **SeatRoleType**: `NoBids` (no one has bid), `Opener`, `Responder`, `Overcaller`.
- **Hand format**: `Spades/Hearts/Diamonds/Clubs` — e.g. `AKJ54/Q82/T9/K63`.

## Running

```bash
# API (serves SignalR hub on localhost:7005)
cd BridgeIt.Api && dotnet run

# UI (Vite dev server)
cd BridgeIt.UI && npm run dev

# Unit tests
dotnet test BridgeIt.Tests/BridgeIt.Tests.csproj

# System tests (all)
dotnet test BridgeIt.TestHarness/BridgeIt.TestHarness.csproj

# System tests (specific)
dotnet test BridgeIt.TestHarness/BridgeIt.TestHarness.csproj --filter "FullyQualifiedName~OpenerRebidSystem"
```

## Known Gaps / Future Work

- **Responder rebids**: After opener rebids (1NT, new suit, raises), responder has no specific rebid rules — falls through to knowledge catch-alls.
- **Opener 3rd-round bids**: No rules for opener's third bid.
- **After Stayman response**: Responder has no rule to place the contract after hearing 2♦/2♥/2♠.
- **Multi-system support**: Rule classes are system-agnostic (e.g. `StandardStayman` works for any NT range). Need a `BiddingSystem` config to select which rules to register.
- **Alert property on IBiddingRule**: Discussed but not implemented.
- **UI deal features**: Random deal, scenario, bespoke, exact hands, restart auction all working.
