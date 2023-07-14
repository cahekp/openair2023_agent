using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "8f2594a6fec7a3cdb6daf2c877d0f787c0da4f3a")]
public class CKeyPressed : Component
{
	public Input.KEY key = Input.KEY.F;
	public List<CEventHandler> onPressed = new List<CEventHandler>();
	
	int awake_frame;

    protected override void OnEnable()
    {
        awake_frame = Game.Frame;
    }

	void Update()
	{
		// workaround for creating switches
		// (so that keyboard triggers do not fire
		// immediately after enable the nodes)
		if (awake_frame == Game.Frame)
			return;

		if (!Console.Active && Input.IsKeyDown(key))
		{
			// notify subscribers
			foreach (var receiver in onPressed)
				receiver.OnReceiveEvent(this);
		}
	}
}