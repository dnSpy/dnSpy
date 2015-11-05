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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts;
using dnSpy.Contracts.Images;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.TreeNodes.Hex {
	public abstract class HexTreeNode : ILSpyTreeNode {
		protected abstract IEnumerable<HexVM> HexVMs { get; }
		protected abstract object ViewObject { get; }

		protected virtual bool IsVirtualizingCollectionVM {
			get { return false; }
		}

		public ulong StartOffset {
			get { return startOffset; }
		}
		readonly ulong startOffset;

		public ulong EndOffset {
			get { return endOffset; }
		}
		readonly ulong endOffset;

		public override object Icon {
			get { return Globals.App.ImageManager.GetImage(GetType().Assembly, IconName, BackgroundType.TreeNode); }
		}

		protected abstract string IconName { get; }

		protected HexTreeNode(ulong start, ulong end) {
			this.startOffset = start;
			this.endOffset = end;
		}

		public sealed override FilterResult Filter(FilterSettings settings) {
			var res = settings.Filter.GetFilterResult(this);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			return base.Filter(settings);
		}

		public sealed override object GetViewObject(DecompilerTextView textView) {
			var obj = uiObjRef == null ? null : (FrameworkElement)uiObjRef.Target;
			// The element is cached but could be opened in two different tab groups. Only return
			// the cached one if it's not in use.
			if (obj != null && obj.Parent == null)
				return obj;

			FrameworkElement newObj;
			if (IsVirtualizingCollectionVM)
				newObj = new ContentPresenter() { Content = ViewObject, Focusable = true };
			else {
				newObj = new ScrollViewer {
					CanContentScroll = true,
					HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
					VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
					Content = ViewObject,
					Focusable = true,
				};
			};

			if (uiObjRef == null)
				uiObjRef = new WeakReference(newObj);
			else
				uiObjRef.Target = newObj;
			return newObj;
		}
		WeakReference uiObjRef;

		public sealed override void Decompile(Language language, ITextOutput output, DecompilationOptions options) {
			language.WriteCommentLine(output, string.Format("{0:X8} - {1:X8} {2}", StartOffset, EndOffset, this.ToString()));
			DecompileFields(language, output);
			var smartOutput = output as ISmartTextOutput;
			if (smartOutput != null)
				smartOutput.MarkAsNonCached();
		}

		protected virtual void DecompileFields(Language language, ITextOutput output) {
			foreach (var vm in HexVMs) {
				language.WriteCommentLine(output, string.Empty);
				language.WriteCommentLine(output, string.Format("{0}:", vm.Name));
				foreach (var field in vm.HexFields)
					language.WriteCommentLine(output, string.Format("{0:X8} - {1:X8} {2} = {3}", field.StartOffset, field.EndOffset, field.FormattedValue, field.Name));
			}
		}

		protected sealed override void Write(ITextOutput output, Language language) {
			Write(output);
		}

		protected abstract void Write(ITextOutput output);

		public virtual void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			if (!HexUtils.IsModified(StartOffset, EndOffset, modifiedStart, modifiedEnd))
				return;

			foreach (var vm in HexVMs)
				vm.OnDocumentModified(modifiedStart, modifiedEnd);
		}
	}
}
