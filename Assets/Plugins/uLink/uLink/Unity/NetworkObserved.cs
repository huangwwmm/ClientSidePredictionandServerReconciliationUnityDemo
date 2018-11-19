#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8997 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-09-02 16:43:28 +0200 (Fri, 02 Sep 2011) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
#if UNITY_BUILD

using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace uLink
{
	/// <summary>
	/// Helper class for caching and binding. 
	/// </summary>
	/// <remarks>This class is used internally in uLink, but it is made public to be used in some custom cases.</remarks>
	public struct NetworkObserved
	{
		public const string EVENT_SERIALIZE_PROXY = NetworkUnity.EVENT_PREFIX + "OnSerializeNetworkView";
		public const string EVENT_SERIALIZE_OWNER = NetworkUnity.EVENT_PREFIX + "OnSerializeNetworkViewOwner";
		public const string EVENT_SERIALIZE_HANDOVER = NetworkUnity.EVENT_PREFIX + "OnHandoverNetworkView";
		public const string EVENT_SERIALIZE_CELLPROXY = NetworkUnity.EVENT_PREFIX + "OnSerializeNetworkViewCellProxy";

		private const float ANIMATION_TIME_MAXERROR = 0.2f;
		private const float RIGIDBODY_POS_MAXSQRERROR = 0.2f;
		private const float RIGIDBODY_ROT_MAXERROR = 1f;
		private const float RIGIDBODY_VEL_MAXSQRERROR = 0.2f;

		private static readonly Dictionary<NetworkTypeHandle, Binder> _binders = new Dictionary<NetworkTypeHandle, Binder>();
		private static readonly Type[] SERIALIZER_TYPES = new[] { typeof(BitStream), typeof(NetworkMessageInfo) };
		private const BindingFlags SERIALIZER_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod;

		/// <summary>
		/// Has the responsibility to assign each serializer member of NetworkObserved to an appropriate delegate.
		/// </summary>
		public delegate void Binder(ref NetworkObserved observed);
		/// <summary>
		/// Callback that gets called to serialize a specific state. 
		/// </summary>
		/// <remarks>For example uLink_OnSerializeNetworkView, uLink_OnSerializeNetworkViewOwner, uLink_OnHandoverNetworkView.</remarks>
		public delegate void Serializer(BitStream stream, NetworkMessageInfo info);

		/// <summary>
		/// Used when no custom binder has been set.
		/// </summary>
		public static Binder defaultBinder = DefaultBinder;

		/// <summary>
		/// The component that this observer is observing for serializaton.
		/// </summary>
		public Component component;
		/// <summary>
		/// Serializer used when serializing proxies.
		/// this is used both for owners and proxies when <see cref="uLink.Network.useDifferentStateSyncForOwner"/> is not set.
		/// </summary>
		public Serializer serializeProxy;
		/// <summary>
		/// Serialization method used for serializing the state for owner when the <see cref="uLink.Network.useDifferentStateForOwner"/> is set.
		/// </summary>
		public Serializer serializeOwner;
		/// <summary>
		/// Serialization method used for serialization when handing an object over.
		/// </summary>
		public Serializer serializeHandover;
		/// <summary>
		/// Serializer method used in Pikko server for serializing between auth cell and proxies for an object.
		/// </summary>
		public Serializer serializeCellProxy;

		static NetworkObserved()
		{
			AddBinder<Transform>(_BindTransform);
			AddBinder<Animation>(_BindAnimation);
			AddBinder<Rigidbody>(_BindRigidbody);
		}

		/// <summary>
		/// Use this constructor. Send your component as argument.
		/// </summary>
		/// <param name="component">The component.</param>
		public NetworkObserved(Component observedComponent)
		{
			component = null;
			serializeProxy = null;
			serializeOwner = null;
			serializeHandover = null;
			serializeCellProxy = null;

			UpdateBinding(observedComponent);
		}

		/// <summary>
		/// Call this if you added a new custom binder for the component which you want to be used from now on.
		/// </summary>
		/// <param name="observedComponent"></param>
		public void UpdateBinding(Component observedComponent)
		{
			if (observedComponent.IsNullOrDestroyed())
			{
				component = null;
				serializeProxy = null;
				serializeOwner = null;
				serializeHandover = null;
				serializeCellProxy = null;
			}
			else if (!component.ReferenceEquals(observedComponent))
			{
				component = observedComponent;

				Binder binder;
				if (_binders.TryGetValue(Type.GetTypeHandle(observedComponent), out binder))
				{
					binder(ref this);
				}
				else
				{
					defaultBinder(ref this);
				}
			}
		}

		/// <summary>
		/// Adds a binder for the type <c>T</c>.
		/// </summary>
		/// <typeparam name="T">The type that we want to add a binder for.</typeparam>
		/// <param name="binder">The binder method for serializing and deserializing the type.</param>
		public static void AddBinder<T>(Binder binder) where T : Component
		{
			AddBinder(typeof(T).TypeHandle, binder);
		}

		/// <summary>
		/// Adds a binder or the type specified by its runtime handle.
		/// </summary>
		/// <param name="typeHandle"></param>
		/// <param name="binder"></param>
		public static void AddBinder(RuntimeTypeHandle typeHandle, Binder binder)
		{
			AddBinder(typeHandle, binder, false);
		}

		/// <summary>
		/// Adds a binder for the type specified.
		/// </summary>
		/// <param name="typeHandle"></param>
		/// <param name="binder"></param>
		/// <param name="replaceIfExists">Indicates if it should replace the custom binder if one already exists or not.</param>
		public static void AddBinder(RuntimeTypeHandle typeHandle, Binder binder, bool replaceIfExists)
		{
			// TODO: catch exception from Add instead of pre-checking with ContainsKey

			if (!replaceIfExists && _binders.ContainsKey(typeHandle))
			{
				Utility.Exception("Can't add Binder because it already exists for type ", typeHandle);
			}

			_binders[typeHandle] = binder;
		}

		/// <summary>
		/// Removes the custom binder for a type.
		/// </summary>
		/// <param name="typeHandle"></param>
		public static void RemoveBinder(RuntimeTypeHandle typeHandle)
		{
			_binders.Remove(typeHandle);
		}


		public static void DefaultBinder(ref NetworkObserved observed)
		{
			Type type = observed.component.GetType();

			observed.serializeProxy = CreateSerializer(observed.component, type, EVENT_SERIALIZE_PROXY);
			observed.serializeOwner = CreateSerializer(observed.component, type, EVENT_SERIALIZE_OWNER);
			observed.serializeHandover = CreateSerializer(observed.component, type, EVENT_SERIALIZE_HANDOVER);
			observed.serializeCellProxy = CreateSerializer(observed.component, type, EVENT_SERIALIZE_CELLPROXY);
		}

		/// <summary>
		/// This is a help function for creating a custom binder.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <param name="type">The type.</param>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		public static Serializer CreateSerializer(object obj, Type type, string name)
		{
			var method = type.GetMethod(name, SERIALIZER_FLAGS, null, SERIALIZER_TYPES, null);
			if (method == null)
			{
				Log.Debug(NetworkLogFlags.Observed, "No method named '", name, "' was found in ", type);
				return null;
			}

			try
			{
				return Delegate.CreateDelegate(typeof(Serializer), obj, method) as Serializer;
			}
			catch (ArgumentException e)
			{
				Log.Debug(NetworkLogFlags.Observed, "Failed to create delegate of method ", method, " with error: ", e);
				return null;
			}
		}

		private static void _BindTransform(ref NetworkObserved observed)
		{
			var transform = (Transform)observed.component;

			Serializer serializer = delegate(BitStream stream, NetworkMessageInfo info)
			{
				if (stream.isReading)
				{
					transform.localPosition = stream.ReadVector3();
					transform.localRotation = stream.ReadQuaternion();
					transform.localScale = stream.ReadVector3();
				}
				else
				{
					stream.WriteVector3(transform.localPosition);
					stream.WriteQuaternion(transform.localRotation);
					stream.WriteVector3(transform.localScale);
				}
			};

			observed.serializeProxy = serializer;
			observed.serializeOwner = serializer;
			observed.serializeHandover = serializer;
			observed.serializeCellProxy = serializer;
		}

		private static void _BindRigidbody(ref NetworkObserved observed)
		{
			var rigidbody = (Rigidbody)observed.component;

			Serializer serializer = delegate(BitStream stream, NetworkMessageInfo info)
			{
				if (stream.isReading)
				{
					float transitTime = (float)(Network.time - info.timestamp);

					Vector3 newPos = stream.ReadVector3();
					Quaternion newRot = stream.ReadQuaternion();
					Vector3 newVel = stream.ReadVector3();
					Vector3 angVel = stream.ReadVector3();

					newRot.eulerAngles += angVel * transitTime;
					rigidbody.angularVelocity = angVel;

					Vector3 gravity = rigidbody.useGravity ? Physics.gravity : Vector3.zero;
					newPos += newVel * transitTime + gravity * transitTime * transitTime * 0.5f;
					newVel += gravity * transitTime;

					if ((rigidbody.position - newPos).sqrMagnitude >= RIGIDBODY_POS_MAXSQRERROR)
					{
						rigidbody.position = newPos;
					}

					if ((rigidbody.velocity - newVel).sqrMagnitude >= RIGIDBODY_VEL_MAXSQRERROR)
					{
						rigidbody.velocity = newVel;
					}

					if (Quaternion.Angle(rigidbody.rotation, newRot) >= RIGIDBODY_ROT_MAXERROR)
					{
						rigidbody.rotation = newRot;
					}
				}
				else
				{
					stream.WriteVector3(rigidbody.position);
					stream.WriteQuaternion(rigidbody.rotation);
					stream.WriteVector3(rigidbody.velocity);
					stream.WriteVector3(rigidbody.angularVelocity);
				}
			};

			observed.serializeProxy = serializer;
			observed.serializeOwner = serializer;
			observed.serializeHandover = serializer;
			observed.serializeCellProxy = serializer;
		}

		private static void _BindAnimation(ref NetworkObserved observed)
		{
			var animation = (Animation)observed.component;

			Serializer serializer = delegate(BitStream stream, NetworkMessageInfo info)
			{
				if (stream.isReading)
				{
					float transitTime = (float) (Network.time - info.timestamp);

					foreach(AnimationState state in animation)
					{
						state.enabled = stream.ReadBoolean();
						float newTime = stream.ReadSingle() + transitTime;
						state.speed = stream.ReadSingle();
						state.weight = stream.ReadSingle();

						if (Mathf.Abs(state.time - newTime) >= ANIMATION_TIME_MAXERROR)
						{
							state.time = newTime;
						}
					}
				}
				else
				{
					foreach (AnimationState state in animation)
					{
						stream.WriteBoolean(state.enabled);
						stream.WriteSingle(state.time);
						stream.WriteSingle(state.speed);
						stream.WriteSingle(state.weight);
					}
				}
			};

			observed.serializeProxy = serializer;
			observed.serializeOwner = serializer;
			observed.serializeHandover = serializer;
			observed.serializeCellProxy = serializer;
		}
	}
}

#endif
