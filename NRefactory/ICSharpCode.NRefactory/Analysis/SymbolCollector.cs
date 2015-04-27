// Copyright (c) 2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;

namespace ICSharpCode.NRefactory.Analysis
{
	/// <summary>
	/// The symbol collector collects related symbols that form a group of symbols that should be renamed
	/// when a name of one symbol changes. For example if a type definition name should be changed
	/// the constructors and destructor names should change as well.
	/// </summary>
	public class SymbolCollector
	{
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="ICSharpCode.NRefactory.Analysis.SymbolCollector"/> should include overloads.
		/// </summary>
		/// <value><c>true</c> if overloads should be included; otherwise, <c>false</c>.</value>
		public bool IncludeOverloads { 
			get;
			set;
		}

		public bool GroupForRenaming {
			get;
			set;
		}

		static IEnumerable<ISymbol> CollectTypeRelatedMembers (ITypeDefinition type)
		{
			yield return type;
			foreach (var c in type.GetDefinition ().GetMembers (m => !m.IsSynthetic && (m.SymbolKind == SymbolKind.Constructor || m.SymbolKind == SymbolKind.Destructor), GetMemberOptions.IgnoreInheritedMembers)) {
				yield return c;
			}
		}

		static IEnumerable<ISymbol> CollectOverloads (IMethod method)
		{
			return method.DeclaringType
				.GetMethods (m => m.Name == method.Name)
				.Where (m => m != method);
		}

		static IMember SearchMember (ITypeDefinition derivedType, IMember method)
		{
			foreach (var m in derivedType.Members) {
				if (m.ImplementedInterfaceMembers.Contains (method))
					return m;
			}
			return null;
		}

		static IEnumerable<ISymbol> MakeUnique (List<ISymbol> symbols)
		{
			HashSet<ISymbol> taken = new HashSet<ISymbol> ();
			foreach (var sym in symbols) {
				if (taken.Contains (sym))
					continue;
				taken.Add (sym);
				yield return sym;
			}
		}

		/// <summary>
		/// Gets the related symbols.
		/// </summary>
		/// <returns>The related symbols.</returns>
		/// <param name="g">The type graph.</param>
		/// <param name="m">The symbol to search</param>
		public IEnumerable<ISymbol> GetRelatedSymbols(Lazy<TypeGraph> g, ISymbol m)
		{
			switch (m.SymbolKind) {
			case SymbolKind.TypeDefinition:
				return CollectTypeRelatedMembers ((ITypeDefinition)m);
	
			case SymbolKind.Field:
			case SymbolKind.Operator:
			case SymbolKind.Variable:
			case SymbolKind.Parameter:
			case SymbolKind.TypeParameter:
				return new ISymbol[] { m };
	
			case SymbolKind.Constructor:
				if (GroupForRenaming)
					return GetRelatedSymbols (g, ((IMethod)m).DeclaringTypeDefinition);
				List<ISymbol> constructorSymbols = new List<ISymbol> ();
				if (IncludeOverloads) {
					foreach (var m3 in CollectOverloads ((IMethod)m)) {
						constructorSymbols.Add (m3);
					}
				}
				return constructorSymbols;

			case SymbolKind.Destructor:
				if (GroupForRenaming)
					return GetRelatedSymbols (g, ((IMethod)m).DeclaringTypeDefinition);
				return new ISymbol[] { m };

			case SymbolKind.Indexer:
			case SymbolKind.Event:
			case SymbolKind.Property:
			case SymbolKind.Method: {
				var member = (IMember)m;
				List<ISymbol> symbols = new List<ISymbol> ();
				if (!member.IsExplicitInterfaceImplementation)
					symbols.Add (member);
				if (GroupForRenaming) {
					foreach (var m2 in member.ImplementedInterfaceMembers) {
						symbols.AddRange (GetRelatedSymbols (g, m2));
					}
				} else {
					symbols.AddRange(member.ImplementedInterfaceMembers);
				}

				if (member.DeclaringType.Kind == TypeKind.Interface) {
					var declaringTypeNode = g.Value.GetNode(member.DeclaringTypeDefinition);
					if (declaringTypeNode != null) {
						foreach (var derivedType in declaringTypeNode.DerivedTypes) {
							var mem = SearchMember (derivedType.TypeDefinition, member);
							if (mem != null)
								symbols.Add (mem);
						}
					}
				}


				if (IncludeOverloads) {
					IncludeOverloads = false;
					if (member is IMethod) {
						foreach (var m3 in CollectOverloads ((IMethod)member)) {
							symbols.AddRange (GetRelatedSymbols (g, m3));
						}
					} else if (member.SymbolKind == SymbolKind.Indexer) {
						symbols.AddRange (member.DeclaringTypeDefinition.GetProperties (p => p.IsIndexer));
					}
				}
				return MakeUnique (symbols);
			}
			case SymbolKind.Namespace:
				// TODO?
				return new ISymbol[] { m };
			default:
				throw new ArgumentOutOfRangeException ("symbol:"+m.SymbolKind);
			}
		}
	}
}
