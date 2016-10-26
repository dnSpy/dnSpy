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
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.Commands {
	sealed class EventNodeCreator {
		public IEnumerable<DocumentTreeNodeData> OriginalNodes {
			get { yield return ownerNode; }
		}

		readonly TypeNode ownerNode;
		readonly EventNode eventNode;

		public EventNodeCreator(ModuleDocumentNode modNode, TypeNode ownerNode, EventDef @event) {
			this.ownerNode = ownerNode;
			this.eventNode = modNode.Context.DocumentTreeView.Create(@event);
		}

		IEnumerable<MethodDef> GetMethods() {
			if (eventNode.EventDef.AddMethod != null)
				yield return eventNode.EventDef.AddMethod;
			if (eventNode.EventDef.RemoveMethod != null)
				yield return eventNode.EventDef.RemoveMethod;
			if (eventNode.EventDef.InvokeMethod != null)
				yield return eventNode.EventDef.InvokeMethod;
			foreach (var m in eventNode.EventDef.OtherMethods)
				yield return m;
		}

		public void Add() {
			ownerNode.TreeNode.EnsureChildrenLoaded();
			ownerNode.TypeDef.Events.Add(eventNode.EventDef);
			ownerNode.TypeDef.Methods.AddRange(GetMethods());
			ownerNode.TreeNode.AddChild(eventNode.TreeNode);
		}

		public void Remove() {
			bool b = ownerNode.TreeNode.Children.Remove(eventNode.TreeNode);
			if (b) {
				foreach (var m in GetMethods())
					b = b && ownerNode.TypeDef.Methods.Remove(m);
			}
			b = b && ownerNode.TypeDef.Events.Remove(eventNode.EventDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}
	}
}
