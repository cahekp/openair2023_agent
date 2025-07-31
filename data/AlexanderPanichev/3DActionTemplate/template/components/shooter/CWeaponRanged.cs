using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "1baa85149411ad63bb15336633f3338dcf17bf88")]
public class CWeaponRanged : AWeapon
{
	public Node muzzle_flash;
	public AssetLinkNode bullet_file;
	public Node bullet_spawn_point;
	public float shots_per_minute = 400.0f;
	public float bullet_spread = 0.75f;
	
	public float recoil_force = 0.15f;
	public float return_speed = 3.0f;

	float cooldown_timer = 0;
	int muzzle_flash_frame = 0;

	public override void Use()
	{
		if (cooldown_timer > 0)
			return;

		// delay between shoots
		cooldown_timer = shots_per_minute > 0 ? 60.0f / shots_per_minute : 0;

		// create bullet
		quat bullet_rot = bullet_spawn_point.GetWorldRotation() * new quat(
			Game.GetRandomFloat(-bullet_spread, bullet_spread),
			Game.GetRandomFloat(-bullet_spread, bullet_spread),
			Game.GetRandomFloat(-bullet_spread, bullet_spread));
		Node bullet_node = bullet_file.Load(bullet_spawn_point.WorldPosition, bullet_rot);
		CBullet bullet = GetComponent<CBullet>(bullet_node);
		bullet.Setup(owner);

		// show muzzle flash
		if (muzzle_flash)
		{
			muzzle_flash.Enabled = true;
			muzzle_flash.Rotate(0, 0, Game.GetRandomFloat(0.0f, 360.0f));
			muzzle_flash_frame = Game.Frame;
		}

		// recoil
		node.Position = new vec3(0, -recoil_force, 0);
	}

	void Init()
	{
		if (muzzle_flash)
			muzzle_flash.Enabled = false;
	}

	void Update()
	{
		if (!owner)
			return;

		// pause between shots
		cooldown_timer = MathLib.Max(0.0f, cooldown_timer - Game.IFps);

		// hide muzzle flash after last shot (plus several frames)
		if (muzzle_flash && Game.Frame > muzzle_flash_frame + 3)
			muzzle_flash.Enabled = false;

		// returning animation after recoil
		node.Position = MathLib.Lerp(node.Position, new vec3(0,0,0), 1.0f - MathLib.Exp(-return_speed * Game.IFps));
	}
}