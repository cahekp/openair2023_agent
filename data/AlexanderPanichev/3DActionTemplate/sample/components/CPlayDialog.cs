using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "c274598235b34af9b8b9f804ef162393ac1653d6")]
public class CPlayDialog : CEventHandler
{
	public AssetLink image_1;
	public string name_1;
	public AssetLink image_2;
	public string name_2;
	public string text;
	public float chars_per_second = 5.0f;
	public AssetLink font;
	public string continue_text = "Press (Enter) to continue";

	bool is_playing = false;
	float duration;
	float time;
	UI_Sprite w_background;
	UI_Sprite w_face1;
	UI_Sprite w_face2;
	UI_Label w_name1;
	UI_Label w_name2;
	UI_Label w_text;
	UI_Label w_continue;

	public override void Activate(Component sender)
	{
		Game.Scale = 0;
		duration = text.Length / chars_per_second;
		time = -1; // a little pause before starting the animation
		is_playing = true;

		w_background = new UI_Sprite(0, 0, 0, 0);
		w_background.SetColor(0, 0, 0, 0.85f);
		w_background.SetAnchorExpand();

		w_face1 = new UI_Sprite(-128 - 20, 40, 256, 256, image_1.Path);
		w_face1.SetAnchorCenterTop(true);
		w_face2 = new UI_Sprite(128 + 20, 40, 256, 256, image_2.Path);
		w_face2.SetAnchorCenterTop(true);

		w_name1 = new UI_Label(0, w_face1.GetHeight() + 20, w_face1.GetWidth(), 30, name_1, w_face1);
		w_name1.SetFont(font.Path);
		w_name1.SetAnchorCenterTop(true);
		w_name1.SetTextAlign(UI_Label.HORIZONTAL_ALIGN.CENTER);
		w_name2 = new UI_Label(0, w_face1.GetHeight() + 20, w_face2.GetWidth(), 30, name_2, w_face2);
		w_name2.SetAnchorCenterTop(true);
		w_name2.SetTextAlign(UI_Label.HORIZONTAL_ALIGN.CENTER);
		w_name2.SetFont(font.Path);

		w_text = new UI_Label(40, w_name1.GetPositionY() + w_name1.GetHeight() + 100, 24, "");
		w_text.SetFont(font.Path);

		w_continue = new UI_Label(0, -30, 0, 30, continue_text);
		w_continue.SetFont(font.Path);
		w_continue.SetAnchorCenterBottom(true);
		w_continue.SetTextAlign(UI_Label.HORIZONTAL_ALIGN.CENTER);
	}

	void Init()
	{
		text = text.Replace("\\n", "\n");
	}
	
	void Update()
	{
		if (!is_playing)
			return;

		bool is_enter_pressed = Input.IsKeyDown(Input.KEY.ENTER);

		time = MathLib.Min(time + Engine.IFps, duration);
		if (time < duration && is_enter_pressed)
		{
			// show all text, skip animation
			time = duration;
			is_enter_pressed = false;
		}

		// text animation
		if (time < 0)
			w_text.SetText("");
		else
			w_text.SetText(text.Substring(0, (int)(text.Length * time / duration)));

		// continue game
		if (time >= duration && is_enter_pressed)
		{
			// stop animation, hide UI
			is_playing = false;

			Game.Scale = 1;

			w_background.Delete();
			w_name1.Delete();
			w_name2.Delete();
			w_face1.Delete();
			w_face2.Delete();
			w_text.Delete();
			w_continue.Delete();
		}		

	}
}