using pEventBus;
using UnityEngine;

public struct EventFromAssembly2 : IEvent
{
	public string a;
	public float b;
}

public class Assembly2Test : MonoBehaviour, IEventReceiver<EventFromAssembly1>, IEventReceiver<EventFromAssembly2>
{
	void Start()
	{
		EventBus.Register(this);
	}

	void OnDestroy()
	{
		EventBus.Unregister(this);
	}

	void OnGUI()
	{
		if (GUI.Button(new Rect(250, 0, 100, 30), "Raise Event1"))
		{
			EventBus<EventFromAssembly1>.Raise(new EventFromAssembly1()
			{
				b = 7,
				a = "Hello from Assembly 2"
			});
		}
		if (GUI.Button(new Rect(250, 35, 100, 30), "Raise Event2"))
		{
			EventBus<EventFromAssembly2>.Raise(new EventFromAssembly2()
			{
				b = 7,
				a = "Hello from Assembly 2"
			});
		}
	}

	public void OnEvent(EventFromAssembly1 e)
	{
		print($"Assembly2Test got EventFromAssembly1: {e.a}");
	}

	public void OnEvent(EventFromAssembly2 e)
	{
		print($"Assembly2Test got EventFromAssembly2: {e.a}");
	}
}