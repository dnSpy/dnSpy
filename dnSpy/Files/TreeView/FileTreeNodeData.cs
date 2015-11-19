/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Shared.UI.Highlighting;
using dnSpy.Shared.UI.TreeView;

namespace dnSpy.Files.TreeView {
	abstract class FileTreeNodeData : TreeNodeData, IFileTreeNodeData {
		public sealed override bool SingleClickExpandsChildren {
			get { return Context.SingleClickExpandsChildren; }
		}

		public IFileTreeNodeDataContext Context { get; set; }

		public abstract NodePathName NodePathName { get; }

		protected abstract ImageReference GetIcon(IDotNetImageManager dnImgMgr);
		protected virtual ImageReference? GetExpandedIcon(IDotNetImageManager dnImgMgr) {
			return null;
		}

		public sealed override ImageReference Icon {
			get { return GetIcon(this.Context.FileTreeView.DotNetImageManager); }
		}

		public sealed override ImageReference? ExpandedIcon {
			get { return GetExpandedIcon(this.Context.FileTreeView.DotNetImageManager); }
		}

		public sealed override object Text {
			get {
				var gen = UISyntaxHighlighter.Create(Context.SyntaxHighlight);

				var cached = cachedText != null ? cachedText.Target : null;
				if (cached != null)
					return cached;

				Write(gen.SyntaxHighlightOutput, Context.Language);

				var text = gen.CreateTextBlock(filterOutNewLines: true);
				cachedText = new WeakReference(text);
				return text;
			}
		}
		WeakReference cachedText;

		protected abstract void Write(ISyntaxHighlightOutput output, ILanguage language);

		protected virtual void WriteToolTip(ISyntaxHighlightOutput output, ILanguage language) {
			Write(output, language);
		}

		public sealed override object ToolTip {
			get {
				var gen = UISyntaxHighlighter.Create(Context.SyntaxHighlight);

				var cached = cachedToolTip != null ? cachedToolTip.Target : null;
				if (cached != null)
					return cached;

				WriteToolTip(gen.SyntaxHighlightOutput, Context.Language);

				var text = gen.CreateTextBlock(filterOutNewLines: false);
				cachedToolTip = new WeakReference(text);
				return text;
			}
		}
		WeakReference cachedToolTip;

		public sealed override string ToString() {
			return ToString(Context.Language);
		}

		public string ToString(ILanguage language) {
			var output = new NoSyntaxHighlightOutput();
			Write(output, language);
			return output.ToString();
		}

		public sealed override void OnRefreshUI() {
			cachedToolTip = null;
			cachedText = null;
		}
	}
}
