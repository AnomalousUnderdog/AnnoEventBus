using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnnoEventBus
{
	/// <summary>
	/// Main entry point for objects to register and unregister from events.
	/// </summary>
	public static class EventBus
	{
		/// <summary>
		/// Allows retrieving all the needed <see cref="EventBus{T}"/> Register and Unregister methods
		/// that a given type is interested in.
		/// </summary>
		static readonly Dictionary<Type, ClassMap> ClassRegisterMap = new();

		/// <summary>
		/// Reference to the <see cref="EventBus{T}.RaiseAsInterface"/> method per <see cref="IEvent"/>.
		/// </summary>
		static readonly Dictionary<Type, Action<IEvent>> CachedRaise = new();

		/// <summary>
		/// Holds reference to the Register and Unregister methods of a particular <see cref="EventBus{T}"/>.
		/// </summary>
		class BusMap
		{
			/// <summary>
			/// Reference to the <see cref="EventBus{T}.Register"/> method.
			/// </summary>
			public Action<IEventReceiverBase> RegisterAction;

			/// <summary>
			/// Reference to the <see cref="EventBus{T}.Unregister"/> method.
			/// </summary>
			public Action<IEventReceiverBase> UnregisterAction;
		}

		/// <summary>
		/// Holds all <see cref="BusMap"/> (reference to the <see cref="EventBus{T}"/> Register and Unregister methods)
		/// that a particular class is interested in.
		/// </summary>
		class ClassMap
		{
			public BusMap[] Buses;
		}

		static EventBus()
		{
			var busRegisterMap = new Dictionary<Type, BusMap>();

			Type delegateType = typeof(Action<>);
			Type delegateGenericRegister = delegateType.MakeGenericType(typeof(IEventReceiverBase));
			Type delegateGenericRaise = delegateType.MakeGenericType(typeof(IEvent));

			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			Type eventHubType = typeof(EventBus<>);

			const string GENERIC_EVENT_BUS_REGISTER_METHOD_NAME = "Register";
			const string GENERIC_EVENT_BUS_UNREGISTER_METHOD_NAME = "Unregister";
			const string GENERIC_EVENT_BUS_RAISE_METHOD_NAME = "RaiseAsInterface";

			Debug.Assert(eventHubType.GetMethod(GENERIC_EVENT_BUS_REGISTER_METHOD_NAME) != null,
				"pEventBus.EventBus<T> needs to have a method: public static void Register(IEventReceiverBase handler)");
			Debug.Assert(eventHubType.GetMethod(GENERIC_EVENT_BUS_UNREGISTER_METHOD_NAME) != null,
				"pEventBus.EventBus<T> needs to have a method: public static void Unregister(IEventReceiverBase handler)");
			Debug.Assert(eventHubType.GetMethod(GENERIC_EVENT_BUS_RAISE_METHOD_NAME) != null,
				"pEventBus.EventBus<T> needs to have a method: public static void RaiseAsInterface(IEvent e)");

			for (int a = 0, aLen = assemblies.Length; a < aLen; ++a)
			{
				var types = assemblies[a].GetTypes();
				foreach (var t in types)
				{
					// go through all that implement IEvent
					if (t != typeof(IEvent) && typeof(IEvent).IsAssignableFrom(t))
					{
						// create an EventBus<> for this IEvent
						Type genMyClass = eventHubType.MakeGenericType(t);
						System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(genMyClass.TypeHandle);

						// keep a reference to the Register/Unregister methods of the created EventBus<>
						var registerMethod = genMyClass.GetMethod(GENERIC_EVENT_BUS_REGISTER_METHOD_NAME);
						var unregisterMethod = genMyClass.GetMethod(GENERIC_EVENT_BUS_UNREGISTER_METHOD_NAME);
						var raiseMethod = genMyClass.GetMethod(GENERIC_EVENT_BUS_RAISE_METHOD_NAME);

						BusMap busMap = new BusMap()
						{
							RegisterAction =
								Delegate.CreateDelegate(delegateGenericRegister, registerMethod) as Action<IEventReceiverBase>,
							UnregisterAction =
								Delegate.CreateDelegate(delegateGenericRegister, unregisterMethod) as Action<IEventReceiverBase>
						};

						busRegisterMap.Add(t, busMap);

						CachedRaise.Add(t, (Action<IEvent>) Delegate.CreateDelegate(delegateGenericRaise, raiseMethod));
					}
				}
			}

			for (int a = 0, aLen = assemblies.Length; a < aLen; ++a)
			{
				var types = assemblies[a].GetTypes();
				foreach (var t in types)
				{
					// go through all that implement IEventReceiverBase
					if (typeof(IEventReceiverBase).IsAssignableFrom(t) && !t.IsInterface)
					{
						// get all the IEventReceiver that this thing implements
						Type[] interfaces = t.GetInterfaces().Where(x =>
							x != typeof(IEventReceiverBase) && typeof(IEventReceiverBase).IsAssignableFrom(x)).ToArray();

						ClassMap map = new ClassMap()
						{
							Buses = new BusMap[interfaces.Length]
						};

						for (int i = 0; i < interfaces.Length; i++)
						{
							// we get the exact IEvent that was specified in each implemented IEventReceiver using GetGenericArguments
							var arg = interfaces[i].GetGenericArguments()[0];

							// we keep a reference to the Register/Unregister methods for the EventBus<> of that specific IEvent
							map.Buses[i] = busRegisterMap[arg];
						}

						ClassRegisterMap.Add(t, map);
					}
				}
			}
		}

		/// <summary>
		/// Call this to be subscribed to events that your object declares
		/// to be interested in from the IEventReceiver interfaces it implements.
		/// A good time to call this is during initialization.
		/// </summary>
		/// <param name="target">The object that wants to be subscribed.</param>
		public static void Register(IEventReceiverBase target)
		{
			Type t = target.GetType();
			ClassMap map = ClassRegisterMap[t];

			foreach (var busMap in map.Buses)
			{
				busMap.RegisterAction(target);
			}
		}

		/// <summary>
		/// Call this to be unsubscribed to events that your object was formerly subscribed to in <see cref="Register"/>.
		/// A good time to call this is when the object is about to be destroyed.
		/// </summary>
		/// <param name="target">The object that wants to be unsubscribed of events.</param>
		public static void Unregister(IEventReceiverBase target)
		{
			Type t = target.GetType();
			ClassMap map = ClassRegisterMap[t];

			foreach (var busMap in map.Buses)
			{
				busMap.UnregisterAction(target);
			}
		}

		/// <summary>
		/// Raise/publish an event. Use this if you only have a reference to the <see cref="IEvent"/> and don't know the concrete type.
		/// </summary>
		/// <param name="ev">The particular event to be raised.</param>
		public static void Raise(IEvent ev)
		{
			CachedRaise[ev.GetType()](ev);
		}
	}
}