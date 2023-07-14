using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "309204134753ba566a53adbdf15d13f88755c6f7")]
public class CDestroyTimer : Component
{
	public float timer = 5.0f;

	void Update()
	{
		timer -= Game.IFps;
		if (timer <= 0)
			node.DeleteLater();
	}
}