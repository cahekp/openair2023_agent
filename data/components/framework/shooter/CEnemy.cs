using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "57e035ab45e6cf0ef8050b15ec27e1702238060f")]
public class CEnemy : Component
{
	// public
	[Parameter(Group = "Eyes")] public Node eyes_node;
	[Parameter(Group = "Eyes", Tooltip = "Field of View (degrees)")]
								public float fov = 60.0f;
	[Parameter(Group = "Eyes")] public float max_distance = 100.0f;
	[ParameterMask(Group = "Eyes", MaskType = ParameterMaskAttribute.TYPE.INTERSECTION)]
								public int obstacle_mask = 1;

	[Parameter(Group = "Patrol")] public Node path;
	[Parameter(Group = "Patrol")] public float standing_time = 5.0f;
	[Parameter(Group = "Patrol")] public float walk_speed = 2.0f;

	[Parameter(Group = "Attack")] public Node hand;
	[Parameter(Group = "Attack")] public CWeaponRanged weapon;
	[Parameter(Group = "Attack")] public float angle_to_attack = 10.0f;

	[Parameter(Group = "Search")] public float searching_time = 5.0f;

	[Parameter(Group = "Animation")] public ObjectMeshSkinned animated_model;
	[Parameter(Group = "Animation")] public AssetLink anim_idle;
	[Parameter(Group = "Animation")] public AssetLink anim_walk;
	[Parameter(Group = "Animation")] public AssetLink anim_attack;

	[Parameter(Group = "Death")] public AssetLink death_node;

	// private
	enum State { Patrol, Attack, Search }
	State state = State.Patrol;

	BodyRigid body;

	void Init()
	{
		// check object type
		Object obj = node as Object;
		if (!obj)
		{
			Log.Error("CEnemy(): enemy is not an \"Object\" type!\n");
			return;
		}

		body = obj.BodyRigid;
		if (!body)
		{
			Log.Error("CEnemy(): enemy has no \"BodyRigid\"!\n");
			return;
		}

		// subscribe to health controller
		CHealth health = GetComponent<CHealth>(node);
		if (health)
		{
			health.onDeath += (Component owner) => { Death(owner); };
			health.onTakeDamage += StartAttack;
		}

		if (weapon)
			weapon.SetOwner(this);

		InitAnimation();
	}
	
	void Update()
	{
		// logic
		UpdateEyes();
		switch (state)
		{
			case State.Patrol: UpdatePatrol(); break;
			case State.Attack: UpdateAttack(); break;
			case State.Search: UpdateSearch(); break;
		}

		// animation
		UpdateAnimation();
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// EYES
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void UpdateEyes()
	{
		CPlayer player = CGame.Get().GetPlayer();
		if (player == null || state == State.Attack)
			return;

		// segment from the eyes to the player's body
		vec3 p0 = eyes_node.WorldPosition;
		vec3 p1 = player.node.HierarchyWorldBoundBox.Center;
		
		// player is far
		if (MathLib.Length(p1 - p0) > max_distance)
			return;

		// player is out of view
		if (MathLib.Angle(MathLib.Normalize(p1 - p0), eyes_node.GetWorldDirection(MathLib.AXIS.Y)) > fov)
			return;
		
		// obstacles between enemy eyes and player 
		if (World.GetIntersection(p0, p1, obstacle_mask))
			return;

		// start attacking
		StartAttack(player);
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// PATROL
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	enum PatrolState { MoveForward, StandAtEnd, MoveBackward, StandAtBegin }
	PatrolState patrol_state = PatrolState.StandAtBegin;
	int path_index = 0;
	float patrol_timer = 0;

	void UpdatePatrol()
	{
		// don't do anything, just standing
		if (path == null || path.NumChildren < 2)
			return;

		switch (patrol_state)
		{
			case PatrolState.MoveForward: PatrolMoveForward(); break;
			case PatrolState.StandAtEnd: PatrolStandAtEnd(); break;
			case PatrolState.MoveBackward: PatrolMoveBackward(); break;
			case PatrolState.StandAtBegin: PatrolStandAtBegin(); break;
		}
	}

	bool TryToMoveToTarget()
	{
		vec3 to_target = path.GetChild(path_index).WorldPosition - node.WorldPosition;
		float dist_to_target = MathLib.Length(to_target.xy);
		
		// arrive to target waypoint
		if (dist_to_target <= 1.0f)
			return false;

		// move to target waypoint
		vec2 move_dir = MathLib.Normalize(to_target.xy);
		vec3 velocity = new vec3(move_dir * walk_speed, body.LinearVelocity.z);
		body.Rotation = MathLib.Slerp(body.Rotation, MathLib.RotationFromDir(new vec3(move_dir)), 1.0f - MathLib.Exp(-5.0f * Game.IFps));
		body.LinearVelocity = velocity;
		return true;
	}

	void PatrolMoveForward()
	{
		if (!TryToMoveToTarget())
		{
			// arrived to target waypoint
			path_index++;
			if (path_index >= path.NumChildren)
			{
				patrol_state = PatrolState.StandAtEnd;
				patrol_timer = 0;
			}
		}
	}

	void PatrolStandAtEnd()
	{
		// waiting
		patrol_timer += Game.IFps;
		if (patrol_timer >= standing_time)
		{
			patrol_state = PatrolState.MoveBackward;
			path_index = path.NumChildren - 2;
		}
	}

	void PatrolMoveBackward()
	{
		if (!TryToMoveToTarget())
		{
			// arrived to target waypoint
			path_index--;
			if (path_index < 0)
			{
				patrol_state = PatrolState.StandAtBegin;
				patrol_timer = 0;
			}
		}
	}

	void PatrolStandAtBegin()
	{
		// waiting
		patrol_timer += Game.IFps;
		if (patrol_timer >= standing_time)
		{
			patrol_state = PatrolState.MoveForward;
			path_index = 1;
		}
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// ATTACK
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////	

	Component target_enemy;
	Node target_enemy_node;
	CHealth target_enemy_health;

	void StartAttack(Component target)
	{
		// no weapon - no attack!
		if (weapon == null)
			return;

		// is attacking already
		if (state == State.Attack && target == target_enemy)
			return;

		// enemy immortal check
		target_enemy_health = GetComponentInChildren<CHealth>(target.node);
		if (target_enemy_health == null)
			return;

		state = State.Attack;
		target_enemy = target;
		target_enemy_node = target.node;
	}

	void UpdateAttack()
	{
		// if target is death
		if (!target_enemy_node || 					// 1. deleted node
			!target_enemy.Enabled ||				// 2. switched to another model?
		 	target_enemy_health.GetHealth() <= 0)	// 3. death
		{
			// return to patrol
			state = State.Patrol;
		}

		// is the target gone?
		if (!IsEnemyVisible())
		{
			StartSearch();
			return;
		}

		// turn body to target
		float k = 1.0f - MathLib.Exp(-5.0f * Game.IFps);
		vec3 body_dir_to_target = new vec3(MathLib.Normalize((target_enemy_node.WorldPosition - node.WorldPosition).xy));
		vec3 velocity = body.LinearVelocity;
		body.Rotation = MathLib.Slerp(body.Rotation, MathLib.RotationFromDir(body_dir_to_target), k);
		body.LinearVelocity = velocity;

		// turn hand (weapon) to target and shoot
		vec3 target_point = target_enemy_node.HierarchyWorldBoundBox.Center;
		vec3 hand_dir_to_target = MathLib.Normalize(target_point - hand.WorldPosition);
		hand.SetWorldRotation(MathLib.Slerp(hand.GetWorldRotation(), MathLib.RotationFromDir(hand_dir_to_target), k));
		if (MathLib.Angle(hand.GetWorldDirection(MathLib.AXIS.Y), hand_dir_to_target) <= angle_to_attack)
			weapon.Use();
	}

	bool IsEnemyVisible()
	{
		if (target_enemy_node == null)
			return false;

		vec3 p0 = weapon.bullet_spawn_point.WorldPosition;
		vec3 p1 = target_enemy_node.HierarchyWorldBoundBox.Center;
		if (World.GetIntersection(p0, p1, obstacle_mask))
			return false;

		return true;
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// SEARCH THE PLAYER
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////	

	float search_timer = 0;

	void StartSearch()
	{
		state = State.Search;
		search_timer = 0;
	}

	void UpdateSearch()
	{
		search_timer += Game.IFps;
		if (search_timer >= searching_time)
		{
			state = State.Patrol;
			target_enemy = null;
			target_enemy_node = null;
			target_enemy_health = null;
			return;
		}

		if (IsEnemyVisible())
		{
			state = State.Attack;
			return;
		}
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// ANIMATION
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	enum AnimationType { Idle, Walk, Attack, NUM };
	float[] anim_weights = new float[(int)AnimationType.NUM];

	void InitAnimation()
	{
		if (!animated_model)
			return;

		animated_model.NumLayers = 3;
		animated_model.SetAnimation((int)AnimationType.Idle, anim_idle.Path);
		animated_model.SetAnimation((int)AnimationType.Walk, anim_walk.Path);
		animated_model.SetAnimation((int)AnimationType.Attack, anim_attack.Path);
		animated_model.Loop = 1;
		
		anim_weights[0] = 1; // idle is default animation
	}

	void UpdateAnimation()
	{
		if (!animated_model)
			return;
		
		// set animation
		if (state == State.Attack)
			SetAnimationSmooth(AnimationType.Attack);
		else
		{
			float move_speed = body.LinearVelocity.Length;
			if (move_speed < walk_speed * 0.5f)
				SetAnimationSmooth(AnimationType.Idle);
			else
				SetAnimationSmooth(AnimationType.Walk);
		}

		// play animation (blend between all animations using weights)
		for (int i = 0; i < (int)AnimationType.NUM; i++)
		{
			animated_model.SetLayer(i, true, anim_weights[i]);
			animated_model.SetFrame(i, Game.Time * animated_model.Speed);
		}
	}

	void SetAnimationSmooth(AnimationType type)
	{
		for (int i = 0; i < (int)AnimationType.NUM; i++)
		{
			if (i == (int)type) // increase weight
				anim_weights[i] = MathLib.MoveTowards(anim_weights[i], 1.0f, 2.0f * Game.IFps);
			else // decrease weight
				anim_weights[i] = MathLib.MoveTowards(anim_weights[i], 0.0f, 2.0f * Game.IFps);
		}
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// DEATH
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Death(Component killer)
	{
		if (death_node.IsFileExist)
		{
			// create "death" version of the enemy
			Node n = World.LoadNode(death_node.Path);

			// align position / rotation to "alive" version
			n.WorldTransform = node.WorldTransform;

			// kick the model
			List<Node> nodes = new List<Node>();
			n.GetHierarchy(nodes);
			foreach (var nn in nodes)
			{
				if (!nn.IsObject || !nn.ObjectBodyRigid)
					continue;
				nn.ObjectBodyRigid.AddImpulse(vec3.ZERO, 3.0f * MathLib.Normalize(n.WorldPosition - killer.node.WorldPosition));
			}
		}

		// delete this node
		node.DeleteLater();
	}
}