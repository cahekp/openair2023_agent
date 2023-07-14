using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "4a64efeda94c90687103b8a77d80ddda9ea91371")]
public class CDoor : CEventHandler
{
	public enum RotationAxis { X, Y, Z };
	public RotationAxis rotation_axis = RotationAxis.Z;
	public float open_angle = 90f;
	
	[Parameter(Tooltip = "Degrees per second")]
	public float animation_speed = 60f;

	vec3 axis;
	quat rotation_close;
	bool is_closed = true;
	bool is_playing = false;

	public override void Activate(Component sender)
	{
		// start animation
		is_closed = !is_closed;
		is_playing = true;
	}

	void Init()
	{
		switch (rotation_axis)
		{
			case RotationAxis.X: axis = new vec3(1,0,0); break;
			case RotationAxis.Y: axis = new vec3(0,1,0); break;
			case RotationAxis.Z: axis = new vec3(0,0,1); break;
		}
		rotation_close = node.GetRotation();
	}
	
	void Update()
	{
		if (!is_playing)
			return;

		// animation
		quat target = is_closed ? rotation_close : rotation_close * new quat(axis, open_angle);
		node.SetRotation(MathLib.RotateTowards(node.GetRotation(), target, animation_speed * Game.IFps));

		// stop check
		if (MathLib.Angle(node.GetRotation(), target) <= MathLib.EPSILON)
			is_playing = false;
	}
}