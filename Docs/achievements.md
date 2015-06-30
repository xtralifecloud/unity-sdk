Working with achievements {#achievements}
===========

CloudBuilder provides a platform independent way of handling achievements for your game. Note that **achievements need to be defined on the backoffice** before being used on the client.

This page is aimed at being a little tutorial on how to work with achievements from the SDK (client). For the next steps, we will suppose that you already know how to create achievements from the backoffice. For a complete list of useful functions in the SDK, take a look at #CotcSdk.GamerAchievements.

Querying the progress of the gamer
-----------

Fetching the state of achievements is a function that require the customer to be logged in. In general, the status of achievements represents an association between a gamer and a game.

The call that we are going to use is #CotcSdk.GamerAchievements.List. An example is shown below, we'll create UI buttons with information about each achievement. The GUI toolkit is virtual, and you may want to implement additional features such as pagination.

~~~~{.cs}
gamer.Achievements.List()
.Done(achievements => {
	foreach (var pair in achievements) {
		string achName = pair.Key;
		AchievementDefinition ach = pair.Value;

		Gui.Button button = CreateButton();
		button.ProgressPercent = ach.Progress * 100;
		
		// You can put additional data in the definition ('gameData' node)
		// Here we use that to hide some achievements by default and reveal them
		// explicitely by storing a property in the 'gamerData' (for the gamer)
		bool hiddenUnlessUnlocked = ach.GameData["initiallyHidden"];
		bool revealedByGamer = ach.GamerData["unhidden"];
		// Do not show hidden achievements
		if (hiddenUnlessUnlocked && !revealedByGamer)
			continue;
		
		// Some data can be displayed depending on the achievement name (used as an unique identifier)
		if (pair.Key == "supersonic") {
			// Display your text, potentially localized based on the state of the gamer
			button.Title = "Supersonic runner!";
			if (ach.Progress >= 1)
				button.Subtitle = "Achievement obtained because you rock!";
			else
				button.Subtitle = "Finish the game in under one hour!";
		}
	}
});
~~~~

Notifying progress
-----------

If you have configured your achievements through the backoffice, you know that achievements are triggered when so-called units reach a certain value. This means that to trigger an achievement, you will increase the value of an unit by using a transaction. Let's consider the following transaction on the server:

~~~~{.js}
"supersonic" {
	"type": "limit",
	"config": {
		"unit": "supersonic",
		"maxValue": 1
	}
}
~~~~

If you want to trigger that achievement, you will simply do a transaction for this unit:

~~~~{.cs}
gamer.Transactions.Post(Bundle.CreateObject("supersonic", +1))
.Done(txResult => {
	Debug.Log(txResult.TriggeredAchievements.Count); // 1
}
~~~~

Domains
-----------

By default, achievements use the key/value storage associated with the private domain (that is, reserved for this game). You may create achievement on different domains, in which case they refer to units posted in the corresponding domain.

Associating user data with an achievement
-----------

Arbitrary data can be associated for an user and an achievement by using the #CotcSdk.GamerAchievements.AssociateData API call. For instance, this could be used to store any useful detail on how the gamer was realized. The following example describes yet another use case: we want to unhide an achievement that has been marked as hidden by default and not listed in the section above. 

~~~~{.cs}
gamer.Transactions.Post(Bundle.CreateObject("supersonic", +1))
.Then(txResult => {
	// Achievement triggered => associate data
	if (txResult.TriggeredAchievements.ContainsKey("supersonic")) {
		return gamer.Achievements.AssociateData("supersonic", Bundle.CreateObject("unhidden", true));
	}
	return null; // Nothing to do next
})
.Done(result => { /* All done properly */ });
~~~~
