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
  internal class PdbScope {
    internal PdbConstant[] constants;
    internal PdbSlot[] slots;
    internal PdbScope[] scopes;
    internal string[] usedNamespaces;

    //internal uint segment;
    internal uint address;
    internal uint offset;
    internal uint length;

    internal PdbScope(uint address, uint length, PdbSlot[] slots, PdbConstant[] constants, string[] usedNamespaces) {
      this.constants = constants;
      this.slots = slots;
      this.scopes = new PdbScope[0];
      this.usedNamespaces = usedNamespaces;
      this.address = address;
      this.offset = 0;
      this.length = length;
    }

    internal PdbScope(uint funcOffset, BlockSym32 block, BitAccess bits, out uint typind) {
      //this.segment = block.seg;
      this.address = block.off;
      this.offset = block.off - funcOffset;
      this.length = block.len;
      typind = 0;

      int constantCount;
      int scopeCount;
      int slotCount;
      int namespaceCount;
      PdbFunction.CountScopesAndSlots(bits, block.end, out constantCount, out scopeCount, out slotCount, out namespaceCount);
      constants = new PdbConstant[constantCount];
      scopes = new PdbScope[scopeCount];
      slots = new PdbSlot[slotCount];
      usedNamespaces = new string[namespaceCount];
      int constant = 0;
      int scope = 0;
      int slot = 0;
      int usedNs = 0;

      while (bits.Position < block.end) {
        ushort siz;
        ushort rec;

        bits.ReadUInt16(out siz);
        int star = bits.Position;
        int stop = bits.Position + siz;
        bits.Position = star;
        bits.ReadUInt16(out rec);

        switch ((SYM)rec) {
          case SYM.S_BLOCK32: {
              BlockSym32 sub = new BlockSym32();

              bits.ReadUInt32(out sub.parent);
              bits.ReadUInt32(out sub.end);
              bits.ReadUInt32(out sub.len);
              bits.ReadUInt32(out sub.off);
              bits.ReadUInt16(out sub.seg);
              bits.SkipCString(out sub.name);

              bits.Position = stop;
              scopes[scope++] = new PdbScope(funcOffset, sub, bits, out typind);
              break;
            }

          case SYM.S_MANSLOT:
            slots[slot++] = new PdbSlot(bits, out typind);
            bits.Position = stop;
            break;

          case SYM.S_UNAMESPACE:
            bits.ReadCString(out usedNamespaces[usedNs++]);
            bits.Position = stop;
            break;

          case SYM.S_END:
            bits.Position = stop;
            break;

          case SYM.S_MANCONSTANT:
            constants[constant++] = new PdbConstant(bits);
            bits.Position = stop;
            break;

          default:
            //throw new PdbException("Unknown SYM in scope {0}", (SYM)rec);
            bits.Position = stop;
            break;
        }
      }

      if (bits.Position != block.end) {
        throw new Exception("Not at S_END");
      }

      ushort esiz;
      ushort erec;
      bits.ReadUInt16(out esiz);
      bits.ReadUInt16(out erec);

      if (erec != (ushort)SYM.S_END) {
        throw new Exception("Missing S_END");
      }
    }
  }
}
