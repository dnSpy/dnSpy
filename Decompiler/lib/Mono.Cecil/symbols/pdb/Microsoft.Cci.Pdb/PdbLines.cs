//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;

namespace Microsoft.Cci.Pdb {
  internal class PdbLines {
    internal PdbSource file;
    internal PdbLine[] lines;

    internal PdbLines(PdbSource file, uint count) {
      this.file = file;
      this.lines = new PdbLine[count];
    }
  }
}
