using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "7daa5fc5da7b4e601d4c2e3f97e16692c60154c9")]
public class CPlayer : Component
{
	[Parameter(Group = "Eyes")] public Player camera;
	[Parameter(Group = "Hand")] public Node hand;
	[Parameter(Group = "Hand")] public Input.MOUSE_BUTTON useHandButton = Input.MOUSE_BUTTON.LEFT;

	[Parameter(Group = "Interaction")] public bool canInteract = true;
	[Parameter(Group = "Interaction")] public Input.KEY interactKey = Input.KEY.F;
	[Parameter(Group = "Interaction")] public bool useSharedInventory = false;
	[Parameter(Group = "Interaction")]
	[ParameterCondition("useSharedInventory", 1)]
	public CPlayer sharedInventory;

	// advanced
	[ParameterMask(Group = "Advanced", MaskType = ParameterMaskAttribute.TYPE.INTERSECTION)]
	public int item_mask = 1 << 2;
	[Parameter(Group = "Advanced")] public float item_selection_angle = 30.0f;
	[Parameter(Group = "Advanced")] public float item_selection_distance = 2.5f;

	int cur_inventory_item = 0;
	CHealth health_info;
	Inventory inventory;
	List<AWeapon> weapons = new List<AWeapon>();
	int cur_weapon = 0;

	public CHealth GetHealthInfo()
	{
		return health_info;
	}

	public bool HasItem(CInteractable item)
	{
		return inventory.FindItem(item) != null;
	}

	protected override void OnEnable()
	{
		Game.Player = camera;
	}

    protected override void OnDisable()
    {
		// disable selection outline
   		if (cur_item != null)
		{
			SetItemOutline(cur_item, false);
			cur_item = null;
		}
    }

	void Init()
	{
		health_info = GetComponentInChildren<CHealth>(node);

		// inventory
		if (useSharedInventory)
			inventory = sharedInventory.inventory;
		else
			inventory = new Inventory();

		// find all weapons
		if (hand != null)
		{
			weapons.AddRange(GetComponentsInChildren<AWeapon>(hand));
			foreach (var w in weapons)
				w.SetOwner(this);
		}
	}
	
	void Update()
	{
		// items
		if (canInteract)
		{
			UpdateItemSelection();
			if (!Console.Active && Input.IsKeyDown(interactKey) && cur_item)
			{
				// try to interact
				if (cur_item.Interact(this))
				{
					// take the item to the inventory
					if (cur_item && cur_item.interact_type == CInteractable.Type.Take)
					{
						Node item_node = cur_item.node;

						// put to inventory
						SetItemOutline(cur_item, false);
						inventory.AddItem(cur_item);
						cur_item = null;

						// weapon case
						AWeapon weapon = GetComponent<AWeapon>(item_node);
						if (weapon != null)
						{
							weapons.Add(weapon);
							weapon.SetOwner(this);
							SetWeapon(weapons.Count - 1);
						}
					}
				}
			}
		}

		// hand/weapons
		if (weapons.Count != 0 && !Console.Active)
		{
			// change weapon (loop)
			int new_weapon = (cur_weapon + Input.MouseWheel) % weapons.Count;
			if (new_weapon < 0) new_weapon += weapons.Count;
			if (new_weapon != cur_weapon)
				SetWeapon(new_weapon);

			// shot / hit
			if (Input.IsMouseButtonPressed(useHandButton))
				weapons[cur_weapon].Use();
		}
	}

	//////////////////////////////////////////////////////////////////////////////////////////////
	// WEAPON
	//////////////////////////////////////////////////////////////////////////////////////////////

	public void SetWeapon(int weapon_index)
	{
		// hide previous weapon
		if (cur_weapon >= 0 && cur_weapon < weapons.Count)
		{
			AWeapon prev_weapon = weapons[cur_weapon];
			prev_weapon.node.Enabled = false;
		}

		// show new weapon
		cur_weapon = weapon_index;
		AWeapon weapon = weapons[cur_weapon];
		weapon.node.Parent = hand;
		weapon.node.Transform = weapon.GetAttachTransform();
		weapon.node.Enabled = true;
	}

	//////////////////////////////////////////////////////////////////////////////////////////////
	// INVENTORY
	//////////////////////////////////////////////////////////////////////////////////////////////

	#region Inventory

	public class Inventory
	{
		List<CInteractable> items = new List<CInteractable>();

		public void AddItem(CInteractable item)
		{
			// move the item to the inventory
			// (unparent, hide and reset transformation)
			item.node.Parent = null;
			item.node.Enabled = false;
			item.node.Position = new vec3(0,0,0);
			item.node.SetRotation(new quat(0,0,0));
			items.Add(item);
		}

		public int GetNumItems()
		{
			return items.Count;
		}

		public CInteractable GetItem(int index)
		{
			if (index < 0 || index >= items.Count)
				return null;

			return items[index];
		}

		public CInteractable FindItem(string item_name)
		{
			return items.Find(i => i.name == item_name);
		}

		public CInteractable FindItem(CInteractable item)
		{
			return items.Find(i => i == item);
		}

		public bool RemoveItem(CInteractable item)
		{
			return items.Remove(item);
		}

	}

	#endregion Inventory

	//////////////////////////////////////////////////////////////////////////////////////////////
	// INTERACTION
	//////////////////////////////////////////////////////////////////////////////////////////////

	#region Interaction

	CInteractable cur_item;

	public CInteractable GetCurrentInventoryItem()
	{
		return inventory.GetItem(cur_inventory_item);
	}

	public CInteractable GetCurrentSelectedItem()
	{
		return cur_item;
	}

	void UpdateItemSelection()
	{
		List<Node> nodes = new List<Node>();
		if (World.GetIntersection(new BoundSphere(camera.WorldPosition, item_selection_distance), Node.TYPE.OBJECT_MESH_STATIC, nodes))
		{
			foreach (Node n in nodes)
			{
				// exclude non-item intersection masks
				Object o = n as Object;
				if (o.NumSurfaces > 0 && (o.GetIntersectionMask(0) & item_mask) == 0)
					continue;

				// exclude non-items
				CInteractable item = GetComponentInParent<CInteractable>(n);
				if (item == null)
					continue;

				// is it visible?
				if (IsItemVisible(item))
				{
					// nothing changed, we are still
					// looking at this item
					if (cur_item == item)
						return;

					// disable outline to previous item
					if (cur_item != null)
						SetItemOutline(cur_item, false);
					
					// enable outline to this item
					cur_item = item;
					SetItemOutline(item, true);
					return;
				}
			}
		}

		// there is no items close to the player, so
		// disable outline from previous selected item!
		if (cur_item != null)
		{
			SetItemOutline(cur_item, false);
			cur_item = null;
		}
	}

	bool IsItemVisible(CInteractable item)
	{
		vec3 offset = item.node.WorldBoundSphere.Center - camera.WorldPosition;
		if (MathLib.Length(offset) > item_selection_distance)
			return false;

		vec3 cam_dir = camera.GetWorldDirection(MathLib.AXIS.NZ);
		if (MathLib.Angle(cam_dir, offset) > item_selection_angle)
			return false;

		return true;
	}

	void SetItemOutline(CInteractable item, bool enabled)
	{
		// set outline custom post material effect
		Render.SetScriptableMaterialEnabled(0, enabled);

		List<Node> nodes = new List<Node>();
		item.node.GetHierarchy(nodes);
		for (int i = 0; i < nodes.Count; i++)
		{
			Node n = nodes[i];
			if (n.Type != Node.TYPE.OBJECT_MESH_STATIC &&
				n.Type != Node.TYPE.OBJECT_MESH_SKINNED)
				continue;
			Object o = n as Object;
			for (int j = 0; j < o.NumSurfaces; j++)
			{
				o.SetMaterialState("auxiliary", enabled ? 1 : 0, j);
			}
		}
	}

	#endregion Interaction
}