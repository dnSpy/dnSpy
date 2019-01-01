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

		public MethodOverriddenNode(MethodDef analyzedMethod) =>
			this.analyzedMethod = analyzedMethod ?? throw new ArgumentNullException(nameof(analyzedMethod));

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.OverridesTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			var type = analyzedMethod.DeclaringType.BaseType.ResolveTypeDef();
			while (type != null) {
				foreach (var method in type.Methods) {
					if (TypesHierarchyHelpers.IsBaseMethod(method, analyzedMethod)) {
						yield return new MethodNode(method) { Context = Context };
						yield break;
					}
				}
				type = type.BaseType.ResolveTypeDef();
			}
		}

		public static bool CanShow(MethodDef method) =>
			method.DeclaringType.BaseType != null &&
			method.IsVirtual &&
			!method.DeclaringType.IsInterface;
	}
}
