using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "5cc55e00c3e8b95f036337325458ace9bc959134")]
public class CWeaponMelee : AWeapon
{
	public vec3 hit_position;
	public vec3 hit_rotation;

	public float damage = 1.0f;
	[ParameterMask] public int intersection_mask = ~0;
	public float hits_per_minute = 120.0f;
	public float attack_distance = 2.5f;

	[Parameter(Group = "Advanced", Tooltip = "Range: [0,1)")]
	public float hit_time_in_animation = 0.3f;

	float hit_duration = 0;
	float animation_timer = 0;
	bool is_before_hit = true;
	quat quat_attach_rotation;
	quat quat_hit_rotation;

	public override void Use()
	{
		// if animation is in progress yet
		if (animation_timer < hit_duration)
			return;

		// start the animation of hit
		animation_timer = 0;
		is_before_hit = true;
	}

	void Init()
	{
		hit_duration = hits_per_minute > 0 ? 60.0f / hits_per_minute : 0;
		animation_timer = hit_duration;
		quat_attach_rotation = new quat(attach_rotation.x, attach_rotation.y, attach_rotation.z);
		quat_hit_rotation = new quat(hit_rotation.x, hit_rotation.y, hit_rotation.z);
	}

	void Update()
	{
		if (!owner || animation_timer >= hit_duration)
			return;

		// play animation
		animation_timer = MathLib.Min(animation_timer + Game.IFps, hit_duration);
		float percent = animation_timer / hit_duration;
		if (percent < hit_time_in_animation)
		{
			// before hit
			is_before_hit = true;
			percent = percent / hit_time_in_animation; // normalize to [0, 1]
		}
		else
		{
			// hit!
			if (!is_before_hit)
				Hit();

			// after hit
			is_before_hit = false;
			percent = 1.0f - (percent - hit_time_in_animation) / (1.0f - hit_time_in_animation); // normalize to [0, 1]
		}
		
		node.Position = MathLib.Lerp(attach_position, hit_position, percent);
		node.SetRotation(MathLib.Slerp(quat_attach_rotation, quat_hit_rotation, percent), true);
	}

	void Hit()
	{

	}
}