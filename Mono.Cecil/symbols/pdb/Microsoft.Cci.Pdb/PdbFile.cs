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
using System.Collections.Generic;
using System.IO;
using System.Diagnostics.SymbolStore;

namespace Microsoft.Cci.Pdb {
  internal class PdbFile {
    private PdbFile()   // This class can't be instantiated.
    {
    }

    static void LoadGuidStream(BitAccess bits, out Guid doctype, out Guid language, out Guid vendor) {
      bits.ReadGuid(out language);
      bits.ReadGuid(out vendor);
      bits.ReadGuid(out doctype);
    }

    static Dictionary<string, int> LoadNameIndex(BitAccess bits, out int age, out Guid guid) {
      Dictionary<string, int> result = new Dictionary<string, int>();
      int ver;
      int sig;
      bits.ReadInt32(out ver);    //  0..3  Version
      bits.ReadInt32(out sig);    //  4..7  Signature
      bits.ReadInt32(out age);    //  8..11 Age
      bits.ReadGuid(out guid);       // 12..27 GUID

      //if (ver != 20000404) {
      //  throw new PdbDebugException("Unsupported PDB Stream version {0}", ver);
      //}

      // Read string buffer.
      int buf;
      bits.ReadInt32(out buf);    // 28..31 Bytes of Strings

      int beg = bits.Position;
      int nxt = bits.Position + buf;

      bits.Position = nxt;

      // Read map index.
      int cnt;        // n+0..3 hash size.
      int max;        // n+4..7 maximum ni.

      bits.ReadInt32(out cnt);
      bits.ReadInt32(out max);

      BitSet present = new BitSet(bits);
      BitSet deleted = new BitSet(bits);
      if (!deleted.IsEmpty) {
        throw new PdbDebugException("Unsupported PDB deleted bitset is not empty.");
      }

      int j = 0;
      for (int i = 0; i < max; i++) {
        if (present.IsSet(i)) {
          int ns;
          int ni;
          bits.ReadInt32(out ns);
          bits.ReadInt32(out ni);

          string name;
          int saved = bits.Position;
          bits.Position = beg + ns;
          bits.ReadCString(out name);
          bits.Position = saved;

          result.Add(name.ToUpperInvariant(), ni);
          j++;
        }
      }
      if (j != cnt) {
        throw new PdbDebugException("Count mismatch. ({0} != {1})", j, cnt);
      }
      return result;
    }

    static IntHashTable LoadNameStream(BitAccess bits) {
      IntHashTable ht = new IntHashTable();

      uint sig;
      int ver;
      bits.ReadUInt32(out sig);   //  0..3  Signature
      bits.ReadInt32(out ver);    //  4..7  Version

      // Read (or skip) string buffer.
      int buf;
      bits.ReadInt32(out buf);    //  8..11 Bytes of Strings

      if (sig != 0xeffeeffe || ver != 1) {
        throw new PdbDebugException("Unsupported Name Stream version. "+
                                            "(sig={0:x8}, ver={1})",
                                    sig, ver);
      }
      int beg = bits.Position;
      int nxt = bits.Position + buf;
      bits.Position = nxt;

      // Read hash table.
      int siz;
      bits.ReadInt32(out siz);    // n+0..3 Number of hash buckets.
      nxt = bits.Position;

      for (int i = 0; i < siz; i++) {
        int ni;
        string name;

        bits.ReadInt32(out ni);

        if (ni != 0) {
          int saved = bits.Position;
          bits.Position = beg + ni;
          bits.ReadCString(out name);
          bits.Position = saved;

          ht.Add(ni, name);
        }
      }
      bits.Position = nxt;

      return ht;
    }

    private static PdbFunction match = new PdbFunction();

    private static int FindFunction(PdbFunction[] funcs, ushort sec, uint off) {
      match.segment = sec;
      match.address = off;

      return Array.BinarySearch(funcs, match, PdbFunction.byAddress);
    }

    static void LoadManagedLines(PdbFunction[] funcs,
                                 IntHashTable names,
                                 BitAccess bits,
                                 MsfDirectory dir,
                                 Dictionary<string, int> nameIndex,
                                 PdbReader reader,
                                 uint limit) {
      Array.Sort(funcs, PdbFunction.byAddressAndToken);
      IntHashTable checks = new IntHashTable();

      // Read the files first
      int begin = bits.Position;
      while (bits.Position < limit) {
        int sig;
        int siz;
        bits.ReadInt32(out sig);
        bits.ReadInt32(out siz);
        int place = bits.Position;
        int endSym = bits.Position + siz;

        switch ((DEBUG_S_SUBSECTION)sig) {
          case DEBUG_S_SUBSECTION.FILECHKSMS:
            while (bits.Position < endSym) {
              CV_FileCheckSum chk;

              int ni = bits.Position - place;
              bits.ReadUInt32(out chk.name);
              bits.ReadUInt8(out chk.len);
              bits.ReadUInt8(out chk.type);

              string name = (string)names[(int)chk.name];
              int guidStream;
              Guid doctypeGuid = SymDocumentType.Text;
              Guid languageGuid = Guid.Empty;
              Guid vendorGuid = Guid.Empty;
              if (nameIndex.TryGetValue("/SRC/FILES/"+name.ToUpperInvariant(), out guidStream)) {
                var guidBits = new BitAccess(0x100);
                dir.streams[guidStream].Read(reader, guidBits);
                LoadGuidStream(guidBits, out doctypeGuid, out languageGuid, out vendorGuid);
              }

              PdbSource src = new PdbSource(/*(uint)ni,*/ name, doctypeGuid, languageGuid, vendorGuid);
              checks.Add(ni, src);
              bits.Position += chk.len;
              bits.Align(4);
            }
            bits.Position = endSym;
            break;

          default:
            bits.Position = endSym;
            break;
        }
      }

      // Read the lines next.
      bits.Position = begin;
      while (bits.Position < limit) {
        int sig;
        int siz;
        bits.ReadInt32(out sig);
        bits.ReadInt32(out siz);
        int endSym = bits.Position + siz;

        switch ((DEBUG_S_SUBSECTION)sig) {
          case DEBUG_S_SUBSECTION.LINES: {
              CV_LineSection sec;

              bits.ReadUInt32(out sec.off);
              bits.ReadUInt16(out sec.sec);
              bits.ReadUInt16(out sec.flags);
              bits.ReadUInt32(out sec.cod);
              int funcIndex = FindFunction(funcs, sec.sec, sec.off);
              if (funcIndex < 0) break;
              var func = funcs[funcIndex];
              if (func.lines == null) {
                while (funcIndex > 0) {
                  var f = funcs[funcIndex-1];
                  if (f.lines != null || f.segment != sec.sec || f.address != sec.off) break;
                  func = f;
                  funcIndex--;
                }
             } else {
                while (funcIndex < funcs.Length-1 && func.lines != null) {
                  var f = funcs[funcIndex+1];
                  if (f.segment != sec.sec || f.address != sec.off) break;
                  func = f;
                  funcIndex++;
                }
              }
              if (func.lines != null) break;

              // Count the line blocks.
              int begSym = bits.Position;
              int blocks = 0;
              while (bits.Position < endSym) {
                CV_SourceFile file;
                bits.ReadUInt32(out file.index);
                bits.ReadUInt32(out file.count);
                bits.ReadUInt32(out file.linsiz);   // Size of payload.
                int linsiz = (int)file.count * (8 + ((sec.flags & 1) != 0 ? 4 : 0));
                bits.Position += linsiz;
                blocks++;
              }

              func.lines = new PdbLines[blocks];
              int block = 0;

              bits.Position = begSym;
              while (bits.Position < endSym) {
                CV_SourceFile file;
                bits.ReadUInt32(out file.index);
                bits.ReadUInt32(out file.count);
                bits.ReadUInt32(out file.linsiz);   // Size of payload.

                PdbSource src = (PdbSource)checks[(int)file.index];
                PdbLines tmp = new PdbLines(src, file.count);
                func.lines[block++] = tmp;
                PdbLine[] lines = tmp.lines;

                int plin = bits.Position;
                int pcol = bits.Position + 8 * (int)file.count;

                for (int i = 0; i < file.count; i++) {
                  CV_Line line;
                  CV_Column column = new CV_Column();

                  bits.Position = plin + 8 * i;
                  bits.ReadUInt32(out line.offset);
                  bits.ReadUInt32(out line.flags);

                  uint lineBegin = line.flags & (uint)CV_Line_Flags.linenumStart;
                  uint delta = (line.flags & (uint)CV_Line_Flags.deltaLineEnd) >> 24;
                  //bool statement = ((line.flags & (uint)CV_Line_Flags.fStatement) == 0);
                  if ((sec.flags & 1) != 0) {
                    bits.Position = pcol + 4 * i;
                    bits.ReadUInt16(out column.offColumnStart);
                    bits.ReadUInt16(out column.offColumnEnd);
                  }

                  lines[i] = new PdbLine(line.offset,
                                         lineBegin,
                                         column.offColumnStart,
                                         lineBegin+delta,
                                         column.offColumnEnd);
                }
              }
              break;
            }
        }
        bits.Position = endSym;
      }
    }

    static void LoadFuncsFromDbiModule(BitAccess bits,
                                       DbiModuleInfo info,
                                       IntHashTable names,
                                       ArrayList funcList,
                                       bool readStrings,
                                       MsfDirectory dir,
                                       Dictionary<string, int> nameIndex,
                                       PdbReader reader) {
      PdbFunction[] funcs = null;

      bits.Position = 0;
      int sig;
      bits.ReadInt32(out sig);
      if (sig != 4) {
        throw new PdbDebugException("Invalid signature. (sig={0})", sig);
      }

      bits.Position = 4;
      // Console.WriteLine("{0}:", info.moduleName);
      funcs = PdbFunction.LoadManagedFunctions(/*info.moduleName,*/
                                               bits, (uint)info.cbSyms,
                                               readStrings);
      if (funcs != null) {
        bits.Position = info.cbSyms + info.cbOldLines;
        LoadManagedLines(funcs, names, bits, dir, nameIndex, reader,
                         (uint)(info.cbSyms + info.cbOldLines + info.cbLines));

        for (int i = 0; i < funcs.Length; i++) {
          funcList.Add(funcs[i]);
        }
      }
    }

    static void LoadDbiStream(BitAccess bits,
                              out DbiModuleInfo[] modules,
                              out DbiDbgHdr header,
                              bool readStrings) {
      DbiHeader dh = new DbiHeader(bits);
      header = new DbiDbgHdr();

      //if (dh.sig != -1 || dh.ver != 19990903) {
      //  throw new PdbException("Unsupported DBI Stream version, sig={0}, ver={1}",
      //                         dh.sig, dh.ver);
      //}

      // Read gpmod section.
      ArrayList modList = new ArrayList();
      int end = bits.Position + dh.gpmodiSize;
      while (bits.Position < end) {
        DbiModuleInfo mod = new DbiModuleInfo(bits, readStrings);
        modList.Add(mod);
      }
      if (bits.Position != end) {
        throw new PdbDebugException("Error reading DBI stream, pos={0} != {1}",
                                    bits.Position, end);
      }

      if (modList.Count > 0) {
        modules = (DbiModuleInfo[])modList.ToArray(typeof(DbiModuleInfo));
      } else {
        modules = null;
      }

      // Skip the Section Contribution substream.
      bits.Position += dh.secconSize;

      // Skip the Section Map substream.
      bits.Position += dh.secmapSize;

      // Skip the File Info substream.
      bits.Position += dh.filinfSize;

      // Skip the TSM substream.
      bits.Position += dh.tsmapSize;

      // Skip the EC substream.
      bits.Position += dh.ecinfoSize;

      // Read the optional header.
      end = bits.Position + dh.dbghdrSize;
      if (dh.dbghdrSize > 0) {
        header = new DbiDbgHdr(bits);
      }
      bits.Position = end;
    }

    internal static PdbFunction[] LoadFunctions(Stream read, bool readAllStrings, out int age, out Guid guid) {
      BitAccess bits = new BitAccess(512 * 1024);
      return LoadFunctions(read, bits, readAllStrings, out age, out guid);
    }

    internal static PdbFunction[] LoadFunctions(Stream read, BitAccess bits, bool readAllStrings, out int age, out Guid guid) {
      PdbFileHeader head = new PdbFileHeader(read, bits);
      PdbReader reader = new PdbReader(read, head.pageSize);
      MsfDirectory dir = new MsfDirectory(reader, head, bits);
      DbiModuleInfo[] modules = null;
      DbiDbgHdr header;

      dir.streams[1].Read(reader, bits);
      Dictionary<string, int> nameIndex = LoadNameIndex(bits, out age, out guid);
      int nameStream;
      if (!nameIndex.TryGetValue("/NAMES", out nameStream)) {
        throw new PdbException("No `name' stream");
      }

      dir.streams[nameStream].Read(reader, bits);
      IntHashTable names = LoadNameStream(bits);

      dir.streams[3].Read(reader, bits);
      LoadDbiStream(bits, out modules, out header, readAllStrings);

      ArrayList funcList = new ArrayList();

      if (modules != null) {
        for (int m = 0; m < modules.Length; m++) {
          if (modules[m].stream > 0) {
            dir.streams[modules[m].stream].Read(reader, bits);
            LoadFuncsFromDbiModule(bits, modules[m], names, funcList,
                                   readAllStrings, dir, nameIndex, reader);
          }
        }
      }

      PdbFunction[] funcs = (PdbFunction[])funcList.ToArray(typeof(PdbFunction));

      // After reading the functions, apply the token remapping table if it exists.
      if (header.snTokenRidMap != 0 && header.snTokenRidMap != 0xffff) {
        dir.streams[header.snTokenRidMap].Read(reader, bits);
        uint[] ridMap = new uint[dir.streams[header.snTokenRidMap].Length / 4];
        bits.ReadUInt32(ridMap);

        foreach (PdbFunction func in funcs) {
          func.token = 0x06000000 | ridMap[func.token & 0xffffff];
        }
      }

      //
      Array.Sort(funcs, PdbFunction.byAddressAndToken);
      //Array.Sort(funcs, PdbFunction.byToken);
      return funcs;
    }
  }
}
