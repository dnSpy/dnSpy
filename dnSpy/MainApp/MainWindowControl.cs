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

		public StackedContentState HorizontalContentState;
		public StackedContentState VerticalContentState;
		public ToolWindowUIState LeftState, RightState, TopState, BottomState;
		public readonly Dictionary<Guid, AppToolWindowLocation> SavedLocations = new Dictionary<Guid, AppToolWindowLocation>();

		public MainWindowControlState Read(ISettingsSection section) {
			HorizontalContentState = StackedContentStateSerializer.TryDeserialize(section.GetOrCreateSection(HORIZONTALCONTENT_SECT));
			VerticalContentState = StackedContentStateSerializer.TryDeserialize(section.GetOrCreateSection(VERTICALCONTENT_SECT));

			foreach (var twSect in section.SectionsWithName(TOOLWINDOWUI_SECT)) {
				var state = ToolWindowUIState.TryDeserialize(twSect);
				if (state == null)
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
				if (guid == null || loc == null)
					continue;
				if (!IsValid(loc.Value))
					continue;
				SavedLocations[guid.Value] = loc.Value;
			}

			return this;
		}

		static bool IsValid(AppToolWindowLocation value) {
			return value == AppToolWindowLocation.Top ||
				value == AppToolWindowLocation.Left ||
				value == AppToolWindowLocation.Right ||
				value == AppToolWindowLocation.Bottom;
		}

		public void Write(ISettingsSection section) {
			Debug.Assert(HorizontalContentState != null && VerticalContentState != null);
			if (HorizontalContentState != null)
				StackedContentStateSerializer.Serialize(section.GetOrCreateSection(HORIZONTALCONTENT_SECT), HorizontalContentState);
			if (VerticalContentState != null)
				StackedContentStateSerializer.Serialize(section.GetOrCreateSection(VERTICALCONTENT_SECT), VerticalContentState);
			Debug.Assert(LeftState != null && RightState != null && TopState != null && BottomState != null);
			if (LeftState != null)
				ToolWindowUIState.Serialize(section.CreateSection(TOOLWINDOWUI_SECT), LeftState);
			if (RightState != null)
				ToolWindowUIState.Serialize(section.CreateSection(TOOLWINDOWUI_SECT), RightState);
			if (TopState != null)
				ToolWindowUIState.Serialize(section.CreateSection(TOOLWINDOWUI_SECT), TopState);
			if (BottomState != null)
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
		public StackedContentState StackedContentState;
		public int Index;
		public bool IsHorizontal;
		public List<ToolWindowGroupState> Groups { get; private set; }

		public ToolWindowUIState() {
			this.Groups = new List<ToolWindowGroupState>();
		}

		public ToolWindowUIState Save(AppToolWindowLocation location, MainWindowControl.ToolWindowUI ui) {
			this.Location = location;
			this.StackedContentState = ((ToolWindowGroupManager)ui.ToolWindowGroupManager).StackedContentState;
			var groups = ui.ToolWindowGroupManager.TabGroups.ToList();
			this.Index = groups.IndexOf(ui.ToolWindowGroupManager.ActiveTabGroup);
			this.IsHorizontal = ui.ToolWindowGroupManager.IsHorizontal;
			foreach (var g in groups)
				Groups.Add(new ToolWindowGroupState().Save(g));
			return this;
		}

		public void Restore(MainWindowControl mainWindowControl, MainWindowControl.ToolWindowUI ui) {
			if (Groups.Count == 0)
				return;

			var mgr = ui.ToolWindowGroupManager;
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
			}
			mainWindowControl.Show(ui);

			var groups = mgr.TabGroups.ToList();
			if ((uint)Index < (uint)groups.Count)
				mgr.ActiveTabGroup = groups[Index];
			((ToolWindowGroupManager)mgr).StackedContentState = this.StackedContentState;
			foreach (var g in groups) {
				if (!g.TabContents.Any())
					mgr.Close(g);
			}
		}

		public static ToolWindowUIState TryDeserialize(ISettingsSection section) {
			var location = section.Attribute<AppToolWindowLocation?>(LOCATION_ATTR);
			int? index = section.Attribute<int?>(INDEX_ATTR);
			bool? isHorizontal = section.Attribute<bool?>(ISHORIZONTAL_ATTR);
			if (location == null || index == null || isHorizontal == null)
				return null;
			var state = new ToolWindowUIState();
			state.Location = location.Value;
			state.Index = index.Value;
			state.IsHorizontal = isHorizontal.Value;

			foreach (var sect in section.SectionsWithName(GROUP_SECT)) {
				var content = ToolWindowGroupState.TryDeserialize(sect);
				if (content == null)
					return null;
				state.Groups.Add(content);
			}

			state.StackedContentState = StackedContentStateSerializer.TryDeserialize(section.GetOrCreateSection(STACKEDCONTENTSTATE_SECTION));
			if (state.StackedContentState == null)
				return null;

			return state;
		}

		public static void Serialize(ISettingsSection section, ToolWindowUIState state) {
			section.Attribute(LOCATION_ATTR, state.Location);
			section.Attribute(INDEX_ATTR, state.Index);
			section.Attribute(ISHORIZONTAL_ATTR, state.IsHorizontal);
			foreach (var content in state.Groups)
				ToolWindowGroupState.Serialize(section.CreateSection(GROUP_SECT), content);
			Debug.Assert(state.StackedContentState != null);
			if (state.StackedContentState != null)
				StackedContentStateSerializer.Serialize(section.GetOrCreateSection(STACKEDCONTENTSTATE_SECTION), state.StackedContentState);
		}
	}

	sealed class ToolWindowGroupState {
		const string INDEX_ATTR = "index";
		const string CONTENT_SECT = "Content";

		public int Index;
		public List<ToolWindowContentState> Contents { get; private set; }

		public ToolWindowGroupState() {
			this.Contents = new List<ToolWindowContentState>();
		}

		public static ToolWindowGroupState TryDeserialize(ISettingsSection section) {
			int? index = section.Attribute<int?>(INDEX_ATTR);
			if (index == null)
				return null;
			var state = new ToolWindowGroupState();
			state.Index = index.Value;

			foreach (var sect in section.SectionsWithName(CONTENT_SECT)) {
				var content = ToolWindowContentState.TryDeserialize(sect);
				if (content == null)
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
			this.Index = contents.IndexOf(g.ActiveTabContent);
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

		public ToolWindowContentState(Guid guid) {
			this.Guid = guid;
		}

		public static ToolWindowContentState TryDeserialize(ISettingsSection section) {
			var guid = section.Attribute<Guid?>(GUID_ATTR);
			if (guid == null)
				return null;

			return new ToolWindowContentState(guid.Value);
		}

		public static void Serialize(ISettingsSection section, ToolWindowContentState state) {
			section.Attribute(GUID_ATTR, state.Guid);
		}

		public ToolWindowContentState Save(IToolWindowContent c) {
			this.Guid = c.Guid;
			return this;
		}
	}

	[Export, Export(typeof(IMainToolWindowManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class MainWindowControl : IStackedContentChild, IMainToolWindowManager {
		readonly StackedContent<IStackedContentChild> horizontalContent;
		readonly StackedContent<IStackedContentChild> verticalContent;
		readonly Dictionary<AppToolWindowLocation, ToolWindowUI> toolWindowUIs;
		readonly Lazy<IMainToolWindowContentCreator>[] contentCreators;
		readonly Dictionary<Guid, AppToolWindowLocation> savedLocations;

		public object UIObject {
			get { return horizontalContent.UIObject; }
		}

		[ImportingConstructor]
		MainWindowControl(IToolWindowManagerCreator toolWindowManagerCreator, [ImportMany] Lazy<IMainToolWindowContentCreator>[] contentCreators) {
			this.horizontalContent = new StackedContent<IStackedContentChild>(true);
			this.verticalContent = new StackedContent<IStackedContentChild>(false);
			this.toolWindowUIs = new Dictionary<AppToolWindowLocation, ToolWindowUI>();
			var toolWindowManager = toolWindowManagerCreator.Create();
			this.contentCreators = contentCreators.ToArray();
			this.savedLocations = new Dictionary<Guid, AppToolWindowLocation>();

			var guid = new Guid(MenuConstants.GUIDOBJ_TOOLWINDOW_TABCONTROL_GUID);
			const double HORIZ_WIDTH = 250, VERT_HEIGHT = 250;
			toolWindowUIs.Add(AppToolWindowLocation.Left, new ToolWindowUI(this, AppToolWindowLocation.Left, HORIZ_WIDTH, horizontalContent, false, toolWindowManager.Create(new ToolWindowGroupManagerOptions(guid))));
			toolWindowUIs.Add(AppToolWindowLocation.Right, new ToolWindowUI(this, AppToolWindowLocation.Right, HORIZ_WIDTH, horizontalContent, true, toolWindowManager.Create(new ToolWindowGroupManagerOptions(guid))));
			toolWindowUIs.Add(AppToolWindowLocation.Top, new ToolWindowUI(this, AppToolWindowLocation.Top, VERT_HEIGHT, verticalContent, false, toolWindowManager.Create(new ToolWindowGroupManagerOptions(guid))));
			toolWindowUIs.Add(AppToolWindowLocation.Bottom, new ToolWindowUI(this, AppToolWindowLocation.Bottom, VERT_HEIGHT, verticalContent, true, toolWindowManager.Create(new ToolWindowGroupManagerOptions(guid))));
		}

		public sealed class ToolWindowUI {
			readonly MainWindowControl mainWindowControl;
			public readonly AppToolWindowLocation Location;
			public readonly StackedContent<IStackedContentChild> StackedContent;
			public readonly bool InsertLast;
			public readonly IToolWindowGroupManager ToolWindowGroupManager;

			public double Length { get; set; }
			public bool IsAdded { get; set; }
			public IStackedContentChild StackedContentChild { get; set; }

			public ToolWindowUI(MainWindowControl mainWindowControl, AppToolWindowLocation location, double length, StackedContent<IStackedContentChild> stackedContent, bool insertLast, IToolWindowGroupManager mgr) {
				this.mainWindowControl = mainWindowControl;
				this.Location = location;
				this.Length = length;
				this.StackedContent = stackedContent;
				this.InsertLast = insertLast;
				this.ToolWindowGroupManager = mgr;
				ToolWindowGroupManager.TabGroupCollectionChanged += ToolWindowGroupManager_TabGroupCollectionChanged;
			}

			void ToolWindowGroupManager_TabGroupCollectionChanged(object sender, ToolWindowGroupCollectionChangedEventArgs e) {
				mainWindowControl.TabGroupCollectionChanged(this);
			}

			public StackedContentChildInfo GetSizeInfo() {
				return new StackedContentChildInfo {
					Horizontal = new GridChildLength(new GridLength(Length, GridUnitType.Pixel)),
					Vertical = new GridChildLength(new GridLength(Length, GridUnitType.Pixel)),
				};
			}
		}

		public void Initialize(IStackedContentChild mainChild, MainWindowControlState state) {
			horizontalContent.AddChild(verticalContent);
			verticalContent.AddChild(mainChild, StackedContentChildInfo.CreateVertical(new GridLength(1, GridUnitType.Star), 30));

			foreach (var kv in state.SavedLocations)
				savedLocations[kv.Key] = kv.Value;

			if (state.LeftState != null && state.RightState != null && state.TopState != null && state.BottomState != null) {
				state.LeftState.Restore(this, toolWindowUIs[AppToolWindowLocation.Left]);
				state.RightState.Restore(this, toolWindowUIs[AppToolWindowLocation.Right]);
				state.TopState.Restore(this, toolWindowUIs[AppToolWindowLocation.Top]);
				state.BottomState.Restore(this, toolWindowUIs[AppToolWindowLocation.Bottom]);
			}
			else
				RestoreDefault();

			if (state.HorizontalContentState != null && state.VerticalContentState != null) {
				horizontalContent.State = state.HorizontalContentState;
				verticalContent.State = state.VerticalContentState;
			}
		}

		void RestoreDefault() {
			var infos = contentCreators.SelectMany(a => a.Value.ContentInfos.Where(b => b.IsDefault)).OrderBy(a => a.Order).ToArray();
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
				foreach (var g in ui.ToolWindowGroupManager.TabGroups) {
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
			AppToolWindowLocation location;
			if (savedLocations.TryGetValue(guid, out location))
				return location;
			return null;
		}

		AppToolWindowLocation GetLocation(Guid guid, AppToolWindowLocation? location) {
			return GetSavedLocation(guid) ?? location ?? GetDefaultLocation(guid);
		}

		AppToolWindowLocation GetDefaultLocation(Guid guid) {
			foreach (var creator in this.contentCreators) {
				foreach (var info in creator.Value.ContentInfos) {
					if (info.Guid == guid)
						return info.Location;
				}
			}
			return AppToolWindowLocation.DefaultHorizontal;
		}

		public void Show(IToolWindowContent content, AppToolWindowLocation? location) {
			if (content == null)
				throw new ArgumentNullException();
			Show(content, GetLocation(content.Guid, location), true, true);
		}

		void Show(IToolWindowContent content, AppToolWindowLocation location, bool active, bool focus) {
			if (content == null)
				throw new ArgumentNullException();
			var t = GetToolWindowGroup(content);
			if (t != null) {
				if (active)
					t.Item2.ActiveTabContent = content;
				if (focus)
					t.Item2.SetFocus(content);
				return;
			}

			var g = GetOrCreateGroup(location);
			g.Add(content);
			SaveLocationAndActivate(g, content, location, active, focus);
		}

		IToolWindowGroup GetOrCreateGroup(AppToolWindowLocation location) {
			ToolWindowUI ui;
			if (!toolWindowUIs.TryGetValue(Convert(location), out ui))
				throw new ArgumentException();
			Show(ui);
			var g = ui.ToolWindowGroupManager.ActiveTabGroup;
			if (g == null)
				g = ui.ToolWindowGroupManager.Create();
			return g;
		}

		void SaveLocationAndActivate(IToolWindowGroup g, IToolWindowContent content, AppToolWindowLocation location, bool active, bool focus) {
			if (active)
				g.ActiveTabContent = content;
			if (focus)
				g.SetFocus(content);
			savedLocations[content.Guid] = location;
		}

		internal bool TryAdd(ToolWindowUI ui, IToolWindowGroup g, Guid guid) {
			var content = Create(guid);
			if (content == null)
				return false;
			if (GetToolWindowGroup(content) != null)
				return false;

			g.Add(content);
			return true;
		}

		internal void Show(ToolWindowUI ui) {
			if (ui.IsAdded)
				return;
			SaveUILengths();
			int index = ui.InsertLast ? ui.StackedContent.Count : 0;
			if (ui.StackedContentChild == null)
				ui.StackedContentChild = StackedContentChildImpl.GetOrCreate(ui.ToolWindowGroupManager, ui.ToolWindowGroupManager.UIObject);
			ui.StackedContent.AddChild(ui.StackedContentChild, ui.GetSizeInfo(), index);
			ui.IsAdded = true;
		}

		public bool IsShown(IToolWindowContent content) {
			return GetToolWindowGroup(content) != null;
		}

		public bool IsShown(Guid guid) {
			return GetToolWindowGroup(guid) != null;
		}

		void Hide(ToolWindowUI ui) {
			Debug.Assert(!ui.ToolWindowGroupManager.TabGroups.Any());
			SaveLength(ui);
			ui.StackedContent.Remove(ui.StackedContentChild);
			ui.IsAdded = false;
		}

		void SaveUILengths() {
			foreach (var ui in toolWindowUIs.Values) {
				if (ui.IsAdded) {
					SaveLength(ui);
					ui.StackedContent.UpdateSize(ui.StackedContentChild, ui.GetSizeInfo());
				}
			}
		}

		void SaveLength(ToolWindowUI ui) {
			var length = ui.StackedContent.GetLength(ui.StackedContentChild);
			Debug.Assert(length.IsAbsolute);
			ui.Length = length.Value;
		}

		public IToolWindowContent Show(Guid guid, AppToolWindowLocation? location) {
			return Show(guid, GetLocation(guid, location), true, true);
		}

		IToolWindowContent Show(Guid guid, AppToolWindowLocation location, bool active, bool focus) {
			var content = Create(guid);
			Debug.Assert(content != null);
			if (content == null)
				return null;
			Show(content, location, active, focus);
			return content;
		}

		IToolWindowContent Create(Guid guid) {
			foreach (var creator in contentCreators) {
				var content = creator.Value.GetOrCreate(guid);
				if (content != null)
					return content;
			}
			return null;
		}

		void TabGroupCollectionChanged(ToolWindowUI ui) {
			if (!ui.ToolWindowGroupManager.TabGroups.Any())
				Hide(ui);
		}

		Tuple<ToolWindowUI, IToolWindowGroup> GetToolWindowGroup(Guid guid) {
			foreach (var ui in this.toolWindowUIs.Values) {
				foreach (var g in ui.ToolWindowGroupManager.TabGroups) {
					foreach (var c in g.TabContents) {
						if (c.Guid == guid)
							return Tuple.Create(ui, g);
					}
				}
			}
			return null;
		}

		Tuple<ToolWindowUI, IToolWindowGroup> GetToolWindowGroup(IToolWindowContent content) {
			foreach (var ui in this.toolWindowUIs.Values) {
				foreach (var g in ui.ToolWindowGroupManager.TabGroups) {
					if (g.TabContents.Contains(content))
						return Tuple.Create(ui, g);
				}
			}
			return null;
		}

		Tuple<ToolWindowUI, IToolWindowGroup> GetToolWindowGroup(IToolWindowGroup group) {
			foreach (var ui in this.toolWindowUIs.Values) {
				if (ui.ToolWindowGroupManager.TabGroups.Contains(group))
					return Tuple.Create(ui, group);
			}
			return null;
		}

		public void Close(IToolWindowContent content) {
			if (content == null)
				throw new ArgumentNullException();
			var t = GetToolWindowGroup(content);
			Debug.Assert(t != null);
			if (t == null)
				throw new InvalidOperationException();
			t.Item2.Close(content);
		}

		public void Close(Guid guid) {
			if (!IsShown(guid))
				return;
			Close(Create(guid));
		}

		public bool Owns(IToolWindowGroup toolWindowGroup) {
			if (toolWindowGroup == null)
				return false;
			foreach (var ui in toolWindowUIs.Values) {
				if (ui.ToolWindowGroupManager.TabGroups.Contains(toolWindowGroup))
					return true;
			}
			return false;
		}

		public bool CanMove(IToolWindowContent content, AppToolWindowLocation location) {
			var t = GetToolWindowGroup(content);
			location = Convert(location);
			if (t == null || t.Item1.Location == location)
				return false;

			return true;
		}

		public void Move(IToolWindowContent content, AppToolWindowLocation location) {
			var t = GetToolWindowGroup(content);
			location = Convert(location);
			if (t == null || t.Item1.Location == location)
				return;

			var g = GetOrCreateGroup(location);
			t.Item2.MoveTo(g, content);
			SaveLocationAndActivate(g, content, location, true, true);
		}

		public bool CanMove(IToolWindowGroup group, AppToolWindowLocation location) {
			if (group == null)
				return false;
			var t = GetToolWindowGroup(group);
			location = Convert(location);
			if (t == null || t.Item1.Location == location || !t.Item2.TabContents.Any())
				return false;

			return true;
		}

		public void Move(IToolWindowGroup group, AppToolWindowLocation location) {
			if (group == null)
				return;
			var t = GetToolWindowGroup(group);
			location = Convert(location);
			if (t == null || t.Item1.Location == location || !t.Item2.TabContents.Any())
				return;

			var activeContent = t.Item2.ActiveTabContent;
			Debug.Assert(activeContent != null);
			foreach (var c in t.Item2.TabContents.ToArray())
				Move(c, location);
			if (activeContent != null) {
				var t2 = GetToolWindowGroup(activeContent);
				Debug.Assert(t2 != null);
				if (t2 != null) {
					t2.Item2.ActiveTabContent = activeContent;
				}
			}
		}
	}
}
