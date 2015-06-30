The indexing API {#indexing}
===========

CotC provides a generic indexing API which can be used to index and search for any kind of data. You can use it to index gamers (for match making), matches, or anything. It is free-form, so it can apply to every use case.

It works in parallel to other APIs. For instance, you could create a match, then index it to allow searching matches. Or you could index gamer's properties for fast retrieval.

The indexing API is accessible under the @ref CotcSdk.Cloud.Index "Cloud.Index" object and therefore does not require to be authenticated.

Indexing matches
-----------

One common case where you will most likely want to use the indexing API is matches. If you allow your players to create matches freely, the number of matches will quickly become huge and you will want to scope them somehow. There are a frew criterias that can be specified when listing the matches (@ref CotcSdk.GamerMatches.List "Gamer.Matches.List"), but they are very generic and will not be enough to properly categorize and do efficient queries among your matches.

Let's take a very simple example: you would like to make some matches private and only list public ones. For that, you will want to associate an additional property with your match that tells whether it is listed publicly. One way to do it is to index your match right after you created it, using the client API as shown below.

~~~~{.cs}
gamer.Matches.Create(maxPlayers: 16)
.Then(match =>
	cloud.Index("matches").IndexObject(
		objectId: match.MatchId,
		properties: Bundle.CreateObject("public", false),
		payload: Bundle.Empty))
.Then(dummy => {
	Debug.Log("Done");
})
.Catch(ex => { ... });
~~~~

However since you want to ensure that this operation is atomic, you should write it in a more complexe shape that ensures safety and uses persistence to make sure that if the index call fails, you will train again some time later. That is why we recommend you to write a hook that indexes the match as such when being created, and de-index it when not supposed to be available anymore (could also be when the number of players reaches the limit, and so on). The following hooks may be used as is (see @ref backoffice "The backoffice"):

~~~~{.js}
function after-match-create(params, customData, mod) {
	"use strict;"
	// don't edit above this line // must be on line 3
  	return this.index.index(this.game.getPrivateDomain(), "matches", params.match._id.toString(), params.match.customProperties, {});
}

function after-match-finish(params, customData, mod) {
	"use strict;"
	// don't edit above this line // must be on line 3
  	return this.index.delete(this.game.getPrivateDomain(), "matches", params.match._id.toString());
}
~~~~

Then using it on the client may be done as follows:

~~~~{.cs}
gamer.Matches.Create(maxPlayers: 16, customProperties: Bundle.CreateObject("public", "true"))
.Then(match => {
	cloud.Index("matches").Search("public: true")
	.Then(result => {
		Assert(result.Hits.Count == 1);
		return match.Finish();
	})
	.Then(dummy => cloud.Index("matches").Search("public: true"))
	.Then(result => {
		Assert(result.Hits.Count == 0);
	});
});
~~~~
