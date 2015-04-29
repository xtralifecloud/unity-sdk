using System;
using System.Collections.Generic;
using System.Text;
using LitJson;
using System.Collections;

namespace CloudBuilderLibrary
{
	// TODO Rework these? (no need to be the same as on the C++ API?)
	public enum ErrorCode {
		/// No error.
		enNoErr = 0,
		
		enFirstError = 1000,
		
		/// 1001 - Trying to access some features, but CClan::Setup has not been called.
		enSetupNotCalled,
		
		/// 1002 - Trying to access some features, but the user is not logged yet.
		enNotLogged,
		
		/// 1003 - Trying to register an event listener twice for a domain
		enEventListenerAlreadyRegistered,
		
		/// 1004 - Networking problem (unable to reach the server)
		enNetworkError,
		
		/// 1005 - Internal server error. Contact CotC.
		enServerError,
		
		/// 1006 - Functionality not yet implemented. Contact CotC.
		enNotImplemented,
		
		/// 1007 - The user is already logged in another session.
		enAlreadyLogged,
		
		/// 1008 - Invalid parameters passed to CClan::Setup.
		enBadAppCredential,
		
		/// 1009 - Trying to use external community features but CClan constructor did not specify it.
		enExternalCommunityNotSetup,
		
		/// 1010 - Something is missing in the JSON you sent with your query.
		enBadParameters,
		
		/// 1011 - User is not logged into GooglePlayServices.
		enUserNotLoggedInGooglePlayServices,
		
		/// 1012 - An error occured when communicating with an external community on the client side.
		enExternalCommunityError,
		
		/// 1013 - The Google+ application is not installed on the device.
		enGooglePlusAppNotInstalled,
		
		/// 1014 - The user has canceled posting on Google+.
		enUserCanceledGooglePlusPost,
		
		/// 1015 - An error occured when using GooglePlay services.
		enErrorGooglePlayServices,
		
		/// 1016 - The operation has been canceled
		enCanceled,
		
		/// 1017 - The operation has been canceled
		enAlreadyLoggedWhileRestoringASession,
		
		/// 1018 - This operation (or a similar one) is already in progress, and this request was discarded as a result
		enOperationAlreadyInProgress,
		
		/// 1019 - You can not be a friend with yourself
		enFriendYourself,
		
		/// 1020 - You are trying to use an object that was created prior to a Terminate call
		enObjectDestroyed,
		
		/// 1021 - No match to resume
		enNoMatchToResume,
		
		/// 1022 - The permission has not been given by the user to perform the operation
		enExternalCommunityRefusedPermission,
		
		/// 1023 - Product not found
		enProductNotFound,
		
		/// 1024 - Logic error
		enLogicError,
		
		/// 1025 - Generic problem with the external "app" store
		enErrorWithExternalStore,
		
		/// 1026 - The user canceled the purchase
		enUserCanceledPurchase,
		
		/// 1027 - Internal error with the library
		enInternalError,
		
		/// You shouldn't receive this error, it's just a convenient value
		enLastError
	}

	public static class ErrorCodeStringifier {
		public static string Description(this ErrorCode code) {
			switch (code) {
			case ErrorCode.enSetupNotCalled: return "Please call setup prior to issuing this command";
			case ErrorCode.enNotLogged: return "You need be logged in to use this functionality";
			case ErrorCode.enEventListenerAlreadyRegistered: return "An event listener for this domain is already registered";
			case ErrorCode.enNetworkError: return "Networking error (unable to reach the server)";
			case ErrorCode.enServerError: return "Server side error";
			case ErrorCode.enNotImplemented: return "Functionality not implemented";
			case ErrorCode.enAlreadyLogged: return "User already logged, call Logout before Login";
			case ErrorCode.enBadAppCredential: return "Bad application credentials passed at Setup";
			case ErrorCode.enExternalCommunityNotSetup: return "Try to use External Community login but the CClan constructor does not specify it";
			case ErrorCode.enBadParameters: return "Parameters passed to the function are either out of constraints or missing some field";
			case ErrorCode.enUserNotLoggedInGooglePlayServices: return "User is not logged into GooglePlay Services";
			case ErrorCode.enExternalCommunityError: return "Error on SocialSDK  Layer (Facebook, GooglePlus or GameCenter)";
			case ErrorCode.enGooglePlusAppNotInstalled: return "The Google+ application is not installed on the device";
			case ErrorCode.enUserCanceledGooglePlusPost: return "The user has canceled posting on Google+";
			case ErrorCode.enErrorGooglePlayServices: return "An error occured when using GooglePlay services";
			case ErrorCode.enCanceled: return "The operation has been canceled";
			case ErrorCode.enAlreadyLoggedWhileRestoringASession: return "The user attempt to resore a session through an url while he is still logged with another account";
			case ErrorCode.enOperationAlreadyInProgress: return "This operation is already in progress";
			case ErrorCode.enFriendYourself: return "Can't be friend with yourself!";
			case ErrorCode.enObjectDestroyed: return "You are trying to use an object that was created prior to a Terminate call";
			case ErrorCode.enNoMatchToResume: return "No match to resume";
			case ErrorCode.enExternalCommunityRefusedPermission: return "The permission to perform this operation has been refused by the user";
			case ErrorCode.enProductNotFound: return "The product does not exist";
			case ErrorCode.enLogicError: return "Logic error, please check your code";
			case ErrorCode.enErrorWithExternalStore: return "Error with the external store";
			case ErrorCode.enUserCanceledPurchase: return "User canceled the purchase";
			case ErrorCode.enInternalError: return "Internal error";
			}
			return "No description available";
		}
	}
}
