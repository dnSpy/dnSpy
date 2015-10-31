// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.AvalonEdit;
using dnSpy.Images;
using ICSharpCode.ILSpy.TextView;

namespace ICSharpCode.ILSpy.AvalonEdit {
	#region Context menu extensibility
	public interface IIconBarContextMenuEntry : IContextMenuEntry<IIconBarObject> {
	}
	public interface IIconBarContextMenuEntry2 : IIconBarContextMenuEntry, IContextMenuEntry2<IIconBarObject> {
	}

	public interface IconBarContextMenuEntryMetadata {
		string Icon { get; }
		string Header { get; }
		string Category { get; }
		string InputGestureText { get; }
		double Order { get; }
	}

	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class ExportIconBarContextMenuEntryAttribute : ExportAttribute, IconBarContextMenuEntryMetadata {
		public ExportIconBarContextMenuEntryAttribute()
			: base(typeof(IIconBarContextMenuEntry)) {
		}

		public string Icon { get; set; }
		public string Header { get; set; }
		public string Category { get; set; }
		public string InputGestureText { get; set; }
		public double Order { get; set; }
	}
	#endregion

	#region Actions (simple clicks) - this will be used for creating bookmarks (e.g. Breakpoint bookmarks)

	public interface IIconBarActionEntry {
		bool IsEnabled(DecompilerTextView textView);
		void Execute(DecompilerTextView textView, int line);
	}

	public interface IIconBarActionMetadata {
		string Category { get; }

		double Order { get; }
	}

	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class ExportIconBarActionEntryAttribute : ExportAttribute, IIconBarActionMetadata {
		public ExportIconBarActionEntryAttribute()
			: base(typeof(IIconBarActionEntry)) {
		}

		public string Icon { get; set; }
		public string Header { get; set; }
		public string Category { get; set; }
		public double Order { get; set; }
	}

	#endregion

	internal class IconMarginActionsProvider {
		/// <summary>
		/// Enables extensible context menu support for the specified icon margin.
		/// </summary>
		public static void Add(IconBarMargin margin, DecompilerTextView textView) {
			var provider = new IconMarginActionsProvider(margin, textView);
			margin.MouseUp += provider.HandleMouseEvent;
			margin.ContextMenu = new ContextMenu();
		}

		readonly IconBarMargin margin;
		readonly DecompilerTextView textView;

		// Prevent big memory leaks (text editor) because the data is put into some MEF data structure.
		// All created instances in this class are shared so this one can be shared as well.
		sealed class MefState {
			public static readonly MefState Instance = new MefState();

			MefState() {
				App.CompositionContainer.ComposeParts(this);
			}

			[ImportMany(typeof(IIconBarContextMenuEntry))]
			public Lazy<IIconBarContextMenuEntry, IconBarContextMenuEntryMetadata>[] contextEntries = null;

			[ImportMany(typeof(IIconBarActionEntry))]
			public Lazy<IIconBarActionEntry, IIconBarActionMetadata>[] actionEntries = null;
		}

		private IconMarginActionsProvider(IconBarMargin margin, DecompilerTextView textView) {
			this.margin = margin;
			this.textView = textView;
		}

		List<IIconBarObject> GetIconBarObjects(IList<IIconBarObject> objects, int line) {
			var list = new List<IIconBarObject>();
			foreach (var obj in objects) {
				if (obj.GetLineNumber(textView) != line)
					continue;
				if (!obj.HasImage)
					continue;
				list.Add(obj);
			}
			list.Sort((a, b) => b.ZOrder.CompareTo(a.ZOrder));
			return list;
		}

		void HandleMouseEvent(object sender, MouseButtonEventArgs e) {
			int line = margin.GetLineFromMousePosition(e);

			if (e.ChangedButton == MouseButton.Left) {
				foreach (var category in MefState.Instance.actionEntries.OrderBy(c => c.Metadata.Order).GroupBy(c => c.Metadata.Category)) {
					foreach (var entryPair in category) {
						IIconBarActionEntry entry = entryPair.Value;

						if (entryPair.Value.IsEnabled(textView)) {
							entry.Execute(textView, line);
						}
					}
				}
			}

			// context menu entries
			var objects = new List<IIconBarObject>(TextLineObjectManager.Instance.GetObjectsOfType<IIconBarObject>());
			if (objects.Count == 0) {
				// don't show the menu
				e.Handled = true;
				this.margin.ContextMenu = null;
				return;
			}

			if (e.ChangedButton == MouseButton.Right) {
				// check if we are on a Member
				var filteredObjects = GetIconBarObjects(objects, line);
				if (filteredObjects.Count == 0) {
					// don't show the menu
					e.Handled = true;
					this.margin.ContextMenu = null;
					return;
				}

				foreach (var bjmark in filteredObjects) {
					ContextMenu menu = new ContextMenu();
					foreach (var category in MefState.Instance.contextEntries.OrderBy(c => c.Metadata.Order).GroupBy(c => c.Metadata.Category)) {
						bool hasAddedSep = menu.Items.Count == 0;
						foreach (var entryPair in category) {
							IIconBarContextMenuEntry entry = entryPair.Value;
							if (entry.IsVisible(bjmark)) {
								if (!hasAddedSep) {
									menu.Items.Add(new Separator());
									hasAddedSep = true;
								}

								MenuItem menuItem = new MenuItem();
								menuItem.Header = entryPair.Metadata.Header;
								bool isEnabled;
								if (entryPair.Value.IsEnabled(bjmark)) {
									menuItem.Click += delegate { entry.Execute(bjmark); };
									isEnabled = true;
								}
								else {
									menuItem.IsEnabled = false;
									isEnabled = false;
								}
								if (!string.IsNullOrEmpty(entryPair.Metadata.Icon))
									MainWindow.CreateMenuItemImage(menuItem, entry, entryPair.Metadata.Icon, BackgroundType.ContextMenuItem, isEnabled);
								menuItem.InputGestureText = entryPair.Metadata.InputGestureText ?? string.Empty;
								var entry2 = entry as IIconBarContextMenuEntry2;
								if (entry2 != null)
									entry2.Initialize(bjmark, menuItem);
								menu.Items.Add(menuItem);
							}
						}
					}
					if (menu.Items.Count > 0) {
						margin.ContextMenu = menu;
						return;
					}
				}
				// hide the context menu.
				e.Handled = true;
			}
		}
	}
}
