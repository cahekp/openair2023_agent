using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "2e87e946881dafed282e506894fa78a761889dda")]
public class CInteractable : Component
{
	public string name;

	[ParameterMask(MaskType = ParameterMaskAttribute.TYPE.INTERSECTION)]	
	public int item_mask = 1 << 2;

	public enum Type { Take, Use, NeedItemToUse }
	public Type interact_type = Type.Take;
	
	[ParameterCondition("interact_type", (int)Type.NeedItemToUse)]
	public CInteractable item_to_use;

	public List<CEventHandler> onInteract = new List<CEventHandler>();

	public bool Interact(CPlayer player)
	{
		if (interact_type == Type.NeedItemToUse && !player.HasItem(item_to_use))
			return false;

		// notify subscribers
		foreach (var receiver in onInteract)
			receiver.OnReceiveEvent(this);

		return true;
	}

	void Init()
	{
		// set intersection masks of all children to "item_mask"
		// (so that the CPlayer can interact with this object)
		Action<Node> recusive_set_intersection_mask = null;
		recusive_set_intersection_mask = (n) =>
		{
			if (n.IsObject)
			{
				Unigine.Object o = n as Unigine.Object;
				for (int j = 0; j < o.NumSurfaces; j++)
					o.SetIntersectionMask(item_mask, j);
			}
			else if (n is NodeReference)
			{
				NodeReference nr = n as NodeReference;
				recusive_set_intersection_mask(nr.Reference);
			}
			
			for (int i = 0; i < n.NumChildren; i++)
				recusive_set_intersection_mask(n.GetChild(i));
		};
		recusive_set_intersection_mask(node);
	}
}