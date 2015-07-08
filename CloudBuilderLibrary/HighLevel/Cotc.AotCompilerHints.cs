using System;

namespace CotcSdk {

#if UNITY_IPHONE
	public static partial class Cotc {
		// Required because of issues on iOS (AOT compilation)
		static Cotc() {
			new List<PromiseHandler<AchievementDefinition>>();
			new List<PromiseHandler<Bundle>>();
			new List<PromiseHandler<byte[]>>();
			new List<PromiseHandler<Cloud>>();
			new List<PromiseHandler<Dictionary<string, AchievementDefinition>>>();
			new List<PromiseHandler<Dictionary<string, Score>>>();
			new List<PromiseHandler<Done>>();
			new List<PromiseHandler<DrawnItemsResult>>();
			new List<PromiseHandler<Exception>>();
			new List<PromiseHandler<Gamer>>();
			new List<PromiseHandler<GamerInfo>>();
			new List<PromiseHandler<GamerProfile>>();
			new List<PromiseHandler<IndexResult>>();
			new List<PromiseHandler<IndexSearchResult>>();
			new List<PromiseHandler<int>>();
			new List<PromiseHandler<List<GamerInfo>>>();
			new List<PromiseHandler<List<Score>>>();
			new List<PromiseHandler<Match>>();
			new List<PromiseHandler<PagedList<MatchListResult>>>();
			new List<PromiseHandler<PagedList<Score>>>();
			new List<PromiseHandler<PagedList<Transaction>>>();
			new List<PromiseHandler<PagedList<UserInfo>>>();
			new List<PromiseHandler<PostedGameScore>>();
			new List<PromiseHandler<SocialNetworkFriendResponse>>();
			new List<PromiseHandler<string>>();
			new List<PromiseHandler<TransactionResult>>();
		}
	}
#endif
}

