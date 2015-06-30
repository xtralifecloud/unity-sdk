Working with matches {#matches}
===========

Clan of the Cloud provides a simple way to run matches between a set of gamers using the network. The match system is designed around a centralized game state stored on the Clan of the Cloud servers, with gamers participating to the match making a move, updating the global game state and notifying the other players on an asynchronous basis.

It means that this match system is better suited for turn by turn games, rather than real time games such as MMORPGs, which may require a more sophisticated logic to be handled on your servers.

As with other functionality, matches are scoped by domain, allowing to share data amongst multiple games. By default, your game has one dedicated domain called `private`, which you should use unless you need cross-game functionality.

API basics
-----------

The match manager is exposed through the #CotcSdk.GamerMatches objects which is obtained through an instance of #CotcSdk.Gamer. This class exposes what is required to create or join a match, as well as managing invitations by other players. Once a match is started, joined or resumed, an instance of #CotcSdk.Match is received and can be used to perform various operations on the ongoing match.

An example of match with two players logged in locally (which usually do not happen as they will be on separate machines) is shown below:

~~~~{.cs}
Cloud.LoginAnonymously().Done((Gamer gamer1) => {
	Cloud.LoginAnonymously().Done((Gamer gamer2) => {
		// Loops are needed in order to catch events (OnMovePosted below)
		gamer1.StartEventLoop();
		gamer2.StartEventLoop();
		
		gamer1.Matches.Create(maxPlayers: 2).Done((Match matchP1) => {
			// We have the match object which can be used to post moves and so on
			// Subscribe to an event
			matchP1.OnMovePosted += (Match sender, MatchMoveEvent e) => {
				Debug.Log(e.PlayerId + " played at " + e.MoveData["position"]);
				// Finish the match
				matchP1.Finish();
			};

			gamer2.Matches.Join(matchP1.MatchId).Done((Match matchP2) => {
				// Same now for P2. Post a move which will be caught by P1.
				matchP2.PostMove(Bundle.CreateObject("position", 10));
			});
		});
	});
});
~~~~

Creating and listing matches
-----------

Creating a match is done by calling #CotcSdk.GamerMatches.CreateMatch. It will result in a #CotcSdk.Match object that you can keep for use later. The following two snippets show how to do it:

~~~~{.cs}
gamer.Matches.Domain("private").Create(
	maxPlayers: 10,
	description: "My match",
	customProperties: Bundle.CreateObject("gameType", "solo"))
.Then(createdMatch => {
	Debug.Log("Match with ID " + createdMatch.MatchId + " created!");
})
.Catch(ex => Debug.LogError("Failed to create match: " + ex.ToString()));
~~~~

Match objects do not need any special lifecycle management. They simply keep a local copy of the match state for easy querying, and provide the necessary methods to interact with the match in its current state. If you drop a reference to a Match object, nothing will happen. You may even have several of them referring to the same match.

Once your game is created, you will want other players to join it, and #CotcSdk.GamerMatches.List is here for that. As described previously, your game has a dedicated domain. On this domain, any player is able to create matches. This means that there are potentially millions of matches scoped to your domain. In order to refine the potential matches that one may want to display, conditions can be specified as parameters. By default, a number of conditions will hide some matches from the list. These include the maximum number of players having been reached or the match being in finished state.

In order to refine the searches, you are encouraged to use indexing. Let's say that your match has an attribute "type", which determines what type of game it is (coop, versus, capture the flag, etc.), and you would like to find all matches from a given type. The solution is to index a match whenever it is created in an index named for instance "matches", with the object ID being the match ID and indexed properties being for instance the type. This should mostly be done on the server by using hooks (`after-match-create`) and so on, but you may as well do it on the client.

~~~~{.cs}
gamer.Matches.Create(maxPlayers: 16)
.Then(match => {
	Bundle matchProps = Bundle.CreateObject("type", "coop", "owner_id", gamer.GamerId);
	// Index the match
	return cloud.Index("matches").IndexObject(match.MatchId, matchProps, Bundle.Empty);
})
.Then(indexResult => {
	// Match indexed, now look it up
	string queryStr = "type:coop AND owner_id:" + gamer.GamerId;
	return cloud.Index("matches").Search(queryStr);
})
.Then(foundMatches => {
	Debug.Log("Matches: " + foundMatches.Hits.Count);
})
.Catch(ex => {...});
~~~~

In that case, if the properties of the match change, you will need to reindex it. That is why it is much safer and simpler to do it on the server using hooks (i.e. `after-match-create`, `after-match-join`, etc.).

The lifecycle of a match
-----------

A match is created by a player which is called `creator`. A `creator` is solely able to perform some administrative functions, such as deleting or finishing a match. When creating a match, you automatically become a player (although you are able to leave it right away). Everyone is free to join the match, and leave it anytime. A notification is sent to every player whenever that happens.

When the match is considered complete, the `creator` should mark it finished by calling #CotcSdk.Match.Finish. He may optionally delete it once finshed successfully. Finished matches are not listed by default and all calls interacting with them do fail.

Match notifications
-----------

Match notifications (aKa events) are broadcasted on the domain corresponding to the match being run. Any player who has joined the match will automatically receive events indicating changes made to the match. These events happen when a player joins the match, leaves it or posts a move.

These events must be processed by each player prior to issuing additional commands. The class #CotcSdk.Match takes care of this internally by catching them via a #CotcSdk.DomainEventLoop. Therefore, you need to start one for the domain your match is running on prior to subscribing to any event or starting/joining/resuming a match. A warning will be issued in the console should you forget to do so, so keep an eye on it.

In a match played properly, a player should only make a move when it is his turn to do so. This should be evaluated by deterministic game rules, in order to avoid situations where two players may make a move at the same time, leading to race conditions. And you should only allow to perform a move to a given player when the the player whose turn was previous has played (subscribe to #CotcSdk.Match.OnMovePosted). Race conditions are detected by CotC and will lead to an exception as shown in the snippet below.

~~~~{.cs}
match.PostMove(...)
.Then(dummy => {...})
.Catch(ex => {
	if (ex.ServerData["name"] == "InvalidLastEventId") {
		// There was a synchronization problem
	}
});
~~~~

In order to improve race condition detection, you may ensure that no event is emitted during the preparation of your next API request by using #CotcSdk.Match.Lock. This will defer any event that may modify the state of the match. It can be useful in cases where the following scenario is possible:

- Let M be a match with players P1 and P2. A player P1 can only attack another player P2 if both are still playing.
- P1 wants to attack P2 and prepares a `PostMove` call for this.
- P2 leaves the match and the event is received by P1.
- P1 posts its move.

In this case, unless you use a `Lock` block, the SDK will think that you handled the fact that P2 has left properly and let your request succeed. Wrapping the preparation of the move along with the `PostMove` call in a `Lock` block will ensure that the leave event is triggered later, triggering a race condition exception as intended when P1 posts his move.

~~~~{.cs}
match.Lock(() => {
	// Condition in order for P1 to be able to play
	if (match.Players.Count == 2) {
		Bundle move = Bundle.CreateObject("x", 1);
		match.PostMove(move)
		.Catch(ex => {
			if (ex.ServerData["name"] == "InvalidLastEventId") { ... }
		});
	}
});
~~~~

In case a player joins the match, the following event will be received by other players. You can subscribe to it at a higher level by using the #Cotc.Match.OnPlayerJoined event.

~~~~{.js}
{
	"event": {
		"_id": "548866edba0ec600005e1941",
		"match_id": "548866ed74c2e0000098db95",
		"playersJoined": [
			{
				"gamer_id": "548866ed74c2e0000098db94",
				"profile": {
					"displayName": "Guest",
					"lang": "en"
				}
			}
		]
	},
	"id": "9155987d-6322-46d4-9403-e90ab4ba5fa3",
	"type": "match.join"
}
~~~~

In case a player leaves the match, similarly the following event will be received by others. You can subscribe to it at a higher level by using the #Cotc.Match.OnPlayerLeft event.

~~~~{.js}
{
	"event": {
		"_id": "548866eeba0ec600005e1948",
		"match_id": "548866ed74c2e0000098db95",
		"playersLeft": [
			{
				"gamer_id": "548866ed74c2e0000098db94",
				"profile": {
					"displayName": "Guest",
					"lang": "en"
				}
			}
		]
	},
	"id": "5007fd0a-1365-4e87-aba8-5d916731e684",
	"type": "match.leave"
}
~~~~

In case the match is marked as finished, the following event will be broadcasted to all players except the one who initiated it. You can subscribe to it at a higher level by using the #Cotc.Match.OnMatchFinished event.

~~~~{.js}
    {
        "type": "match.finish",
        "event": {
            "_id": "54784d07a0fcf2000086457d",
            "finished": 1,
            "match_id": "54784d07f10c190000cb96e3"
        },
        "id": "d2f908d1-40d8-4f73-802e-fe0086bb6cd5"
    }
~~~~

In case a move was posted, an event like the following one is fired. You can subscribe to it at a higher level by using the #Cotc.Match.OnShoeDrawn event.

~~~~{.js}
{
	"type": "match.move",
	"event": {
		"_id": "54784176a8df72000049340e",
		"player_id": "547841755e4cfe0000bf6907",
		"move": {
			"what": "changed"
		}
	},
	"id": "d0fb71c1-3cd0-4bbd-a9ec-29cb46cf9203"
}
~~~~

In case an element was drawn from the shoe, an event like this is fired. You can subscribe to it at a higher level by using the #Cotc.Match.OnMovePosted event.

~~~~{.js}
	{
		"type": "match.shoedraw",
		"event": {
			"_id": "5492a7c7a6725f0000b59e4f",
			"count": 2,
			"match_id": "5492a7c6a6725f0000b59e46"
		},
		"id": "5e4e82c1-d728-4a4b-be40-e4abec320440"
	}
~~~~


Synchronizing progress amongst players
-----------

Aside from spontaneous events (player joins, leaves, etc.), matches are made up of moves, which act as checkpoints for each "measurable step" in the game. A move is performed anytime by a player (the game logic should determine who has its turn each time), and is made up of two parameters:
- moveData: a freeform JSON that describes what happened. In the case of a chess game, it could contain "moved knight to B3".
- updatedGameState: an optional freeform JSON that can be updated incrementally (i.e. only present attributes are affected, others are left untouched) and can contain the global state of the game. In the case of a chess game, it could contain the game piece matrix. Passing a global state will clear the history of events.

All events (move, join, etc.) are kept in the history of the match and will be passed along when fetching information about an individual match. This history can be used to reproduce the state of the game, by starting up clean and performing the pending moves locally. This allows to resume a match or simply join it anytime.

A global state is to be passed when you have a simple and concise way to describe the game field. If you do not pass a global state, individual events will be kept in the history of the match which might become too big at some point, depending on your game.

Therefore, you do not need to pass a global state with each move: it should be used to avoid messing up the history and if your game can support it. You need to pick an appropriate policy according to your game (maximum number of moves, checkpoints in-between, etc.).

Private matches and invitations
-----------

Matches can not be made private directly, but you can use the indexing as shown previously to add an attribute that exclude them from public listing (such as {"visibility": "public"}) and include this property in the filter when listing the public matches.

Once a player has created a given match, he can then invite other players he would like to join by sending an invitation. Of course, you should not send more invitations than the number of vacant players in the match (it is allowed but will produce an error if more players than allowed join).

~~~~{.cs}
gamer.Matches.OnMatchInvitation += (MatchInviteEvent e) => {
	Debug.Log("Invited by " + e.Inviter.GamerId + " to match " + e.Match.MatchId);
};

match.InvitePlayer(gamer.GamerId);
~~~~

The list of pending invitations can be shown by using #CotcSdk.GamerMatches.List and setting `invited` to true. An invitation is cleared when the player joins the match or by calling #CotcSdk.MatchInfo.DismissInvitation.

Random-generator based games (Casino "shoe"-like functionality)
-----------

CloudBuilder provides functionality to help building games of chance, allowing each individual players to check the outcome of the match in order to ensure consistency. It does this by providing a server-based randomizing device, which hides sensitive information. Therefore, it can help ensure that all players, including the match creator have the same chances. And that no one is able to predict the outcome of the match (or fiddle/hack with it either, as other players are able to detect it).

The seed element that is returned with the detailed version of the match (i.e. when joining or fetching it), along with the shoe are the two elements which help building games of chance. The seed is a random number that is generated upon creation of every match. It can be used to initialize a pseudorandom number generator inside the game, and allows to ensure that every player will thus have the same sequence of values.

The shoe, as in Casino, is basically a container of possible values that are returned in a random order as they are poked. It could represent the values of the cards for instance. It is possible to have more than once the same element in the shoe (as when having two card sets). The shoe is posted by the person who creates the game and then shuffled. The shoe remains hidden, with no one having access to it, until the match is finished.

Players can draw one or more elements off the shoe by posting a request to this resource. The shoe is shared with all players, meaning that any element from the shoe is returned only once to a player having requested it.
When all items have been drawn from the shoe and more items are requested, the existing serie is duplicated, shuffled and appended to the current shoe, meaning an endless play can be considered.
Drawing items from the shoe will trigger one event of type #CotcSdk.MatchShoeDrawnEvent per request, sent to players except the caller. When the match finishes, fetching detailed info about the match will return the shoe. It can be used by all players to check that the game has been fair: should one player have hacked the game, it is possible to detect it by comparing the shoe to the actual moves.

To use the shoe in your match, the match creator must post an array of values in the configuration JSON. These values may be any object, from a simple number to an object with a complex hierarchy. We suggest using values as simple as possible though, to spare bandwidth when drawing items from the shoe. An example is shown below.

~~~~{.cs}
gamer.Matches.Create(
	maxPlayers: 10,
	shoe: Bundle.CreateArray(1, 2, 3))
.Done(match => {...});
~~~~

Drawing objects can be done this way:

~~~~{.cs}
match.DrawFromShoe(count: 1)
.Done((DrawnItemsResult result) => {
	Debug.Log("Item drawn: " + (int)result.Items[0]);
});
~~~~

When the game is finished, anyone is able check the contents of the shoe by using #CotcSdk.GamerMatches.Fetch and inspecting the `shoe` subnode of the `match` node present in the result.
