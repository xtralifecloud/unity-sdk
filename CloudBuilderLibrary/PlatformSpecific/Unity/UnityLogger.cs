using System;
using UnityEngine;

namespace CotcSdk
{
	internal class UnityLogger: ILogger
	{
		private static readonly UnityLogger Instance_ = new UnityLogger();
		
		private UnityLogger() {}
		
		public static UnityLogger Instance {
			get { return Instance_; }
		}

		#region ILogger implementation
		void ILogger.Log(LogLevel level, string text) {
			switch (level) {
				case LogLevel.Error: Debug.LogError(text); break;
				case LogLevel.Info:
				case LogLevel.Verbose: Debug.Log(text); break;
				case LogLevel.Warning: Debug.LogWarning(text); break;
			}
		}
		#endregion
	}
}

