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

namespace Microsoft.Cci.Pdb {
  internal struct BitSet {
    internal BitSet(BitAccess bits) {
      bits.ReadInt32(out size);    // 0..3 : Number of words
      words = new uint[size];
      bits.ReadUInt32(words);
    }

    //internal BitSet(int size) {
    //  this.size = size;
    //  words = new uint[size];
    //}

    internal bool IsSet(int index) {
      int word = index / 32;
      if (word >= this.size) return false;
      return ((words[word] & GetBit(index)) != 0);
    }

    //internal void Set(int index) {
    //  int word = index / 32;
    //  if (word >= this.size) return;
    //  words[word] |= GetBit(index);
    //}

    //internal void Clear(int index) {
    //  int word = index / 32;
    //  if (word >= this.size) return;
    //  words[word] &= ~GetBit(index);
    //}

    private static uint GetBit(int index) {
      return ((uint)1 << (index % 32));
    }

    //private static uint ReverseBits(uint value) {
    //  uint o = 0;
    //  for (int i = 0; i < 32; i++) {
    //    o = (o << 1) | (value & 1);
    //    value >>= 1;
    //  }
    //  return o;
    //}

    internal bool IsEmpty {
      get { return size == 0; }
    }

    //internal bool GetWord(int index, out uint word) {
    //  if (index < size) {
    //    word = ReverseBits(words[index]);
    //    return true;
    //  }
    //  word = 0;
    //  return false;
    //}

    private int size;
    private uint[] words;
  }

}
