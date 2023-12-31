using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "13643e263aff3f4d1d22d848b8689f07d0cad0ba")]
public class CUIButton : Component
{
	public AssetLink sound_hover;
	public AssetLink sound_click;

	public List<CEventHandler> onClicked = new List<CEventHandler>(); 

	WorldIntersection intersection = new WorldIntersection();
	Unigine.Object button_obj;
	AmbientSource sound;
	bool prev_hover;

	void Init()
	{
		// hide background of the button by default
		button_obj = node as Unigine.Object;
		button_obj.SetMaterialParameterFloat4("emission_color", new vec4(0), 0);		
	}

	void Update()
	{
		if (IsHover())
		{
			// play hover sound
			if (!prev_hover)
			{
				prev_hover = true;
				PlaySound(sound_hover.Path);
			}

			// animation (appearing)
			SetVisibilitySmooth(true);

			// click
			if (Input.IsMouseButtonDown(Input.MOUSE_BUTTON.LEFT))
			{
				// play sound
				PlaySound(sound_click.Path);

				// notify subscribers
				foreach (var receiver in onClicked)
					receiver.OnReceiveEvent(this);
			}
		}
		else
		{
			prev_hover = false;

			// animation (hiding)
			SetVisibilitySmooth(false);
		}
		
	}

	bool IsHover()
	{
		// getting direction from the current mouse position
		ivec2 mouse = Input.MousePosition;
		vec3 dir = Game.Player.GetDirectionFromMainWindow(mouse.x, mouse.y);
		
		// get points for intersection
		vec3 p0 = Game.Player.WorldPosition;
		vec3 p1 = p0 + dir * Game.Player.ZFar;

		// find the intersection
		Unigine.Object obj = World.GetIntersection(p1, p0, 1, intersection);
		
		// return true if the mouse cursor is over this element
		return obj == node;
	}

	void SetVisibilitySmooth(bool visible)
	{
		vec4 color = button_obj.GetMaterialParameterFloat4("emission_color", 0);
		vec4 target_color = new vec4(visible ? 1 : 0);
		button_obj.SetMaterialParameterFloat4("emission_color", MathLib.Lerp(color, target_color, 7.5f * Game.IFps), 0);
	}

	void PlaySound(string name)
	{
		if (sound == null)
			sound = new AmbientSource(name); // auto play
		else
		{
			sound.SampleName = name;
			sound.Play();
		}
	}
}