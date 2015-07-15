/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

namespace ICSharpCode.ILSpy.AsmEditor.MethodBody
{
	sealed class MethodBodyVM : ViewModelBase
	{
		readonly MethodBodyOptions origOptions;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public bool IsNativeBody {
			get { return (MethodBodyType)MethodBodyTypeVM.SelectedItem == MethodBodyType.Native; }
		}

		public bool IsCilBody {
			get { return (MethodBodyType)MethodBodyTypeVM.SelectedItem == MethodBodyType.Cil; }
		}

		internal static readonly EnumVM[] methodBodyTypeList = new EnumVM[] {
			new EnumVM(MethodBodyType.None, "None"),
			new EnumVM(MethodBodyType.Cil, "IL"),
			new EnumVM(MethodBodyType.Native, "Native"),
		};
		public EnumListVM MethodBodyTypeVM {
			get { return methodBodyTypeVM; }
		}
		readonly EnumListVM methodBodyTypeVM;

		public EnumListVM CodeTypeVM {
			get { return codeTypeVM; }
		}
		readonly EnumListVM codeTypeVM = new EnumListVM(Method.MethodOptionsVM.codeTypeList);

		public NativeMethodBodyVM NativeMethodBodyVM {
			get { return nativeMethodBodyVM; }
		}
		readonly NativeMethodBodyVM nativeMethodBodyVM;

		public CilBodyVM CilBodyVM {
			get { return cilBodyVM; }
		}
		readonly CilBodyVM cilBodyVM;

		public MethodBodyVM(MethodBodyOptions options, ModuleDef ownerModule, Language language, TypeDef ownerType, MethodDef ownerMethod)
		{
			this.origOptions = options;

			this.nativeMethodBodyVM = new MethodBody.NativeMethodBodyVM(options.NativeMethodBodyOptions, false);
			NativeMethodBodyVM.PropertyChanged += (s, e) => HasErrorUpdated();
			this.cilBodyVM = new MethodBody.CilBodyVM(options.CilBodyOptions, ownerModule, language, ownerType, ownerMethod, false);
			CilBodyVM.PropertyChanged += (s, e) => HasErrorUpdated();
			this.methodBodyTypeVM = new EnumListVM(methodBodyTypeList, (a, b) => OnMethodBodyTypeChanged());

			Reinitialize();
		}

		void OnMethodBodyTypeChanged()
		{
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

			OnPropertyChanged("IsNativeBody");
			OnPropertyChanged("IsCilBody");
			HasErrorUpdated();
		}

		void Reinitialize()
		{
			InitializeFrom(origOptions);
		}

		public MethodBodyOptions CreateMethodBodyOptions()
		{
			return CopyTo(new MethodBodyOptions());
		}

		void InitializeFrom(MethodBodyOptions options)
		{
			NativeMethodBodyVM.InitializeFrom(options.NativeMethodBodyOptions);
			CilBodyVM.InitializeFrom(options.CilBodyOptions);
			MethodBodyTypeVM.SelectedItem = options.BodyType;

			// Initialize this last since it gets updated when body type gets changed
			CodeTypeVM.SelectedItem = (Method.CodeType)options.CodeType;
		}

		MethodBodyOptions CopyTo(MethodBodyOptions options)
		{
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
