using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public static class Common {
		internal static void InvokeHandler(ResultHandler resultHandler, CloudResult result) {
			if (resultHandler != null) {
				resultHandler(result);
			}
		}
		internal static void InvokeHandler(ResultHandler resultHandler, HttpResponse response) {
			InvokeHandler(resultHandler, new CloudResult(response));
		}
		internal static void InvokeHandler(ResultHandler resultHandler, ErrorCode error, string message = null) {
			InvokeHandler(resultHandler, new CloudResult(error, message));
		}
	}

	public delegate void ResultHandler(CloudResult result);
}
