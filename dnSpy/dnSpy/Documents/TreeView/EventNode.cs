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
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Documents.TreeView {
	sealed class EventNode : DocumentTreeNodeData, IEventNode {
		public EventDef EventDef { get; }
		public override Guid Guid => new Guid(DocumentTreeViewConstants.EVENT_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, EventDef.FullName);
		IMDTokenProvider IMDTokenNode.Reference => EventDef;
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => dnImgMgr.GetImageReference(EventDef);
		public override ITreeNodeGroup TreeNodeGroup { get; }

		public EventNode(ITreeNodeGroup treeNodeGroup, EventDef @event) {
			this.TreeNodeGroup = treeNodeGroup;
			this.EventDef = @event;
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			new NodePrinter().Write(output, decompiler, EventDef, Context.ShowToken);

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			if (EventDef.AddMethod != null)
				yield return new MethodNode(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.MethodTreeNodeGroupEvent), EventDef.AddMethod);
			if (EventDef.RemoveMethod != null)
				yield return new MethodNode(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.MethodTreeNodeGroupEvent), EventDef.RemoveMethod);
			if (EventDef.InvokeMethod != null)
				yield return new MethodNode(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.MethodTreeNodeGroupEvent), EventDef.InvokeMethod);
			foreach (var m in EventDef.OtherMethods)
				yield return new MethodNode(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.MethodTreeNodeGroupEvent), m);
		}

		public IMethodNode Create(MethodDef method) => Context.DocumentTreeView.CreateEvent(method);

		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) {
			var res = filter.GetResult(this.EventDef);
			if (res.FilterType != FilterType.Default)
				return res.FilterType;
			if (Context.Decompiler.ShowMember(this.EventDef))
				return FilterType.Visible;
			return FilterType.Hide;
		}
	}
}
