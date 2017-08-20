/*
    Copyright (C) 2017 HoLLy

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

using System;
using System.Collections.Generic;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	/// <summary>
	/// Searches methods that are overridden by the analyzed method.
	/// </summary>
	sealed class MethodOverriddenNode : SearchNode {
		readonly MethodDef analyzedMethod;

		public MethodOverriddenNode(MethodDef analyzedMethod) {
			if (analyzedMethod == null)
				throw new ArgumentNullException(nameof(analyzedMethod));

			this.analyzedMethod = analyzedMethod;
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.OverridesTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			//note: only goes up 1 level
			AnalyzerTreeNodeData newNode = null;
			try {
				//get base type (if any)
				if (analyzedMethod.DeclaringType.BaseType == null) {
					yield break;
				}
				ITypeDefOrRef baseType = analyzedMethod.DeclaringType.BaseType;

				while (baseType != null) { 
					//only typedef has a Methods property
					if (baseType is TypeDef def) {
						foreach (var method in def.Methods) {
							if (TypesHierarchyHelpers.IsBaseMethod(method, analyzedMethod)) {
								bool hidesParent = !method.IsVirtual ^ method.IsNewSlot;
								newNode = new MethodNode(method, hidesParent) {Context = Context};
								break; //there can be only one
							}
						}
						//escape from the while loop if we have a match (cannot yield return in try/catch)
						if (newNode != null)
							break;

						baseType = def.BaseType;
					}
					else {
						//try to resolve the TypeRef
						//will be null if resolving failed
						baseType = baseType.Resolve();
					}
				}
			}
			catch (ResolveException) {
				//ignored
			}
			if (newNode != null)
				yield return newNode;
		}

		public static bool CanShow(MethodDef method) =>
			method.DeclaringType.BaseType != null &&
			method.IsVirtual &&
			!method.DeclaringType.IsInterface;
	}
}
