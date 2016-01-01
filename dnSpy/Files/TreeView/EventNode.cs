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
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.UI.Files.TreeView;

namespace dnSpy.Files.TreeView {
	sealed class EventNode : FileTreeNodeData, IEventNode {
		public EventDef EventDef {
			get { return @event; }
		}
		readonly EventDef @event;

		public override Guid Guid {
			get { return new Guid(FileTVConstants.EVENT_NODE_GUID); }
		}

		public override NodePathName NodePathName {
			get { return new NodePathName(Guid, @event.FullName); }
		}

		IMDTokenProvider IMDTokenNode.Reference {
			get { return @event; }
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			return dnImgMgr.GetImageReference(@event);
		}

		public override ITreeNodeGroup TreeNodeGroup {
			get { return treeNodeGroup; }
		}
		readonly ITreeNodeGroup treeNodeGroup;

		public EventNode(ITreeNodeGroup treeNodeGroup, EventDef @event) {
			this.treeNodeGroup = treeNodeGroup;
			this.@event = @event;
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			new NodePrinter().Write(output, language, @event, Context.ShowToken);
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			if (@event.AddMethod != null)
				yield return new MethodNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.MethodTreeNodeGroupEvent), @event.AddMethod);
			if (@event.RemoveMethod != null)
				yield return new MethodNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.MethodTreeNodeGroupEvent), @event.RemoveMethod);
			if (@event.InvokeMethod != null)
				yield return new MethodNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.MethodTreeNodeGroupEvent), @event.InvokeMethod);
			foreach (var m in @event.OtherMethods)
				yield return new MethodNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.MethodTreeNodeGroupEvent), m);
		}

		public IMethodNode Create(MethodDef method) {
			return Context.FileTreeView.CreateEvent(method);
		}

		public override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			var res = filter.GetResult(this.EventDef);
			if (res.FilterType != FilterType.Default)
				return res.FilterType;
			if (Context.Language.ShowMember(this.EventDef, Context.DecompilerSettings))
				return FilterType.Visible;
			return FilterType.Hide;
		}
	}
}
