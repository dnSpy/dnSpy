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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Search;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class SecurityAttributeVM : ViewModelBase {
		public IDnlibTypePicker DnlibTypePicker {
			set => dnlibTypePicker = value;
		}
		IDnlibTypePicker? dnlibTypePicker;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public ICommand PickAttributeTypeCommand => new RelayCommand(a => PickAttributeType());

		public string FullName {
			get {
				var sb = new StringBuilder();
				sb.Append(AttributeType is null ? "<<<null>>>" : AttributeType.FullName);
				sb.Append('(');
				bool first = true;
				foreach (var namedArg in CANamedArgumentsVM.Collection) {
					if (!first)
						sb.Append(", ");
					first = false;
					sb.Append(namedArg.ToString());
				}
				sb.Append(')');
				return sb.ToString();
			}
		}

		public ITypeDefOrRef? AttributeType {
			get => attributeType;
			set {
				if (attributeType != value) {
					attributeType = value;
					OnPropertyChanged(nameof(AttributeType));
					OnPropertyChanged(nameof(FullName));
					HasErrorUpdated();
				}
			}
		}
		ITypeDefOrRef? attributeType;

		public CANamedArgumentsVM CANamedArgumentsVM { get; }

		readonly SecurityAttribute origSa;
		readonly ModuleDef ownerModule;

		public SecurityAttributeVM(SecurityAttribute sa, ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef? ownerType, MethodDef? ownerMethod) {
			origSa = sa;
			this.ownerModule = ownerModule;
			CANamedArgumentsVM = new CANamedArgumentsVM(ownerModule, decompilerService, ownerType, ownerMethod, a => {
				// The named args blob length must also be at most 0x1FFFFFFF bytes but we can't verify it here
				return a.Collection.Count < ModelUtils.COMPRESSED_UINT32_MAX;
			});
			CANamedArgumentsVM.Collection.CollectionChanged += Args_CollectionChanged;

			Reinitialize();
		}

		void Args_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
			Hook(e);
			OnPropertyChanged(nameof(FullName));
			HasErrorUpdated();
		}

		void Hook(NotifyCollectionChangedEventArgs e) {
			if (!(e.OldItems is null)) {
				foreach (INotifyPropertyChanged? i in e.OldItems)
					i!.PropertyChanged -= arg_PropertyChanged;
			}
			if (!(e.NewItems is null)) {
				foreach (INotifyPropertyChanged? i in e.NewItems)
					i!.PropertyChanged += arg_PropertyChanged;
			}
		}

		void arg_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			OnPropertyChanged(nameof(FullName));
			HasErrorUpdated();
		}

		void PickAttributeType() {
			if (dnlibTypePicker is null)
				throw new InvalidOperationException();
			var newAttrType = dnlibTypePicker.GetDnlibType(dnSpy_AsmEditor_Resources.Pick_Type, new FlagsDocumentTreeNodeFilter(VisibleMembersFlags.TypeDef), AttributeType, ownerModule);
			if (!(newAttrType is null))
				AttributeType = newAttrType;
		}

		void Reinitialize() => InitializeFrom(origSa);

		void InitializeFrom(SecurityAttribute sa) {
			AttributeType = sa.AttributeType;
			CANamedArgumentsVM.InitializeFrom(sa.NamedArguments);
		}

		public SecurityAttribute CreateSecurityAttribute() {
			var sa = new SecurityAttribute(AttributeType);
			sa.NamedArguments.AddRange(CANamedArgumentsVM.Collection.Select(a => a.CreateCANamedArgument()));
			return sa;
		}

		public override bool HasError => AttributeType is null || CANamedArgumentsVM.Collection.Any(a => a.HasError);
	}
}
