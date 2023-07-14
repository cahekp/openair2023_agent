using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "957f398422b3771321d891566fbc98bdf85302f8")]
public class CPhysicalTrigger : Component
{
	public enum Mode
	{
		Single,
		Multiple,
	}
	public Mode activation_mode;
	public List<CEventHandler> onEnter = new List<CEventHandler>();

	PhysicalTrigger physicalTrigger;

	public void Activate()
	{
		// if the trigger activated in single activation mode
		if (!Enabled)
			return;

		// notify subscribers
		foreach (var receiver in onEnter)
			receiver.OnReceiveEvent(this);	

		// disable trigger (component)
		if (activation_mode == Mode.Single)
			Enabled = false;
	}

	void Init()
	{
		physicalTrigger = node as PhysicalTrigger; 
		physicalTrigger?.AddEnterCallback((Body body) => { Activate(); });
	}

	void Update()
	{
		// our player is a BodyDummy,
		// so to trigger this PhysicalTrigger,
		// we need to call updateContacts()
		physicalTrigger?.UpdateContacts();
	}
}