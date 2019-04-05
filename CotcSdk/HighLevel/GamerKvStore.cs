namespace CotcSdk {

	/// @ingroup gamer_classes
	/// <summary>
	/// Represents a key/value system with ACL rights, also known as key/value store. This class is scoped by
	/// domain, meaning that you can call .Domain("yourdomain") and perform additional calls that are scoped.
	///
	/// ACL rights simply define the lists of gamers authorized to perform certain actions related to a specific key.
	/// There are 3 types of rights:
	/// - `r` (read): Allows gamers to get key's value and ACL rights
	/// - `w` (write): Allows gamers to set key's value (but not ACL rights!)
	/// - `a` (acl/delete): Allows gamers to change key's ACL rights setup and to delete the key
	///
	/// Each of those ACL rights can take one of the following values:
	/// - `["gamerID1", "gamerID2", ...]`: An array of gamerIDs (all gamers with their `gamerID` in this array will be
	///     authorized for the corresponding ACL right, the other gamers won't)
	/// - `"*"`: A wildcard string (all gamers will be authorized for the corresponding ACL right)
	///
	/// According to all this, an example "ACL setup object" would look like:
	/// ~~~~{.json}
	/// {r: "*", w: ["gamerID1", "gamerID2"], a: ["gamerID1"]}
	/// ~~~~
	///
	/// Meaning:
	/// - Everyone may read the related key
	/// - Only gamer1 and gamer2 may write key's value
	/// - Only gamer1 may change key's ACL rights
	///
	/// An equivalent C# code to generate this object would be:
	/// ~~~~{.cs}
	/// Bundle kvStoreAcl = Bundle.FromJson("{\"r\":\"*\",\"w\":[\"gamerID1\",\"gamerID2\"],\"a\":[\"gamerID1\"]}");
	/// ~~~~
	///
	/// Or:
	/// ~~~~{.cs}
	/// Bundle kvStoreAcl = Bundle.CreateObject();
	/// kvStoreAcl["r"] = new Bundle("*");
	/// kvStoreAcl["w"] = Bundle.CreateArray(new Bundle[] { new Bundle(gamerID1), new Bundle(gamerID2) });
	/// kvStoreAcl["a"] = Bundle.CreateArray(new Bundle[] { new Bundle(gamerID1) });
	/// ~~~~
	///
	/// One last thing: as the GamerKvStore feature is similar to the GameVfs one (keys are stored globally, not scoped
	/// by gamers), client SDKs are unauthorized to directly create keys by themselves. As you can't `set` a key before
	/// you created it before, you'll have to call for a gamer batch to `create` it first (see GamerBatches).
	///
	/// Moreover, to be able to use the KvStore API to create a key you'll have to convert all gamerIDs into ObjectIDs.
	///
	/// Here is a sample Javascript batch code you can directly paste for this:
	/// ~~~~{.json}
	/// function __KvStore_CreateKey(params, customData, mod) {
	/// 	"use strict";
	/// 	// don't edit above this line // must be on line 3
	/// 	mod.debug("params.request ›› " + JSON.stringify(params.request));
	///
	/// 	if (typeof params.request.keyAcl.r === "object") { params.request.keyAcl.r = mod.ObjectIDs(params.request.keyAcl.r); }
	/// 	if (typeof params.request.keyAcl.w === "object") { params.request.keyAcl.w = mod.ObjectIDs(params.request.keyAcl.w); }
	/// 	if (typeof params.request.keyAcl.a === "object") { params.request.keyAcl.a = mod.ObjectIDs(params.request.keyAcl.a); }
	///
	/// 	return this.kv.create(this.game.getPrivateDomain(), params.user_id, params.request.keyName, params.request.keyValue, params.request.keyAcl)
	/// 	.then(function(result)
	/// 	{
	/// 		mod.debug("Success ›› " + JSON.stringify(result));
	/// 		return result;
	/// 	})
	/// 	.catch(function(error)
	/// 	{
	/// 		mod.debug("Error ›› " + error.name + ": " + error.message);
	/// 		return { error: { name: error.name, message: error.message } };
	/// 	});
	/// } // must be on last line, no CR
	/// ~~~~
	///
	/// Finally, the corresponding C# code to run this batch with the corresponding parameters:
	/// ~~~~{.cs}
	/// Bundle batchParams = Bundle.CreateObject();
	/// batchParams["keyName"] = new Bundle("KvStoreKeyA");
	/// batchParams["keyValue"] = new Bundle("KvStoreValueA");
	/// batchParams["keyAcl"] = Bundle.FromJson("{\"r\":\"*\",\"w\":[\"gamerID1\",\"gamerID2\"],\"a\":[\"gamerID1\"]}");
	///
	/// gamer.Batches.Run("KvStore_CreateKey", batchParams).Done(
	/// 	delegate(Bundle result) { Debug.Log("Success Create Key: " + result.ToString()); },
	/// 	delegate(Exception error) { Debug.LogError("Error Create Key: " + error.ToString()); }
	/// );
	/// ~~~~
	/// </summary>
	public sealed class GamerKvStore {

        /// <summary>Sets the domain affected by this object.
        /// You should typically use it this way: `gamer.KvStore.Domain("private").Set(...);`</summary>
        /// <param name="domain">Domain on which to scope the key/value store. Defaults to `private` if not specified.</param>
        /// <returns>This object for operation chaining.</returns>
        public GamerKvStore Domain(string domain) {
			this.domain = domain;
			return this;
		}

        /// <summary>Retrieves an individual key from the key/value store if the gamer calling this API is granted
        /// `read right` for this key. About ACL rights, have a look at GamerKvStore's class comments.</summary>
        /// <returns>Promise resolved when the operation has completed. The attached bundle contains the fetched
        /// key's properties. As usual with bundles, it can be casted to the proper type you are expecting.
        /// If the key doesn't exist or the gamer has no read right for this key, the call is marked as failed
        /// with a 404 status. The most important returned Bundle's properties are `value` (key's value) and
        /// `acl` (key's ACL rights setup).</returns>
        /// <param name="key">The name of the key to be fetched. A null or empty key name will be responded with
        /// a 404 (ObsoleteRoute) error.</param>
        public Promise<Bundle> GetValue(string key)
        {
            UrlBuilder url = new UrlBuilder("/v1/gamer/kv").Path(domain);
            if (key != null && key != "")
	            url = url.Path(key);
            HttpRequest req = Gamer.MakeHttpRequest(url);
            return Common.RunInTask<Bundle>(req, (response, task) => {
                task.PostResult(response.BodyJson);
            });
        }

        /// <summary>Sets the value of an individual key from the key/value store if the gamer calling this API
        /// is granted `write right` for this key. About ACL rights, have a look at GamerKvStore's class comments.</summary>
        /// <returns>Promise resolved when the operation has completed. You should check for the returned Bundle's
        /// `Successful` attribute to be `true` to confirm a key has been set (if the given key doesn't exist or
        /// the calling gamer doesn't have proper right, `Successful == false` would be returned).</returns>
        /// <param name="key">The name of the key to set the value for.</param>
        /// <param name="value">The value to set, as a Bundle.</param>
        public Promise<Done> SetValue(string key, Bundle value)
        {
            UrlBuilder url = new UrlBuilder("/v1/gamer/kv").Path(domain);
            if (key != null && key != "")
                url = url.Path(key);
            HttpRequest req = Gamer.MakeHttpRequest(url);
            req.BodyJson = Bundle.CreateObject("value", value);
            req.Method = "POST";
            return Common.RunInTask<Done>(req, (response, task) => {
	            Bundle serverData = response.BodyJson;
                task.PostResult(new Done(serverData["n"].AsInt() > 0, serverData));
            });
        }

        /// <summary>Removes an individual key from the key/value store if the gamer calling this API is granted
        /// `acl/delete right` for this key. About ACL rights, have a look at GamerKvStore's class comments.</summary>
        /// <returns>Promise resolved when the operation has completed. You should check for the returned Bundle's
        /// `Successful` attribute to be `true` to confirm a key has been deleted (if the given key doesn't exist or
        /// the calling gamer doesn't have proper right, `Successful == false` would be returned).</returns>
        /// <param name="key">The name of the key to remove.</param>
        public Promise<Done> DeleteKey(string key)
        {
            UrlBuilder url = new UrlBuilder("/v1/gamer/kv").Path(domain);
            if (key != null && key != "")
                url = url.Path(key);
            HttpRequest req = Gamer.MakeHttpRequest(url);
            req.Method = "DELETE";
            return Common.RunInTask<Done>(req, (response, task) => {
	            Bundle serverData = response.BodyJson;
	            task.PostResult(new Done(serverData["n"].AsInt() > 0, serverData));
            });
        }

        /// <summary>Changes ACL rights setup of an individual key from the key/value store if the gamer calling
        /// this API is granted `acl/delete right` for this key. About ACL rights, have a look at GamerKvStore's
        /// class comments.</summary>
        /// <returns>Promise resolved when the operation has completed. You should check for the returned Bundle's
        /// `Successful` attribute to be `true` to confirm a key has been changed (if the given key doesn't exist or
        /// the calling gamer doesn't have proper right, `Successful == false` would be returned).</returns>
        /// <param name="key">The name of the key to change the ACL rights for.</param>
        /// <param name="value">The ACL rights setup value to set, as a Bundle.</param>
        public Promise<Done> ChangeACL(string key, Bundle value)
        {
	        UrlBuilder url = new UrlBuilder("/v1/gamer/kv").Path(domain);
	        if (key != null && key != "")
		        url = url.Path(key).Path("/acl");
	        HttpRequest req = Gamer.MakeHttpRequest(url);
	        req.BodyJson = Bundle.CreateObject("acl", value);
	        req.Method = "POST";
	        return Common.RunInTask<Done>(req, (response, task) => {
		        Bundle serverData = response.BodyJson;
		        task.PostResult(new Done(serverData["n"].AsInt() > 0, serverData));
	        });
        }

        #region Private
        internal GamerKvStore(Gamer gamer) {
			Gamer = gamer;
		}

		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		#endregion
	}
}
