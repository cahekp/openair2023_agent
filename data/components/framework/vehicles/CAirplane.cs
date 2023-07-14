using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "2d0356874d43223a4024cc511b9e997cd1c026ab")]
public class CAirplane : Component
{
	public float MaxThrust = 30f;

	public float ThrustSpeed = 5f;
	public float PitchSpeed = 90f;
	public float YawSpeed = 45f;
	public float RollSpeed = 90f;
	public float AngularSmoothFactor = 2f;

	float thrust, pitch, yaw, roll;
	BodyRigid body;

	void Init()
	{
		body = node.ObjectBodyRigid;
	}
	
	void Update()
	{
		// thrust
		float t = 0;
		if (Input.IsKeyPressed(Input.KEY.LEFT_SHIFT))
			t += ThrustSpeed;
		if (Input.IsKeyPressed(Input.KEY.SPACE))
			t -= ThrustSpeed;
		thrust = MathLib.Clamp(thrust + t * Game.IFps, 0, MaxThrust);

		// pitch
		float p = 0;
		if (Input.IsKeyPressed(Input.KEY.S))
			p += PitchSpeed;
		if (Input.IsKeyPressed(Input.KEY.W))
			p -= PitchSpeed;
		p *= Game.IFps;
		pitch = LerpIFps(pitch, p, AngularSmoothFactor);

		// yaw
		float y = 0;
		if (Input.IsKeyPressed(Input.KEY.Q))
			y += YawSpeed;
		if (Input.IsKeyPressed(Input.KEY.E))
			y -= YawSpeed;
		y *= Game.IFps;
		yaw = LerpIFps(yaw, y, AngularSmoothFactor);

		// roll
		float r = 0;
		if (Input.IsKeyPressed(Input.KEY.D))
			r += RollSpeed;
		if (Input.IsKeyPressed(Input.KEY.A))
			r -= RollSpeed;
		r *= Game.IFps;
		roll = LerpIFps(roll, r, AngularSmoothFactor);
	}

	void UpdatePhysics()
	{
		// move
		vec3 axis_forward = node.GetWorldDirection(MathLib.AXIS.Y);
		body.LinearVelocity = MathLib.Lerp(body.LinearVelocity, axis_forward * thrust, thrust / MaxThrust);
		
		// rotate
		vec3 axis_pitch = node.GetWorldDirection(MathLib.AXIS.X);
		vec3 axis_roll = axis_forward;
		vec3 axis_yaw = node.GetWorldDirection(MathLib.AXIS.Z);
		body.AngularVelocity = axis_pitch * pitch + axis_yaw * yaw + axis_roll * roll;
	}

	float LerpIFps(float a, float b, float rate)
	{
		return MathLib.Lerp(a, b, 1.0f - MathLib.Exp(-rate * Game.IFps));
	}
}