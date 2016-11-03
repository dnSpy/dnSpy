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

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using Emit = dnlib.DotNet.Emit;

namespace dnSpy.AsmEditor.Commands {
	sealed class EditedMethodBodyUpdater {
		public IEnumerable<DocumentTreeNodeData> OriginalNodes {
			get { yield return ownerNode; }
		}

		struct BodyState {
			readonly Emit.MethodBody body;
			readonly MethodImplAttributes implAttributes;
			readonly CustomAttribute[] customAttributes;
			readonly bool isBodyModified;

			public BodyState(MethodDef method, bool isBodyModified) {
				this.body = method.MethodBody;
				this.implAttributes = method.ImplAttributes;
				this.customAttributes = method.CustomAttributes.ToArray();
				this.isBodyModified = isBodyModified;
			}

			public BodyState(Emit.MethodBody body, MethodImplAttributes implAttributes, CustomAttribute[] customAttributes, bool isBodyModified) {
				this.body = body;
				this.implAttributes = implAttributes;
				this.customAttributes = customAttributes;
				this.isBodyModified = isBodyModified;
			}

			public void CopyTo(MethodDef method, IMethodAnnotations methodAnnotations) {
				method.MethodBody = body;
				method.ImplAttributes = implAttributes;
				method.CustomAttributes.Clear();
				method.CustomAttributes.AddRange(customAttributes);
				methodAnnotations.SetBodyModified(method, isBodyModified);
			}
		}

		readonly Lazy<IMethodAnnotations> methodAnnotations;
		readonly MethodNode ownerNode;
		readonly MethodDef method;
		readonly BodyState originalBodyState;
		readonly BodyState newBodyState;

		public EditedMethodBodyUpdater(Lazy<IMethodAnnotations> methodAnnotations, ModuleDocumentNode modNode, MethodDef originalMethod, Emit.MethodBody newBody, MethodImplAttributes newImplAttributes, CustomAttribute[] newCustomAttributes) {
			this.methodAnnotations = methodAnnotations;
			this.ownerNode = modNode.Context.DocumentTreeView.FindNode(originalMethod);
			if (ownerNode == null)
				throw new InvalidOperationException();
			this.method = originalMethod;
			this.originalBodyState = new BodyState(originalMethod, methodAnnotations.Value.IsBodyModified(method));
			this.newBodyState = new BodyState(newBody, newImplAttributes, newCustomAttributes, true);
		}

		public void Add() => newBodyState.CopyTo(method, methodAnnotations.Value);
		public void Remove() => originalBodyState.CopyTo(method, methodAnnotations.Value);
	}
}
