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

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnSpy.AsmEditor.Method;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using Emit = dnlib.DotNet.Emit;

namespace dnSpy.AsmEditor.Commands {
	sealed class EditedMethodUpdater {
		public IEnumerable<DocumentTreeNodeData> OriginalNodes {
			get { yield return ownerNode; }
		}

		readonly struct MethodState {
			readonly Emit.MethodBody body;
			readonly MethodDefOptions methodDefOptions;
			readonly bool isBodyModified;

			public MethodState(MethodDef method, bool isBodyModified) {
				body = method.MethodBody;
				methodDefOptions = new MethodDefOptions(method);
				this.isBodyModified = isBodyModified;
			}

			public MethodState(Emit.MethodBody body, MethodDefOptions methodDefOptions, bool isBodyModified) {
				this.body = body;
				this.methodDefOptions = methodDefOptions;
				this.isBodyModified = isBodyModified;
			}

			public void CopyTo(MethodDef method, IMethodAnnotations methodAnnotations) {
				method.MethodBody = body;
				methodDefOptions.CopyTo(method);
				methodAnnotations.SetBodyModified(method, isBodyModified);
			}
		}

		readonly Lazy<IMethodAnnotations> methodAnnotations;
		readonly MethodNode ownerNode;
		readonly MethodDef method;
		readonly MethodState originalMethodState;
		readonly MethodState newMethodState;

		public EditedMethodUpdater(Lazy<IMethodAnnotations> methodAnnotations, ModuleDocumentNode modNode, MethodDef originalMethod, Emit.MethodBody newBody, MethodDefOptions methodDefOptions) {
			this.methodAnnotations = methodAnnotations;
			ownerNode = modNode.Context.DocumentTreeView.FindNode(originalMethod);
			if (ownerNode == null)
				throw new InvalidOperationException();
			method = originalMethod;
			originalMethodState = new MethodState(originalMethod, methodAnnotations.Value.IsBodyModified(method));
			newMethodState = new MethodState(newBody, methodDefOptions, true);
		}

		public void Add() => newMethodState.CopyTo(method, methodAnnotations.Value);
		public void Remove() => originalMethodState.CopyTo(method, methodAnnotations.Value);
	}
}
