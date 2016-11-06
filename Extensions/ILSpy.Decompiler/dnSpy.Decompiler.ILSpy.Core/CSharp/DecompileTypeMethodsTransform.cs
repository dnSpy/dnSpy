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
using ICSharpCode.NRefactory.TypeSystem;

namespace dnSpy.Decompiler.ILSpy.Core.CSharp {
	sealed class DecompileTypeMethodsTransform : IAstTransform {
		readonly HashSet<IMemberDef> defsToShow;
		readonly HashSet<TypeDef> partialTypes;
		readonly bool showDefinitions;
		readonly bool makeEverythingPublic;
		readonly bool showAll;

		public DecompileTypeMethodsTransform(HashSet<MethodDef> methods, bool showDefinitions, bool makeEverythingPublic, bool showAll) {
			this.defsToShow = new HashSet<IMemberDef>();
			this.partialTypes = new HashSet<TypeDef>();
			this.showDefinitions = showDefinitions;
			this.makeEverythingPublic = makeEverythingPublic;
			this.showAll = showAll;

			foreach (var method in methods) {
				// If it's part of a property or event, include the property or event since there are no partial props/events
				var prop = method.DeclaringType.Properties.FirstOrDefault(a => a.GetMethods.Contains(method) || a.SetMethods.Contains(method));
				if (prop != null) {
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
					if (evt != null) {
						defsToShow.Add(evt);
						if (evt.AddMethod != null)
							defsToShow.Add(evt.AddMethod);
						if (evt.RemoveMethod != null)
							defsToShow.Add(evt.RemoveMethod);
						if (evt.InvokeMethod != null)
							defsToShow.Add(evt.InvokeMethod);
						foreach (var m in evt.OtherMethods)
							defsToShow.Add(m);
					}
					else
						defsToShow.Add(method);
				}
			}

			foreach (var def in defsToShow) {
				for (var declType = def.DeclaringType; declType != null; declType = declType.DeclaringType)
					partialTypes.Add(declType);
			}
		}

		public void Run(AstNode compilationUnit) {
			foreach (var en in compilationUnit.Descendants.OfType<EntityDeclaration>()) {
				var def = en.Annotation<IMemberDef>();
				Debug.Assert(def != null);
				if (def == null)
					continue;

				// The decompiler doesn't remove IteratorStateMachineAttributes/AsyncStateMachineAttribute.
				// These attributes usually contain the name of a type with invalid characters in its name
				// and will prevent the user from compiling the code.
				if (en.SymbolKind == SymbolKind.Method) {
					foreach (var sect in en.Attributes) {
						foreach (var attr in sect.Attributes) {
							var ca = attr.Annotation<CustomAttribute>();
							var fn = ca?.TypeFullName;
							if (fn == "System.Runtime.CompilerServices.IteratorStateMachineAttribute" ||
								fn == "System.Runtime.CompilerServices.AsyncStateMachineAttribute")
								attr.Remove();
						}
						if (!sect.Attributes.Any())
							sect.Remove();
					}
				}

				if (makeEverythingPublic) {
					const Modifiers accessFlags = Modifiers.Private | Modifiers.Internal | Modifiers.Protected | Modifiers.Public;
					en.Modifiers = (en.Modifiers & ~accessFlags) | Modifiers.Public;

					bool clearModifiers = false;

					var owner = en.Parent as TypeDeclaration;
					if (owner?.ClassType == ClassType.Enum || owner?.ClassType == ClassType.Interface)
						clearModifiers = true;
					else if (en is Accessor) {
						// If it's a getter/setter/adder/remover, its owner (the property/event) already is public,
						// so remove the modifier from the accessor
						clearModifiers = true;
					}
					else if (en.SymbolKind == SymbolKind.Destructor)
						clearModifiers = true;
					else if (en.SymbolKind == SymbolKind.Constructor && en.HasModifier(Modifiers.Static))
						clearModifiers = true;
					else if (en is MethodDeclaration) {
						var md = (MethodDeclaration)en;
						if (!md.PrivateImplementationType.IsNull || (md.Parent as TypeDeclaration)?.ClassType == ClassType.Interface)
							clearModifiers = true;
					}
					else if (en is CustomEventDeclaration) {
						var ed = (CustomEventDeclaration)en;
						if (!ed.PrivateImplementationType.IsNull || (ed.Parent as TypeDeclaration)?.ClassType == ClassType.Interface)
							clearModifiers = true;
					}
					else if (en is PropertyDeclaration) {
						var pd = (PropertyDeclaration)en;
						if (!pd.PrivateImplementationType.IsNull || (pd.Parent as TypeDeclaration)?.ClassType == ClassType.Interface)
							clearModifiers = true;
					}

					if (clearModifiers)
						en.Modifiers &= ~accessFlags;
				}

				if (partialTypes.Contains(def)) {
					var tdecl = en as TypeDeclaration;
					Debug.Assert(tdecl != null);
					if (tdecl != null) {
						tdecl.Modifiers |= Modifiers.Partial;
						if (!showDefinitions) {
							tdecl.BaseTypes.Clear();
							tdecl.Attributes.Clear();
						}
					}
				}
				else {
					// The decompiler doesn't support Roslyn yet so remove this since
					// it will break compilation
					if (def is TypeDef && def.Name == "<>c")
						en.Remove();

					if (showDefinitions) {
						if (!showAll && !defsToShow.Contains(def))
							en.Remove();
					}
					else {
						if (showAll || defsToShow.Contains(def))
							en.Remove();
						else if (en is CustomEventDeclaration) {
							// Convert this hidden event to an event without accessor bodies.
							// AstBuilder doesn't write empty bodies to it if it's a hidden event because
							// then it can't be optimized to an auto event. We want real auto events to
							// become auto events and custom events to stay custom, but without bodies.

							var ced = (CustomEventDeclaration)en;
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
