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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Menus;

namespace dnSpy.Files.Tabs.TextEditor {
	interface IIconBarCommandManager {
		void Initialize(IIconBarMargin iconBarMargin);
	}

	sealed class IconBarCommandContext : IIconBarCommandContext {
		public int Line { get; private set; }
		public ITextEditorUIContext UIContext { get; private set; }

		public IconBarCommandContext(ITextEditorUIContext uiContext, int line) {
			this.UIContext = uiContext;
			this.Line = line;
		}
	}

	[Export, Export(typeof(IIconBarCommandManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class IconBarCommandManager : IIconBarCommandManager {
		readonly IMenuManager menuManager;
		readonly ITextLineObjectManager textLineObjectManager;
		readonly Lazy<IIconBarCommand>[] iconBarCommands;

		[ImportingConstructor]
		IconBarCommandManager(IMenuManager menuManager, ITextLineObjectManager textLineObjectManager, [ImportMany] IEnumerable<Lazy<IIconBarCommand>> iconBarCommands) {
			this.menuManager = menuManager;
			this.textLineObjectManager = textLineObjectManager;
			this.iconBarCommands = iconBarCommands.ToArray();
		}

		public void Initialize(IIconBarMargin iconBarMargin) {
			iconBarMargin.FrameworkElement.MouseLeftButtonUp += (s, e) => {
				IconBarMargin_MouseLeftButtonUp(iconBarMargin);
				e.Handled = true;
			};
			menuManager.InitializeContextMenu(iconBarMargin.FrameworkElement, new Guid(MenuConstants.GUIDOBJ_TEXTEDITOR_ICONBAR_GUID), new GuidObjectsCreator(textLineObjectManager), null, new Guid(MenuConstants.TEXTEDITOR_ICONBAR_GUID));
		}

		void IconBarMargin_MouseLeftButtonUp(IIconBarMargin iconBarMargin) {
			var line = iconBarMargin.GetLineFromMousePosition();
			if (line == null)
				return;
			var ctx = new IconBarCommandContext(iconBarMargin.UIContext, line.Value);
			foreach (var cmd in iconBarCommands) {
				if (cmd.Value.IsEnabled(ctx))
					cmd.Value.Execute(ctx);
			}
		}

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly ITextLineObjectManager textLineObjectManager;

			public GuidObjectsCreator(ITextLineObjectManager textLineObjectManager) {
				this.textLineObjectManager = textLineObjectManager;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObject creatorObject, bool openedFromKeyboard) {
				var iconBarMargin = (IIconBarMargin)creatorObject.Object;
				yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTEDITORUICONTEXT_GUID, iconBarMargin.UIContext);

				var line = iconBarMargin.GetLineFromMousePosition();
				if (line != null) {
					var objects = new List<IIconBarObject>(textLineObjectManager.GetObjectsOfType<IIconBarObject>());
					var filteredObjects = GetIconBarObjects(objects, iconBarMargin.UIContext, line.Value);
					foreach (var o in filteredObjects)
						yield return new GuidObject(MenuConstants.GUIDOBJ_IICONBAROBJECT_GUID, o);
				}
			}

			static List<IIconBarObject> GetIconBarObjects(IList<IIconBarObject> objects, ITextEditorUIContext uiContext, int line) {
				var list = new List<IIconBarObject>();
				foreach (var obj in objects) {
					if (obj.GetLineNumber(uiContext) != line)
						continue;
					if (obj.ImageReference == null)
						continue;
					list.Add(obj);
				}
				list.Sort((a, b) => b.ZOrder.CompareTo(a.ZOrder));
				return list;
			}
		}
	}
}
