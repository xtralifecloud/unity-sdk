using System;

namespace CloudBuilderLibrary
{
	public abstract class ManagerBase {

		internal virtual HttpRequest MakeHttpRequest(string path) {
			return CloudBuilder.UserManager.MakeHttpRequest(path);
		}
		internal virtual HttpRequest MakeUnauthenticatedHttpRequest(string path) {
			return CloudBuilder.Clan.MakeUnauthenticatedHttpRequest(path);
		}

		/** Call this if your method requires the SDK to be set up properly. Returns false and calls the delegate if not. */
		internal bool RequireSetup<T>(Action<CloudResult, T> calledInCaseOfError) where T: class {
			if (!CloudBuilder.Clan.IsSetup) {
				Common.InvokeHandler(calledInCaseOfError, ErrorCode.enSetupNotCalled);
				return false;
			}
			return true;
		}

		/** Call this if your method requires to be logged in. Returns false and calls the delegate if not. */
		internal bool RequireLoggedIn<T>(Action<CloudResult, T> calledInCaseOfError) where T: class {
			if (!RequireSetup(calledInCaseOfError)) return false;
			if (!CloudBuilder.UserManager.IsLogged) {
				Common.InvokeHandler(calledInCaseOfError, ErrorCode.enNotLogged);
				return false;
			}
			return true;
		}
	}
}

