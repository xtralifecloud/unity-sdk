using System;
using System.Collections.Generic;

namespace CotcSdk {

	public class CloudIndexing {

		/**
		 * Deletes an indexed entry. If you just want to update an entry, simply use IndexObject.
		 * @param done callback invoked when the operation has finished, either successfully or not.
		 * @param objectId ID of the object to delete, as passed when indexing.
		 */
		public IPromise<Done> DeleteObject(string objectId) {
			UrlBuilder url = new UrlBuilder("/v1/index").Path(Domain).Path(IndexName).Path(objectId);
			HttpRequest req = Cloud.MakeUnauthenticatedHttpRequest(url);
			req.Method = "DELETE";
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(true, response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Fetches a previously indexed object.
		 * @param done callback invoked when the operation has finished, either successfully or not.
		 * @param objectId ID of the object to look for, as passed when indexing.
		 */
		public IPromise<IndexResult> GetObject(string objectId) {
			UrlBuilder url = new UrlBuilder("/v1/index").Path(Domain).Path(IndexName).Path(objectId);
			HttpRequest req = Cloud.MakeUnauthenticatedHttpRequest(url);
			return Common.RunInTask<IndexResult>(req, (response, task) => {
				task.PostResult(new IndexResult(response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Indexes a new object.
		 * Use this API to add or update an object in an index. You can have as many indexes as you need: one
		 * for gamer properties, one for matches, one for finished matches, etc. It only depends on what you
		 * want to search for.
		 * @param done callback invoked when the operation has finished, either successfully or not.
		 * @param objectId the ID of the object to be indexed. It can be anything; this ID only needs to uniquely
		 *     identify your document. Therefore, using the match ID to index a match is recommended for instance.
		 * @param properties a freeform object, whose attributes will be indexed and searchable. These properties
		 *     are typed! So if 'age' is once passed as an int, it must always be an int, or an error will be
		 *     thrown upon insertion.
		 * @param payload another freeform object. These properties are attached to the document in the same way
		 *     as the properties, however those are not indexed (cannot be looked for in a search request). Its
		 *     content is returned in searches (#IndexResult.Payload property).
		 */
		public IPromise<Done> IndexObject(string objectId, Bundle properties, Bundle payload) {
			UrlBuilder url = new UrlBuilder("/v1/index").Path(Domain).Path(IndexName);
			HttpRequest req = Cloud.MakeUnauthenticatedHttpRequest(url);
			req.BodyJson = Bundle.CreateObject(
				"id", objectId,
				"properties", properties,
				"payload", payload
			);
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(true, response.BodyJson), response.BodyJson);
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
		public IPromise<IndexSearchResult> Search(string query, List<string> sortingProperties = null, int limit = 30, int offset = 0) {
			return Search(query, null, sortingProperties, limit, offset);
		}

		/**
		 * Alternative search function (see #Search for more information) that takes a bundle as a search criteria.
		 * 
		 * It allows using the full Elastic search capabilities with full query DSL search. The query bundle represents
		 * the JSON document as documented here:
		 * https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl.html
		 * 
		 * @param done callback invoked when the operation has finished, either successfully or not. The
		 *     attached object contains various information about the results, including a Hits member,
		 *     which handles the results in a paginated way.
		 * @param limit the maximum number of results to return per page.
		 * @param offset number of the first result.
		 */
		public IPromise<IndexSearchResult> SearchExtended(Bundle query, int limit = 30, int offset = 0) {
			return Search(null, query, null, limit, offset);
		}


		#region Private
		internal CloudIndexing(Cloud cloud, string indexName, string domain) {
			Cloud = cloud;
			Domain = domain;
			IndexName = indexName;
		}

		private IPromise<IndexSearchResult> Search(string query, Bundle jsonData, List<string> sortingProperties, int limit, int offset) {
			UrlBuilder url = new UrlBuilder("/v1/index").Path(Domain).Path(IndexName).Path("search");
			if (query != null) url.QueryParam("q", query);
			url.QueryParam("from", offset).QueryParam("max", limit);
			// Build sort property
			if (sortingProperties != null) {
				Bundle sort = Bundle.CreateArray();
				foreach (string s in sortingProperties) sort.Add(s);
				url.QueryParam("sort", sort.ToJson());
			}
			var request = Cloud.MakeUnauthenticatedHttpRequest(url);
			request.Method = "POST";
			if (jsonData != null) request.BodyJson = jsonData;
			return Common.RunInTask<IndexSearchResult>(request, (response, task) => {
				// Fetch listed scores
				IndexSearchResult result = new IndexSearchResult(response.BodyJson, offset);
				foreach (Bundle b in response.BodyJson["hits"].AsArray()) {
					result.Hits.Add(new IndexResult(b));
				}
				// Handle pagination
				if (offset > 0) {
					result.Hits.Previous = () => {
						var promise = new Promise<PagedList<IndexResult>>();
						Search(query, jsonData, sortingProperties, limit, offset - limit)
							.Then(r => promise.Resolve(r.Hits))
							.Catch(e => promise.Reject(e));
						return promise;
					};
				}
				if (offset + result.Hits.Count < result.Hits.Total) {
					result.Hits.Next = () => {
						var promise = new Promise<PagedList<IndexResult>>();
						Search(query, jsonData, sortingProperties, limit, offset + limit)
							.Then(r => promise.Resolve(r.Hits))
							.Catch(e => promise.Reject(e));
						return promise;
					};
				}
				task.PostResult(result, response.BodyJson);
			});
		}

		private Cloud Cloud;
		private string Domain, IndexName;
		#endregion
	}
}
