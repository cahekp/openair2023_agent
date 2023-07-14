using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "2e87e946881dafed282e506894fa78a761889dda")]
public class CInteractable : Component
{
	public enum Type { Take, Use, NeedItemToUse }
	public Type interact_type = Type.Take;

	public string name;
	[ParameterCondition("interact_type", (int)Type.NeedItemToUse)]
	public CInteractable item;

	public List<CEventHandler> onInteract = new List<CEventHandler>();

	public bool Interact(CPlayer player)
	{
		if (interact_type == Type.NeedItemToUse && !player.HasItem(item))
			return false;

		// notify subscribers
		foreach (var receiver in onInteract)
			receiver.OnReceiveEvent(this);

		return true;
	}
}