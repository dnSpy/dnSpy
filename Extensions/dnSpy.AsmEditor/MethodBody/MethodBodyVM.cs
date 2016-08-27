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
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class MethodBodyVM : ViewModelBase {
		readonly MethodBodyOptions origOptions;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public bool IsNativeBody => (MethodBodyType)MethodBodyTypeVM.SelectedItem == MethodBodyType.Native;
		public bool IsCilBody => (MethodBodyType)MethodBodyTypeVM.SelectedItem == MethodBodyType.Cil;

		internal static readonly EnumVM[] methodBodyTypeList = new EnumVM[] {
			new EnumVM(MethodBodyType.None, dnSpy_AsmEditor_Resources.MethodBodyType_None),
			new EnumVM(MethodBodyType.Cil, dnSpy_AsmEditor_Resources.MethodBodyType_IL),
			new EnumVM(MethodBodyType.Native, dnSpy_AsmEditor_Resources.MethodBodyType_Native),
		};
		public EnumListVM MethodBodyTypeVM { get; }
		public EnumListVM CodeTypeVM { get; } = new EnumListVM(Method.MethodOptionsVM.codeTypeList);
		public NativeMethodBodyVM NativeMethodBodyVM { get; }
		public CilBodyVM CilBodyVM { get; }

		public MethodBodyVM(MethodBodyOptions options, ModuleDef ownerModule, IDecompilerManager decompilerManager, TypeDef ownerType, MethodDef ownerMethod) {
			this.origOptions = options;

			this.NativeMethodBodyVM = new MethodBody.NativeMethodBodyVM(options.NativeMethodBodyOptions, false);
			NativeMethodBodyVM.PropertyChanged += (s, e) => HasErrorUpdated();
			this.CilBodyVM = new MethodBody.CilBodyVM(options.CilBodyOptions, ownerModule, decompilerManager, ownerType, ownerMethod, false);
			CilBodyVM.PropertyChanged += (s, e) => HasErrorUpdated();
			this.MethodBodyTypeVM = new EnumListVM(methodBodyTypeList, (a, b) => OnMethodBodyTypeChanged());

			Reinitialize();
		}

		void OnMethodBodyTypeChanged() {
			switch ((MethodBodyType)MethodBodyTypeVM.SelectedItem) {
			case MethodBodyType.None:
				CodeTypeVM.SelectedItem = Method.CodeType.IL;
				break;

			case MethodBodyType.Cil:
				CodeTypeVM.SelectedItem = Method.CodeType.IL;
				break;

			case MethodBodyType.Native:
				CodeTypeVM.SelectedItem = Method.CodeType.Native;
				break;

			default:
				throw new InvalidOperationException();
			}

			OnPropertyChanged(nameof(IsNativeBody));
			OnPropertyChanged(nameof(IsCilBody));
			HasErrorUpdated();
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public MethodBodyOptions CreateMethodBodyOptions() => CopyTo(new MethodBodyOptions());

		void InitializeFrom(MethodBodyOptions options) {
			NativeMethodBodyVM.InitializeFrom(options.NativeMethodBodyOptions);
			CilBodyVM.InitializeFrom(options.CilBodyOptions);
			MethodBodyTypeVM.SelectedItem = options.BodyType;

			// Initialize this last since it gets updated when body type gets changed
			CodeTypeVM.SelectedItem = (Method.CodeType)options.CodeType;
		}

		MethodBodyOptions CopyTo(MethodBodyOptions options) {
			NativeMethodBodyVM.CopyTo(options.NativeMethodBodyOptions);
			CilBodyVM.CopyTo(options.CilBodyOptions);
			options.BodyType = (MethodBodyType)MethodBodyTypeVM.SelectedItem;
			options.CodeType = (MethodImplAttributes)(Method.CodeType)CodeTypeVM.SelectedItem;
			return options;
		}

		public override bool HasError {
			get {
				switch ((MethodBodyType)MethodBodyTypeVM.SelectedItem) {
				case MethodBodyType.None:
					break;

				case MethodBodyType.Cil:
					if (CilBodyVM.HasError)
						return true;
					break;

				case MethodBodyType.Native:
					if (NativeMethodBodyVM.HasError)
						return true;
					break;

				default:
					throw new InvalidOperationException();
				}

				return false;
			}
		}
	}
}
