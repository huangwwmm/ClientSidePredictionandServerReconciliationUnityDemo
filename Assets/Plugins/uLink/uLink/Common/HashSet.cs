
using System;
using System.Collections;
using System.Collections.Generic;

namespace uLink
{
	internal class HashSet<T> : HashSet<T, EqualityComparer<T>>
		where T : IEquatable<T>
	{
		public HashSet() { }
		public HashSet(int capacity) : base(capacity) { }
		public HashSet(ICollection<T> collection) : base(collection) { }
	}

	/// <summary> 
	/// Implementation notes:
	/// This uses an array-based implementation similar to Dictionary<T>, using a buckets array
	/// to map hash values to the Slots array. Items in the Slots array that hash to the same value
	/// are chained together through the "next" indices. 
	///
	/// The capacity is always prime; so during resizing, the capacity is chosen as the next prime 
	/// greater than double the last capacity. 
	///
	/// The underlying data structures are lazily initialized. Because of the observation that, 
	/// in practice, hashtables tend to contain only a few elements, the initial capacity is
	/// set very small (3 elements) unless the ctor with a collection is used.
	///
	/// The +/- 1 modifications in methods that add, check for containment, etc allow us to 
	/// distinguish a hash code of 0 from an uninitialized bucket. This saves us from having to
	/// reset each bucket to -1 when resizing. See Contains, for example. 
	/// 
	/// Set methods such as UnionWith, IntersectWith, ExceptWith, and SymmetricExceptWith modify
	/// this set. 
	///
	/// Some operations can perform faster if we can assume "other" contains unique elements
	/// according to this equality comparer. The only times this is efficient to check is if
	/// other is a hashset. Note that checking that it's a hashset alone doesn't suffice; we 
	/// also have to check that the hashset is using the same equality comparer. If other
	/// has a different equality comparer, it will have unique elements according to its own 
	/// equality comparer, but not necessarily according to ours. Therefore, to go these 
	/// optimized routes we check that other is a hashset using the same equality comparer.
	/// 
	/// A HashSet with no elements has the properties of the empty set. (See IsSubset, etc. for
	/// special empty set checks.)
	///
	/// A couple of methods have a special case if other is this (e.g. SymmetricExceptWith). 
	/// If we didn't have these checks, we could be iterating over the set and modifying at
	/// the same time. 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal class HashSet<T, TComparer> : ICollection<T>
		where TComparer : struct, IEqualityComparer<T>
	{

		// store lower 31 bits of hash code
		private const int Lower31BitMask = 0x7FFFFFFF; 
		// cutoff point, above which we won't do stackallocs. This corresponds to 100 integers.
		private const int StackAllocThreshold = 100; 
		// when constructing a hashset from an existing collection, it may contain duplicates, 
		// so this is used as the max acceptable excess ratio of capacity to count. Note that
		// this is only used on the ctor and not to automatically shrink if the hashset has, e.g, 
		// a lot of adds followed by removes. Users must explicitly shrink by calling TrimExcess.
		// This is set to 3 because capacity is acceptable as 2x rounded up to nearest prime.
		private const int ShrinkThreshold = 3;

		private int[] m_buckets; 
		private Slot[] m_slots;
		private int m_count; 
		private int m_lastIndex; 
		private int m_freeList;
		private int m_version;
 
		public HashSet()
		{
			m_lastIndex = 0;
			m_count = 0;
			m_freeList = -1;
			m_version = 0; 
		}

		public HashSet(int capacity)
			: this()
		{
			Initialize(capacity);
		}

		/// <summary>
		/// Implementation Notes:
		/// Since resizes are relatively expensive (require rehashing), this attempts to minimize
		/// the need to resize by setting the initial capacity based on size of collection. 
		/// </summary>
		/// <param name="collection"></param>
		public HashSet(ICollection<T> collection)
			: this()
		{ 
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}

			// to avoid excess resizes, first set size based on collection's count. Collection 
			// may contain duplicates, so call TrimExcess if resulting hashset is larger than 
			// threshold
			Initialize(collection.Count);
 
			UnionWith(collection); 
			if ((m_count == 0 && m_slots.Length > HashHelper.MinPrime) ||
				(m_count > 0 && m_slots.Length / m_count > ShrinkThreshold)) { 
				TrimExcess();
			}
		}
 
		/// <summary>
		/// Add item to this hashset. This is the explicit implementation of the ICollection<T>
		/// interface. The other Add method returns bool indicating whether item was added.
		/// </summary> 
		/// <param name="item">item to add</param>
		void ICollection<T>.Add(T value)
		{ 
			AddIfNotPresent(value); 
		}
 
		/// <summary>
		/// Remove all items from this set. This clears the elements but not the underlying
		/// buckets and slots array. Follow this call by TrimExcess to release these.
		/// </summary> 
		public void Clear()
		{
			if (m_lastIndex > 0)
			{
				// clear the elements so that the gc can reclaim the references. 
				// clear only up to m_lastIndex for m_slots
				Array.Clear(m_slots, 0, m_lastIndex);
				Array.Clear(m_buckets, 0, m_buckets.Length);
				m_lastIndex = 0; 
				m_count = 0;
				m_freeList = -1; 
			} 
			m_version++;
		} 

		/// <summary>
		/// Checks if this hashset contains the item
		/// </summary> 
		/// <param name="item">item to check for containment</param>
		/// <returns>true if item contained; false if not</returns> 
		public bool Contains(T value)
		{ 
			if (m_buckets != null) {
				int hashCode = InternalGetHashCode(value); 
				// see note at "HashSet" level describing why "- 1" appears in for loop
				for (int i = m_buckets[hashCode % m_buckets.Length] - 1; i >= 0; i = m_slots[i].next) {
					if (Equals(m_slots[i], hashCode, value)) {
						return true; 
					}
				} 
			} 
			// either m_buckets is null or wasn't found
			return false; 
		}

		/// <summary>
		/// Copy items in this hashset to array, starting at arrayIndex 
		/// </summary>
		/// <param name="array">array to add items to</param> 
		/// <param name="arrayIndex">index to start at</param> 
		public void CopyTo(T[] array, int arrayIndex) {
			CopyTo(array, arrayIndex, m_count); 
		}

		/// <summary>
		/// Remove item from this hashset 
		/// </summary>
		/// <param name="value">item to remove</param> 
		/// <returns>true if removed; false if not (i.e. if the item wasn't in the HashSet)</returns> 
		public bool Remove(T value) {
			if (m_buckets != null) { 
				int hashCode = InternalGetHashCode(value);
				int bucket = hashCode % m_buckets.Length;
				int last = -1;
				for (int i = m_buckets[bucket] - 1; i >= 0; last = i, i = m_slots[i].next) { 
					if (Equals(m_slots[i], hashCode, value)) {
						if (last < 0) { 
							// first iteration; update buckets 
							m_buckets[bucket] = m_slots[i].next + 1;
						} 
						else {
							// subsequent iterations; update 'next' pointers
							m_slots[last].next = m_slots[i].next;
						} 
						m_slots[i].hashCode = -1;
						m_slots[i].value = default(T); 
						m_slots[i].next = m_freeList; 

						m_count--; 
						m_version++;
						if (m_count == 0) {
							m_lastIndex = 0;
							m_freeList = -1; 
						}
						else { 
							m_freeList = i; 
						}
						return true; 
					}
				}
			}
			// either m_buckets is null or wasn't found 
			return false;
		} 
 
		/// <summary>
		/// Number of elements in this hashset 
		/// </summary>
		public int Count {
			get { return m_count; }
		} 

		/// <summary> 
		/// Whether this is readonly 
		/// </summary>
		bool ICollection<T>.IsReadOnly { 
			get { return false; }
		}
 
		public Enumerator GetEnumerator() {
			return new Enumerator(this); 
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator() {
			return new Enumerator(this); 
		}
 
		IEnumerator IEnumerable.GetEnumerator() { 
			return new Enumerator(this);
		}

		/// <summary> 
		/// Add item to this HashSet. Returns bool indicating whether item was added (won't be 
		/// added if already present)
		/// </summary> 
		/// <param name="value"></param>
		/// <returns>true if added, false if already present</returns>
		public bool Add(T value) {
			return AddIfNotPresent(value); 
		}
 
		/// <summary> 
		/// Take the union of this HashSet with other. Modifies this set.
		/// 
		/// Implementation note: GetSuggestedCapacity (to increase capacity in advance avoiding
		/// multiple resizes ended up not being useful in practice; quickly gets to the
		/// point where it's a wasteful check.
		/// </summary> 
		/// <param name="other">enumerable with items to add</param>
		public void UnionWith(IEnumerable<T> other) { 
			if (other == null) { 
				throw new ArgumentNullException("other");
			}

			foreach (T value in other) {
				AddIfNotPresent(value); 
			}
		} 
 
		/// <summary>
		/// Takes the intersection of this set with other. Modifies this set. 
		///
		/// Implementation Notes:
		/// We get better perf if other is a hashset using same equality comparer, because we
		/// get constant contains check in other. Resulting cost is O(n1) to iterate over this. 
		///
		/// If we can't go above route, iterate over the other and mark intersection by checking 
		/// contains in this. Then loop over and delete any unmarked elements. Total cost is n2+n1. 
		///
		/// Attempts to return early based on counts alone, using the property that the 
		/// intersection of anything with the empty set is the empty set.
		/// </summary>
		/// <param name="other">enumerable with items to add </param>
		public void IntersectWith(HashSet<T, TComparer> other) { 
			if (other == null) {
				throw new ArgumentNullException("other"); 
			}
 
			// intersection of anything with empty set is empty set, so return if count is 0
			if (m_count == 0) {
				return;
			}

			IntersectWithHashSetWithSameEC(other);
		} 

		/// <summary> 
		/// Remove items in other from this set. Modifies this set.
		/// </summary>
		/// <param name="other">enumerable with items to remove</param>
		public void ExceptWith(IEnumerable<T> other) { 
			if (other == null) {
				throw new ArgumentNullException("other"); 
			}
 
			// this is already the empty set; return
			if (m_count == 0) {
				return;
			} 

			// special case if other is this; a set minus itself is the empty set 
			if (other == this) { 
				Clear();
				return; 
			}

			// remove every element in other from this
			foreach (T element in other) { 
				Remove(element);
			} 
		} 

		/// <summary> 
		/// Takes symmetric difference (XOR) with other and this set. Modifies this set.
		/// </summary>
		/// <param name="other">enumerable with items to XOR</param>
		public void SymmetricExceptWith(HashSet<T, TComparer> other) { 
			if (other == null) {
				throw new ArgumentNullException("other"); 
			}
 
			// if set is empty, then symmetric difference is other
			if (m_count == 0) {
				UnionWith(other);
				return; 
			}
 
			// special case this; the symmetric difference of a set with itself is the empty set 
			if (other == this) {
				Clear(); 
				return;
			}

			SymmetricExceptWithUniqueHashSet(other);
		} 

		/// <summary> 
		/// Checks if this is a subset of other.
		///
		/// Implementation Notes:
		/// The following properties are used up-front to avoid element-wise checks: 
		/// 1. If this is the empty set, then it's a subset of anything, including the empty set
		/// 2. If other has unique elements according to this equality comparer, and this has more 
		/// elements than other, then it can't be a subset. 
		///
		/// Furthermore, if other is a hashset using the same equality comparer, we can use a 
		/// faster element-wise check.
		/// </summary>
		/// <param name="other"></param>
		/// <returns>true if this is a subset of other; false if not</returns> 
		public bool IsSubsetOf(HashSet<T, TComparer> other) {
			if (other == null) { 
				throw new ArgumentNullException("other"); 
			}

			// The empty set is a subset of any set
			if (m_count == 0) {
				return true; 
			}

			// if this has more elements then it can't be a subset
			if (m_count > other.Count)
			{
				return false; 
			}

			return IsSubsetOfHashSetWithSameEC(other);
		} 
 
		/// <summary>
		/// Checks if this is a proper subset of other (i.e. strictly contained in) 
		///
		/// Implementation Notes:
		/// The following properties are used up-front to avoid element-wise checks:
		/// 1. If this is the empty set, then it's a proper subset of a set that contains at least 
		/// one element, but it's not a proper subset of the empty set.
		/// 2. If other has unique elements according to this equality comparer, and this has >= 
		/// the number of elements in other, then this can't be a proper subset. 
		///
		/// Furthermore, if other is a hashset using the same equality comparer, we can use a 
		/// faster element-wise check.
		/// </summary>
		/// <param name="other"></param>
		/// <returns>true if this is a proper subset of other; false if not</returns> 
		public bool IsProperSubsetOf(HashSet<T, TComparer> other) {
			if (other == null) { 
				throw new ArgumentNullException("other"); 
			}


			// the empty set is a proper subset of anything but the empty set 
			if (m_count == 0) {
				return other.Count > 0; 
			}

			if (m_count >= other.Count)
			{
				return false;
			}

			return IsSubsetOfHashSetWithSameEC(other);
		}
 
		/// <summary> 
		/// Checks if this is a superset of other
		/// 
		/// Implementation Notes:
		/// The following properties are used up-front to avoid element-wise checks:
		/// 1. If other has no elements (it's the empty set), then this is a superset, even if this
		/// is also the empty set. 
		/// 2. If other has unique elements according to this equality comparer, and this has less
		/// than the number of elements in other, then this can't be a superset 
		/// 
		/// </summary>
		/// <param name="other"></param> 
		/// <returns>true if this is a superset of other; false if not</returns>
		public bool IsSupersetOf(HashSet<T, TComparer> other) {
			if (other == null) {
				throw new ArgumentNullException("other"); 
			}

			// if other is the empty set then this is a superset
			if (other.Count == 0)
			{
				return true; 
			}

			if (other.Count > m_count)
			{
				return false;
			}
 
			return ContainsAllElements(other); 
		}
 
		/// <summary>
		/// Checks if this is a proper superset of other (i.e. other strictly contained in this)
		///
		/// Implementation Notes: 
		/// This is slightly more complicated than above because we have to keep track if there
		/// was at least one element not contained in other. 
		/// 
		/// The following properties are used up-front to avoid element-wise checks:
		/// 1. If this is the empty set, then it can't be a proper superset of any set, even if 
		/// other is the empty set.
		/// 2. If other is an empty set and this contains at least 1 element, then this is a proper
		/// superset.
		/// 3. If other has unique elements according to this equality comparer, and other's count 
		/// is greater than or equal to this count, then this can't be a proper superset
		/// 
		/// Furthermore, if other has unique elements according to this equality comparer, we can 
		/// use a faster element-wise check.
		/// </summary> 
		/// <param name="other"></param>
		/// <returns>true if this is a proper superset of other; false if not</returns>
		public bool IsProperSupersetOf(HashSet<T, TComparer> other)
		{
			if (other == null) { 
				throw new ArgumentNullException("other");
			}

			// the empty set isn't a proper superset of any set. 
			if (m_count == 0) {
				return false;
			}
 
			// if other is the empty set then this is a superset 
			if (other.Count == 0) {
				// note that this has at least one element, based on above check 
				return true;
			}

			if (other.Count >= m_count)
			{ 
				return false; 
			}

			// now perform element check 
			return ContainsAllElements(other); 
		}
 
		/// <summary>
		/// Checks if this set overlaps other (i.e. they share at least one item)
		/// </summary>
		/// <param name="other"></param> 
		/// <returns>true if these have at least one common element; false if disjoint</returns>
		public bool Overlaps(HashSet<T, TComparer> other)
		{ 
			if (other == null) { 
				throw new ArgumentNullException("other");
			}

			if (m_count == 0) {
				return false; 
			}
 
			foreach (T element in other) { 
				if (Contains(element)) {
					return true; 
				}
			}

			return false;
		} 

		/// <summary> 
		/// Checks if this and other contain the same elements. This is set equality: 
		/// duplicates and order are ignored
		/// </summary> 
		/// <param name="other"></param>
		/// <returns></returns>
		public bool SetEquals(HashSet<T, TComparer> other)
		{
			if (other == null) { 
				throw new ArgumentNullException("other");
			}

			// attempt to return early: since both contain unique elements, if they have
			// different counts, then they can't be equal 
			if (m_count != other.Count)
			{
				return false; 
			} 

			// already confirmed that the sets have the same number of distinct elements, so if 
			// one is a superset of the other then they must be equal
			return ContainsAllElements(other);
		} 
 
		public void CopyTo(T[] array) { CopyTo(array, 0, m_count); }
 
		public void CopyTo(T[] array, int arrayIndex, int count) {
			if (array == null) {
				throw new ArgumentNullException("array");
			}
 
			// check array index valid index into array 
			if (arrayIndex < 0) {
				throw new ArgumentOutOfRangeException("arrayIndex"); 
			}

			// also throw if count less than 0
			if (count < 0) { 
				throw new ArgumentOutOfRangeException("count");
			} 
 
			// will array, starting at arrayIndex, be able to hold elements? Note: not
			// checking arrayIndex >= array.Length (consistency with list of allowing 
			// count of 0; subsequent check takes care of the rest)
			if (arrayIndex > array.Length || count > array.Length - arrayIndex) {
				throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");
			} 

			int numCopied = 0; 
			for (int i = 0; i < m_lastIndex && numCopied < count; i++) { 
				if (m_slots[i].hashCode >= 0) {
					array[arrayIndex + numCopied] = m_slots[i].value; 
					numCopied++;
				}
			}
		} 

		/// <summary> 
		/// Remove elements that match specified predicate. Returns the number of elements removed 
		/// </summary>
		/// <param name="match"></param> 
		/// <returns></returns>
		public int RemoveWhere(Predicate<T> match) {
			if (match == null) {
				throw new ArgumentNullException("match"); 
			}
 
			int numRemoved = 0;
			for (int i = 0; i < m_lastIndex; i++) { 
				if (m_slots[i].hashCode >= 0) {
					// cache value in case delegate removes it
					T value = m_slots[i].value;
					if (match(value)) { 
						// check again that remove actually removed it
						if (Remove(value)) { 
							numRemoved++; 
						}
					} 
				}
			}
			return numRemoved;
		}
 
		/// <summary> 
		/// Sets the capacity of this list to the size of the list (rounded up to nearest prime),
		/// unless count is 0, in which case we release references. 
		///
		/// This method can be used to minimize a list's memory overhead once it is known that no
		/// new elements will be added to the list. To completely clear a list and release all
		/// memory referenced by the list, execute the following statements: 
		///
		/// list.Clear(); 
		/// list.TrimExcess(); 
		/// </summary>
		public void TrimExcess()
		{
			if (m_count == 0)
			{
				// if count is zero, clear references 
				m_buckets = null;
				m_slots = null; 
				m_version++; 
			}
			else
			{
				// similar to IncreaseCapacity but moves down elements in case add/remove/etc
				// caused fragmentation 
				int newSize = HashHelper.GetPrime(m_count);
				Slot[] newSlots = new Slot[newSize]; 
				int[] newBuckets = new int[newSize]; 

				// move down slots and rehash at the same time. newIndex keeps track of current 
				// position in newSlots array
				int newIndex = 0;
				for (int i = 0; i < m_lastIndex; i++) {
					if (m_slots[i].hashCode >= 0) { 
						newSlots[newIndex] = m_slots[i];
 
						// rehash 
						int bucket = newSlots[newIndex].hashCode % newSize;
						newSlots[newIndex].next = newBuckets[bucket] - 1; 
						newBuckets[bucket] = newIndex + 1;

						newIndex++;
					} 
				}

				m_lastIndex = newIndex; 
				m_slots = newSlots;
				m_buckets = newBuckets;
				m_freeList = -1;
			} 
		}

		/// <summary>
		/// Initializes buckets and slots arrays. Uses suggested capacity by finding next prime 
		/// greater than or equal to capacity.
		/// </summary> 
		/// <param name="capacity"></param> 
		private void Initialize(int capacity)
		{
			int size = HashHelper.GetPrime(capacity);

			m_buckets = new int[size]; 
			m_slots = new Slot[size];
		} 
 
		/// <summary>
		/// Expand to new capacity. New capacity is next prime greater than or equal to suggested 
		/// size. This is called when the underlying array is filled. This performs no
		/// defragmentation, allowing faster execution; note that this is reasonable since
		/// AddIfNotPresent attempts to insert new elements in re-opened spots.
		/// </summary> 
		/// <param name="sizeSuggestion"></param>
		private void IncreaseCapacity()
		{
			int newSize = HashHelper.ExpandPrime(m_count);
			if (newSize <= m_count)
			{
				throw new ArgumentException("HashSet capacity overflow");
			}
 
			// Able to increase capacity; copy elements to larger array and rehash
			SetCapacity(newSize, false); 
		} 

		/// <summary> 
		/// Set the underlying buckets array to size newSize and rehash.  Note that newSize
		/// *must* be a prime.  It is very likely that you want to call IncreaseCapacity()
		/// instead of this method.
		/// </summary>
		private void SetCapacity(int newSize, bool forceNewHashCodes)
		{
			Slot[] newSlots = new Slot[newSize];
			if (m_slots != null) {
				Array.Copy(m_slots, 0, newSlots, 0, m_lastIndex);
			} 

			if(forceNewHashCodes) { 
				for(int i = 0; i < m_lastIndex; i++) { 
					if(newSlots[i].hashCode != -1) {
						newSlots[i].hashCode = InternalGetHashCode(newSlots[i].value); 
					}
				}
			}
 
			int[] newBuckets = new int[newSize];
			for (int i = 0; i < m_lastIndex; i++) { 
				int bucket = newSlots[i].hashCode % newSize; 
				newSlots[i].next = newBuckets[bucket] - 1;
				newBuckets[bucket] = i + 1; 
			}
			m_slots = newSlots;
			m_buckets = newBuckets;
		} 

		/// <summary> 
		/// Adds value to HashSet if not contained already 
		/// Returns true if added and false if already present
		/// </summary> 
		/// <param name="value">value to find</param>
		/// <returns></returns>
		private bool AddIfNotPresent(T value) {
			if (m_buckets == null) { 
				Initialize(0);
			} 
 
			int hashCode = InternalGetHashCode(value);
			int bucket = hashCode % m_buckets.Length;

			for (int i = m_buckets[hashCode % m_buckets.Length] - 1; i >= 0; i = m_slots[i].next) {
				if (Equals(m_slots[i], hashCode, value))
				{
					return false; 
				}
			}

			int index; 
			if (m_freeList >= 0) {
				index = m_freeList; 
				m_freeList = m_slots[index].next; 
			}
			else { 
				if (m_lastIndex == m_slots.Length) {
					IncreaseCapacity();
					// this will change during resize
					bucket = hashCode % m_buckets.Length; 
				}
				index = m_lastIndex; 
				m_lastIndex++; 
			}
			m_slots[index].hashCode = hashCode; 
			m_slots[index].value = value;
			m_slots[index].next = m_buckets[bucket] - 1;
			m_buckets[bucket] = index + 1;
			m_count++; 
			m_version++;
 
			return true;
		} 
 
		/// <summary>
		/// Checks if this contains of other's elements. Iterates over other's elements and 
		/// returns false as soon as it finds an element in other that's not in this.
		/// Used by SupersetOf, ProperSupersetOf, and SetEquals.
		/// </summary>
		/// <param name="other"></param> 
		/// <returns></returns>
		private bool ContainsAllElements(IEnumerable<T> other) { 
			foreach (T element in other) { 
				if (!Contains(element)) {
					return false; 
				}
			}
			return true;
		} 

		/// <summary> 
		/// Implementation Notes: 
		/// If other is a hashset and is using same equality comparer, then checking subset is
		/// faster. Simply check that each element in this is in other. 
		///
		/// Note: if other doesn't use same equality comparer, then Contains check is invalid,
		/// which is why callers must take are of this.
		/// 
		/// If callers are concerned about whether this is a proper subset, they take care of that.
		/// 
		/// </summary> 
		/// <param name="other"></param>
		/// <returns></returns> 
		private bool IsSubsetOfHashSetWithSameEC(HashSet<T, TComparer> other)
		{
			foreach (T value in this) {
				if (!other.Contains(value)) { 
					return false;
				} 
			} 
			return true;
		} 

		/// <summary>
		/// If other is a hashset that uses same equality comparer, intersect is much faster
		/// because we can use other's Contains 
		/// </summary>
		/// <param name="other"></param> 
		private void IntersectWithHashSetWithSameEC(HashSet<T, TComparer> other) { 
			for (int i = 0; i < m_lastIndex; i++) {
				if (m_slots[i].hashCode >= 0) { 
					T value = m_slots[i].value;
					if (!other.Contains(value)) {
						Remove(value);
					} 
				}
			} 
		} 

		/// <summary> 
		/// Used internally by set operations which have to rely on bit array marking. This is like
		/// Contains but returns index in slots array. 
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private int InternalIndexOf(T value)
		{
			int hashCode = InternalGetHashCode(value); 
			for (int i = m_buckets[hashCode % m_buckets.Length] - 1; i >= 0; i = m_slots[i].next) {
				if (Equals(m_slots[i], hashCode, value)) { 
					return i;
				}
			}
			// wasn't found 
			return -1;
		} 
 
		/// <summary>
		/// if other is a set, we can assume it doesn't have duplicate elements, so use this 
		/// technique: if can't remove, then it wasn't present in this set, so add.
		///
		/// As with other methods, callers take care of ensuring that other is a hashset using the
		/// same equality comparer. 
		/// </summary>
		/// <param name="other"></param> 
		private void SymmetricExceptWithUniqueHashSet(HashSet<T, TComparer> other)
		{ 
			foreach (T value in other) {
				if (!Remove(value)) { 
					AddIfNotPresent(value);
				}
			}
		} 

		/// <summary>
		/// Add if not already in hashset. Returns an out param indicating index where added. This 
		/// is used by SymmetricExcept because it needs to know the following things:
		/// - whether the item was already present in the collection or added from other 
		/// - where it's located (if already present, it will get marked for removal, otherwise 
		/// marked for keeping)
		/// </summary> 
		/// <param name="value"></param>
		/// <param name="location"></param>
		/// <returns></returns>
		private bool AddOrGetLocation(T value, out int location)
		{
			int hashCode = InternalGetHashCode(value); 
			int bucket = hashCode % m_buckets.Length;
			for (int i = m_buckets[hashCode % m_buckets.Length] - 1; i >= 0; i = m_slots[i].next) { 
				if (Equals(m_slots[i], hashCode, value)) {
					location = i;
					return false; //already present
				} 
			}
			int index; 
			if (m_freeList >= 0) { 
				index = m_freeList;
				m_freeList = m_slots[index].next; 
			}
			else {
				if (m_lastIndex == m_slots.Length) {
					IncreaseCapacity(); 
					// this will change during resize
					bucket = hashCode % m_buckets.Length; 
				} 
				index = m_lastIndex;
				m_lastIndex++; 
			}
			m_slots[index].hashCode = hashCode;
			m_slots[index].value = value;
			m_slots[index].next = m_buckets[bucket] - 1; 
			m_buckets[bucket] = index + 1;
			m_count++; 
			m_version++; 
			location = index;
			return true; 
		}

		/// <summary> 
		/// Copies this to an array. Used for DebugView 
		/// </summary>
		/// <returns></returns> 
		internal T[] ToArray() {
			T[] newArray = new T[Count];
			CopyTo(newArray);
			return newArray; 
		}
 
		/// <summary> 
		/// Internal method used for HashSetEqualityComparer. Compares set1 and set2 according
		/// to specified comparer. 
		///
		/// Because items are hashed according to a specific equality comparer, we have to resort
		/// to n^2 search if they're using different equality comparers.
		/// </summary> 
		/// <param name="set1"></param>
		/// <param name="set2"></param> 
		/// <param name="comparer"></param> 
		/// <returns></returns>
		internal static bool HashSetEquals(HashSet<T, TComparer> set1, HashSet<T, TComparer> set2, IEqualityComparer<T> comparer) { 
			// handle null cases first
			if (set1 == null) {
				return (set2 == null);
			} 
			
			if (set2 == null) {
				// set1 != null 
				return false; 
			}
 
			if (set1.Count != set2.Count) {
				return false; 
			}

			// suffices to check subset 
			foreach (T value in set2) { 
				if (!set1.Contains(value)) {
					return false; 
				}
			}

			return true;
		}
 
		/// <summary> 
		/// Workaround Comparers that throw ArgumentNullException for GetHashCode(null).
		/// </summary> 
		/// <param name="value"></param>
		/// <returns>hash code</returns>
		private int InternalGetHashCode(T value) {
			if (value == null) { 
				return 0;
			} 
			return value.GetHashCode() & Lower31BitMask; 
		}

		// used for set checking operations (using enumerables) that rely on counting
		internal struct ElementCount { 
			internal int uniqueCount;
			internal int unfoundCount; 
		} 

		internal struct Slot { 
			internal int hashCode;      // Lower 31 bits of hash code, -1 if unused
			internal T value;
			internal int next;          // Index of next entry, -1 if last
		} 

		[Serializable]
		public struct Enumerator : IEnumerator<T>
		{
			private HashSet<T, TComparer> set;
			private int index;
			private T current;
 
			internal Enumerator(HashSet<T, TComparer> set) { 
				this.set = set;
				index = 0;
				current = default(T);
			}
 
			public void Dispose() { } 
 
			public bool MoveNext() {
				while (index < set.m_lastIndex) { 
					if (set.m_slots[index].hashCode >= 0) {
						current = set.m_slots[index].value; 
						index++; 
						return true;
					} 
					index++;
				}
				index = set.m_lastIndex + 1;
				current = default(T); 
				return false;
			} 
 
			public T Current {
				get { 
					return current;
				}
			}

			object IEnumerator.Current
			{
				get {
					return current;
				}
			}
 
			void IEnumerator.Reset() {
				index = 0;
				current = default(T);
			}
		}

		private static bool Equals(Slot slot, int hashCode, T value)
		{
			return slot.hashCode == hashCode && Equals(slot.value, value);
		}

		private static bool Equals(T a, T b)
		{
			return new TComparer().Equals(a, b);
		}
	}
}
