using AnnoEventBus;
using UnityEngine;

public struct EventFromAssembly1 : IEvent
{
	public string a;
	public float b;
}

public class Assembly1Test : MonoBehaviour, IEventReceiver<EventFromAssembly1>
{
	public int PerFrame = 1000;
	public bool TestPerformance;

	void Start()
	{
		EventBus.Register(this);
	}

	void OnDestroy()
	{
		EventBus.Unregister(this);
	}

	void Update()
	{
		if (TestPerformance)
		{
			for (int i = 0; i < PerFrame; i++)
			{
				EventBus<EventFromAssembly1>.Raise(new EventFromAssembly1()
				{
					b = 7,
					a = "Hello from Assembly 1"
				});
			}
		}
	}

	void OnGUI()
	{
		if (GUI.Button(new Rect(0, 0, 100, 30), "Raise Event1"))
		{
			EventBus<EventFromAssembly1>.Raise(new EventFromAssembly1()
			{
				b = 7,
				a = "Hello from Assembly 1"
			});
		}
	}

	public void OnEvent(EventFromAssembly1 e)
	{
		print($"Assembly1Test got EventFromAssembly1: {e.a}");
	}
}