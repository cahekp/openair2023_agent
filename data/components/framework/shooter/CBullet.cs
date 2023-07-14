using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "8f3797296b7cf6748d4daf437ac08bc6d63cf06d")]
public class CBullet : Component
{
	public float damage = 1.0f;
	[ParameterMask] public int intersection_mask = ~0;
	public AssetLinkNode bullet_hole_file;

	public enum Mode { Raycast, Projectile }
	public Mode mode = Mode.Projectile;

	[ParameterCondition(nameof(mode), (int)Mode.Raycast)]
	public float raycast_distance = 1000.0f;

	[ParameterCondition(nameof(mode), (int)Mode.Projectile)]
	public float speed = 100.0f;
	[ParameterCondition(nameof(mode), (int)Mode.Projectile)]
	public float lifetime = 3.0f;

	Component owner;
	WorldIntersectionNormal intersection = new WorldIntersectionNormal();

	public void Setup(Component in_owner)
	{
		owner = in_owner;

		if (mode == Mode.Raycast)
		{
			vec3 p0 = node.WorldPosition;
			vec3 p1 = p0 + node.GetWorldDirection(MathLib.AXIS.Y) * raycast_distance;
			CheckIntersection(p0, p1);
			node.DeleteLater();
		}
	}

	void Update()
	{
		if (mode != Mode.Projectile)
			return;

		// move the bullet
		vec3 prev_pos = node.WorldPosition;
		node.WorldPosition = prev_pos + node.GetWorldDirection(MathLib.AXIS.Y) * speed * Game.IFps;

		// check intersection
		CheckIntersection(prev_pos, node.WorldPosition);

		// check lifetime
		lifetime -= Game.IFps;
		if (lifetime <= 0)
			node.DeleteLater();
	}

	void CheckIntersection(vec3 p0, vec3 p1)
	{
		Object hit = World.GetIntersection(p0, p1, intersection_mask, intersection);
		if (!hit)
			return;

		// apply hit (damage)
		CHealth damage_receiver = GetComponentInParent<CHealth>(hit);
		if (damage_receiver)
			damage_receiver.TakeDamage(owner, damage);

		// draw bullet hole
		if (bullet_hole_file.IsFileExist)
		{
			Node bullet_hole_node = bullet_hole_file.Load(intersection.Point, MathLib.RotationFromDir(intersection.Normal));
			bullet_hole_node.SetWorldParent(hit);
		}

		// destroy the bullet
		node.DeleteLater();
	}
}