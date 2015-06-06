using System;
using UnityEngine;
using CloudBuilderLibrary;
using System.Reflection;
using IntegrationTests;

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
}
