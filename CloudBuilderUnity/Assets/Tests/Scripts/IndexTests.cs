using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;
using System.Collections.Generic;

public class IndexTests : TestBase {
	[InstanceMethod(typeof(IndexTests))]
	public string TestMethodName;

	void Start() {
		RunTestMethod(TestMethodName);
	}

	[Test("Indexes an object and retrieves it.")]
	public void ShouldIndexObject(Cloud cloud) {
		string indexName = "test" + Guid.NewGuid().ToString();
		string objectId = Guid.NewGuid().ToString();
		cloud.Index(indexName).IndexObject(
			objectId: objectId,
			properties: Bundle.CreateObject("prop", "value"),
			payload: Bundle.CreateObject("pkey", "pvalue"))
		.ExpectSuccess(result => {
			// Then retrieve it
			cloud.Index(indexName).GetObject(objectId)
			.ExpectSuccess(gotObject => {
				Assert(gotObject.IndexName == indexName, "Wrong index name");
				Assert(gotObject.ObjectId == objectId, "Wrong object ID");
				Assert(gotObject.Payload["pkey"] == "pvalue", "Wrong payload");
				Assert(gotObject.Properties["prop"] == "value", "Wrong properties content");
				CompleteTest();
			});
		});
	}

	[Test("Indexes an object and deletes it. Checks that it cannot be accessed anymore then.")]
	public void ShouldDeleteObject(Cloud cloud) {
		string indexName = "test" + Guid.NewGuid().ToString();
		string objectId = Guid.NewGuid().ToString();
		// Index
		cloud.Index(indexName).IndexObject(
			objectId: objectId,
			properties: Bundle.CreateObject("prop", "value"),
			payload: Bundle.CreateObject("pkey", "pvalue"))
		.ExpectSuccess(result => {
			// Delete
			cloud.Index(indexName).DeleteObject(objectId)
			.ExpectSuccess(deleted => {
				// TODO
//				Assert(deleted["found"] == true, "Expected the item to be found");
				// Should not find anymore
				cloud.Index(indexName).GetObject(objectId)
				.ExpectFailure(gotObject => {
					Assert(gotObject.HttpStatusCode == 404, "Should return 404");
					CompleteTest();
				});
			});
		});
	}

	[Test("Indexes a few objects and tries to query for them in various ways, assessing that the search arguments and pagination work as expected.")]
	public void ShouldSearchForObjects(Cloud cloud) {
		var index = cloud.Index("test" + Guid.NewGuid().ToString());
		// Index a few items
		index.IndexObject("item1", Bundle.CreateObject("item", "gold"), Bundle.CreateObject("key1", "value1"))
		.ExpectSuccess(dummy => index.IndexObject("item2", Bundle.CreateObject("item", "silver"), Bundle.CreateObject("key2", "value2")))
		.ExpectSuccess(dummy => index.IndexObject("item3", Bundle.CreateObject("item", "bronze"), Bundle.CreateObject("key3", "value3")))
		.ExpectSuccess(dummy => index.IndexObject("item4", Bundle.CreateObject("item", "silver", "qty", 10), Bundle.CreateObject("key4", "value4")))
			// Then check results
		.ExpectSuccess(dummy => {
			return index.Search("item:gold")
			.ExpectSuccess(result => {
				// Should only return one item
				Assert(result.Hits.Total == 1, "Should have one hit");
				Assert(result.MaxScore == result.Hits[0].ResultScore, "Max score doesn't match first item score");
				Assert(result.Hits[0].ObjectId == "item1", "Expected 'item1'");
			});
		})
		.ExpectSuccess(dummy => {
			return index.Search("item:silver")
			.ExpectSuccess(result => {
				// Should only return one item
				Assert(result.Hits.Total == 2, "Should have two hits");
				Assert(result.Hits[0].ObjectId == "item2", "Expected 'item2'");
				Assert(result.Hits[1].ObjectId == "item4", "Expected 'item4'");
				Assert(result.Hits[1].Payload["key4"] == "value4", "Invalid payload of 'item4'");
				Assert(result.Hits[1].Properties["qty"] == 10, "Invalid qty payload of 'item4'");
			});
		})
		.ExpectSuccess(dummy => {
			return index.Search(
				query: "item:*",
				sortingProperties: new List<string>() { "item:desc" },
				limit: 3,
				offset: 0)
			.ExpectSuccess(result => {
				var hits = result.Hits;
				// Should return all results
				Assert(hits.Total == 4, "Should have all four hits");
				// First time
				Assert(hits.Count == 3, "Yet only three hits at once");
				Assert(hits[2].Properties["item"] == "gold", "If sorting occurred correctly, third item should be gold");
				Assert(hits.HasNext, "Should have next page");
				Assert(!hits.HasPrevious, "Should not have previous page");
				hits.FetchNext()
				.ExpectSuccess(nextHits => {
					Assert(nextHits.Count == 1, "Yet only one hit for the last page");
					Assert(!nextHits.HasNext, "Should not have next page");
					Assert(nextHits.HasPrevious, "Should have previous page");
					CompleteTest();
				});
			});
		});
	}

	[Test("Tests that the API can be used to search for matches")]
	public void ShouldBeUsableToSearchForMatches(Cloud cloud) {
		Login2NewUsers(cloud, (gamer1, gamer2) => {

			gamer1.Matches.Create(maxPlayers: 2)
			.ExpectSuccess(match1 => {
				Bundle matchProp = Bundle.CreateObject("public", true, "owner_id", gamer1.GamerId);
				string queryStr = "public:true AND owner_id:" + gamer1.GamerId;
				// Index the match
				cloud.Index("matches").IndexObject(match1.MatchId, matchProp, Bundle.Empty)
				.ExpectSuccess(indexResult => {

					// Create another match
					gamer1.Matches.Create(maxPlayers: 2)
					.ExpectSuccess(match2 => {

						// Index it
						cloud.Index("matches").IndexObject(match2.MatchId, Bundle.CreateObject("public", false), Bundle.Empty)
						.ExpectSuccess(indexResult2 => {

							// Check that we can find match1 by looking for public matches
							cloud.Index("matches").Search(queryStr)
							.ExpectSuccess(found => {
								Assert(found.Hits.Count == 1, "Should find one match");
								Assert(found.Hits[0].ObjectId == match1.MatchId, "Should find one match");
								CompleteTest();
							});
						});
					});
				});
			});
		});
	}
}
