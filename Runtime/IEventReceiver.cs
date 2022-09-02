namespace AnnoEventBus
{
	public interface IEventReceiverBase
	{
	}

	public interface IEventReceiver<in T> : IEventReceiverBase where T : struct, IEvent
	{
		void OnEvent(T e);
	}
}