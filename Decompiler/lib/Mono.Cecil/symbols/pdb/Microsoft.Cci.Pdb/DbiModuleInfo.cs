//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;

namespace Microsoft.Cci.Pdb {
  internal class DbiModuleInfo {
    internal DbiModuleInfo(BitAccess bits, bool readStrings) {
      bits.ReadInt32(out opened);
      section = new DbiSecCon(bits);
      bits.ReadUInt16(out flags);
      bits.ReadInt16(out stream);
      bits.ReadInt32(out cbSyms);
      bits.ReadInt32(out cbOldLines);
      bits.ReadInt32(out cbLines);
      bits.ReadInt16(out files);
      bits.ReadInt16(out pad1);
      bits.ReadUInt32(out offsets);
      bits.ReadInt32(out niSource);
      bits.ReadInt32(out niCompiler);
      if (readStrings) {
        bits.ReadCString(out moduleName);
        bits.ReadCString(out objectName);
      } else {
        bits.SkipCString(out moduleName);
        bits.SkipCString(out objectName);
      }
      bits.Align(4);
      //if (opened != 0 || pad1 != 0) {
      //  throw new PdbException("Invalid DBI module. "+
      //                                 "(opened={0}, pad={1})", opened, pad1);
      //}
    }

    internal int opened;                 //  0..3
    internal DbiSecCon section;                //  4..31
    internal ushort flags;                  // 32..33
    internal short stream;                 // 34..35
    internal int cbSyms;                 // 36..39
    internal int cbOldLines;             // 40..43
    internal int cbLines;                // 44..57
    internal short files;                  // 48..49
    internal short pad1;                   // 50..51
    internal uint offsets;
    internal int niSource;
    internal int niCompiler;
    internal string moduleName;
    internal string objectName;
  }
}
