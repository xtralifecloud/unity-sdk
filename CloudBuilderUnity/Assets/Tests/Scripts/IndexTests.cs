using System;
using UnityEngine;
using CloudBuilderLibrary;
using System.Reflection;
using IntegrationTests;
using System.Collections.Generic;

public class IndexTests : TestBase {
	[InstanceMethod(typeof(IndexTests))]
	public string TestMethodName;

	void Start() {
		// Invoke the method described on the integration test script (TestMethodName)
		var met = GetType().GetMethod(TestMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		// Test methods have a Clan param (and we do the setup here)
		FindObjectOfType<CloudBuilderGameObject>().GetClan(clan => {
			met.Invoke(this, new object[] { clan });
		});
	}

	[Test("Indexes an object and retrieves it.")]
	public void ShouldIndexObject(Clan clan) {
		string indexName = "test" + Guid.NewGuid().ToString();
		string objectId = Guid.NewGuid().ToString();
		clan.Index(indexName).IndexObject(
			objectId: objectId,
			properties: Bundle.CreateObject("prop", "value"),
			payload: Bundle.CreateObject("pkey", "pvalue"),
			done: result => {
				Assert(result.IsSuccessful, "Failed to index object");
				// Then retrieve it
				clan.Index(indexName).GetObject(
					objectId: objectId,
					done: gotObject => {
						Assert(gotObject.IsSuccessful, "Failed to retrieve object");
						Assert(gotObject.Value.IndexName == indexName, "Wrong index name");
						Assert(gotObject.Value.ObjectId == objectId, "Wrong object ID");
						Assert(gotObject.Value.Payload["pkey"] == "pvalue", "Wrong payload");
						Assert(gotObject.Value.Properties["prop"] == "value", "Wrong properties content");
						CompleteTest();
					}
				);
			}
		);
	}

	[Test("Indexes an object and deletes it. Checks that it cannot be accessed anymore then.")]
	public void ShouldDeleteObject(Clan clan) {
		string indexName = "test" + Guid.NewGuid().ToString();
		string objectId = Guid.NewGuid().ToString();
		// Index
		clan.Index(indexName).IndexObject(
			objectId: objectId,
			properties: Bundle.CreateObject("prop", "value"),
			payload: Bundle.CreateObject("pkey", "pvalue"),
			done: result => {
				Assert(result.IsSuccessful, "Failed to index object");
				// Delete
				clan.Index(indexName).DeleteObject(
					objectId: objectId,
					done: deleted => {
						Assert(deleted.IsSuccessful, "Failed to delete object");
						Assert(deleted.ServerData["found"] == true, "Expected the item to be found");
						// Should not find anymore
						clan.Index(indexName).GetObject(objectId: objectId, done: gotObject => {
							Assert(!gotObject.IsSuccessful, "Should not find object anymore");
							Assert(gotObject.HttpStatusCode == 404, "Should return 404");
							CompleteTest();
						});
					}
				);
			}
		);
	}

	[Test("Indexes a few objects and tries to query for them in various ways, assessing that the search arguments and pagination work as expected.")]
	public void ShouldSearchForObjects(Clan clan) {
		var index = clan.Index("test" + Guid.NewGuid().ToString());
		// Indexes one item and returns a promise object
		Func<string, Bundle, Bundle, Promise<int>> indexItem = (objectId, properties, payload) => {
			Promise<int> p = new Promise<int>();
			index.IndexObject(response => {
				Assert(response.IsSuccessful, "Failed to index item");
				p.Return(0);
			}, objectId, properties, payload);
			return p;
		};
		// Index a few items
		indexItem("item1", Bundle.CreateObject("item", "gold"), Bundle.CreateObject("key1", "value1"))
		.Then(dummy => indexItem("item2", Bundle.CreateObject("item", "silver"), Bundle.CreateObject("key2", "value2")))
		.Then(dummy => indexItem("item3", Bundle.CreateObject("item", "bronze"), Bundle.CreateObject("key3", "value3")))
		.Then(dummy => indexItem("item4", Bundle.CreateObject("item", "silver", "qty", 10), Bundle.CreateObject("key4", "value4")))
		// Then check results
		.Then((dummy, p) => {
			index.Search(
				query: "item:gold",
				done: result => {
					// Should only return one item
					Assert(result.IsSuccessful, "Should have searched for properties");
					Assert(result.Value.Hits.Total == 1, "Should have one hit");
					Assert(result.Value.MaxScore == result.Value.Hits[0].ResultScore, "Max score doesn't match first item score");
					Assert(result.Value.Hits[0].ObjectId == "item1", "Expected 'item1'");
					p.Return(0);
				}
			);
		})
		.Then((dummy, p) => {
			index.Search(
				query: "item:silver",
				done: result => {
					// Should only return one item
					Assert(result.IsSuccessful, "Should have searched for properties #2");
					Assert(result.Value.Hits.Total == 2, "Should have two hits");
					Assert(result.Value.Hits[0].ObjectId == "item2", "Expected 'item2'");
					Assert(result.Value.Hits[1].ObjectId == "item4", "Expected 'item4'");
					Assert(result.Value.Hits[1].Payload["key4"] == "value4", "Invalid payload of 'item4'");
					Assert(result.Value.Hits[1].Properties["qty"] == 10, "Invalid qty payload of 'item4'");
					p.Return(0);
				}
			);
		})
		.Then((dummy, p) => {
			index.Search(
				query: "item:*",
				sortingProperties: new List<string>() { "item:desc" },
				limit: 3,
				offset: 0,
				done: result => {
					var hits = result.Value.Hits;
					// Should return all results
					Assert(result.IsSuccessful, "Should have searched for properties #3");
					Assert(hits.Total == 4, "Should have all four hits");
					// First time
					if (hits.Offset == 0) {
						Assert(hits.Count == 3, "Yet only three hits at once");
						Assert(hits[2].Properties["item"] == "gold", "If sorting occurred correctly, third item should be gold");
						Assert(hits.HasNext, "Should have next page");
						Assert(!hits.HasPrevious, "Should not have previous page");
						hits.FetchNext();
					}
					else {
						Assert(hits.Count == 1, "Yet only one hit for the last page");
						Assert(!hits.HasNext, "Should not have next page");
						Assert(hits.HasPrevious, "Should have previous page");
						CompleteTest();
					}
				}
			);
		});
	}
}
