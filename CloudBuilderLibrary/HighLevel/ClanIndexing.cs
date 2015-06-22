using System;
using System.Collections.Generic;

namespace CotcSdk {

	public class ClanIndexing {

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
		public ResultTask<IndexSearchResult> Search(string query, List<string> sortingProperties = null, int limit = 30, int offset = 0) {
			UrlBuilder url = new UrlBuilder("/v1/index").Path(Domain).Path(IndexName);
			url.QueryParam("from", offset).QueryParam("max", limit).QueryParam("q", query);
			// Build sort property
			Bundle sort = Bundle.CreateArray();
			if (sortingProperties != null) {
				foreach (string s in sortingProperties) sort.Add(s);
			}
			url.QueryParam("sort", sort.ToJson());
			return Common.RunInTask<IndexSearchResult>(Cloud.MakeUnauthenticatedHttpRequest(url), (response, task) => {
				// Fetch listed scores
				IndexSearchResult result = new IndexSearchResult(response.BodyJson, offset);
				foreach (Bundle b in response.BodyJson["hits"].AsArray()) {
					result.Hits.Add(new IndexResult(b));
				}
				// Handle pagination
				if (offset > 0) {
					result.Hits.Previous = () => Search(query, sortingProperties, limit, offset - limit);
				}
				if (offset + result.Hits.Count < result.Hits.Total) {
					result.Hits.Next = () => Search(query, sortingProperties, limit, offset + limit);
				}
				task.PostResult(result, response.BodyJson);
			});
		}

		#region Private
		internal ClanIndexing(Cloud cloud, string indexName, string domain) {
			Cloud = cloud;
			Domain = domain;
			IndexName = indexName;
		}

		private Cloud Cloud;
		private string Domain, IndexName;
		#endregion
	}
}
