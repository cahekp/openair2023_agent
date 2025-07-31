using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unigine;

[Component(PropertyGuid = "b3e0c039b1006c44f73b8ad31260121636aaa27d")]
public class CKillEnemiesTask : Component
{
	public CHealth[] enemies;
	public List<CEventHandler> onKillAll = new List<CEventHandler>();

	void Init()
	{
		foreach (var e in enemies)
			e.onDeath += OnEnemyKilled;
	}

	void OnEnemyKilled(Component killer)
	{
		// using System.Linq for the method "Any"
		bool any_alive = enemies.Any(x => x.GetHealth() > 0);
		if (!any_alive)
		{
			// notify subscribers
			foreach (var receiver in onKillAll)
				receiver.OnReceiveEvent(this);	
		}
	}
}