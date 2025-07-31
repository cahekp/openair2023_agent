using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "45b8b1e6bd27a00d427c420891d57dc7fcd2a07a")]
public class AWeapon : Component
{
	public vec3 attach_position;
	public vec3 attach_rotation;

	protected Component owner = null;

	public virtual void Use()
	{
		// attack / shot / kick / hit...
	}

	public void SetOwner(Component in_owner)
	{
		owner = in_owner;
	}

	public mat4 GetAttachTransform()
	{
		return new mat4(new quat(attach_rotation.x, attach_rotation.y, attach_rotation.z), attach_position);
	}	
}