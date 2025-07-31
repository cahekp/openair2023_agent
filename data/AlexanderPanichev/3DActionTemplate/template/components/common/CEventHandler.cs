using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "60f410ec0e9dd75be62424feefd008451c07dce0")]
public class CEventHandler : Component
{
	public Action onActivated;

	public virtual void Activate(Component sender) {}

	public void OnReceiveEvent(Component sender)
	{
		Activate(sender); // notify derived class
		onActivated?.Invoke(); // notify subscribers
	}
}
