using System;
using UnityEngine;

namespace CloudBuilderLibrary
{
	internal class UnityLogger: ILogger
	{
		private static readonly UnityLogger instance = new UnityLogger();
		
		private UnityLogger() {}
		
		public static UnityLogger Instance {
			get { return instance; }
		}

		#region ILogger implementation
		void ILogger.Log(LogLevel level, string text)
		{
			Debug.Log(text);
		}
		#endregion
	}
}

