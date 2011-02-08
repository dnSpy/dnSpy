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

  internal sealed class UsedNamespace : IUsedNamespace {

    internal UsedNamespace(IName alias, IName namespaceName) {
      this.alias = alias;
      this.namespaceName = namespaceName;
    }

    public IName Alias {
      get { return this.alias; }
    }
    readonly IName alias;

    public IName NamespaceName {
      get { return this.namespaceName; }
    }
    readonly IName namespaceName;

  }

  internal class NamespaceScope : INamespaceScope {

    internal NamespaceScope(IEnumerable<IUsedNamespace> usedNamespaces) {
      this.usedNamespaces = usedNamespaces;
    }

    public IEnumerable<IUsedNamespace> UsedNamespaces {
      get { return this.usedNamespaces; }
    }
    readonly IEnumerable<IUsedNamespace> usedNamespaces;

  }

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