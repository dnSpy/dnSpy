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
  internal struct PdbLine {
    internal uint offset;
    internal uint lineBegin;
    internal uint lineEnd;
    internal ushort colBegin;
    internal ushort colEnd;

    internal PdbLine(uint offset, uint lineBegin, ushort colBegin, uint lineEnd, ushort colEnd) {
      this.offset = offset;
      this.lineBegin = lineBegin;
      this.colBegin = colBegin;
      this.lineEnd = lineEnd;
      this.colEnd = colEnd;
    }
  }
}
