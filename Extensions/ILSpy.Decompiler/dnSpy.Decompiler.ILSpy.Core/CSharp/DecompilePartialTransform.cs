/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.CSharp;

namespace dnSpy.Decompiler.ILSpy.CSharp {
	sealed class DecompilePartialTransform : IAstTransform {
		readonly TypeDef type;
		readonly HashSet<IMemberDef> definitions;
		readonly bool showDefinitions;
		readonly bool addPartialKeyword;
		readonly HashSet<ITypeDefOrRef> ifacesToRemove;

		public DecompilePartialTransform(TypeDef type, HashSet<IMemberDef> definitions, bool showDefinitions, bool addPartialKeyword, IEnumerable<ITypeDefOrRef> ifacesToRemove) {
			this.type = type;
			this.definitions = definitions;
			this.showDefinitions = showDefinitions;
			this.addPartialKeyword = addPartialKeyword;
			this.ifacesToRemove = new HashSet<ITypeDefOrRef>(ifacesToRemove, TypeEqualityComparer.Instance);
		}

		public void Run(AstNode compilationUnit) {
			foreach (var en in compilationUnit.Descendants.OfType<EntityDeclaration>()) {
				var def = en.Annotation<IMemberDef>();
				Debug.Assert(def != null);
				if (def == null)
					continue;
				if (def == type) {
					var tdecl = en as TypeDeclaration;
					Debug.Assert(tdecl != null);
					if (tdecl != null) {
						if (addPartialKeyword)
							tdecl.Modifiers |= Modifiers.Partial;
						foreach (var iface in tdecl.BaseTypes) {
							var tdr = iface.Annotation<ITypeDefOrRef>();
							if (tdr != null && ifacesToRemove.Contains(tdr))
								iface.Remove();
						}
					}
				}
				else {
					if (showDefinitions) {
						if (!definitions.Contains(def))
							en.Remove();
					}
					else {
						if (definitions.Contains(def))
							en.Remove();
					}
				}
			}
		}
	}
}
