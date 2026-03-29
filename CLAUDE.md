# BridgeIt - Contract Bridge Bidding Engine

## What This Project Is

BridgeIt is a contract bridge bidding engine and trainer. It models the Acol bidding system with a rule-based engine that can bid hands automatically, explain bids, and infer information about unseen hands from the auction. The UI supports multiple modes: observe robot auctions, play as North, practice with a partner, analyse uploaded PBN match files, and debug the engine's reasoning.

The system is JSON-configurable — rules are loaded from `BridgeIt.Systems/Systems/acol-modern.json` which defines HCP ranges, convention settings, and rule priorities. The long-term aim is to support multiple bidding systems (Standard American, etc.) by swapping JSON config files.

## Architecture Overview

```
BridgeIt.Core          Core domain, bidding engine, rules, constraints, analysis
BridgeIt.Api           ASP.NET Core API + SignalR hubs + REST controllers
BridgeIt.UI            React 19 + TypeScript + Vite + MUI frontend
BridgeIt.Dealer        Monte Carlo hand generation with constraints
BridgeIt.Dds           Double Dummy Solver via P/Invoke to native DDS v2.9.0
BridgeIt.Systems       JSON-driven bidding system loader (acol-modern.json)
BridgeIt.Analysis      PBN file parsing and match analysis models
BridgeIt.Tests         NUnit unit tests (697+ tests, 41 test classes)
BridgeIt.TestHarness   System tests with full deal/auction sequences (15 test classes)
BridgeIt.CLI           Console app for local testing
BridgeIt.AI            ML experiments (future)
```

## Key Domain Concepts

- **Seat**: North=0, East=1, South=2, West=3. `GetNextSeat()` = `(seat+1)%4`. `GetPartner()` = `(seat+2)%4`. Auction proceeds N->E->S->W.
- **Hand**: 13 cards, `ToString()` format is `Spades/Hearts/Diamonds/Clubs` using rank chars (A K Q J T 9-2). Empty suits are valid (e.g. `K7652//862/KJ865`).
- **Suit**: Clubs=0, Diamonds=1, Hearts=2, Spades=3.
- **Rank**: Two=2 through Ace=14.
- **Bid**: Can be suit bid (level + suit), no-trumps bid (level), Pass, Double, or Redouble.
- **AuctionHistory**: Ordered list of `AuctionBid(Seat, Bid)`. Auction ends after 3 consecutive passes following an opening bid.

## BiddingEngine Design

### Rule System

Rules implement `IBiddingRule` via `BiddingRuleBase`. The engine iterates rules by priority (highest first), calling `CouldMakeBid()` then `Apply()`. First matching rule wins.

Each rule has **two directions**:
- **Forward** (bidding): `CouldMakeBid()` -> `Apply()` -> produces a bid
- **Backward** (inference): `CouldExplainBid()` -> `GetConstraintForBid()` -> extracts what we learn about a player from their bid

And a **forward constraint** for negative inference:
- `GetForwardConstraints()` -> the full conjunction of conditions that must be true for this rule to fire. Used by `PlayerKnowledgeEvaluator` to infer what we learn when a player *doesn't* use a rule (passes through it).

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

- `HcpConstraint(min, max)` -- high card point range
- `SuitLengthConstraint(suit, expr)` -- card count in a suit (e.g. ">=5")
- `BalancedConstraint()` -- 4-3-3-3, 4-4-3-2, or 5-3-3-2 distribution
- `CompositeConstraint` -- AND-conjunction of multiple constraints

### Knowledge System (TableKnowledge)

`TableKnowledge` holds a `PlayerKnowledge` for each seat, built by replaying the auction through `RuleLookupService` and `PlayerKnowledgeEvaluator`.

Key design decisions:
- **`Me`** = what I've communicated through my bids (purely bid-derived, NOT cross-table inferred). Rules can compare `Me` vs `HandEvaluation` to find hidden strength/length.
- **`Partner`** = what partner has shown + cross-table inference from opponents.
- **Cross-table inference** skips `_mySeat` so `Me` stays clean.
- **Negative inference**: When a player passes through a rule they could have used, `PlayerKnowledgeEvaluator.ResolveNegation` / `CollectConstraints` / `ApplyNegation` negates the forward constraint conjunction.
- **Knowledge rules guard**: Use `PartnerLastNonPassBid != null` (not `HasNarrowedRanges`) to confirm partner actively communicated, because opponent bids trigger cross-table inference that narrows partner ranges without partner saying anything.

### Engine-Level Bid Validation

`BiddingEngine.ChooseBid()` validates every non-pass bid via `IBidValidityChecker.IsValid()` after `rule.Apply()`. Invalid bids (below current contract, illegal doubles) are logged and skipped.

## Bidding System Configuration

Rules are loaded from JSON via `BiddingSystemLoader`. The current system is `acol-modern.json` ("Modern Acol, BFA Level 2"). Key configuration values:

| Setting | Value |
|---------|-------|
| 1NT Opening | 12-14 HCP, balanced |
| 2NT Opening | 20-22 HCP, balanced |
| 1-Suit Opening | 11-19 HCP, 4+ suit, Rule of 20 |
| Strong Opening (2C) | 20+ unbalanced / 23+ balanced |
| Preempts (3x/4x) | 6-10 HCP, 7+ (3-level) / 8+ (4-level) |
| Simple Overcall | 8-15 HCP, 5+ suit |
| Jump Overcall | Intermediate style, 12-16 HCP, 6+ suit |
| 1NT Overcall | Direct 16-18, Protective 11-14 |
| Takeout Double | 12+ HCP (16+ strong override) |
| Negative Double | Up to 2S level |

## Complete Rule Inventory

Rules are organised in `BridgeIt.Core/BiddingEngine/Rules/` with priorities from `acol-modern.json`.

### Opening Rules (5 rules)

Directory: `Rules/Openings/`

| Rule Class | Priority | Bid | Conditions |
|------------|----------|-----|------------|
| `Acol2NTOpeningRule` | 20 | 2NT | 20-22 HCP, balanced |
| `Acol1NTOpeningRule` | 20 | 1NT | 12-14 HCP, balanced |
| `AcolStrongOpening` | 19 | 2C | 20+ HCP unbalanced OR 23+ balanced |
| `Acol1SuitOpeningRule` | 10 | 1x | 11-19 HCP, 4+ suit, Rule of 20 |
| `WeakOpeningRule` | 9 | 3x/4x | 6-10 HCP, 6+ suit (reserved bids: 2C) |

### Conventions (5 rule classes, 3 contexts each = up to 15 instances)

Directory: `BiddingEngine/Conventions/`

Parameterised by `NTConventionContext` for reuse across NT sequences:

| Context | Level | Stayman Min HCP | Trigger |
|---------|-------|-----------------|---------|
| After1NT | 2 | 11 | Partner opened 1NT, round 1 |
| After2NT | 3 | 4 | Partner opened 2NT, round 1 |
| After2C2D2NT | 3 | 0 | Partner opened 2C, responded 2D, opener rebid 2NT, round 2 |

| Rule Class | Priority | Bid | Description |
|------------|----------|-----|-------------|
| `StandardTransfer` | 30 | 2D/2H (or 3D/3H) | Jacoby transfer showing 5+ major |
| `StandardStayman` | 29 | 2C (or 3C) | Asking for 4-card major |
| `StaymanResponse` | 60 | 2D/2H/2S (or 3D/3H/3S) | Opener's reply to Stayman |
| `CompleteTransfer` | 30 | 2H/2S (or 3H/3S) | Opener completes the transfer |
| `AcolResponderAfterStayman` | 58 | Various | Responder places contract after Stayman response |

### Responses to 1-Suit Opening (5 rules)

Directory: `Rules/Responder/ResponsesTo1Suit/`

| Rule Class | Priority | Bid | Conditions |
|------------|----------|-----|------------|
| `AcolJacoby2NTOver1Major` | 55 | 2NT (alertable) | 13+ HCP, 4+ fit in opened major |
| `AcolRaiseMajorOver1Suit` | 50 | 2M/3M/4M | 4+ support, LTC-based level |
| `AcolNewSuitOver1Suit` | 40 | New suit (1 or 2 level) | 4+ suit (5+ at 2-level), 6+ HCP |
| `AcolRaiseMinorOver1Suit` | 35 | 2m/3m | 4+ support, no 4-card major |
| `Acol1NTResponseTo1Suit` | 30 | 1NT | 6-9 HCP |

### Responses to Other Openings (3 rules)

Directory: `Rules/Responder/ResponsesTo1NT/` and `Rules/Responder/`

| Rule Class | Priority | Bid | Conditions |
|------------|----------|-----|------------|
| `AcolResponseTo2C` | 50 | 2D (alertable, artificial negative) | Any hand, response to 2C strong opening |
| `AcolResponseToWeakPreempt` | 30 | 4M/3NT/extend/Pass | Varies by support and strength |
| `AcolNTRaiseOver1NT` | 25 | Pass/2NT/3NT | 0-10 Pass, 12 invite, 13+ game |

### Opener Rebid Rules (8 rules)

Directory: `Rules/OpenerRebid/`

| Rule Class | Priority | Bid | Conditions |
|------------|----------|-----|------------|
| `AcolOpenerRebidAfter2C` | 60 | 2-3NT or suit | After 2C-2D: balanced (2NT/3NT) or longest suit |
| `AcolOpenerAfterJacoby2NT` | -- | 3M/3NT/4x/game | Describes hand after Jacoby 2NT (shortness, extra length, etc.) |
| `AcolOpenerAfterNTInvite` | 50 | Pass or 3NT | Verdict-based: 12 HCP Pass, 13+ 3NT |
| `AcolOpenerAfterMajorRaise` | 45 | 3M/4M/Pass | LTC-based: uses ExpectedTricks |
| `AcolRebidOwnSuit` | 42 | 2-3 of opening suit | 6+ cards, 12-15 simple, 16-19 jump |
| `AcolRebidNewSuit` | 40 | New suit (1-2 level) | 4+ suit, 12-15 simple, 16+ reverse/jump |
| `AcolRebidRaiseSuit` | 35 | 2-4 of partner's suit | 12-15 simple, 16-18 jump, 19+ game |
| `AcolRebidBalanced` | 25 | 1NT/2NT | 15-16 (1NT), 17-18 (2NT), balanced |

### Responder Rebid Rules (6 rules)

Directory: `Rules/Responder/ResponderRebids/`

| Rule Class | Priority | Bid | Conditions |
|------------|----------|-----|------------|
| `AcolResponderAfter2CSuitRebid` | 55 | Raise/new suit/NT | Game-forcing; 3+ fit, else 5+ suit, else NT |
| `AcolResponderAfterOpenerRaisedSuit` | 50 | Pass/raise/game | Verdict-based: SignOff, Invite, BidGame |
| `AcolResponderAfterOpener1NTRebid` | 45 | Pass/2-3 level bids | 6-9 weak, 10-12 invite, 13+ game |
| `AcolResponderAfterOpener2NTRebid` | 45 | Pass/3NT/4M | 6-7 Pass, 7+ game |
| `AcolResponderAfterOpenerNewSuit` | 40 | Preference/own suit/NT | 6-9 sign-off, 10-12 invite, 13+ game |
| `AcolResponderAfterOpenerRebidOwnSuit` | 40 | Pass/raise/NT | 6-9 weak, 10-12 invite, 13+ game |

### Competitive Rules - Overcaller (6 rules)

Directory: `Rules/Competitive/`

| Rule Class | Priority | Bid | Conditions |
|------------|----------|-----|------------|
| `Double1NT` | -- | Double | 16-20 HCP, balanced, vs 1NT opening |
| `NTOvercallRule` | 16 | 1NT | Direct 16-18 / Protective 11-14, balanced, stopper |
| `SimpleOvercallRule` | 15 | 1-3 level suit | 8-15 HCP, 5+ suit (4 in protective) |
| `JumpOvercallRule` | 14 | Jump suit | Intermediate: 12-16 HCP, 6+ suit |
| `TakeoutDoubleRule` | 13 | Double | 12+ HCP classic shape OR 16+ any shape |
| `NegativeDoubleRule` | 12 | Double | 6+ HCP, 4+ in each unbid major, responder vs overcall |

### Competitive Rules - Advancer (4 rules)

Directory: `Rules/Competitive/Advancer/`

Advancer = partner of the overcaller.

| Rule Class | Priority | Bid | Conditions |
|------------|----------|-----|------------|
| `RaiseOvercallRule` | 11 | Raise partner's suit | 3+ support + 8-11 HCP, or 4+ support + 0-7 HCP (jump) |
| `NewSuitOverOvercallRule` | 10 | New suit | 8+ HCP, 5+ suit |
| `NTResponseToOvercallRule` | 9 | 1-2NT | 8-11 HCP, balanced, stopper, no 3+ fit |
| `AdvanceAfterTakeoutDoubleRule` | 8 | Suit/jump/NT/cue | Forced: 0-8 min, 9-11 jump, 12+ cue, 6-10 balanced NT |

### Knowledge Rules (8 rules, catch-all)

Directory: `Rules/Knowledge/`

These rules produce bids based on inferred partnership knowledge, serving as catch-alls when no specific rule matches.

| Rule Class | Priority | Bid | Conditions |
|------------|----------|-----|------------|
| `KnowledgeShapeCorrection` | 4 | 2-4 level suit rebid | Actual suit length > shown length, 5+ cards |
| `KnowledgeBidGameInSuit` | 2 | 4M/5m | Verdict=BidGame, fit exists |
| `KnowledgeInviteInSuit` | 2 | 3M/4m | Verdict=Invite, fit exists |
| `KnowledgeInviteInNT` | 2 | 2NT | Verdict=Invite, no major fit |
| `KnowledgeBidGameInNT` | 1 | 3NT | Verdict=BidGame, no major fit |
| `KnowledgeSignOffInFit` | 1 | 2-3 level suit raise | Verdict=SignOff, fit exists |
| `KnowledgeSignOffInNT` | 1 | 2NT | Verdict=Invite, no major fit (fallback) |
| `KnowledgeSignOff` | 0 | Pass | Absolute catch-all (lowest priority) |

**Total: 47 active rule classes** (some instantiated multiple times with different convention contexts).

## DDS Integration (Double Dummy Solver)

`BridgeIt.Dds` provides double dummy analysis via P/Invoke to native DDS v2.9.0.

- **`IDdsService.Analyse(deal, dealer)`** -> `DdsAnalysis` containing:
  - `DdsTrickTable` -- trick counts for all 20 seat/strain combinations
  - `ParResult` -- par contract for each vulnerability
  - `MaxMakeableContract` -- highest makeable contract per side (via `MaxMakeableCalculator`)
- Used by the match analysis REST endpoints for board-level analysis.

## Frontend (BridgeIt.UI)

React 19 + TypeScript + Vite + Material-UI. Communicates via SignalR (`/gamehub`, `/practicehub`) and REST (`/api/match`).

### Pages

| Page | Route | Description |
|------|-------|-------------|
| Test | `/test` | Green felt table, 4 hands, auction table, bidding board, debug panel |
| Practice Host Setup | `/practice/host` | Configure partner practice session |
| Practice Guest Join | `/practice/join` | Join a waiting host session |
| Practice Table | `/practice/table` | Live partner practice with bidding |
| Match Analysis | `/analysis/match` | Upload PBN file and browse boards |
| Board Detail | `/analysis/board/:id` | Individual board: hands, auction, DDS tricks, engine comparison |
| Partnership Simulation | `/analysis/partnership` | Simulate partnership auction vs actual |

### Key Components

- `TestGameTable` -- displays all 4 hands and central auction
- `TestInfoPanel` -- mode toggle (observer/player), deal buttons, restart auction
- `TestDealDialog` -- 3 tabs: Scenario (pre-built), Bespoke (custom constraints), Exact Hands (paste hand strings)
- `DebugPanel` -- engine decision log with bid reasoning
- `BiddingBoard` -- interactive bid entry for human players
- `GameContext` -- React context holding all SignalR state and functions
- DDS tricks table and analysis display components
- Bid trace / reasoning sidebar for analysis

## API Layer

### SignalR Hubs

**GameHub** (`/gamehub`) -- test/observer mode:
- `StartObserverGame` / `StartTestGame` -- all-robot or play-as-North
- `DealScenario(key)` / `DealScenarioV2(dto)` / `DealBespoke(dto)` / `DealExactHands(handText)` -- deal types
- `GetScenarios()` -- available scenarios grouped by category
- `RestartAuction` -- re-run auction on current deal
- `MakeBid(string)` -- human player bid submission
- `LoadSystem(string systemJson)` -- hot-swap bidding system at runtime

**PracticeHub** (`/practicehub`) -- partner practice mode:
- `StartPracticeSession(config)` -- host creates session
- `JoinPracticeSession()` -- guest joins waiting host
- `GetSessionInfo()` -- query active session state
- `DealNextHand()` / `RestartAuction()` / `MakeBid(string)` -- gameplay

### REST API

**MatchAnalysisController** (`/api/match`):
- `POST /upload` -- upload and parse PBN file
- `GET /{matchId}` -- retrieve parsed match
- `GET /{matchId}/board/{boardNumber}` -- board details
- `GET /{matchId}/board/{boardNumber}/engine-auction` -- engine rebid of the deal
- `GET /{matchId}/board/{boardNumber}/engine-auction-detail` -- engine auction with full reasoning
- `GET /{matchId}/board/{boardNumber}/dds` -- DDS analysis (trick table, par, max-makeable)
- `GET /{matchId}/board/{boardNumber}/simulate-partnership` -- simulate partnership auction vs actual

## Dealer (Monte Carlo)

`Dealer.GenerateConstrainedDeal()` shuffles a deck up to 100,000 times per deal, checking hand specifications for all 4 seats. Methods:

- `GenerateRandomDeal()` -- basic random 4-hand deal
- `GenerateConstrainedDeal(northConstraint, southConstraint)` -- Monte Carlo with per-seat predicates
- `GenerateConstrainedDeal(north, east, south, west)` -- all 4 seats constrained
- `GenerateMultipleConstrainedDeals(count, ...)` -- batch generation
- `GenerateScenarioDeal(north, south, board)` -- scenario-specific with cross-seat constraints
- `GeneratePuppetDeal(opener, responder)` -- pre-configured puppet Stayman hands

`HandSpecification` has pre-built specs (Acol openings, responses, passing opponents) and a generic `Generator()` for custom constraints.

## PBN File Support

`BridgeIt.Analysis/PbnParser.cs` parses PBN files into `PbnBoard` objects:
- Parses standard PBN tags: Event, Board, Dealer, Vulnerable, Deal, Auction, Contract, Result, Score
- Deal format: `N:S.H.D.C S.H.D.C S.H.D.C S.H.D.C` (PBN standard)
- Filters boards by known partnership player names
- Used by match analysis pipeline for uploaded files

## Testing

- **Unit tests** (`BridgeIt.Tests`): 697+ tests covering rules, constraints, auction evaluation, knowledge inference. NUnit + Moq. 41 test classes.
- **System tests** (`BridgeIt.TestHarness`): Full deal->auction sequences with generated hands. 15 test classes in `SystemTests/Acol/` namespace. Uses `TestBridgeEnvironment` to wire up the full engine.
- **Pre-existing failures**: ~225 Board/PBN tests in TestHarness are known failures (Phase 3b, not yet addressed).

## Common Patterns

- **DecisionContext**: Bundles `BiddingContext` (hand, seat, auction), `HandEvaluation` (HCP, shape, balanced), `AuctionEvaluation` (seat role, partner bids, bidding round), `TableKnowledge`, and `PartnershipBiddingState`.
- **PartnershipBiddingState**: `Unknown`, `ConstructiveSearch`, `GameInvitational`, `FitEstablished`, `SlamExploration`, `SignOff`.
- **SeatRoleType**: `NoBids` (no one has bid), `Opener`, `Responder`, `Overcaller`.
- **Hand format**: `Spades/Hearts/Diamonds/Clubs` -- e.g. `AKJ54/Q82/T9/K63`.

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

### Bidding Rules Not Yet Implemented
- **Opener 3rd-round bids**: No rules for opener's third bid
- **Slam bidding**: Blackwood/RKCB configured in JSON but no rule implementation
- **Fourth Suit Forcing**: Configured but no rule implementation
- **Splinter bids**: Configured but no rule implementation
- **Trial bids**: Configured but no rule implementation
- **Michaels cue bid**: Configured but no rule implementation
- **Unusual 2NT**: Configured but no rule implementation
- **Weak twos**: Configured (5-9 HCP, 6+ suit, H/S only) but no dedicated rule (preempts cover some cases)
- **Acol twos**: Configured (16-22 HCP) but no rule implementation
- **Benjamin twos**: Configured but no rule implementation
- **Baron convention**: Configured for 1NT/2NT but no rule implementation
- **Weakness takeouts**: Configured but no rule implementation
- **Minor transfers**: Configured but no rule implementation
- **Penalty doubles / Redoubles**: Not implemented
- **Responsive doubles**: Not implemented

### System/Infrastructure
- **Multi-system support**: JSON system files exist but only `acol-modern.json` is populated with rule implementations
- **Alert property on IBiddingRule**: Discussed but not implemented
- **YAML rules system**: Exists (`YamlDerivedRule`) but commented out in test environment
