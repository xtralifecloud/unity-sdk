using UnityEngine;

namespace CotcSdk
{
	internal class UnitySystemFunctions: ISystemFunctions
	{
		#region ISystemFunctions implementation
		Bundle ISystemFunctions.CollectDeviceInformation ()
		{
			Bundle result = Bundle.CreateObject();
			result["id"] = SystemInfo.deviceName;
			result["model"] = SystemInfo.deviceModel;
			result["version"] = "1";
			result["osname"] = ((ISystemFunctions)this).GetOsName();
			result["osversion"] = SystemInfo.operatingSystem;
			result["name"] = SystemInfo.deviceName;
			return result;
		}

		string ISystemFunctions.GetOsName() {
			return Application.platform.ToString();
		}
		#endregion
	}
}

