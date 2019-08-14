/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dnSpy.Decompiler.ILSpy.Core.CSharp {
	sealed class DecompileTypeMethodsTransform : IAstTransform {
		readonly HashSet<IMemberDef> defsToShow;
		readonly HashSet<TypeDef> partialTypes;
		readonly bool showDefinitions;
		readonly bool showAll;

		public DecompileTypeMethodsTransform(HashSet<TypeDef> types, HashSet<MethodDef> methods, bool showDefinitions, bool showAll) {
			defsToShow = new HashSet<IMemberDef>();
			partialTypes = new HashSet<TypeDef>();
			this.showDefinitions = showDefinitions;
			this.showAll = showAll;

			foreach (var method in methods) {
				// If it's part of a property or event, include the property or event since there are no partial props/events
				var prop = method.DeclaringType.Properties.FirstOrDefault(a => a.GetMethods.Contains(method) || a.SetMethods.Contains(method));
				if (!(prop is null)) {
					defsToShow.Add(prop);
					foreach (var m in prop.GetMethods)
						defsToShow.Add(m);
					foreach (var m in prop.SetMethods)
						defsToShow.Add(m);
					foreach (var m in prop.OtherMethods)
						defsToShow.Add(m);
				}
				else {
					var evt = method.DeclaringType.Events.FirstOrDefault(a => a.AddMethod == method || a.RemoveMethod == method);
					if (!(evt is null)) {
						defsToShow.Add(evt);
						if (!(evt.AddMethod is null))
							defsToShow.Add(evt.AddMethod);
						if (!(evt.RemoveMethod is null))
							defsToShow.Add(evt.RemoveMethod);
						if (!(evt.InvokeMethod is null))
							defsToShow.Add(evt.InvokeMethod);
						foreach (var m in evt.OtherMethods)
							defsToShow.Add(m);
					}
					else
						defsToShow.Add(method);
				}
			}
			foreach (var type in types) {
				if (!type.IsEnum) {
					defsToShow.Add(type);
					partialTypes.Add(type);
				}
			}
			foreach (var def in defsToShow) {
				for (var declType = def.DeclaringType; !(declType is null); declType = declType.DeclaringType)
					partialTypes.Add(declType);
			}
			foreach (var type in types) {
				if (type.IsEnum) {
					defsToShow.Add(type);
					foreach (var f in type.Fields)
						defsToShow.Add(f);
				}
			}
		}

		public void Run(AstNode compilationUnit) {
			foreach (var en in compilationUnit.Descendants.OfType<EntityDeclaration>()) {
				var def = en.Annotation<IMemberDef>();
				Debug2.Assert(!(def is null));
				if (def is null)
					continue;

				if (partialTypes.Contains(def)) {
					var tdecl = en as TypeDeclaration;
					Debug2.Assert(!(tdecl is null));
					if (!(tdecl is null)) {
						if (tdecl.ClassType != ClassType.Enum)
							tdecl.Modifiers |= Modifiers.Partial;
						if (!showDefinitions) {
							tdecl.BaseTypes.Clear();
							tdecl.Attributes.Clear();
						}

						// Make sure the comments are still shown before the method and its modifiers
						var comments = en.GetChildrenByRole(Roles.Comment).Reverse().ToArray();
						foreach (var c in comments) {
							c.Remove();
							en.InsertChildAfter(null, c, Roles.Comment);
						}
					}
				}
				else {
					if (showDefinitions) {
						if (!showAll && !defsToShow.Contains(def))
							en.Remove();
					}
					else {
						if (showAll || defsToShow.Contains(def))
							en.Remove();
						else if (en is CustomEventDeclaration ced) {
							// Convert this hidden event to an event without accessor bodies.
							// AstBuilder doesn't write empty bodies to it if it's a hidden event because
							// then it can't be optimized to an auto event. We want real auto events to
							// become auto events and custom events to stay custom, but without bodies.
							if (!ced.AddAccessor.IsNull)
								ced.AddAccessor.Body = new BlockStatement();
							if (!ced.RemoveAccessor.IsNull)
								ced.RemoveAccessor.Body = new BlockStatement();
						}
					}
				}
			}
		}
	}
}
