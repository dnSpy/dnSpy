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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Controls;
using dnSpy.ToolWindows;

namespace dnSpy.MainApp {
	sealed class MainWindowControlState {
		const string HORIZONTALCONTENT_SECT = "Horizontal";
		const string VERTICALCONTENT_SECT = "Vertical";
		const string TOOLWINDOWUI_SECT = "ToolWindow";
		const string LOCATION_SECT = "Location";
		const string LOCATION_GUID_ATTR = "g";
		const string LOCATION_ATTR = "l";

		public StackedContentState? HorizontalContentState;
		public StackedContentState? VerticalContentState;
		public ToolWindowUIState? LeftState, RightState, TopState, BottomState;
		public readonly Dictionary<Guid, AppToolWindowLocation> SavedLocations = new Dictionary<Guid, AppToolWindowLocation>();

		public MainWindowControlState Read(ISettingsSection section) {
			HorizontalContentState = StackedContentStateSerializer.TryDeserialize(section.GetOrCreateSection(HORIZONTALCONTENT_SECT));
			VerticalContentState = StackedContentStateSerializer.TryDeserialize(section.GetOrCreateSection(VERTICALCONTENT_SECT));

			foreach (var twSect in section.SectionsWithName(TOOLWINDOWUI_SECT)) {
				var state = ToolWindowUIState.TryDeserialize(twSect);
				if (state is null)
					continue;
				switch (state.Location) {
				case AppToolWindowLocation.Left:	LeftState = state; break;
				case AppToolWindowLocation.Right:	RightState = state; break;
				case AppToolWindowLocation.Top:		TopState = state; break;
				case AppToolWindowLocation.Bottom:	BottomState = state; break;
				}
			}

			foreach (var locSect in section.SectionsWithName(LOCATION_SECT)) {
				var guid = locSect.Attribute<Guid?>(LOCATION_GUID_ATTR);
				var loc = locSect.Attribute<AppToolWindowLocation?>(LOCATION_ATTR);
				if (guid is null || loc is null)
					continue;
				if (!IsValid(loc.Value))
					continue;
				SavedLocations[guid.Value] = loc.Value;
			}

			return this;
		}

		static bool IsValid(AppToolWindowLocation value) =>
			value == AppToolWindowLocation.Top ||
			value == AppToolWindowLocation.Left ||
			value == AppToolWindowLocation.Right ||
			value == AppToolWindowLocation.Bottom;

		public void Write(ISettingsSection section) {
			Debug2.Assert(!(HorizontalContentState is null) && !(VerticalContentState is null));
			if (!(HorizontalContentState is null))
				StackedContentStateSerializer.Serialize(section.GetOrCreateSection(HORIZONTALCONTENT_SECT), HorizontalContentState);
			if (!(VerticalContentState is null))
				StackedContentStateSerializer.Serialize(section.GetOrCreateSection(VERTICALCONTENT_SECT), VerticalContentState);
			Debug2.Assert(!(LeftState is null) && !(RightState is null) && !(TopState is null) && !(BottomState is null));
			if (!(LeftState is null))
				ToolWindowUIState.Serialize(section.CreateSection(TOOLWINDOWUI_SECT), LeftState);
			if (!(RightState is null))
				ToolWindowUIState.Serialize(section.CreateSection(TOOLWINDOWUI_SECT), RightState);
			if (!(TopState is null))
				ToolWindowUIState.Serialize(section.CreateSection(TOOLWINDOWUI_SECT), TopState);
			if (!(BottomState is null))
				ToolWindowUIState.Serialize(section.CreateSection(TOOLWINDOWUI_SECT), BottomState);
			foreach (var kv in SavedLocations) {
				var sect = section.CreateSection(LOCATION_SECT);
				sect.Attribute(LOCATION_GUID_ATTR, kv.Key);
				sect.Attribute(LOCATION_ATTR, kv.Value);
			}
		}
	}

	sealed class ToolWindowUIState {
		const string LOCATION_ATTR = "location";
		const string INDEX_ATTR = "index";
		const string ISHORIZONTAL_ATTR = "is-horizontal";
		const string GROUP_SECT = "Group";
		const string STACKEDCONTENTSTATE_SECTION = "StackedContent";

		public AppToolWindowLocation Location;
		public StackedContentState? StackedContentState;
		public int Index;
		public bool IsHorizontal;
		public List<ToolWindowGroupState> Groups { get; }

		public ToolWindowUIState() => Groups = new List<ToolWindowGroupState>();

		public ToolWindowUIState Save(AppToolWindowLocation location, MainWindowControl.ToolWindowUI ui) {
			Location = location;
			StackedContentState = ((ToolWindowGroupService)ui.ToolWindowGroupService).StackedContentState;
			var groups = ui.ToolWindowGroupService.TabGroups.ToList();
			Index = groups.IndexOf(ui.ToolWindowGroupService.ActiveTabGroup!);
			IsHorizontal = ui.ToolWindowGroupService.IsHorizontal;
			foreach (var g in groups)
				Groups.Add(new ToolWindowGroupState().Save(g));
			return this;
		}

		public void Restore(MainWindowControl mainWindowControl, MainWindowControl.ToolWindowUI ui) {
			if (Groups.Count == 0)
				return;

			var mgr = ui.ToolWindowGroupService;
			mgr.IsHorizontal = IsHorizontal;

			foreach (var gs in Groups) {
				if (gs.Contents.Count == 0)
					continue;
				var g = mgr.Create();
				foreach (var cs in gs.Contents)
					mainWindowControl.TryAdd(ui, g, cs.Guid);
				var cl = g.TabContents.ToList();
				if ((uint)gs.Index < (uint)cl.Count)
					g.ActiveTabContent = cl[gs.Index];
				else if (cl.Count > 0)
					g.ActiveTabContent = cl[0];
			}
			mainWindowControl.Show(ui);

			var groups = mgr.TabGroups.ToList();
			if ((uint)Index < (uint)groups.Count)
				mgr.ActiveTabGroup = groups[Index];
			else if (groups.Count > 0)
				mgr.ActiveTabGroup = groups[0];
			((ToolWindowGroupService)mgr).StackedContentState = StackedContentState!;
			foreach (var g in groups) {
				if (!g.TabContents.Any())
					mgr.Close(g);
			}
		}

		public static ToolWindowUIState? TryDeserialize(ISettingsSection section) {
			var location = section.Attribute<AppToolWindowLocation?>(LOCATION_ATTR);
			int? index = section.Attribute<int?>(INDEX_ATTR);
			bool? isHorizontal = section.Attribute<bool?>(ISHORIZONTAL_ATTR);
			if (location is null || index is null || isHorizontal is null)
				return null;
			var state = new ToolWindowUIState();
			state.Location = location.Value;
			state.Index = index.Value;
			state.IsHorizontal = isHorizontal.Value;

			foreach (var sect in section.SectionsWithName(GROUP_SECT)) {
				var content = ToolWindowGroupState.TryDeserialize(sect);
				if (content is null)
					return null;
				state.Groups.Add(content);
			}

			state.StackedContentState = StackedContentStateSerializer.TryDeserialize(section.GetOrCreateSection(STACKEDCONTENTSTATE_SECTION));
			if (state.StackedContentState is null)
				return null;

			return state;
		}

		public static void Serialize(ISettingsSection section, ToolWindowUIState state) {
			section.Attribute(LOCATION_ATTR, state.Location);
			section.Attribute(INDEX_ATTR, state.Index);
			section.Attribute(ISHORIZONTAL_ATTR, state.IsHorizontal);
			foreach (var content in state.Groups)
				ToolWindowGroupState.Serialize(section.CreateSection(GROUP_SECT), content);
			Debug2.Assert(!(state.StackedContentState is null));
			if (!(state.StackedContentState is null))
				StackedContentStateSerializer.Serialize(section.GetOrCreateSection(STACKEDCONTENTSTATE_SECTION), state.StackedContentState);
		}
	}

	sealed class ToolWindowGroupState {
		const string INDEX_ATTR = "index";
		const string CONTENT_SECT = "Content";

		public int Index;
		public List<ToolWindowContentState> Contents { get; }

		public ToolWindowGroupState() => Contents = new List<ToolWindowContentState>();

		public static ToolWindowGroupState? TryDeserialize(ISettingsSection section) {
			int? index = section.Attribute<int?>(INDEX_ATTR);
			if (index is null)
				return null;
			var state = new ToolWindowGroupState();
			state.Index = index.Value;

			foreach (var sect in section.SectionsWithName(CONTENT_SECT)) {
				var content = ToolWindowContentState.TryDeserialize(sect);
				if (content is null)
					return null;
				state.Contents.Add(content);
			}

			return state;
		}

		public static void Serialize(ISettingsSection section, ToolWindowGroupState state) {
			section.Attribute(INDEX_ATTR, state.Index);
			foreach (var content in state.Contents)
				ToolWindowContentState.Serialize(section.CreateSection(CONTENT_SECT), content);
		}

		public ToolWindowGroupState Save(IToolWindowGroup g) {
			var contents = g.TabContents.ToList();
			Index = contents.IndexOf(g.ActiveTabContent!);
			foreach (var c in contents)
				Contents.Add(new ToolWindowContentState().Save(c));
			return this;
		}
	}

	sealed class ToolWindowContentState {
		const string GUID_ATTR = "g";

		public Guid Guid;

		public ToolWindowContentState() {
		}

		public ToolWindowContentState(Guid guid) => Guid = guid;

		public static ToolWindowContentState? TryDeserialize(ISettingsSection section) {
			var guid = section.Attribute<Guid?>(GUID_ATTR);
			if (guid is null)
				return null;

			return new ToolWindowContentState(guid.Value);
		}

		public static void Serialize(ISettingsSection section, ToolWindowContentState state) => section.Attribute(GUID_ATTR, state.Guid);

		public ToolWindowContentState Save(ToolWindowContent c) {
			Guid = c.Guid;
			return this;
		}
	}

	[Export, Export(typeof(IDsToolWindowService))]
	sealed class MainWindowControl : IStackedContentChild, IDsToolWindowService {
		readonly StackedContent<IStackedContentChild> horizontalContent;
		readonly StackedContent<IStackedContentChild> verticalContent;
		readonly Dictionary<AppToolWindowLocation, ToolWindowUI> toolWindowUIs;
		readonly Lazy<IToolWindowContentProvider>[] mainToolWindowContentProviders;
		readonly Dictionary<Guid, AppToolWindowLocation> savedLocations;

		public object? UIObject => horizontalContent.UIObject;

		[ImportingConstructor]
		MainWindowControl(IToolWindowServiceProvider toolWindowServiceProvider, [ImportMany] Lazy<IToolWindowContentProvider>[] mainToolWindowContentProviders) {
			horizontalContent = new StackedContent<IStackedContentChild>(true);
			verticalContent = new StackedContent<IStackedContentChild>(false);
			toolWindowUIs = new Dictionary<AppToolWindowLocation, ToolWindowUI>();
			var toolWindowService = toolWindowServiceProvider.Create();
			this.mainToolWindowContentProviders = mainToolWindowContentProviders.ToArray();
			savedLocations = new Dictionary<Guid, AppToolWindowLocation>();

			var guid = new Guid(MenuConstants.GUIDOBJ_TOOLWINDOW_TABCONTROL_GUID);
			const double HORIZ_WIDTH = 250, VERT_HEIGHT = 250;
			toolWindowUIs.Add(AppToolWindowLocation.Left, new ToolWindowUI(this, AppToolWindowLocation.Left, HORIZ_WIDTH, horizontalContent, false, toolWindowService.Create(new ToolWindowGroupServiceOptions(guid))));
			toolWindowUIs.Add(AppToolWindowLocation.Right, new ToolWindowUI(this, AppToolWindowLocation.Right, HORIZ_WIDTH, horizontalContent, true, toolWindowService.Create(new ToolWindowGroupServiceOptions(guid))));
			toolWindowUIs.Add(AppToolWindowLocation.Top, new ToolWindowUI(this, AppToolWindowLocation.Top, VERT_HEIGHT, verticalContent, false, toolWindowService.Create(new ToolWindowGroupServiceOptions(guid))));
			toolWindowUIs.Add(AppToolWindowLocation.Bottom, new ToolWindowUI(this, AppToolWindowLocation.Bottom, VERT_HEIGHT, verticalContent, true, toolWindowService.Create(new ToolWindowGroupServiceOptions(guid))));
		}

		public sealed class ToolWindowUI {
			readonly MainWindowControl mainWindowControl;
			public readonly AppToolWindowLocation Location;
			public readonly StackedContent<IStackedContentChild> StackedContent;
			public readonly bool InsertLast;
			public readonly IToolWindowGroupService ToolWindowGroupService;

			public double Length { get; set; }
			public bool IsAdded { get; set; }
			public IStackedContentChild? StackedContentChild { get; set; }

			public ToolWindowUI(MainWindowControl mainWindowControl, AppToolWindowLocation location, double length, StackedContent<IStackedContentChild> stackedContent, bool insertLast, IToolWindowGroupService mgr) {
				this.mainWindowControl = mainWindowControl;
				Location = location;
				Length = length;
				StackedContent = stackedContent;
				InsertLast = insertLast;
				ToolWindowGroupService = mgr;
				ToolWindowGroupService.TabGroupCollectionChanged += ToolWindowGroupService_TabGroupCollectionChanged;
			}

			void ToolWindowGroupService_TabGroupCollectionChanged(object? sender, ToolWindowGroupCollectionChangedEventArgs e) => mainWindowControl.TabGroupCollectionChanged(this);

			const double DEFAULT_MIN_HEIGHT = 20;
			const double DEFAULT_MIN_WIDTH = 20;
			public StackedContentChildInfo GetSizeInfo() => new StackedContentChildInfo {
				Horizontal = new GridChildLength(new GridLength(Length, GridUnitType.Pixel), min: DEFAULT_MIN_WIDTH),
				Vertical = new GridChildLength(new GridLength(Length, GridUnitType.Pixel), min: DEFAULT_MIN_HEIGHT),
			};
		}

		public void Initialize(IStackedContentChild mainChild, MainWindowControlState state) {
			horizontalContent.AddChild(verticalContent);
			verticalContent.AddChild(mainChild, StackedContentChildInfo.CreateVertical(new GridLength(1, GridUnitType.Star), 30));

			foreach (var kv in state.SavedLocations)
				savedLocations[kv.Key] = kv.Value;

			if (!(state.LeftState is null) && !(state.RightState is null) && !(state.TopState is null) && !(state.BottomState is null)) {
				state.LeftState.Restore(this, toolWindowUIs[AppToolWindowLocation.Left]);
				state.RightState.Restore(this, toolWindowUIs[AppToolWindowLocation.Right]);
				state.TopState.Restore(this, toolWindowUIs[AppToolWindowLocation.Top]);
				state.BottomState.Restore(this, toolWindowUIs[AppToolWindowLocation.Bottom]);
			}
			else
				RestoreDefault();

			if (!(state.HorizontalContentState is null) && !(state.VerticalContentState is null)) {
				horizontalContent.State = state.HorizontalContentState;
				verticalContent.State = state.VerticalContentState;
			}
		}

		void RestoreDefault() {
			var infos = mainToolWindowContentProviders.SelectMany(a => a.Value.ContentInfos.Where(b => b.IsDefault)).OrderBy(a => a.Order).ToArray();
			foreach (var info in infos)
				Show(info.Guid, info.Location, false, false);

			var hash = new HashSet<AppToolWindowLocation>();
			foreach (var info in infos) {
				var location = Convert(info.Location);
				if (hash.Contains(location))
					continue;
				hash.Add(location);
				Show(info.Guid, info.Location, true, false);
			}
		}

		public MainWindowControlState CreateState() {
			var state = new MainWindowControlState();
			state.HorizontalContentState = horizontalContent.State;
			state.VerticalContentState = verticalContent.State;
			state.LeftState = new ToolWindowUIState().Save(AppToolWindowLocation.Left, toolWindowUIs[AppToolWindowLocation.Left]);
			state.RightState = new ToolWindowUIState().Save(AppToolWindowLocation.Right, toolWindowUIs[AppToolWindowLocation.Right]);
			state.TopState = new ToolWindowUIState().Save(AppToolWindowLocation.Top, toolWindowUIs[AppToolWindowLocation.Top]);
			state.BottomState = new ToolWindowUIState().Save(AppToolWindowLocation.Bottom, toolWindowUIs[AppToolWindowLocation.Bottom]);
			foreach (var kv in savedLocations)
				state.SavedLocations[kv.Key] = kv.Value;
			foreach (var ui in toolWindowUIs.Values) {
				foreach (var g in ui.ToolWindowGroupService.TabGroups) {
					foreach (var c in g.TabContents)
						state.SavedLocations[c.Guid] = ui.Location;
				}
			}
			return state;
		}

		AppToolWindowLocation Convert(AppToolWindowLocation location) {
			switch (location) {
			case AppToolWindowLocation.Left:
			case AppToolWindowLocation.Right:
			case AppToolWindowLocation.Top:
			case AppToolWindowLocation.Bottom:
				return location;
			case AppToolWindowLocation.DefaultHorizontal:
				return AppToolWindowLocation.Bottom;
			case AppToolWindowLocation.DefaultVertical:
				return AppToolWindowLocation.Left;
			default: throw new ArgumentException();
			}
		}

		AppToolWindowLocation? GetSavedLocation(Guid guid) {
			if (savedLocations.TryGetValue(guid, out var location))
				return location;
			return null;
		}

		AppToolWindowLocation GetLocation(Guid guid, AppToolWindowLocation? location) => GetSavedLocation(guid) ?? location ?? GetDefaultLocation(guid);

		AppToolWindowLocation GetDefaultLocation(Guid guid) {
			foreach (var provider in mainToolWindowContentProviders) {
				foreach (var info in provider.Value.ContentInfos) {
					if (info.Guid == guid)
						return info.Location;
				}
			}
			return AppToolWindowLocation.DefaultHorizontal;
		}

		public void Show(ToolWindowContent content, AppToolWindowLocation? location) {
			if (content is null)
				throw new ArgumentNullException(nameof(content));
			Show(content, GetLocation(content.Guid, location), true, true);
		}

		void Show(ToolWindowContent content, AppToolWindowLocation location, bool active, bool focus) {
			if (content is null)
				throw new ArgumentNullException(nameof(content));
			var t = GetToolWindowGroup(content);
			if (!(t is null)) {
				if (active)
					t.Value.group.ActiveTabContent = content;
				if (focus)
					t.Value.group.SetFocus(content);
				return;
			}

			var g = GetOrCreateGroup(location);
			g.Add(content);
			SaveLocationAndActivate(g, content, location, active, focus);
		}

		IToolWindowGroup GetOrCreateGroup(AppToolWindowLocation location) {
			if (!toolWindowUIs.TryGetValue(Convert(location), out var ui))
				throw new ArgumentException();
			Show(ui);
			var g = ui.ToolWindowGroupService.ActiveTabGroup;
			if (g is null)
				g = ui.ToolWindowGroupService.Create();
			return g;
		}

		void SaveLocationAndActivate(IToolWindowGroup g, ToolWindowContent content, AppToolWindowLocation location, bool active, bool focus) {
			if (active)
				g.ActiveTabContent = content;
			if (focus)
				g.SetFocus(content);
			savedLocations[content.Guid] = location;
		}

		internal bool TryAdd(ToolWindowUI ui, IToolWindowGroup g, Guid guid) {
			var content = Create(guid);
			if (content is null)
				return false;
			if (!(GetToolWindowGroup(content) is null))
				return false;

			g.Add(content);
			return true;
		}

		internal void Show(ToolWindowUI ui) {
			if (ui.IsAdded)
				return;
			SaveUILengths();
			int index = ui.InsertLast ? ui.StackedContent.Count : 0;
			if (ui.StackedContentChild is null)
				ui.StackedContentChild = StackedContentChildImpl.GetOrCreate(ui.ToolWindowGroupService, ui.ToolWindowGroupService.UIObject);
			ui.StackedContent.AddChild(ui.StackedContentChild, ui.GetSizeInfo(), index);
			ui.IsAdded = true;
		}

		public bool IsShown(ToolWindowContent content) => !(GetToolWindowGroup(content) is null);
		public bool IsShown(Guid guid) => !(GetToolWindowGroup(guid) is null);

		void Hide(ToolWindowUI ui) {
			Debug.Assert(!ui.ToolWindowGroupService.TabGroups.Any());
			SaveLength(ui);
			ui.StackedContent.Remove(ui.StackedContentChild!);
			ui.IsAdded = false;
		}

		void SaveUILengths() {
			foreach (var ui in toolWindowUIs.Values) {
				if (ui.IsAdded) {
					SaveLength(ui);
					ui.StackedContent.UpdateSize(ui.StackedContentChild!, ui.GetSizeInfo());
				}
			}
		}

		void SaveLength(ToolWindowUI ui) {
			var length = ui.StackedContent.GetLength(ui.StackedContentChild!);
			Debug.Assert(length.IsAbsolute);
			ui.Length = length.Value;
		}

		public ToolWindowContent? Show(Guid guid, AppToolWindowLocation? location) => Show(guid, GetLocation(guid, location), true, true);

		ToolWindowContent? Show(Guid guid, AppToolWindowLocation location, bool active, bool focus) {
			var content = Create(guid);
			Debug2.Assert(!(content is null));
			if (content is null)
				return null;
			Show(content, location, active, focus);
			return content;
		}

		ToolWindowContent? Create(Guid guid) {
			foreach (var provider in mainToolWindowContentProviders) {
				var content = provider.Value.GetOrCreate(guid);
				if (!(content is null))
					return content;
			}
			return null;
		}

		void TabGroupCollectionChanged(ToolWindowUI ui) {
			if (!ui.ToolWindowGroupService.TabGroups.Any())
				Hide(ui);
		}

		(ToolWindowUI ui, IToolWindowGroup group)? GetToolWindowGroup(Guid guid) {
			foreach (var ui in toolWindowUIs.Values) {
				foreach (var g in ui.ToolWindowGroupService.TabGroups) {
					foreach (var c in g.TabContents) {
						if (c.Guid == guid)
							return (ui, g);
					}
				}
			}
			return null;
		}

		(ToolWindowUI ui, IToolWindowGroup group)? GetToolWindowGroup(ToolWindowContent? content) {
			foreach (var ui in toolWindowUIs.Values) {
				foreach (var g in ui.ToolWindowGroupService.TabGroups) {
					if (g.TabContents.Contains(content))
						return (ui, g);
				}
			}
			return null;
		}

		(ToolWindowUI ui, IToolWindowGroup group)? GetToolWindowGroup(IToolWindowGroup group) {
			foreach (var ui in toolWindowUIs.Values) {
				if (ui.ToolWindowGroupService.TabGroups.Contains(group))
					return (ui, group);
			}
			return null;
		}

		public void Close(ToolWindowContent content) {
			if (content is null)
				throw new ArgumentNullException(nameof(content));
			var t = GetToolWindowGroup(content);
			Debug2.Assert(!(t is null));
			if (t is null)
				throw new InvalidOperationException();
			t.Value.group.Close(content);
		}

		public void Close(Guid guid) {
			if (!IsShown(guid))
				return;
			if (Create(guid) is ToolWindowContent content)
				Close(content);
		}

		public bool Owns(IToolWindowGroup toolWindowGroup) {
			if (toolWindowGroup is null)
				return false;
			foreach (var ui in toolWindowUIs.Values) {
				if (ui.ToolWindowGroupService.TabGroups.Contains(toolWindowGroup))
					return true;
			}
			return false;
		}

		public bool CanMove(ToolWindowContent? content, AppToolWindowLocation location) {
			var t = GetToolWindowGroup(content);
			location = Convert(location);
			if (t is null || t.Value.ui.Location == location)
				return false;

			return true;
		}

		public void Move(ToolWindowContent? content, AppToolWindowLocation location) {
			var t = GetToolWindowGroup(content);
			location = Convert(location);
			if (t is null || t.Value.ui.Location == location)
				return;
			Debug2.Assert(!(content is null));

			var g = GetOrCreateGroup(location);
			t.Value.group.MoveTo(g, content);
			SaveLocationAndActivate(g, content, location, true, true);
		}

		public bool CanMove(IToolWindowGroup group, AppToolWindowLocation location) {
			if (group is null)
				return false;
			var t = GetToolWindowGroup(group);
			location = Convert(location);
			if (t is null || t.Value.ui.Location == location || !t.Value.group.TabContents.Any())
				return false;

			return true;
		}

		public void Move(IToolWindowGroup group, AppToolWindowLocation location) {
			if (group is null)
				return;
			var t = GetToolWindowGroup(group);
			location = Convert(location);
			if (t is null || t.Value.ui.Location == location || !t.Value.group.TabContents.Any())
				return;

			var activeContent = t.Value.group.ActiveTabContent;
			Debug2.Assert(!(activeContent is null));
			foreach (var c in t.Value.group.TabContents.ToArray())
				Move(c, location);
			if (!(activeContent is null)) {
				var t2 = GetToolWindowGroup(activeContent);
				Debug2.Assert(!(t2 is null));
				if (!(t2 is null)) {
					t2.Value.group.ActiveTabContent = activeContent;
				}
			}
		}
	}
}
