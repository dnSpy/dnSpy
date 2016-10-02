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
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Properties;

namespace dnSpy.Output {
	[Export(typeof(IMainToolWindowContentProvider))]
	sealed class OutputToolWindowContentProvider : IMainToolWindowContentProvider {
		readonly Lazy<IOutputContent> outputContent;

		public OutputToolWindowContent OutputToolWindowContent => outputToolWindowContent ?? (outputToolWindowContent = new OutputToolWindowContent(outputContent));
		OutputToolWindowContent outputToolWindowContent;

		[ImportingConstructor]
		OutputToolWindowContentProvider(Lazy<IOutputContent> outputContent) {
			this.outputContent = outputContent;
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(OutputToolWindowContent.THE_GUID, OutputToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_OUTPUT, false); }
		}

		public IToolWindowContent GetOrCreate(Guid guid) => guid == OutputToolWindowContent.THE_GUID ? OutputToolWindowContent : null;
	}

	sealed class OutputToolWindowContent : IToolWindowContent, IZoomable {
		public static readonly Guid THE_GUID = new Guid("90A45E97-727E-4F31-8692-06E19218D99A");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public IInputElement FocusedElement => outputContent.Value.FocusedElement;
		public FrameworkElement ZoomElement => outputContent.Value.ZoomElement;
		public Guid Guid => THE_GUID;
		public string Title => dnSpy_Resources.Window_Output;
		public object ToolTip => null;
		public object UIObject => outputContent.Value.UIObject;
		double IZoomable.ZoomValue => outputContent.Value.ZoomLevel / 100.0;

		readonly Lazy<IOutputContent> outputContent;

		public OutputToolWindowContent(Lazy<IOutputContent> outputContent) {
			this.outputContent = outputContent;
		}

		public void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				outputContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				outputContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				outputContent.Value.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				outputContent.Value.OnHidden();
				break;
			}
		}
	}

	[ExportAutoLoaded]
	sealed class ShowOutputWindowCommandLoader : IAutoLoaded {
		public static readonly RoutedCommand ShowOutputWindowRoutedCommand = new RoutedCommand("ShowOutputWindowRoutedCommand", typeof(ShowOutputWindowCommandLoader));

		[ImportingConstructor]
		ShowOutputWindowCommandLoader(IWpfCommandService wpfCommandService, IDsToolWindowService toolWindowService) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
			cmds.Add(ShowOutputWindowRoutedCommand,
				(s, e) => toolWindowService.Show(OutputToolWindowContent.THE_GUID),
				(s, e) => e.CanExecute = true);
			cmds.Add(ShowOutputWindowRoutedCommand, ModifierKeys.Alt, Key.D2);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "res:Window_Output2", InputGestureText = "res:ShortCutKeyAlt2", Icon = DsImagesAttribute.Output, Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS, Order = 20)]
	sealed class ShowCSharpInteractiveCommand : MenuItemCommand {
		ShowCSharpInteractiveCommand()
			: base(ShowOutputWindowCommandLoader.ShowOutputWindowRoutedCommand) {
		}
	}
}
