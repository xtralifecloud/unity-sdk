using System;

namespace CotcSdk
{
	internal interface ISystemFunctions
	{
		Bundle CollectDeviceInformation();
		string GetOsName();
		// TODO AchieveRegisterDevice
	}
}

