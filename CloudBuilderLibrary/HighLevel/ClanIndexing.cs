using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary {

	public class ClanIndexing {

		/**
		 * Deletes an indexed entry. If you just want to update an entry, simply use IndexObject.
		 * @param done callback invoked when the operation has finished, either successfully or not.
		 * @param objectId ID of the object to delete, as passed when indexing.
		 */
		public void DeleteObject(ResultHandler<bool> done, string objectId) {
			UrlBuilder url = new UrlBuilder("/v1/index").Path(Domain).Path(IndexName).Path(objectId);
			HttpRequest req = Clan.MakeUnauthenticatedHttpRequest(url);
			req.Method = "DELETE";
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, true, response.BodyJson);
			});
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

		/**
		 * Searches the index.
		 * 
		 * You can search documents in the index with this API. It allows you to make complex queries.
		 * See the Elastic documentation to learn the full syntax. It’s easy and quite powerful.
		 * http://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl-query-string-query.html
		 * 
		 * @param done callback invoked when the operation has finished, either successfully or not. The
		 *     attached object contains various information about the results, including a Hits member,
		 *     which handles the results in a paginated way.
		 * @param query query string. Example: "item:silver". See Elastic documentation.
		 * @param sortingProperties name of properties (fields) to sort the results with. Example:
		 *     new List<string>() { "item:asc" }.
		 * @param limit the maximum number of results to return per page.
		 * @param offset number of the first result.
		 */
		public void Search(ResultHandler<IndexSearchResult> done, string query, List<string> sortingProperties = null, int limit = 30, int offset = 0) {
			UrlBuilder url = new UrlBuilder("/v1/index").Path(Domain).Path(IndexName);
			url.QueryParam("from", offset).QueryParam("max", limit).QueryParam("q", query);
			// Build sort property
			Bundle sort = Bundle.CreateArray();
			if (sortingProperties != null) {
				foreach (string s in sortingProperties) sort.Add(s);
			}
			url.QueryParam("sort", sort.ToJson());
			Common.RunHandledRequest(Clan.MakeUnauthenticatedHttpRequest(url), done, (HttpResponse response) => {
				// Fetch listed scores
				IndexSearchResult result = new IndexSearchResult(response.BodyJson, offset);
				foreach (Bundle b in response.BodyJson["hits"].AsArray()) {
					result.Hits.Add(new IndexResult(b));
				}
				// Handle pagination
				if (offset > 0) {
					result.Hits.Previous = () => Search(done, query, sortingProperties, limit, offset - limit);
				}
				if (offset + result.Hits.Count < result.Hits.Total) {
					result.Hits.Next = () => Search(done, query, sortingProperties, limit, offset + limit);
				}
				Common.InvokeHandler(done, result);
			});
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
