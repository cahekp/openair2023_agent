using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "0d3a0642a35190b9294e02c4afa32f53cbae6c96")]
public class CSwitchPlayer : CEventHandler
{
	public CPlayer player;

	public struct NodeSwitch
	{
		public Node node_from;
		public Node node_to;
	}
	public NodeSwitch[] nodes_to_switch;

	public override void Activate(Component sender)
	{
		// disable old player's node
		CGame.Get().GetPlayer().node.Enabled = false;
		
		// set's the new player
		CGame.Get().SetPlayer(player);

		// switch nodes
		foreach (var n in nodes_to_switch)
		{
			// align position
			if (n.node_from != null && n.node_to != null)
				n.node_to.WorldTransform = n.node_from.WorldTransform;

			// disable/enable node
			if (n.node_from != null)
				n.node_from.Enabled = false;
			if (n.node_to != null)
				n.node_to.Enabled = true;
		}
	}
}