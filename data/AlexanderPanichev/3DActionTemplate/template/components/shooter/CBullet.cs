using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unigine;

#if UNIGINE_DOUBLE
using Scalar = System.Double;
using Vec2 = Unigine.dvec2;
using Vec3 = Unigine.dvec3;
using Vec4 = Unigine.dvec4;
using Mat4 = Unigine.dmat4;
#else
using Scalar = System.Single;
using Vec2 = Unigine.vec2;
using Vec3 = Unigine.vec3;
using Vec4 = Unigine.vec4;
using Mat4 = Unigine.mat4;
using WorldBoundBox = Unigine.BoundBox;
using WorldBoundSphere = Unigine.BoundSphere;
using WorldBoundFrustum = Unigine.BoundFrustum;
#endif

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
			Vec3 p0 = node.WorldPosition;
			Vec3 p1 = p0 + node.GetWorldDirection(MathLib.AXIS.Y) * raycast_distance;
			CheckIntersection(p0, p1);
			node.DeleteLater();
		}
	}

	void Init()
	{
		// disable intersections of all children to prevent self-intersections
		// in the method CheckIntersection()
		Action<Node> recusive_disable_intersection = null;
		recusive_disable_intersection = (n) =>
		{
			if (n.IsObject)
			{
				Unigine.Object o = n as Unigine.Object;
				for (int j = 0; j < o.NumSurfaces; j++)
					o.SetIntersection(false, j);
			}
			else if (n is NodeReference)
			{
				NodeReference nr = n as NodeReference;
				recusive_disable_intersection(nr.Reference);
			}			

			for (int i = 0; i < n.NumChildren; i++)
				recusive_disable_intersection(n.GetChild(i));
		};
		recusive_disable_intersection(node);
	}

	void Update()
	{
		if (mode != Mode.Projectile)
			return;

		// move the bullet
		Vec3 prev_pos = node.WorldPosition;
		node.WorldPosition = prev_pos + node.GetWorldDirection(MathLib.AXIS.Y) * speed * Game.IFps;

		// check intersection
		CheckIntersection(prev_pos, node.WorldPosition);

		// check lifetime
		lifetime -= Game.IFps;
		if (lifetime <= 0)
			node.DeleteLater();
	}

	void CheckIntersection(Vec3 p0, Vec3 p1)
	{
		Unigine.Object hit = World.GetIntersection(p0, p1, intersection_mask, intersection);
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