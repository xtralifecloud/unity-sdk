using System;
using System.Collections.Generic;
using System.Text;
using LitJson;
using System.Collections;

namespace CotcSdk
{
	public enum ErrorCode {
		/// No error.
		Ok = 0,

		NetworkError = 2000,
		ServerError = 2001,
		NotImplemented = 2002,
		LogicError = 2003,
		InternalError = 2004,
		Canceled = 2005,
		AlreadyInProgress = 2006,

		NotSetup = 2100,
		BadAppCredentials = 2101,
		NotLoggedIn = 2102,
		BadParameters = 2104,
		EventListenerAlreadyRegistered = 2105,
		AlreadySetup = 2106,
		SocialNetworkError = 2107,
		LoginCanceled = 2108,

		/// You shouldn't receive this error, it's just a convenient value
		LastError
	}

	public static class ErrorCodeStringifier {
		public static string Description(this ErrorCode code) {
			switch (code) {
			case ErrorCode.Ok: return "";

			case ErrorCode.NetworkError: return "Networking error (unable to reach the server)";
			case ErrorCode.ServerError: return "Server side error";
			case ErrorCode.NotImplemented: return "Functionality not implemented";
			case ErrorCode.LogicError: return "Logic error, please check your code";
			case ErrorCode.InternalError: return "Internal error";
			case ErrorCode.Canceled: return "The operation has been canceled";
			case ErrorCode.AlreadyInProgress: return "This operation is already in progress";

			case ErrorCode.NotSetup: return "Please call setup prior to issuing this command";
			case ErrorCode.BadAppCredentials: return "Bad application credentials passed at Setup";
			case ErrorCode.NotLoggedIn: return "You need be logged in to use this functionality";
			case ErrorCode.BadParameters: return "Parameters passed to the function are either out of constraints or missing some field";
			case ErrorCode.EventListenerAlreadyRegistered: return "An event listener for this domain is already registered";
			case ErrorCode.AlreadySetup: return "Cannot set up twice";
			case ErrorCode.SocialNetworkError: return "Error with a social network";
			case ErrorCode.LoginCanceled: return "The login was canceled by the player";
			}
			return "No description available";
		}
	}
}
