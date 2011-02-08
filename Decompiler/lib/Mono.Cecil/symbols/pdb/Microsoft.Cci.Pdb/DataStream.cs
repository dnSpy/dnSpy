//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.IO;

namespace Microsoft.Cci.Pdb {
  internal class DataStream {
    internal DataStream() {
      this.contentSize = 0;
      this.pages = null;
    }

    internal DataStream(int contentSize, BitAccess bits, int count) {
      this.contentSize = contentSize;
      if (count > 0) {
        this.pages = new int[count];
        bits.ReadInt32(this.pages);
      }
    }

    internal void Read(PdbReader reader, BitAccess bits) {
      bits.MinCapacity(contentSize);
      Read(reader, 0, bits.Buffer, 0, contentSize);
    }

    internal void Read(PdbReader reader, int position,
                     byte[] bytes, int offset, int data) {
      if (position + data > contentSize) {
        throw new PdbException("DataStream can't read off end of stream. " +
                                       "(pos={0},siz={1})",
                               position, data);
      }
      if (position == contentSize) {
        return;
      }

      int left = data;
      int page = position / reader.pageSize;
      int rema = position % reader.pageSize;

      // First get remained of first page.
      if (rema != 0) {
        int todo = reader.pageSize - rema;
        if (todo > left) {
          todo = left;
        }

        reader.Seek(pages[page], rema);
        reader.Read(bytes, offset, todo);

        offset += todo;
        left -= todo;
        page++;
      }

      // Now get the remaining pages.
      while (left > 0) {
        int todo = reader.pageSize;
        if (todo > left) {
          todo = left;
        }

        reader.Seek(pages[page], 0);
        reader.Read(bytes, offset, todo);

        offset += todo;
        left -= todo;
        page++;
      }
    }

    internal void Write(PdbWriter writer, byte[] bytes) {
      Write(writer, bytes, bytes.Length);
    }

    internal void Write(PdbWriter writer, byte[] bytes, int data) {
      if (bytes == null || data == 0) {
        return;
      }

      int left = data;
      int used = 0;
      int rema = contentSize % writer.pageSize;
      if (rema != 0) {
        int todo = writer.pageSize - rema;
        if (todo > left) {
          todo = left;
        }

        int lastPage = pages[pages.Length - 1];
        writer.Seek(lastPage, rema);
        writer.Write(bytes, used, todo);
        used += todo;
        left -= todo;
      }

      if (left > 0) {
        int count = (left + writer.pageSize - 1) / writer.pageSize;
        int page0 = writer.AllocatePages(count);

        writer.Seek(page0, 0);
        writer.Write(bytes, used, left);

        AddPages(page0, count);
      }

      contentSize += data;
    }

    private void AddPages(int page0, int count) {
      if (pages == null) {
        pages = new int[count];
        for (int i = 0; i < count; i++) {
          pages[i] = page0 + i;
        }
      } else {
        int[] old = pages;
        int used = old.Length;

        pages = new int[used + count];
        Array.Copy(old, pages, used);
        for (int i = 0; i < count; i++) {
          pages[used + i] = page0 + i;
        }
      }
    }

    internal int Pages {
      get { return pages == null ? 0 : pages.Length; }
    }

    internal int Length {
      get { return contentSize; }
    }

    internal int GetPage(int index) {
      return pages[index];
    }

    internal int contentSize;
    internal int[] pages;
  }
}
