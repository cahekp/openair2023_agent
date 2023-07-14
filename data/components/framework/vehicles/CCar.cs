using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

// based on this article:
// https://developer.unigine.com/en/docs/2.16.1/code/usage/car_wheel_joints/index?rlang=cs

[Component(PropertyGuid = "438b6b46b11749edba372268051922a7e4d33fda")]
public class CCar : Component
{
	// driving parameters
	public float acceleration = 50.0f;
	public float max_velocity = 90.0f;
	float max_turn_angle = 30.0f;

	public float turn_speed = 30.0f;
	public float default_torque = 3.0f;  
	public float car_base = 3.0f;
	public float car_width = 2.0f;

	public float down_force = 1.0f;

	// nodes with the joints
	public Node wheel_bl;
	public Node wheel_br;
	public Node wheel_fl;
	public Node wheel_fr;

	// camera class
	public Player player;

	// body
	BodyRigid body;

	// wheel joints
	JointWheel joint_wheel_bl;
	JointWheel joint_wheel_br;
	JointWheel joint_wheel_fl;
	JointWheel joint_wheel_fr;

	// settings of input controls 
	Controls controls;

	// current driving parameters
	float current_velocity = 0.0f;
	float current_torque = 0.0f;

	float current_turn_angle = 0.0f;

	void Init()
	{
		body = node.ObjectBodyRigid;

		// get the wheel joints from the nodes
		if (wheel_bl)
			joint_wheel_bl = wheel_bl.ObjectBody.GetJoint(0) as JointWheel;

		if (wheel_br)
			joint_wheel_br = wheel_br.ObjectBody.GetJoint(0) as JointWheel;

		if (wheel_fl)
			joint_wheel_fl = wheel_fl.ObjectBody.GetJoint(0) as JointWheel;

		if (wheel_fr)
			joint_wheel_fr = wheel_fr.ObjectBody.GetJoint(0) as JointWheel;	

		// get the settings of input controls relevant to the player (camera)
		if (player)	
			controls = player.Controls;	
	}

	void Update()
	{
		// get the time it took to render the previous frame
		float deltaTime = Game.IFps;

		current_torque = 0.0f;

		// process control inputs
		if (controls)
		{

			// set the torque and increase the current velocity if the forward button is pressed
			if (controls.GetState(Controls.STATE_FORWARD) != 0)
			{
				current_torque = default_torque;
				current_velocity = MathLib.Max(current_velocity, 0.0f);
				current_velocity += deltaTime * acceleration; 
			}
			// set the torque and decrease the current velocity if the backward button is pressed
			else if (controls.GetState(Controls.STATE_BACKWARD) != 0) 
			{
				current_torque = default_torque;
				current_velocity = MathLib.Min(current_velocity, 0.0f);
				current_velocity -= deltaTime * acceleration; 
			}
			// exponentially decrease the current velocity when neither throttle nor brakes are applied
			else
			{
				current_velocity *= MathLib.Exp(-deltaTime);
			}

			/*...*/
			// turn the front wheels based on the direction input
			if (controls.GetState(Controls.STATE_MOVE_LEFT) != 0)
				current_turn_angle += deltaTime * turn_speed;
			else if (controls.GetState(Controls.STATE_MOVE_RIGHT) != 0)
				current_turn_angle -= deltaTime * turn_speed;
			else
			{
				// get rid of the front wheel wiggle
				if (MathLib.Abs(current_turn_angle) < 0.25f)
					current_turn_angle = 0.0f;

				// align the front wheels if there are no more direction input
				current_turn_angle -= MathLib.Sign(current_turn_angle) * turn_speed * deltaTime;
			}

			// apply braking by maxing out the angular damping if the brake button is pressed
			if (controls.GetState(Controls.STATE_USE) != 0)
			{
				joint_wheel_bl.AngularDamping = 10000.0f;
				joint_wheel_br.AngularDamping = 10000.0f;
			}
			else
			{
				joint_wheel_bl.AngularDamping = 0.0f;
				joint_wheel_br.AngularDamping = 0.0f;
			}

		}

		// clamp the velocity and the front wheels turn angle
		current_velocity = MathLib.Clamp(current_velocity, -max_velocity, max_velocity);
		current_turn_angle = MathLib.Clamp(current_turn_angle, -max_turn_angle, max_turn_angle);

		// figure out the correct turn angle for the front wheels
		float angle_0 = current_turn_angle;
		float angle_1 = current_turn_angle;
		if (MathLib.Abs(current_turn_angle) > MathLib.EPSILON)
		{
			float radius = car_base / MathLib.Tan(current_turn_angle * MathLib.DEG2RAD);
			float radius_0 = radius - car_width * 0.5f;
			float radius_1 = radius + car_width * 0.5f;

			angle_0 = MathLib.Atan(car_base / radius_0) * MathLib.RAD2DEG;
			angle_1 = MathLib.Atan(car_base / radius_1) * MathLib.RAD2DEG;
		}

		joint_wheel_fr.Axis10 = MathLib.RotateZ(angle_1).GetColumn3(0);
		joint_wheel_fl.Axis10 = MathLib.RotateZ(angle_0).GetColumn3(0);

	}

	// actually apply the torque and velocity to the front wheel joints
	void UpdatePhysics()
	{
		joint_wheel_fl.AngularVelocity = current_velocity;
		joint_wheel_fr.AngularVelocity = current_velocity;

		joint_wheel_fl.AngularTorque = current_torque;
		joint_wheel_fr.AngularTorque = current_torque;

		// downforce
		float force = down_force * MathLib.Pow2(body.LinearVelocity.Length);
		body.AddForce(node.GetWorldDirection(MathLib.AXIS.NZ) * force);
	}
}