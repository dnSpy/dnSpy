//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;

namespace Microsoft.Cci.Pdb {
  internal struct DbiHeader {
    internal DbiHeader(BitAccess bits) {
      bits.ReadInt32(out sig);
      bits.ReadInt32(out ver);
      bits.ReadInt32(out age);
      bits.ReadInt16(out gssymStream);
      bits.ReadUInt16(out vers);
      bits.ReadInt16(out pssymStream);
      bits.ReadUInt16(out pdbver);
      bits.ReadInt16(out symrecStream);
      bits.ReadUInt16(out pdbver2);
      bits.ReadInt32(out gpmodiSize);
      bits.ReadInt32(out secconSize);
      bits.ReadInt32(out secmapSize);
      bits.ReadInt32(out filinfSize);
      bits.ReadInt32(out tsmapSize);
      bits.ReadInt32(out mfcIndex);
      bits.ReadInt32(out dbghdrSize);
      bits.ReadInt32(out ecinfoSize);
      bits.ReadUInt16(out flags);
      bits.ReadUInt16(out machine);
      bits.ReadInt32(out reserved);
    }

    internal int sig;                        // 0..3
    internal int ver;                        // 4..7
    internal int age;                        // 8..11
    internal short gssymStream;                // 12..13
    internal ushort vers;                       // 14..15
    internal short pssymStream;                // 16..17
    internal ushort pdbver;                     // 18..19
    internal short symrecStream;               // 20..21
    internal ushort pdbver2;                    // 22..23
    internal int gpmodiSize;                 // 24..27
    internal int secconSize;                 // 28..31
    internal int secmapSize;                 // 32..35
    internal int filinfSize;                 // 36..39
    internal int tsmapSize;                  // 40..43
    internal int mfcIndex;                   // 44..47
    internal int dbghdrSize;                 // 48..51
    internal int ecinfoSize;                 // 52..55
    internal ushort flags;                      // 56..57
    internal ushort machine;                    // 58..59
    internal int reserved;                   // 60..63
  }
}
