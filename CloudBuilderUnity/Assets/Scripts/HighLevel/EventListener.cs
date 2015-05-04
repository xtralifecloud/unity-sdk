using System;
using System.Collections.Generic;
using System.Text;
using LitJson;
using System.Collections;

namespace CloudBuilderLibrary
{
	public delegate void EventReceivedDelegate(EventReceivedArgs e);

	public class EventReceivedArgs {
		public string Domain;
		public Bundle EventData;
	}
}
