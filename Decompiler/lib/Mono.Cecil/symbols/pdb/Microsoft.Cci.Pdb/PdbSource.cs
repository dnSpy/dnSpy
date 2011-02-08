//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;

namespace Microsoft.Cci.Pdb {
  internal class PdbSource {
    internal uint index;
    internal string name;
    internal Guid doctype;
    internal Guid language;
    internal Guid vendor;

    internal PdbSource(uint index, string name, Guid doctype, Guid language, Guid vendor) {
      this.index = index;
      this.name = name;
      this.doctype = doctype;
      this.language = language;
      this.vendor = vendor;
    }
  }
}
