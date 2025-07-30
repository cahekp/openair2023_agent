using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "0f4f7c08cbab6fc7cd1bea7aac307791f7b1cbbc")]
public class CHealth : Component
{
	[ShowInEditor] float health = 5.0f;
	public bool godMode = false;
	
	public Action<Component> onAddHealth;
	public Action<Component> onTakeDamage;
	public Action<Component> onDeath;

	float max_health = 1.0f;

	public float GetHealth()
	{
		return health;
	}

	public float GetMaxHealth()
	{
		return max_health;
	}

	public float GetHealthPercent()
	{
		return MathLib.Saturate(health / max_health);
	}

	public void TakeDamage(Component owner, float damage)
	{
		if (health <= 0)
			return;

		if (!godMode)
			health = MathLib.Max(0, health - damage);

		if (onTakeDamage != null)
			onTakeDamage.Invoke(owner);
		if (health <= 0 && onDeath != null)
			onDeath.Invoke(owner);
	}

	public void AddHealth(Component owner, float add_healh)
	{
		health += add_healh;
		if (onAddHealth != null)
			onAddHealth.Invoke(owner);
	}

	void Init()
	{
		max_health = health;
	}
}