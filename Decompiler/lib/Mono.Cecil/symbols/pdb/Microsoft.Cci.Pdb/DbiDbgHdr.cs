//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;

namespace Microsoft.Cci.Pdb {
  internal struct DbiDbgHdr {
    internal DbiDbgHdr(BitAccess bits) {
      bits.ReadUInt16(out snFPO);
      bits.ReadUInt16(out snException);
      bits.ReadUInt16(out snFixup);
      bits.ReadUInt16(out snOmapToSrc);
      bits.ReadUInt16(out snOmapFromSrc);
      bits.ReadUInt16(out snSectionHdr);
      bits.ReadUInt16(out snTokenRidMap);
      bits.ReadUInt16(out snXdata);
      bits.ReadUInt16(out snPdata);
      bits.ReadUInt16(out snNewFPO);
      bits.ReadUInt16(out snSectionHdrOrig);
    }

    internal ushort snFPO;                 // 0..1
    internal ushort snException;           // 2..3 (deprecated)
    internal ushort snFixup;               // 4..5
    internal ushort snOmapToSrc;           // 6..7
    internal ushort snOmapFromSrc;         // 8..9
    internal ushort snSectionHdr;          // 10..11
    internal ushort snTokenRidMap;         // 12..13
    internal ushort snXdata;               // 14..15
    internal ushort snPdata;               // 16..17
    internal ushort snNewFPO;              // 18..19
    internal ushort snSectionHdrOrig;      // 20..21
  }
}
