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
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;

namespace dnSpy.Debugger.Evaluation.UI {
	abstract class VariablesWindowToolWindowContentProviderBase : IToolWindowContentProvider {
		public TWContent[] Contents => contents;
		readonly TWContent[] contents;

		public sealed class TWContent {
			public int Index { get; }
			public Guid Guid { get; }
			public string Title { get; }
			public AppToolWindowLocation DefaultLocation => AppToolWindowLocation.DefaultHorizontal;

			public VariablesWindowToolWindowContent Content {
				get {
					if (content is null)
						content = createContent();
					return content;
				}
			}
			VariablesWindowToolWindowContent? content;

			readonly Func<VariablesWindowToolWindowContent> createContent;

			public TWContent(int windowIndex, Guid guid, string title, Func<VariablesWindowToolWindowContent> createContent) {
				Index = windowIndex;
				Guid = guid;
				Title = title;
				this.createContent = createContent;
			}
		}

		readonly double contentOrder;

		protected VariablesWindowToolWindowContentProviderBase(int maxWindows, Guid contentGuid, double contentOrder) {
			this.contentOrder = contentOrder;
			contents = new TWContent[maxWindows];
			var guidString = contentGuid.ToString();
			Debug.Assert(guidString.Length == 36);
			var guidBase = guidString.Substring(0, 36 - 8);
			uint lastDword = uint.Parse(guidString.Substring(36 - 8), NumberStyles.HexNumber);
			for (int i = 0; i < contents.Length; i++) {
				var tmpIndex = i;
				var guid = new Guid(guidBase + (lastDword + (uint)i).ToString("X8"));
				var title = GetWindowTitle(i);
				contents[i] = new TWContent(i, guid, title, () => CreateContent(tmpIndex));
			}
		}

		VariablesWindowToolWindowContent CreateContent(int index) {
			var content = contents[index];
			return new VariablesWindowToolWindowContent(content.Guid, content.Title, CreateVariablesWindowContent(index));
		}

		protected abstract string GetWindowTitle(int windowIndex);
		protected abstract Lazy<IVariablesWindowContent> CreateVariablesWindowContent(int windowIndex);

		public Guid GetWindowGuid(int index) => contents[index].Guid;

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get {
				for (int i = 0; i < contents.Length; i++) {
					var info = contents[i];
					yield return new ToolWindowContentInfo(info.Guid, info.DefaultLocation, contentOrder + (double)i / contents.Length, false);
				}
			}
		}

		public ToolWindowContent? GetOrCreate(Guid guid) {
			foreach (var info in contents) {
				if (info.Guid == guid)
					return info.Content;
			}
			return null;
		}
	}

	sealed class VariablesWindowToolWindowContent : ToolWindowContent, IFocusable {
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override IInputElement? FocusedElement => variablesWindowContent.Value.FocusedElement;
		public override FrameworkElement? ZoomElement => variablesWindowContent.Value.ZoomElement;
		public override Guid Guid { get; }
		public override string Title { get; }
		public override object? UIObject => variablesWindowContent.Value.UIObject;
		public bool CanFocus => true;

		readonly Lazy<IVariablesWindowContent> variablesWindowContent;

		public VariablesWindowToolWindowContent(Guid guid, string contentTitle, Lazy<IVariablesWindowContent> variablesWindowContent) {
			Guid = guid;
			Title = contentTitle;
			this.variablesWindowContent = variablesWindowContent;
		}

		public void Focus() => variablesWindowContent.Value.Focus();

		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				variablesWindowContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				variablesWindowContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				variablesWindowContent.Value.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				variablesWindowContent.Value.OnHidden();
				break;
			}
		}
	}
}
