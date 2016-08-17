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
using System.Windows.Controls;
using dnSpy.Contracts.Resources;
using dnSpy.Contracts.Settings.Dialog;

namespace dnSpy.Settings.Dialog {
	[ExportDynamicAppSettingsTab(Guid = AppSettingsConstants.GUID_DYNTAB_MISC, Order = AppSettingsConstants.ORDER_SETTINGS_TAB_MISC, Title = "res:MiscDlgTabTitle")]
	sealed class OtherDynamicAppSettingsTab : IDynamicAppSettingsTab {
	}

	[Export(typeof(IAppSettingsTabProvider))]
	sealed class DynamicAppSettingsTabProvider : IAppSettingsTabProvider {
		sealed class DynTab {
			public double Order { get; set; }
			public string Title { get; set; }
			public List<Lazy<ISimpleAppOptionProvider, ISimpleAppOptionProviderMetadata>> Providers => simpleAppOptionProviders;
			readonly List<Lazy<ISimpleAppOptionProvider, ISimpleAppOptionProviderMetadata>> simpleAppOptionProviders = new List<Lazy<ISimpleAppOptionProvider, ISimpleAppOptionProviderMetadata>>();
		}

		readonly Dictionary<Guid, DynTab> guidToDynTab;

		[ImportingConstructor]
		DynamicAppSettingsTabProvider([ImportMany] IEnumerable<Lazy<IDynamicAppSettingsTab, IDynamicAppSettingsTabMetadata>> mefDynTabs, [ImportMany] IEnumerable<Lazy<ISimpleAppOptionProvider, ISimpleAppOptionProviderMetadata>> simpleAppOptionProviders) {
			this.guidToDynTab = new Dictionary<Guid, DynTab>();
			foreach (var dt in mefDynTabs) {
				if (string.IsNullOrWhiteSpace(dt.Metadata.Title)) {
					Debug.Fail(string.Format("Missing dyn tab title. Type: {0}", dt.Value.GetType()));
					continue;
				}
				Guid guid;
				if (!Guid.TryParse(dt.Metadata.Guid, out guid)) {
					Debug.Fail(string.Format("Could not parse guid: '{0}'", dt.Metadata.Guid));
					continue;
				}
				DynTab dynTab;
				if (guidToDynTab.TryGetValue(guid, out dynTab))
					Debug.Assert(dynTab.Order == dt.Metadata.Order && dynTab.Title == dt.Metadata.Title);
				else {
					dynTab = new DynTab {
						Order = dt.Metadata.Order,
						Title = ResourceHelper.GetString(dt.Value, dt.Metadata.Title),
					};
					guidToDynTab.Add(guid, dynTab);
				}
			}

			foreach (var mo in simpleAppOptionProviders) {
				Guid guid;
				if (!Guid.TryParse(mo.Metadata.Guid, out guid)) {
					Debug.Fail(string.Format("Could not parse guid: '{0}'", mo.Metadata.Guid));
					continue;
				}
				DynTab dynTab;
				if (!guidToDynTab.TryGetValue(guid, out dynTab)) {
					Debug.Fail(string.Format("Couldn't find a dyn tab with guid {0}", guid));
					continue;
				}
				dynTab.Providers.Add(mo);
			}
		}

		public IEnumerable<IAppSettingsTab> Create() {
			foreach (var dt in guidToDynTab.Values) {
				var tab = Create(dt);
				if (tab != null)
					yield return tab;
			}
		}

		IAppSettingsTab Create(DynTab dt) {
			var tab = new DynAppSettingsTab(dt);
			return tab.Count == 0 ? null : tab;
		}

		sealed class DynAppSettingsTab : IAppSettingsTab {
			readonly List<ISimpleAppOption> options;

			public int Count => options.Count;
			public double Order { get; }
			public string Title { get; }
			public object UIObject { get; }

			public DynAppSettingsTab(DynTab dt) {
				this.Order = dt.Order;
				this.Title = dt.Title;
				this.options = new List<ISimpleAppOption>();

				var grid = new Grid();
				grid.Margin = new Thickness(5);
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

				foreach (var option in dt.Providers.SelectMany(a => a.Value.Create()).OrderBy(a => a.Order)) {
					var uiObjs = CreateUIElements(option);
					if (uiObjs == null) {
						Debug.Fail(string.Format("Couldn't create a dyn option UI object. It must implement ISimpleAppOptionCheckBox, ISimpleAppOptionButton, ISimpleAppOptionTextBox, or ISimpleAppOptionUserContent. Type: {0}", option.GetType()));
						continue;
					}
					Debug.Assert(uiObjs.Length == 1 || uiObjs.Length == 2);
					switch (uiObjs.Length) {
					case 1:
						Grid.SetColumnSpan(uiObjs[0], 2);
						Grid.SetColumn(uiObjs[0], 0);
						break;

					case 2:
						for (int i = 0; i < uiObjs.Length; i++) {
							uiObjs[i].ClearValue(Grid.ColumnSpanProperty);
							Grid.SetColumn(uiObjs[i], i);
						}
						break;

					default:
						continue;
					}

					for (int i = 0; i < uiObjs.Length; i++) {
						var f = uiObjs[i] as FrameworkElement;
						if (f == null)
							continue;
						f.Margin = new Thickness(i > 0 ? 5 : 0, grid.RowDefinitions.Count == 0 ? 0 : 5, 0, 0);
					}

					foreach (var o in uiObjs) {
						Grid.SetRow(o, grid.RowDefinitions.Count);
						o.ClearValue(Grid.RowSpanProperty);
						grid.Children.Add(o);
					}
					grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
					options.Add(option);
				}

				this.UIObject = new ScrollViewer {
					// Disable the horizontal scrollbar since textboxes will grow if the text
					// doesn't fit and there's a horizontal scrollbar.
					HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
					VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
					Content = grid,
				};
			}

			UIElement[] CreateUIElements(ISimpleAppOption option) {
				if (option is ISimpleAppOptionCheckBox)
					return CreateUIElements((ISimpleAppOptionCheckBox)option);
				if (option is ISimpleAppOptionButton)
					return CreateUIElements((ISimpleAppOptionButton)option);
				if (option is ISimpleAppOptionTextBox)
					return CreateUIElements((ISimpleAppOptionTextBox)option);
				if (option is ISimpleAppOptionUserContent)
					return CreateUIElements((ISimpleAppOptionUserContent)option);
				return null;
			}

			UIElement[] CreateUIElements(ISimpleAppOptionCheckBox option) {
				var cb = new CheckBox {
					Content = option.Text,
					IsThreeState = option.IsThreeState,
					IsChecked = option.Value,
					ToolTip = option.ToolTip,
				};
				cb.Unchecked += (s, e) => option.Value = cb.IsChecked;
				cb.Checked += (s, e) => option.Value = cb.IsChecked;
				cb.Indeterminate += (s, e) => option.Value = cb.IsChecked;
				return new UIElement[] { cb };
			}

			UIElement[] CreateUIElements(ISimpleAppOptionButton option) {
				var button = new Button {
					Content = option.Text,
					Command = option.Command,
					ToolTip = option.ToolTip,
				};
				return new UIElement[] { button };
			}

			UIElement[] CreateUIElements(ISimpleAppOptionTextBox option) {
				var tb = new TextBox { Text = option.Value, ToolTip = option.ToolTip };
				var lbl = new Label { Content = option.Text, Target = tb, ToolTip = option.ToolTip };
				tb.TextChanged += (s, e) => option.Value = tb.Text;
				return new UIElement[] { lbl, tb };
			}

			UIElement[] CreateUIElements(ISimpleAppOptionUserContent option) {
				var uiContent = option.UIContent;
				Debug.Assert(uiContent != null);
				if (uiContent == null)
					return null;
				var uiel = uiContent as UIElement;
				if (uiel == null)
					uiel = new ContentPresenter { Content = uiContent };
				return new UIElement[] { uiel };
			}

			public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
				foreach (var option in options)
					option.OnClosed(saveSettings, appRefreshSettings);
			}
		}
	}
}
