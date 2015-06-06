using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary {

	public class ClanIndexing {

		public void DeleteObject(ResultHandler<bool> done, string objectId) {

		}

		/**
		 * Fetches a previously indexed object.
		 * @param done callback invoked when the operation has finished, either successfully or not.
		 * @param objectId ID of the object to look for, as passed when indexing.
		 */
		public void GetObject(ResultHandler<IndexResult> done, string objectId) {
			UrlBuilder url = new UrlBuilder("/v1/index").Path(Domain).Path(IndexName).Path(objectId);
			HttpRequest req = Clan.MakeUnauthenticatedHttpRequest(url);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, new IndexResult(response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Indexes a new object.
		 * Use this API to add or update an object in an index. You can have as many indexes as you need: one
		 * for gamer properties, one for matches, one for finished matches, etc. It only depends on what you
		 * want to search for.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached
		 *     boolean value indicates success.
		 * @param objectId the ID of the object to be indexed. It can be anything; this ID only needs to uniquely
		 *     identify your document. Therefore, using the match ID to index a match is recommended for instance.
		 * @param properties a freeform object, whose attributes will be indexed and searchable. These properties
		 *     are typed! So if 'age' is once passed as an int, it must always be an int, or an error will be
		 *     thrown upon insertion.
		 * @param payload another freeform object. These properties are attached to the document in the same way
		 *     as the properties, however those are not indexed (cannot be looked for in a search request). Its
		 *     content is returned in searches (#IndexResult.Payload property).
		 */
		public void IndexObject(ResultHandler<bool> done, string objectId, Bundle properties, Bundle payload) {
			UrlBuilder url = new UrlBuilder("/v1/index").Path(Domain).Path(IndexName);
			HttpRequest req = Clan.MakeUnauthenticatedHttpRequest(url);
			req.BodyJson = Bundle.CreateObject(
				"id", objectId,
				"properties", properties,
				"payload", payload
			);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, true, response.BodyJson);
			});
		}

		// Sorting properties -> [name:asc, ...] -> essayer
		public void Search(PagedResult<IndexResult> done, string query, List<string> sortingProperties, int limit = 30, int offset = 0) {
		}

		#region Private
		internal ClanIndexing(Clan clan, string indexName, string domain) {
			Clan = clan;
			Domain = domain;
			IndexName = indexName;
		}

		private Clan Clan;
		private string Domain, IndexName;
		#endregion
	}
}
