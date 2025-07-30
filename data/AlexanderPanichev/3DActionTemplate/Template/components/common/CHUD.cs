using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "b8860d3f4fabb992d3598bc94c4e22949ea1ace3")]
public class CHUD : Component
{
	UI_Label mission; // mission target description
	UI_Label help; // keys helper

	UI_Sprite health_bg; // health bar background
	UI_Sprite health_fg; // health bar foreground

	UI_Label item_name;
	UI_Label item_helper;

	void Init()
	{
		mission = new UI_Label(10, 10, 300, 20, "Mission #1");

		help = new UI_Label(-10, 10, 20,
			"<p align=right>WASD - Move<br>" +
			"Left Ctrl - Crouch<br>" +
			"Left Shift - Run<br>" +
			"Mouse - Look<br>" +
			"Mouse Scroll - Change Weapon<br>" +
			"Left Mouse Button - Shoot<br>" +
			"F - Use<br>" +
			"Shift - Increase Throttle (Aircraft)<br>" +
			"Space - Decrease Throttle (Aircraft)</p>"
		);
		help.SetFontColor(new vec4(1,1,1,0.5f));
		help.SetFontRich(true);
		help.SetTextAlign(UI_Label.HORIZONTAL_ALIGN.RIGHT);
		help.SetAnchorRightTop(true);

		health_bg = new UI_Sprite(10, -10, 400, 20);
		health_bg.SetAnchorLeftBottom(true);
		health_bg.SetColor(0,0,0,0.75f);

		health_fg = new UI_Sprite(0, 0, 0, 0);
		health_bg.AddChild(health_fg);
		health_fg.SetPosition(3, 3);
		health_fg.SetWidth(health_bg.GetWidth() - 6);
		health_fg.SetHeight(health_bg.GetHeight() - 6);
		health_fg.SetColor(1,0.2f,0.2f,1);

		item_name = new UI_Label(0, -280, 300, 30, "Item #1");
		item_name.SetTextAlign(UI_Label.HORIZONTAL_ALIGN.CENTER);
		item_name.SetAnchorCenterBottom(true);

		item_helper = new UI_Label(0, -250, 300, 20, "Press (F) to Use");
		item_helper.SetTextAlign(UI_Label.HORIZONTAL_ALIGN.CENTER);
		item_helper.SetAnchorCenterBottom(true);

	}
	
	void Update()
	{
		var game = CGame.Get();
		CPlayer player = game.GetPlayer();
		if (player == null)
			return;

		mission.SetText(game.GetCurrentMissionTaskName());

		help.arrange();

		CHealth health_info = player.GetHealthInfo();
		if (health_info != null)
			health_fg.SetWidth(MathLib.Lerp(0.0f, health_bg.GetWidth() - 6.0f, health_info.GetHealthPercent()));
		else
			health_fg.SetWidth(health_bg.GetWidth() - 6.0f);

		CInteractable item = player.GetCurrentSelectedItem();
		if (item != null)
		{
			item_name.SetText(item.name);
			
			string t = "Press (F) to ";
			switch (item.interact_type)
			{
				case CInteractable.Type.Take: t += "Take"; break;
				case CInteractable.Type.Use: t += "Use"; break;
				case CInteractable.Type.NeedItemToUse:
					if (player.HasItem(item.item))
						t += "Use";
					else
						t = "Need item to Use";
					break;
			}
			item_helper.SetText(t);
		}
		else
		{
			item_name.SetText("");
			item_helper.SetText("");
		}

		
		
	}
}