﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Promise<T> {
	private List<Func<T, Promise<T>>> Pending = new List<Func<T, Promise<T>>>();
	private bool AlreadyReturned;
	private T Result;

	[MethodImpl(MethodImplOptions.Synchronized)]
	private void HandlePending(T result) {
		for (int i = 0; i < Pending.Count; ) {
			Promise<T> after = Pending[i](result);
			Pending.RemoveAt(i);
			if (after != null) {
				after.Then(r => { HandlePending(r); return null; });
				break;
			}
		}
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	public void Return(T result) {
		if (AlreadyReturned) throw new ArgumentException("Returned multiple times in promise");
		HandlePending(result);
		AlreadyReturned = true;
		Result = result;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	public Promise<T> Then(Func<T, Promise<T>> action) {
		if (AlreadyReturned) {
			var next = action(Result);
			return next ?? this;
		}
		else {
			Pending.Add(action);
		}
		return this;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	public Promise<T> Then(Action<T, Promise<T>> action) {
		Promise<T> p = new Promise<T>();
		return Then(result => {
			action(result, p);
			return p;
		});
	}
}
