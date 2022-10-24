using System.Collections.Generic;

namespace AnnoEventBus
{
	/// <summary>
	/// Handler for one particular <see cref="IEvent"/> implementation.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public static class EventBus<T> where T : struct, IEvent
	{
		/// <summary>
		/// Loopable collection of the subscriber list,
		/// faster to iterate over compared to traversing the HashSet.
		/// </summary>
		static IEventReceiver<T>[] _buffer;

		/// <summary>
		/// Number of subscribers to this event type.
		/// We use this instead of HashSet.Count.
		/// </summary>
		static int _count;

		/// <summary>
		/// How much to increase the size of our <see cref="_buffer"/> subscriber list once it's not big enough.
		/// </summary>
		const int BlockSize = 256;

		/// <summary>
		/// Collection of the subscriber list that makes adding and removing fast.
		/// </summary>
		static readonly HashSet<IEventReceiver<T>> Hash;

		static EventBus()
		{
			Hash = new HashSet<IEventReceiver<T>>();
			_buffer = new IEventReceiver<T>[0];
		}

		/// <summary>
		/// Register an event receiver to this event type.
		/// Used by <see cref="EventBus"/> to automatically register subscribers to this event type.
		/// </summary>
		/// <param name="handler">The object that wants to be subscribed to the events.</param>
		public static void Register(IEventReceiverBase handler)
		{
			_count++;
			Hash.Add(handler as IEventReceiver<T>);
			if (_buffer.Length < _count)
			{
				_buffer = new IEventReceiver<T>[_count + BlockSize];
			}

			Hash.CopyTo(_buffer);
		}

		/// <summary>
		/// Unregister an event receiver from this event type.
		/// Used by <see cref="EventBus"/> to automatically unregister subscribers from this event type.
		/// </summary>
		/// <param name="handler">The object that wants to be unsubscribed from the events.</param>
		public static void Unregister(IEventReceiverBase handler)
		{
			Hash.Remove(handler as IEventReceiver<T>);
			Hash.CopyTo(_buffer);
			_count--;
		}

		/// <summary>
		/// Raise/publish an event.
		/// Use this if you know the concrete type of the event.
		/// </summary>
		/// <param name="e">The particular event to be raised.</param>
		public static void Raise(T e = default)
		{
			for (int i = 0; i < _count; i++)
			{
				_buffer[i].OnEvent(e);
			}
		}

		/// <summary>
		/// Raise/publish an event.
		/// Use this if you only have a reference to the <see cref="IEvent"/> and can't refer to the concrete type.
		/// The <see cref="IEvent"/> passed should be the type that this EventBus is for.
		/// </summary>
		/// <param name="e">The particular event to be raised.</param>
		public static void RaiseAsInterface(IEvent e)
		{
			Raise((T) e);
		}

		/// <summary>
		/// Remove all subscribers to this event type.
		/// </summary>
		public static void Clear()
		{
			Hash.Clear();
		}
	}
}