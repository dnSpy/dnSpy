//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.IO;

namespace Microsoft.Cci.Pdb {
  internal class PdbDebugException : IOException {
    internal PdbDebugException(String format, params object[] args)
      : base(String.Format(format, args)) {
    }
  }
}
