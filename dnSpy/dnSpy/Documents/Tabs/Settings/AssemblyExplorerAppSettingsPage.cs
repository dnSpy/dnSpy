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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Documents.TreeView;
using dnSpy.Properties;

namespace dnSpy.Documents.Tabs.Settings {
	[Export(typeof(IAppSettingsPageProvider))]
	sealed class AssemblyExplorerAppSettingsPageProvider : IAppSettingsPageProvider {
		readonly DocumentTreeViewSettingsImpl documentTreeViewSettings;

		[ImportingConstructor]
		AssemblyExplorerAppSettingsPageProvider(DocumentTreeViewSettingsImpl documentTreeViewSettings) {
			this.documentTreeViewSettings = documentTreeViewSettings;
		}

		public IEnumerable<AppSettingsPage> Create() {
			yield return new AssemblyExplorerAppSettingsPage(documentTreeViewSettings);
		}
	}

	sealed class AssemblyExplorerAppSettingsPage : AppSettingsPage, IAppSettingsPage2, INotifyPropertyChanged {
		public override Guid Guid => new Guid("F8B8DA74-9318-4BEE-B50A-1139147D3C82");
		public override double Order => AppSettingsConstants.ORDER_ASSEMBLY_EXPLORER;
		public override string Title => dnSpy_Resources.AssemblyExplorerTitle;
		public override object UIObject => this;

		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		readonly DocumentTreeViewSettingsImpl documentTreeViewSettings;

		public bool ShowToken {
			get { return showToken; }
			set {
				if (showToken != value) {
					showToken = value;
					OnPropertyChanged(nameof(ShowToken));
				}
			}
		}
		bool showToken;

		public bool ShowAssemblyVersion {
			get { return showAssemblyVersion; }
			set {
				if (showAssemblyVersion != value) {
					showAssemblyVersion = value;
					OnPropertyChanged(nameof(ShowAssemblyVersion));
				}
			}
		}
		bool showAssemblyVersion;

		public bool ShowAssemblyPublicKeyToken {
			get { return showAssemblyPublicKeyToken; }
			set {
				if (showAssemblyPublicKeyToken != value) {
					showAssemblyPublicKeyToken = value;
					OnPropertyChanged(nameof(ShowAssemblyPublicKeyToken));
				}
			}
		}
		bool showAssemblyPublicKeyToken;

		public bool SingleClickExpandsTreeViewChildren {
			get { return singleClickExpandsTreeViewChildren; }
			set {
				if (singleClickExpandsTreeViewChildren != value) {
					singleClickExpandsTreeViewChildren = value;
					OnPropertyChanged(nameof(SingleClickExpandsTreeViewChildren));
				}
			}
		}
		bool singleClickExpandsTreeViewChildren;

		public bool SyntaxHighlight {
			get { return syntaxHighlight; }
			set {
				if (syntaxHighlight != value) {
					syntaxHighlight = value;
					OnPropertyChanged(nameof(SyntaxHighlight));
				}
			}
		}
		bool syntaxHighlight;

		public DocumentFilterTypeVM[] DocumentFilterTypes { get; }

		public DocumentFilterTypeVM FilterDraggedItems {
			get { return filterDraggedItems; }
			set {
				if (filterDraggedItems != value) {
					filterDraggedItems = value;
					OnPropertyChanged(nameof(FilterDraggedItems));
				}
			}
		}
		DocumentFilterTypeVM filterDraggedItems;

		public MemberKindVM[] MemberKindsArray => memberKindVMs2;
		readonly MemberKindVM[] memberKindVMs;
		readonly MemberKindVM[] memberKindVMs2;

		public MemberKindVM MemberKind0 {
			get { return memberKindVMs[0]; }
			set { SetMemberKind(0, value); }
		}

		public MemberKindVM MemberKind1 {
			get { return memberKindVMs[1]; }
			set { SetMemberKind(1, value); }
		}

		public MemberKindVM MemberKind2 {
			get { return memberKindVMs[2]; }
			set { SetMemberKind(2, value); }
		}

		public MemberKindVM MemberKind3 {
			get { return memberKindVMs[3]; }
			set { SetMemberKind(3, value); }
		}

		public MemberKindVM MemberKind4 {
			get { return memberKindVMs[4]; }
			set { SetMemberKind(4, value); }
		}

		void SetMemberKind(int index, MemberKindVM newValue) {
			Debug.Assert(newValue != null);
			if (newValue == null)
				throw new ArgumentNullException(nameof(newValue));
			if (memberKindVMs[index] == newValue)
				return;

			int otherIndex = Array.IndexOf(memberKindVMs, newValue);
			Debug.Assert(otherIndex >= 0);
			if (otherIndex >= 0) {
				memberKindVMs[otherIndex] = memberKindVMs[index];
				memberKindVMs[index] = newValue;

				OnPropertyChanged(string.Format("MemberKind{0}", otherIndex));
			}
			OnPropertyChanged(string.Format("MemberKind{0}", index));
		}

		public AssemblyExplorerAppSettingsPage(DocumentTreeViewSettingsImpl documentTreeViewSettings) {
			if (documentTreeViewSettings == null)
				throw new ArgumentNullException(nameof(documentTreeViewSettings));
			this.documentTreeViewSettings = documentTreeViewSettings;

			ShowToken = documentTreeViewSettings.ShowToken;
			ShowAssemblyVersion = documentTreeViewSettings.ShowAssemblyVersion;
			ShowAssemblyPublicKeyToken = documentTreeViewSettings.ShowAssemblyPublicKeyToken;
			SingleClickExpandsTreeViewChildren = documentTreeViewSettings.SingleClickExpandsTreeViewChildren;
			SyntaxHighlight = documentTreeViewSettings.SyntaxHighlight;

			var filterObjs = typeof(DocumentFilterType).GetEnumValues().Cast<DocumentFilterType>().ToArray();
			DocumentFilterTypes = new DocumentFilterTypeVM[filterObjs.Length];
			for (int i = 0; i < filterObjs.Length; i++)
				this.DocumentFilterTypes[i] = new DocumentFilterTypeVM(filterObjs[i], ToString(filterObjs[i]));

			this.FilterDraggedItems = this.DocumentFilterTypes.First(a => a.FilterType == documentTreeViewSettings.FilterDraggedItems);

			var defObjs = typeof(MemberKind).GetEnumValues().Cast<MemberKind>().ToArray();
			this.memberKindVMs = new MemberKindVM[defObjs.Length];
			for (int i = 0; i < defObjs.Length; i++)
				this.memberKindVMs[i] = new MemberKindVM(defObjs[i], ToString(defObjs[i]));
			this.memberKindVMs2 = this.memberKindVMs.ToArray();

			this.MemberKind0 = this.memberKindVMs.First(a => a.Object == documentTreeViewSettings.MemberKind0);
			this.MemberKind1 = this.memberKindVMs.First(a => a.Object == documentTreeViewSettings.MemberKind1);
			this.MemberKind2 = this.memberKindVMs.First(a => a.Object == documentTreeViewSettings.MemberKind2);
			this.MemberKind3 = this.memberKindVMs.First(a => a.Object == documentTreeViewSettings.MemberKind3);
			this.MemberKind4 = this.memberKindVMs.First(a => a.Object == documentTreeViewSettings.MemberKind4);
		}

		static string ToString(MemberKind o) {
			switch (o) {
			case MemberKind.NestedTypes:	return dnSpy_Resources.MemberKind_NestedTypes;
			case MemberKind.Fields:			return dnSpy_Resources.MemberKind_Fields;
			case MemberKind.Events:			return dnSpy_Resources.MemberKind_Events;
			case MemberKind.Properties:		return dnSpy_Resources.MemberKind_Properties;
			case MemberKind.Methods:		return dnSpy_Resources.MemberKind_Methods;
			default:
				Debug.Fail("Shouldn't be here");
				return "???";
			}
		}

		static string ToString(DocumentFilterType o) {
			switch (o) {
			case DocumentFilterType.All:			return dnSpy_Resources.DocumentFilterType_All;
			case DocumentFilterType.AllSupported:	return dnSpy_Resources.DocumentFilterType_AllSupported;
			case DocumentFilterType.DotNetOnly:		return dnSpy_Resources.DocumentFilterType_DotNetOnly;
			default:
				Debug.Fail("Shouldn't be here");
				return "???";
			}
		}

		public override void OnApply() { throw new InvalidOperationException(); }

		public void OnApply(IAppRefreshSettings appRefreshSettings) {
			documentTreeViewSettings.ShowToken = ShowToken;
			documentTreeViewSettings.ShowAssemblyVersion = ShowAssemblyVersion;
			documentTreeViewSettings.ShowAssemblyPublicKeyToken = ShowAssemblyPublicKeyToken;
			documentTreeViewSettings.SingleClickExpandsTreeViewChildren = SingleClickExpandsTreeViewChildren;
			documentTreeViewSettings.SyntaxHighlight = SyntaxHighlight;
			documentTreeViewSettings.FilterDraggedItems = FilterDraggedItems.FilterType;

			bool update =
				documentTreeViewSettings.MemberKind0 != MemberKind0.Object ||
				documentTreeViewSettings.MemberKind1 != MemberKind1.Object ||
				documentTreeViewSettings.MemberKind2 != MemberKind2.Object ||
				documentTreeViewSettings.MemberKind3 != MemberKind3.Object ||
				documentTreeViewSettings.MemberKind4 != MemberKind4.Object;
			if (update) {
				appRefreshSettings.Add(DocumentTreeViewAppSettingsConstants.REFRESH_ASSEMBLY_EXPLORER_MEMBER_ORDER);
				documentTreeViewSettings.MemberKind0 = MemberKind0.Object;
				documentTreeViewSettings.MemberKind1 = MemberKind1.Object;
				documentTreeViewSettings.MemberKind2 = MemberKind2.Object;
				documentTreeViewSettings.MemberKind3 = MemberKind3.Object;
				documentTreeViewSettings.MemberKind4 = MemberKind4.Object;
			}
		}

		public override string[] GetSearchStrings() => MemberKindsArray.Select(a => a.Text).Concat(DocumentFilterTypes.Select(a => a.Text)).ToArray();
	}

	sealed class MemberKindVM {
		public MemberKind Object { get; }
		public string Text { get; }

		public MemberKindVM(MemberKind memberKind, string text) {
			this.Object = memberKind;
			this.Text = text;
		}
	}

	sealed class DocumentFilterTypeVM {
		public DocumentFilterType FilterType { get; }
		public string Text { get; }

		public DocumentFilterTypeVM(DocumentFilterType documentFilterType, string text) {
			this.FilterType = documentFilterType;
			this.Text = text;
		}
	}
}
