//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.Pdb;
using System.Text;
using System.Diagnostics.SymbolStore;

namespace Microsoft.Cci {

  internal sealed class PdbIteratorScope : ILocalScope {

    internal PdbIteratorScope(uint offset, uint length) {
      this.offset = offset;
      this.length = length;
    }

    public uint Offset {
      get { return this.offset; }
    }
    uint offset;

    public uint Length {
      get { return this.length; }
    }
    uint length;
  }
}