package com.clanofthecloud.cotcinapppurchase.iab;

/**
 * Created by Florian on 07.08.2015.
 */
public enum ErrorCode {
	/// No error.
	Ok(0),

	NetworkError(2000),
	ServerError(2001),
	NotImplemented(2002),
	LogicError(2003),
	InternalError(2004),
	Canceled(2005),
	AlreadyInProgress(2006),

	NotSetup(2100),
	BadAppCredentials(2101),
	NotLoggedIn(2102),
	BadParameters(2104),
	EventListenerAlreadyRegistered(2105),
	AlreadySetup(2106),
	SocialNetworkError(2107),
	LoginCanceled(2108),
	ErrorWithExternalStore(2109);

	ErrorCode(int code) {
		this.code = code;
	}

	public int code;
}
