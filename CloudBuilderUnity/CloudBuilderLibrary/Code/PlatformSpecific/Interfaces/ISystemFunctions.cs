using System;

namespace CloudBuilderLibrary
{
	internal interface ISystemFunctions
	{
		Bundle CollectDeviceInformation();
		string GetOsName();
		// TODO AchieveRegisterDevice
	}
}

