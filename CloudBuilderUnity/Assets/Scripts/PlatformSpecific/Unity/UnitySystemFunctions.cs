using System;
using UnityEngine;

namespace CloudBuilderLibrary
{
	internal class UnitySystemFunctions: ISystemFunctions
	{
		#region ISystemFunctions implementation
		Bundle ISystemFunctions.CollectDeviceInformation ()
		{
			Bundle result = Bundle.CreateObject();
			result["model"] = SystemInfo.deviceModel;
			result["version"] = CloudBuilder.Version;
			result["osname"] = SystemInfo.operatingSystem;
			result["name"] = SystemInfo.deviceName;
			return result;
		}
		#endregion
	}
}

