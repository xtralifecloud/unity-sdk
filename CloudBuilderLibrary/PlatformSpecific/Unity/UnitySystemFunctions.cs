using UnityEngine;

namespace CotcSdk
{
	internal class UnitySystemFunctions: ISystemFunctions
	{
		#region ISystemFunctions implementation
		Bundle ISystemFunctions.CollectDeviceInformation ()
		{
			Bundle result = Bundle.CreateObject();
			result["model"] = SystemInfo.deviceModel;
			result["version"] = Common.SdkVersion;
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

