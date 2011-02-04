//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.IO;

namespace Microsoft.Cci.Pdb {
  internal class PdbException : IOException {
    internal PdbException(String format, params object[] args)
      : base(String.Format(format, args)) {
    }
  }
}
