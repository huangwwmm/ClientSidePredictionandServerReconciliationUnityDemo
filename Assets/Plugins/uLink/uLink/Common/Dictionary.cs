
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Reflection;
namespace uLink
{
	internal class Dictionary<TKey, TValue> : Dictionary<TKey, TValue, EqualityComparer<TKey>>
		where TKey : IEquatable<TKey>
	{
		public Dictionary() { }
		public Dictionary(int capacity) : base(capacity) { }
		public Dictionary(ICollection<KeyValuePair<TKey, TValue>> collection) : base(collection) { }
	}

	internal class Dictionary<TKey, TValue, TComparer> : IDictionary<TKey, TValue>
		where TComparer : struct, IEqualityComparer<TKey>
	{
		private struct Entry
		{ 
			public int hashCode;    // Lower 31 bits of hash code, -1 if unused 
			public int next;        // Index of next entry, -1 if last
			public KeyValuePair<TKey, TValue> pair;        // Key & Value of entry
		}

		private int[] buckets;
		private Entry[] entries;
		private int count;
		private int freeList;
		private int freeCount;

		//by WuNan @2016/09/13 15:33:00 用于避免bool Equals(TKey a, TKey b)中产生的GC
		private static TComparer s_comparer = new TComparer();

		public Dictionary(): this(0) {}

		public Dictionary(int capacity)
		{ 
			Initialize(capacity);
		} 

		public Dictionary(ICollection<KeyValuePair<TKey, TValue>> collection):
			this(collection.Count)
		{
			foreach (var pair in collection)
			{ 
				Add(pair); 
			}
		}

		public int Count
		{ 
			get { return count - freeCount; } 
		}
 
		public KeyCollection Keys
		{
			get { return new KeyCollection(this); }
		} 

		ICollection<TKey> IDictionary<TKey, TValue>.Keys
		{
			get { return Keys; }
		}
 
		public ValueCollection Values
		{
			get { return new ValueCollection(this); }
		}

		ICollection<TValue> IDictionary<TKey, TValue>.Values
		{
			get { return Values; }
		}

		public TValue this[TKey key]
		{
			get { return GetOrAddDefault(key); }
			set { Add(key, value); } 
		}

		public bool Contains(KeyValuePair<TKey, TValue> pair)
		{
			int i = Find(pair.Key);
			return (i >= 0 && entries[i].pair.Value.Equals(pair.Value));
		}

		public bool Remove(KeyValuePair<TKey, TValue> pair)
		{
			int i = Find(pair.Key);
			if (i >= 0 && entries[i].pair.Value.Equals(pair.Value))
			{
				Remove(pair.Key); 
				return true;
			}

			return false; 
		}
 
		public void Clear()
		{
			if (count > 0)
			{
				for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
				Array.Clear(entries, 0, count); 
				freeList = -1;
				count = 0; 
				freeCount = 0;
			} 
		}

		public bool ContainsKey(TKey key)
		{
			return Find(key) >= 0; 
		}
 
		public bool ContainsValue(TValue value)
		{ 
			for (int i = 0; i < count; i++)
			{
				if (entries[i].hashCode >= 0 && entries[i].pair.Value.Equals(value)) return true; 
			}

			return false;
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
		{
			for (int i = 0; i < count; i++)
			{
				if (entries[i].hashCode >= 0)
				{
					array[index++] = entries[i].pair;
				} 
			}
		} 
 
		public Enumerator GetEnumerator()
		{
			return new Enumerator(this); 
		}

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			return new Enumerator(this); 
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(this);
		}
 
		private int Find(TKey key)
		{
			if (buckets != null)
			{
				int hashCode = key.GetHashCode() & 0x7FFFFFFF;
				for (int i = buckets[hashCode % buckets.Length]; i >= 0; i = entries[i].next)
				{
					if (Equals(entries[i], hashCode, key)) return i;
				}
			}

			return -1;
		} 

		private void Initialize(int capacity)
		{
			int size = HashHelper.GetPrime(capacity); 
			buckets = new int[size];
			for (int i = 0; i < buckets.Length; i++) buckets[i] = -1; 
			entries = new Entry[size];
			freeList = -1;
		}

		public void Add(TKey key, TValue value)
		{
			Add(new KeyValuePair<TKey, TValue>(key, value));
		}

		public void Add(KeyValuePair<TKey, TValue> pair)
		{
			if (buckets == null) Initialize(0);
			int hashCode = pair.Key.GetHashCode() & 0x7FFFFFFF;
			int targetBucket = hashCode % buckets.Length;

			for (int i = buckets[targetBucket]; i >= 0; i = entries[i].next)
			{
				if (Equals(entries[i], hashCode, pair.Key))
				{
					entries[i].pair = pair;
					return;
				}
			}

			int index; 
			if (freeCount > 0) { 
				index = freeList;
				freeList = entries[index].next; 
				freeCount--;
			}
			else {
				if (count == entries.Length) 
				{
					Resize(); 
					targetBucket = hashCode % buckets.Length; 
				}
				index = count; 
				count++;
			}

			entries[index].hashCode = hashCode; 
			entries[index].next = buckets[targetBucket];
			entries[index].pair = pair;
			buckets[targetBucket] = index;
		}

		public TValue GetOrAddDefault(TKey key)
		{
			return GetOrAddDefault(new KeyValuePair<TKey, TValue>(key, default(TValue)));
		}

		public TValue GetOrAddDefault(KeyValuePair<TKey, TValue> pair)
		{
			if (buckets == null) Initialize(0);
			int hashCode = pair.Key.GetHashCode() & 0x7FFFFFFF;
			int targetBucket = hashCode % buckets.Length;

			for (int i = buckets[targetBucket]; i >= 0; i = entries[i].next)
			{
				if (Equals(entries[i], hashCode, pair.Key))
				{
					return pair.Value;
				}
			}

			int index;
			if (freeCount > 0)
			{
				index = freeList;
				freeList = entries[index].next;
				freeCount--;
			}
			else
			{
				if (count == entries.Length)
				{
					Resize();
					targetBucket = hashCode % buckets.Length;
				}
				index = count;
				count++;
			}

			entries[index].hashCode = hashCode;
			entries[index].next = buckets[targetBucket];
			entries[index].pair = pair;
			buckets[targetBucket] = index;

			return pair.Value;
		}

		public void Resize()
		{
			Resize(HashHelper.GetPrime(count * 2));
		}

		public void Resize(int newSize)
		{ 
			var newBuckets = new int[newSize];
			for (int i = 0; i < newBuckets.Length; i++) newBuckets[i] = -1; 
			var newEntries = new Entry[newSize];
			Array.Copy(entries, 0, newEntries, 0, count);

			for (int i = 0; i < count; i++)
			{
				int bucket = newEntries[i].hashCode % newSize;
				newEntries[i].next = newBuckets[bucket];
				newBuckets[bucket] = i; 
			}
			buckets = newBuckets; 
			entries = newEntries; 
		}
 
		public bool Remove(TKey key)
		{
			if (buckets != null)
			{
				int hashCode = key.GetHashCode() & 0x7FFFFFFF; 
				int bucket = hashCode % buckets.Length;
				int last = -1; 
				for (int i = buckets[bucket]; i >= 0; last = i, i = entries[i].next)
				{
					if (Equals(entries[i], hashCode, key))
					{
						if (last < 0)
						{
							buckets[bucket] = entries[i].next; 
						}
						else
						{ 
							entries[last].next = entries[i].next; 
						}
						entries[i].hashCode = -1;
						entries[i].next = freeList;
						entries[i].pair = new KeyValuePair<TKey, TValue>();
						freeList = i;
						freeCount++;
						return true; 
					}
				} 
			}

			return false;
		}
 
		public bool TryGetValue(TKey key, out TValue value)
		{
			int i = Find(key);
			if (i >= 0)
			{ 
				value = entries[i].pair.Value;
				return true; 
			}

			value = default(TValue);
			return false;
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
		{
			get { return false; }
		}

		public struct Enumerator: IEnumerator<KeyValuePair<TKey, TValue>> 
		{
			private readonly Dictionary<TKey, TValue, TComparer> dictionary;

			private int index;
			private KeyValuePair<TKey, TValue> current;
 
			internal Enumerator(Dictionary<TKey, TValue, TComparer> dictionary)
			{
				this.dictionary = dictionary;
				index = 0;
				current = new KeyValuePair<TKey, TValue>(); 
			}
 
			public bool MoveNext()
			{
				// Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
				// dictionary.count+1 could be negative if dictionary.count is Int32.MaxValue 
				while ((uint)index < (uint)dictionary.count)
				{
					if (dictionary.entries[index].hashCode >= 0)
					{
						current = dictionary.entries[index].pair; 
						index++;
						return true; 
					}
					index++;
				}
 
				index = dictionary.count + 1;
				current = new KeyValuePair<TKey, TValue>(); 
				return false; 
			}
 
			public KeyValuePair<TKey, TValue> Current
			{
				get { return current; }
			}
 
			public void Dispose() { }

			object IEnumerator.Current
			{
				get { return current; }
			}

			void IEnumerator.Reset()
			{
				index = 0; 
				current = new KeyValuePair<TKey, TValue>();
			}
		}
		
		public struct KeyCollection: ICollection<TKey>
		{
			private readonly Dictionary<TKey, TValue, TComparer> dictionary; 

			public KeyCollection(Dictionary<TKey, TValue, TComparer> dictionary)
			{
				this.dictionary = dictionary;
			}

			public KeyEnumerator GetEnumerator()
			{ 
				return new KeyEnumerator(dictionary);
			} 
 
			public void CopyTo(TKey[] array, int index)
			{
				int count = dictionary.count;
				Entry[] entries = dictionary.entries;
				
				for (int i = 0; i < count; i++) {
					if (entries[i].hashCode >= 0) array[index++] = entries[i].pair.Key; 
				} 
			}
 
			public int Count
			{
				get { return dictionary.Count; }
			}
 
			bool ICollection<TKey>.IsReadOnly
			{
				get { return true; } 
			} 

			void ICollection<TKey>.Add(TKey item)
			{
				throw new NotSupportedException();
			}

			void ICollection<TKey>.Clear()
			{
				throw new NotSupportedException();
			} 
 
			bool ICollection<TKey>.Contains(TKey item)
			{
				return dictionary.ContainsKey(item); 
			}

			bool ICollection<TKey>.Remove(TKey item)
			{
				throw new NotSupportedException();
			} 
 
			IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
			{
				return new KeyEnumerator(dictionary); 
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return new KeyEnumerator(dictionary); 
			}
			
			public struct KeyEnumerator : IEnumerator<TKey> 
			{ 
				private readonly Dictionary<TKey, TValue, TComparer> dictionary;
				private int index; 
				private TKey currentKey;

				internal KeyEnumerator(Dictionary<TKey, TValue, TComparer> dictionary)
				{ 
					this.dictionary = dictionary;
					index = 0; 
					currentKey = default(TKey);
				} 

				public void Dispose() { }
 
				public bool MoveNext()
				{ 
					while ((uint)index < (uint)dictionary.count)
					{
						if (dictionary.entries[index].hashCode >= 0)
						{
							currentKey = dictionary.entries[index].pair.Key;
							index++; 
							return true;
						} 
						index++; 
					}
 
					index = dictionary.count + 1;
					currentKey = default(TKey);
					return false;
				} 

				public TKey Current
				{ 
					get { return currentKey; }
				}

				object IEnumerator.Current
				{
					get { return currentKey; }
				}

				void IEnumerator.Reset()
				{
					index = 0;
					currentKey = default(TKey);
				}
			}
		} 

		public struct ValueCollection: ICollection<TValue>
		{
			private readonly Dictionary<TKey, TValue, TComparer> dictionary;

			public ValueCollection(Dictionary<TKey, TValue, TComparer> dictionary)
			{
				this.dictionary = dictionary;
			} 

			public ValueEnumerator GetEnumerator()
			{
				return new ValueEnumerator(dictionary);
			} 

			public void CopyTo(TValue[] array, int index)
			{ 
				int count = dictionary.count;
				Entry[] entries = dictionary.entries;
				
				for (int i = 0; i < count; i++)
				{
					if (entries[i].hashCode >= 0) array[index++] = entries[i].pair.Value; 
				}
			} 
 
			public int Count
			{
				get { return dictionary.Count; } 
			}

			bool ICollection<TValue>.IsReadOnly
			{
				get { return true; } 
			}
 
			void ICollection<TValue>.Add(TValue item)
			{ 
				throw new NotSupportedException();
			} 

			bool ICollection<TValue>.Remove(TValue item)
			{
				throw new NotSupportedException();
			}
 
			void ICollection<TValue>.Clear()
			{
				throw new NotSupportedException();
			} 

			bool ICollection<TValue>.Contains(TValue item)
			{
				return dictionary.ContainsValue(item);
			} 

			IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
			{ 
				return new ValueEnumerator(dictionary); 
			}
 
			IEnumerator IEnumerable.GetEnumerator()
			{
				return new ValueEnumerator(dictionary);
			}

			public struct ValueEnumerator : IEnumerator<TValue>
			{ 
				private readonly Dictionary<TKey, TValue, TComparer> dictionary;
				private int index;
				private TValue currentValue;
 
				internal ValueEnumerator(Dictionary<TKey, TValue, TComparer> dictionary)
				{
					this.dictionary = dictionary;
					index = 0; 
					currentValue = default(TValue);
				} 
 
				public void Dispose() { } 

				public bool MoveNext()
				{ 
					while ((uint)index < (uint)dictionary.count)
					{ 
						if (dictionary.entries[index].hashCode >= 0)
						{
							currentValue = dictionary.entries[index].pair.Value; 
							index++;
							return true;
						}
						index++; 
					}
					
					index = dictionary.count + 1; 
					currentValue = default(TValue); 
					return false;
				} 

				public TValue Current
				{
					get { return currentValue; }
				}

				object IEnumerator.Current
				{
					get { return currentValue; }
				}

				void IEnumerator.Reset()
				{
					index = 0;
					currentValue = default(TValue);
				}
			} 
		}

		private static bool Equals(Entry entry, int hashCode, TKey key)
		{
			return entry.hashCode == hashCode && Equals(entry.pair.Key, key);
		}

		private static bool Equals(TKey a, TKey b)
		{
			//by WuNan @2016/09/19 21:20:33
			// Todo: s_comparer.Equals(a, b)中NullReference的Exception，原因待查，暂时做了容错处理
			//		 详见public bool EqualityComparer<T>.Equals(T a, T b)

			//if (!Object.ReferenceEquals(s_comparer.GetType(), (new TComparer()).GetType()))
			/*TComparer newTComparer = new TComparer();
			FieldInfo[] fields = newTComparer.GetType().GetFields();
			foreach (var item in fields)
			{

				UnityEngine.Debug.LogErrorFormat("s_comparer: {0},{1}\n newTComparer: {2} ",
					item.Name, item.GetValue(s_comparer), item.GetValue(newTComparer) );
			}

			UnityEngine.Debug.LogError("*** " + (new TComparer()).GetType().ToString() + "--- " + s_comparer.GetType());
			*/

			return s_comparer.Equals(a, b);
		}
	}
}

