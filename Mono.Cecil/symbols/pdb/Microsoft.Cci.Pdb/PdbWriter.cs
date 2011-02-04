//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.IO;

namespace Microsoft.Cci.Pdb {
  internal class PdbWriter {
    internal PdbWriter(Stream writer, int pageSize) {
      this.pageSize = pageSize;
      this.usedBytes = pageSize * 3;
      this.writer = writer;

      writer.SetLength(usedBytes);
    }

    internal void WriteMeta(DataStream[] streams, BitAccess bits) {
      PdbFileHeader head = new PdbFileHeader(pageSize);

      WriteDirectory(streams,
                     out head.directoryRoot,
                     out head.directorySize,
                     bits);
      WriteFreeMap();

      head.freePageMap = 2;
      head.pagesUsed = usedBytes / pageSize;

      writer.Seek(0, SeekOrigin.Begin);
      head.Write(writer, bits);
    }

    private void WriteDirectory(DataStream[] streams,
                                out int directoryRoot,
                                out int directorySize,
                                BitAccess bits) {
      DataStream directory = new DataStream();

      int pages = 0;
      for (int s = 0; s < streams.Length; s++) {
        if (streams[s].Length > 0) {
          pages += streams[s].Pages;
        }
      }

      int use = 4 * (1 + streams.Length + pages);
      bits.MinCapacity(use);
      bits.WriteInt32(streams.Length);
      for (int s = 0; s < streams.Length; s++) {
        bits.WriteInt32(streams[s].Length);
      }
      for (int s = 0; s < streams.Length; s++) {
        if (streams[s].Length > 0) {
          bits.WriteInt32(streams[s].pages);
        }
      }
      directory.Write(this, bits.Buffer, use);
      directorySize = directory.Length;

      use = 4 * directory.Pages;
      bits.MinCapacity(use);
      bits.WriteInt32(directory.pages);

      DataStream ddir = new DataStream();
      ddir.Write(this, bits.Buffer, use);

      directoryRoot = ddir.pages[0];
    }

    private void WriteFreeMap() {
      byte[] buffer = new byte[pageSize];

      // We configure the old free map with only the first 3 pages allocated.
      buffer[0] = 0xf8;
      for (int i = 1; i < pageSize; i++) {
        buffer[i] = 0xff;
      }
      Seek(1, 0);
      Write(buffer, 0, pageSize);

      // We configure the new free map with all of the used pages gone.
      int count = usedBytes / pageSize;
      int full = count / 8;
      for (int i = 0; i < full; i++) {
        buffer[i] = 0;
      }
      int rema = count % 8;
      buffer[full] = (byte)(0xff << rema);

      Seek(2, 0);
      Write(buffer, 0, pageSize);
    }

    internal int AllocatePages(int count) {
      int begin = usedBytes;

      usedBytes += count * pageSize;
      writer.SetLength(usedBytes);

      if (usedBytes > pageSize * pageSize * 8) {
        throw new Exception("PdbWriter does not support multiple free maps.");
      }
      return begin / pageSize;
    }

    internal void Seek(int page, int offset) {
      writer.Seek(page * pageSize + offset, SeekOrigin.Begin);
    }

    internal void Write(byte[] bytes, int offset, int count) {
      writer.Write(bytes, offset, count);
    }

    //////////////////////////////////////////////////////////////////////
    //
    internal int PageSize {
      get { return pageSize; }
    }

    //////////////////////////////////////////////////////////////////////
    //
    internal readonly int pageSize;
    private Stream writer;
    private int usedBytes;
  }

}
