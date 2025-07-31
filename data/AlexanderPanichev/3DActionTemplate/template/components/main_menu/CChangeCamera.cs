using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "9af0e5837fe7cd061c7ac0ad6854aa28e26f8767")]
public class CChangeCamera : CEventHandler
{
	public Player camera;

	public override void Activate(Component sender)
	{
		Game.Player = camera;
	}
}