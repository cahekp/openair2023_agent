using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "5d23a5d55d1e3d30fd092ecf50c3cde2757a73d9")]
public class CGame : Component
{
	[ShowInEditor] CPlayer player;
	CHealth player_health;

	public struct MissionTask
	{
		public string name;
		public Node target_position;
		public CEventHandler event_handler;
	}
	public MissionTask[] mission_tasks;
	int current_mission_task = 0;

	public AssetLink world_success;
	public AssetLink world_fail;

	static CGame instance;
	public static CGame Get() { return instance; }

	public void SetPlayer(CPlayer new_player)
	{
		// unsubscribe
		if (player_health != null)
			player_health.onDeath -= OnPlayerDead;

		// change player
		player = new_player;

		// subscribe
		player_health = GetComponentInChildren<CHealth>(new_player.node);
		if (player_health)
			player_health.onDeath += OnPlayerDead;
	}

	public CPlayer GetPlayer()
	{
		return player;
	}

	public string GetCurrentMissionTaskName()
	{
		if (mission_tasks == null ||
			current_mission_task < 0 ||
			current_mission_task >= mission_tasks.Length)
			return string.Empty;

		return mission_tasks[current_mission_task].name;
	}

	[MethodInit(Order = -1)]
	void Init()
	{
		instance = this;

		SetPlayer(player);

		// set mouse to "game" mode (not "ui" mode)
		Input.MouseHandle = Input.MOUSE_HANDLE.GRAB;

		// waiting for complete first task (using Actions)
		SubscribeToMissionTaskTrigger();
	}
	
	bool SubscribeToMissionTaskTrigger()
	{
		if (mission_tasks == null || current_mission_task >= mission_tasks.Length)
			return false;

		CEventHandler handler = mission_tasks[current_mission_task].event_handler;
		if (handler)
			handler.onActivated += OnCompleteMissionTask;
		return true;
	}

	void OnCompleteMissionTask()
	{
		// unsubscribe
		CEventHandler handler = mission_tasks[current_mission_task].event_handler;
		handler.onActivated -= OnCompleteMissionTask;
		
		// task is completed, go to next task!
		current_mission_task++;
		if (!SubscribeToMissionTaskTrigger())
		{
			// mission complete, load next level!
			if (world_success.IsFileExist)
				World.LoadWorld(world_success.Path);
		}
	}

	void OnPlayerDead(Component killer)
	{
		// mission failed!
		if (world_fail.IsFileExist)
			World.LoadWorld(world_fail.Path);
		else
			World.ReloadWorld();
	}
}