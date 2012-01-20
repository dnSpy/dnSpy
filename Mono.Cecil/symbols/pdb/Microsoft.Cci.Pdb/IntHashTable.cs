//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections;

namespace Microsoft.Cci.Pdb {
  // The IntHashTable class represents a dictionary of associated keys and
  // values with constant lookup time.
  //
  // Objects used as keys in a hashtable must implement the GetHashCode
  // and Equals methods (or they can rely on the default implementations
  // inherited from Object if key equality is simply reference
  // equality). Furthermore, the GetHashCode and Equals methods of
  // a key object must produce the same results given the same parameters
  // for the entire time the key is present in the hashtable. In practical
  // terms, this means that key objects should be immutable, at least for
  // the time they are used as keys in a hashtable.
  //
  // When entries are added to a hashtable, they are placed into
  // buckets based on the hashcode of their keys. Subsequent lookups of
  // keys will use the hashcode of the keys to only search a particular
  // bucket, thus substantially reducing the number of key comparisons
  // required to find an entry. A hashtable's maximum load factor, which
  // can be specified when the hashtable is instantiated, determines the
  // maximum ratio of hashtable entries to hashtable buckets. Smaller load
  // factors cause faster average lookup times at the cost of increased
  // memory consumption. The default maximum load factor of 1.0 generally
  // provides the best balance between speed and size. As entries are added
  // to a hashtable, the hashtable's actual load factor increases, and when
  // the actual load factor reaches the maximum load factor value, the
  // number of buckets in the hashtable is automatically increased by
  // approximately a factor of two (to be precise, the number of hashtable
  // buckets is increased to the smallest prime number that is larger than
  // twice the current number of hashtable buckets).
  //
  // Each object provides their own hash function, accessed by calling
  // GetHashCode().  However, one can write their own object
  // implementing IHashCodeProvider and pass it to a constructor on
  // the IntHashTable.  That hash function would be used for all objects in
  // the table.
  //
  // This IntHashTable is implemented to support multiple concurrent readers
  // and one concurrent writer without using any synchronization primitives.
  // All read methods essentially must protect themselves from a resize
  // occuring while they are running.  This was done by enforcing an
  // ordering on inserts & removes, as well as removing some member variables
  // and special casing the expand code to work in a temporary array instead
  // of the live bucket array.  All inserts must set a bucket's value and
  // key before setting the hash code & collision field.
  //
  // By Brian Grunkemeyer, algorithm by Patrick Dussud.
  // Version 1.30 2/20/2000
  //| <include path='docs/doc[@for="IntHashTable"]/*' />
  internal class IntHashTable {//: IEnumerable {
    /*
      Implementation Notes:

      This IntHashTable uses double hashing.  There are hashsize buckets in
      the table, and each bucket can contain 0 or 1 element.  We a bit to
      mark whether there's been a collision when we inserted multiple
      elements (ie, an inserted item was hashed at least a second time and
      we probed this bucket, but it was already in use).  Using the
      collision bit, we can terminate lookups & removes for elements that
      aren't in the hash table more quickly.  We steal the most
      significant bit from the hash code to store the collision bit.

      Our hash function is of the following form:

      h(key, n) = h1(key) + n*h2(key)

      where n is the number of times we've hit a collided bucket and
      rehashed (on this particular lookup).  Here are our hash functions:

      h1(key) = GetHash(key);  // default implementation calls key.GetHashCode();
      h2(key) = 1 + (((h1(key) >> 5) + 1) % (hashsize - 1));

      The h1 can return any number.  h2 must return a number between 1 and
      hashsize - 1 that is relatively prime to hashsize (not a problem if
      hashsize is prime).  (Knuth's Art of Computer Programming, Vol. 3,
      p. 528-9)

      If this is true, then we are guaranteed to visit every bucket in
      exactly hashsize probes, since the least common multiple of hashsize
      and h2(key) will be hashsize * h2(key).  (This is the first number
      where adding h2 to h1 mod hashsize will be 0 and we will search the
      same bucket twice).

      We previously used a different h2(key, n) that was not constant.
      That is a horrifically bad idea, unless you can prove that series
      will never produce any identical numbers that overlap when you mod
      them by hashsize, for all subranges from i to i+hashsize, for all i.
      It's not worth investigating, since there was no clear benefit from
      using that hash function, and it was broken.

      For efficiency reasons, we've implemented this by storing h1 and h2
      in a temporary, and setting a variable called seed equal to h1.  We
      do a probe, and if we collided, we simply add h2 to seed each time
      through the loop.

      A good test for h2() is to subclass IntHashTable, provide your own
      implementation of GetHash() that returns a constant, then add many
      items to the hash table.  Make sure Count equals the number of items
      you inserted.

      -- Brian Grunkemeyer, 10/28/1999
    */

    // A typical resize algorithm would pick the smallest prime number in this array
    // that is larger than twice the previous capacity.
    // Suppose our Hashtable currently has capacity x and enough elements are added
    // such that a resize needs to occur. Resizing first computes 2x then finds the
    // first prime in the table greater than 2x, i.e. if primes are ordered
    // p_1, p_2, …, p_i,…, it finds p_n such that p_n-1 < 2x < p_n.
    // Doubling is important for preserving the asymptotic complexity of the
    // hashtable operations such as add.  Having a prime guarantees that double
    // hashing does not lead to infinite loops.  IE, your hash function will be
    // h1(key) + i*h2(key), 0 <= i < size.  h2 and the size must be relatively prime.
    private static readonly int[] primes = {
            3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
            1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
            17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
            187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
            1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369};

    private static int GetPrime(int minSize) {
      if (minSize < 0) {
        throw new ArgumentException("Arg_HTCapacityOverflow");
      }
      for (int i = 0; i < primes.Length; i++) {
        int size = primes[i];
        if (size >= minSize) {
          return size;
        }
      }
      throw new ArgumentException("Arg_HTCapacityOverflow");
    }

    // Deleted entries have their key set to buckets

    // The hash table data.
    // This cannot be serialised
    private struct bucket {
      internal int key;
      internal int hash_coll;   // Store hash code; sign bit means there was a collision.
      internal Object val;
    }

    private bucket[] buckets;

    // The total number of entries in the hash table.
    private int count;

    // The total number of collision bits set in the hashtable
    private int occupancy;

    private int loadsize;
    private int loadFactorPerc;    // 100 = 1.0

    private int version;

    // Constructs a new hashtable. The hashtable is created with an initial
    // capacity of zero and a load factor of 1.0.
    //| <include path='docs/doc[@for="IntHashTable.IntHashTable"]/*' />
    internal IntHashTable()
      : this(0, 100) {
    }

    //// Constructs a new hashtable with the given initial capacity and a load
    //// factor of 1.0. The capacity argument serves as an indication of
    //// the number of entries the hashtable will contain. When this number (or
    //// an approximation) is known, specifying it in the constructor can
    //// eliminate a number of resizing operations that would otherwise be
    //// performed when elements are added to the hashtable.
    ////
    ////| <include path='docs/doc[@for="IntHashTable.IntHashTable1"]/*' />
    //internal IntHashTable(int capacity)
    //  : this(capacity, 100) {
    //}

    // Constructs a new hashtable with the given initial capacity and load
    // factor. The capacity argument serves as an indication of the
    // number of entries the hashtable will contain. When this number (or an
    // approximation) is known, specifying it in the constructor can eliminate
    // a number of resizing operations that would otherwise be performed when
    // elements are added to the hashtable. The loadFactorPerc argument
    // indicates the maximum ratio of hashtable entries to hashtable buckets.
    // Smaller load factors cause faster average lookup times at the cost of
    // increased memory consumption. A load factor of 1.0 generally provides
    // the best balance between speed and size.
    //
    //| <include path='docs/doc[@for="IntHashTable.IntHashTable3"]/*' />
    internal IntHashTable(int capacity, int loadFactorPerc) {
      if (capacity < 0)
        throw new ArgumentOutOfRangeException("capacity", "ArgumentOutOfRange_NeedNonNegNum");
      if (!(loadFactorPerc >= 10 && loadFactorPerc <= 100))
        throw new ArgumentOutOfRangeException("loadFactorPerc", String.Format("ArgumentOutOfRange_IntHashTableLoadFactor", 10, 100));

      // Based on perf work, .72 is the optimal load factor for this table.
      this.loadFactorPerc = (loadFactorPerc * 72) / 100;

      int hashsize = GetPrime((int)(capacity / this.loadFactorPerc));
      buckets = new bucket[hashsize];

      loadsize = (int)(this.loadFactorPerc * hashsize) / 100;
      if (loadsize >= hashsize)
        loadsize = hashsize-1;
    }

    // Computes the hash function:  H(key, i) = h1(key) + i*h2(key, hashSize).
    // The out parameter seed is h1(key), while the out parameter
    // incr is h2(key, hashSize).  Callers of this function should
    // add incr each time through a loop.
    private static uint InitHash(int key, int hashsize, out uint seed, out uint incr) {
      // Hashcode must be positive.  Also, we must not use the sign bit, since
      // that is used for the collision bit.
      uint hashcode = (uint)key & 0x7FFFFFFF;
      seed = (uint)hashcode;
      // Restriction: incr MUST be between 1 and hashsize - 1, inclusive for
      // the modular arithmetic to work correctly.  This guarantees you'll
      // visit every bucket in the table exactly once within hashsize
      // iterations.  Violate this and it'll cause obscure bugs forever.
      // If you change this calculation for h2(key), update putEntry too!
      incr = (uint)(1 + (((seed >> 5) + 1) % ((uint)hashsize - 1)));
      return hashcode;
    }

    // Adds an entry with the given key and value to this hashtable. An
    // ArgumentException is thrown if the key is null or if the key is already
    // present in the hashtable.
    //
    //| <include path='docs/doc[@for="IntHashTable.Add"]/*' />
    internal void Add(int key, Object value) {
      Insert(key, value, true);
    }

    //// Removes all entries from this hashtable.
    ////| <include path='docs/doc[@for="IntHashTable.Clear"]/*' />
    //internal void Clear() {
    //  if (count == 0)
    //    return;

    //  for (int i = 0; i < buckets.Length; i++) {
    //    buckets[i].hash_coll = 0;
    //    buckets[i].key = -1;
    //    buckets[i].val = null;
    //  }

    //  count = 0;
    //  occupancy = 0;
    //}

    // Checks if this hashtable contains an entry with the given key.  This is
    // an O(1) operation.
    //
    //| <include path='docs/doc[@for="IntHashTable.Contains"]/*' />
    //internal bool Contains(int key) {
    //  if (key < 0) {
    //    throw new ArgumentException("Argument_KeyLessThanZero");
    //  }

    //  uint seed;
    //  uint incr;
    //  // Take a snapshot of buckets, in case another thread resizes table
    //  bucket[] lbuckets = buckets;
    //  uint hashcode = InitHash(key, lbuckets.Length, out seed, out incr);
    //  int ntry = 0;

    //  bucket b;
    //  do {
    //    int bucketNumber = (int)(seed % (uint)lbuckets.Length);
    //    b = lbuckets[bucketNumber];
    //    if (b.val == null) {
    //      return false;
    //    }
    //    if (((b.hash_coll & 0x7FFFFFFF) == hashcode) && b.key == key) {
    //      return true;
    //    }
    //    seed += incr;
    //  } while (b.hash_coll < 0 && ++ntry < lbuckets.Length);
    //  return false;
    //}

    // Returns the value associated with the given key. If an entry with the
    // given key is not found, the returned value is null.
    //
    //| <include path='docs/doc[@for="IntHashTable.this"]/*' />
    internal Object this[int key] {
      get {
        if (key < 0) {
          throw new ArgumentException("Argument_KeyLessThanZero");
        }
        uint seed;
        uint incr;
        // Take a snapshot of buckets, in case another thread does a resize
        bucket[] lbuckets = buckets;
        uint hashcode = InitHash(key, lbuckets.Length, out seed, out incr);
        int ntry = 0;

        bucket b;
        do {
          int bucketNumber = (int)(seed % (uint)lbuckets.Length);
          b = lbuckets[bucketNumber];
          if (b.val == null) {
            return null;
          }
          if (((b.hash_coll & 0x7FFFFFFF) == hashcode) && key == b.key) {
            return b.val;
          }
          seed += incr;
        } while (b.hash_coll < 0 && ++ntry < lbuckets.Length);
        return null;
      }
      //set {
      //  Insert(key, value, false);
      //}
    }

    // Increases the bucket count of this hashtable. This method is called from
    // the Insert method when the actual load factor of the hashtable reaches
    // the upper limit specified when the hashtable was constructed. The number
    // of buckets in the hashtable is increased to the smallest prime number
    // that is larger than twice the current number of buckets, and the entries
    // in the hashtable are redistributed into the new buckets using the cached
    // hashcodes.
    private void expand() {
      rehash(GetPrime(1+buckets.Length*2));
    }

    // We occationally need to rehash the table to clean up the collision bits.
    private void rehash() {
      rehash(buckets.Length);
    }

    private void rehash(int newsize) {

      // reset occupancy
      occupancy=0;

      // Don't replace any internal state until we've finished adding to the
      // new bucket[].  This serves two purposes:
      //   1) Allow concurrent readers to see valid hashtable contents
      //      at all times
      //   2) Protect against an OutOfMemoryException while allocating this
      //      new bucket[].
      bucket[] newBuckets = new bucket[newsize];

      // rehash table into new buckets
      int nb;
      for (nb = 0; nb < buckets.Length; nb++) {
        bucket oldb = buckets[nb];
        if (oldb.val != null) {
          putEntry(newBuckets, oldb.key, oldb.val, oldb.hash_coll & 0x7FFFFFFF);
        }
      }

      // New bucket[] is good to go - replace buckets and other internal state.
      version++;
      buckets = newBuckets;
      loadsize = (int)(loadFactorPerc * newsize) / 100;

      if (loadsize >= newsize) {
        loadsize = newsize-1;
      }

      return;
    }

    // Returns an enumerator for this hashtable.
    // If modifications made to the hashtable while an enumeration is
    // in progress, the MoveNext and Current methods of the
    // enumerator will throw an exception.
    //
    //| <include path='docs/doc[@for="IntHashTable.IEnumerable.GetEnumerator"]/*' />
    //IEnumerator IEnumerable.GetEnumerator() {
    //  return new IntHashTableEnumerator(this);
    //}

    // Internal method to compare two keys.
    //
    // Inserts an entry into this hashtable. This method is called from the Set
    // and Add methods. If the add parameter is true and the given key already
    // exists in the hashtable, an exception is thrown.
    private void Insert(int key, Object nvalue, bool add) {
      if (key < 0) {
        throw new ArgumentException("Argument_KeyLessThanZero");
      }
      if (nvalue == null) {
        throw new ArgumentNullException("nvalue", "ArgumentNull_Value");
      }
      if (count >= loadsize) {
        expand();
      } else if (occupancy > loadsize && count > 100) {
        rehash();
      }

      uint seed;
      uint incr;
      // Assume we only have one thread writing concurrently.  Modify
      // buckets to contain new data, as long as we insert in the right order.
      uint hashcode = InitHash(key, buckets.Length, out seed, out incr);
      int ntry = 0;
      int emptySlotNumber = -1; // We use the empty slot number to cache the first empty slot. We chose to reuse slots
      // create by remove that have the collision bit set over using up new slots.

      do {
        int bucketNumber = (int)(seed % (uint)buckets.Length);

        // Set emptySlot number to current bucket if it is the first available bucket that we have seen
        // that once contained an entry and also has had a collision.
        // We need to search this entire collision chain because we have to ensure that there are no
        // duplicate entries in the table.

        // Insert the key/value pair into this bucket if this bucket is empty and has never contained an entry
        // OR
        // This bucket once contained an entry but there has never been a collision
        if (buckets[bucketNumber].val == null) {
          // If we have found an available bucket that has never had a collision, but we've seen an available
          // bucket in the past that has the collision bit set, use the previous bucket instead
          if (emptySlotNumber != -1) { // Reuse slot
            bucketNumber = emptySlotNumber;
          }

          // We pretty much have to insert in this order.  Don't set hash
          // code until the value & key are set appropriately.
          buckets[bucketNumber].val = nvalue;
          buckets[bucketNumber].key = key;
          buckets[bucketNumber].hash_coll |= (int)hashcode;
          count++;
          version++;
          return;
        }

        // The current bucket is in use
        // OR
        // it is available, but has had the collision bit set and we have already found an available bucket
        if (((buckets[bucketNumber].hash_coll & 0x7FFFFFFF) == hashcode) &&
                    key == buckets[bucketNumber].key) {
          if (add) {
            throw new ArgumentException("Argument_AddingDuplicate__" + buckets[bucketNumber].key);
          }
          buckets[bucketNumber].val = nvalue;
          version++;
          return;
        }

        // The current bucket is full, and we have therefore collided.  We need to set the collision bit
        // UNLESS
        // we have remembered an available slot previously.
        if (emptySlotNumber == -1) {// We don't need to set the collision bit here since we already have an empty slot
          if (buckets[bucketNumber].hash_coll >= 0) {
            buckets[bucketNumber].hash_coll |= unchecked((int)0x80000000);
            occupancy++;
          }
        }
        seed += incr;
      } while (++ntry < buckets.Length);

      // This code is here if and only if there were no buckets without a collision bit set in the entire table
      if (emptySlotNumber != -1) {
        // We pretty much have to insert in this order.  Don't set hash
        // code until the value & key are set appropriately.
        buckets[emptySlotNumber].val = nvalue;
        buckets[emptySlotNumber].key  = key;
        buckets[emptySlotNumber].hash_coll |= (int)hashcode;
        count++;
        version++;
        return;

      }

      // If you see this assert, make sure load factor & count are reasonable.
      // Then verify that our double hash function (h2, described at top of file)
      // meets the requirements described above. You should never see this assert.
      throw new InvalidOperationException("InvalidOperation_HashInsertFailed");
    }

    private void putEntry(bucket[] newBuckets, int key, Object nvalue, int hashcode) {
      uint seed = (uint)hashcode;
      uint incr = (uint)(1 + (((seed >> 5) + 1) % ((uint)newBuckets.Length - 1)));

      do {
        int bucketNumber = (int)(seed % (uint)newBuckets.Length);

        if ((newBuckets[bucketNumber].val == null)) {
          newBuckets[bucketNumber].val = nvalue;
          newBuckets[bucketNumber].key = key;
          newBuckets[bucketNumber].hash_coll |= hashcode;
          return;
        }

        if (newBuckets[bucketNumber].hash_coll >= 0) {
          newBuckets[bucketNumber].hash_coll |= unchecked((int)0x80000000);
          occupancy++;
        }
        seed += incr;
      } while (true);
    }

    // Returns the number of associations in this hashtable.
    //
    //| <include path='docs/doc[@for="IntHashTable.Count"]/*' />
    //internal int Count {
    //  get { return count; }
    //}

    // Implements an enumerator for a hashtable. The enumerator uses the
    // internal version number of the hashtabke to ensure that no modifications
    // are made to the hashtable while an enumeration is in progress.
    //private class IntHashTableEnumerator : IEnumerator {
    //  private IntHashTable hashtable;
    //  private int bucket;
    //  private int version;
    //  private bool current;
    //  //private int currentKey;
    //  private Object currentValue;

    //  internal IntHashTableEnumerator(IntHashTable hashtable) {
    //    this.hashtable = hashtable;
    //    bucket = hashtable.buckets.Length;
    //    version = hashtable.version;
    //  }

    //  public bool MoveNext() {
    //    if (version != hashtable.version)
    //      throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
    //    while (bucket > 0) {
    //      bucket--;
    //      Object val = hashtable.buckets[bucket].val;
    //      if (val != null) {
    //        //currentKey = hashtable.buckets[bucket].key;
    //        currentValue = val;
    //        current = true;
    //        return true;
    //      }
    //    }
    //    current = false;
    //    return false;
    //  }

    //  //internal int Key {
    //  //  get {
    //  //    if (current == false)
    //  //      throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
    //  //    return currentKey;
    //  //  }
    //  //}

    //  public Object Current {
    //    get {
    //      if (current == false)
    //        throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
    //      return currentValue;
    //    }
    //  }

    //  //public Object Value {
    //  //  get {
    //  //    if (version != hashtable.version)
    //  //      throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
    //  //    if (current == false)
    //  //      throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
    //  //    return currentValue;
    //  //  }
    //  //}

    //  public void Reset() {
    //    if (version != hashtable.version) throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
    //    current = false;
    //    bucket = hashtable.buckets.Length;
    //    //currentKey = -1;
    //    currentValue = null;
    //  }
    //}
  }
}
