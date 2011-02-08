using System;
using System.Collections.Generic;

namespace Microsoft.Cci {

  /// <summary>
  /// A range of CLR IL operations that comprise a lexical scope, specified as an IL offset and a length.
  /// </summary>
  public interface ILocalScope {
    /// <summary>
    /// The offset of the first operation in the scope.
    /// </summary>
    uint Offset { get; }

    /// <summary>
    /// The length of the scope. Offset+Length equals the offset of the first operation outside the scope, or equals the method body length.
    /// </summary>
    uint Length { get; }
  }

  /// <summary>
  /// A description of the lexical scope in which a namespace type has been nested. This scope is tied to a particular
  /// method body, so that partial types can be accommodated.
  /// </summary>
  public interface INamespaceScope {

    /// <summary>
    /// Zero or more used namespaces. These correspond to using clauses in C#.
    /// </summary>
    IEnumerable<IUsedNamespace> UsedNamespaces { get; }

  }


  /// <summary>
  /// A namespace that is used (imported) inside a namespace scope.
  /// </summary>
  public interface IUsedNamespace {
    /// <summary>
    /// An alias for a namespace. For example the "x" of "using x = y.z;" in C#. Empty if no alias is present.
    /// </summary>
    IName Alias { get; }

    /// <summary>
    /// The name of a namepace that has been aliased.  For example the "y.z" of "using x = y.z;" or "using y.z" in C#.
    /// </summary>
    IName NamespaceName { get; }
  }

  /// <summary>
  /// The name of an entity. Typically name instances come from a common pool. Within the pool no two distinct instances will have the same Value or UniqueKey.
  /// </summary>
  public interface IName {
    /// <summary>
    /// An integer that is unique within the pool from which the name instance has been allocated. Useful as a hashtable key.
    /// </summary>
    int UniqueKey {
      get;
        //^ ensures result > 0;
    }

    /// <summary>
    /// An integer that is unique within the pool from which the name instance has been allocated. Useful as a hashtable key.
    /// All name instances in the pool that have the same string value when ignoring the case of the characters in the string
    /// will have the same key value.
    /// </summary>
    int UniqueKeyIgnoringCase {
      get;
      //^ ensures result > 0;
    }

    /// <summary>
    /// The string value corresponding to this name.
    /// </summary>
    string Value { get; }
  }
}