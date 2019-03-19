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
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class LocalVM : ViewModelBase, IIndexedItem {
		public static readonly LocalVM Null = new LocalVM();

		LocalOptions origOptions;

		public ITypeSigCreator TypeSigCreator {
			set { typeSigCreator = value; }
		}
		ITypeSigCreator typeSigCreator;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public ICommand EditTypeCommand => new RelayCommand(a => EditType());

		public int Index {
			get => index;
			set {
				if (index != value) {
					index = value;
					OnPropertyChanged(nameof(Index));
				}
			}
		}
		int index;

		public bool IsPinned {
			get => Type is PinnedSig;
			set {
				var t = Type;
				if (t == null)
					return;
				if (value) {
					if (!(t is PinnedSig))
						Type = new PinnedSig(t);
				}
				else {
					if (t is PinnedSig)
						Type = t.Next;
				}
			}
		}

		public TypeSig Type {
			get => type;
			set {
				if (type != value) {
					type = value;
					OnPropertyChanged(nameof(Type));
					OnPropertyChanged(nameof(IsPinned));
				}
			}
		}
		TypeSig type;

		public string Name {
			get => name;
			set {
				if (name != value) {
					name = value;
					OnPropertyChanged(nameof(Name));
				}
			}
		}
		string name;

		public bool DebuggerHidden {
			get => (Attributes & PdbLocalAttributes.DebuggerHidden) != 0;
			set {
				if (value)
					Attributes |= PdbLocalAttributes.DebuggerHidden;
				else
					Attributes &= ~PdbLocalAttributes.DebuggerHidden;
			}
		}

		public PdbLocalAttributes Attributes {
			get => attributes;
			set {
				if (attributes != value) {
					attributes = value;
					OnPropertyChanged(nameof(Attributes));
					OnPropertyChanged(nameof(DebuggerHidden));
				}
			}
		}
		PdbLocalAttributes attributes;

		readonly TypeSigCreatorOptions typeSigCreatorOptions;

		LocalVM() {
		}

		public LocalVM(TypeSigCreatorOptions typeSigCreatorOptions, LocalOptions options) {
			this.typeSigCreatorOptions = typeSigCreatorOptions.Clone(dnSpy_AsmEditor_Resources.CreateLocalType);
			this.typeSigCreatorOptions.IsLocal = true;
			this.typeSigCreatorOptions.NullTypeSigAllowed = false;
			origOptions = options;

			Reinitialize();
		}

		void EditType() {
			if (typeSigCreator == null)
				throw new InvalidOperationException();

			var newType = typeSigCreator.Create(typeSigCreatorOptions, Type, out bool canceled);
			if (canceled)
				return;

			Type = newType;
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public LocalOptions CreateLocalOptions() => CopyTo(new LocalOptions());

		public void InitializeFrom(LocalOptions options) {
			Type = options.Type;
			Name = options.Name;
			Attributes = options.Attributes;
		}

		public LocalOptions CopyTo(LocalOptions options) {
			options.Type = Type;
			options.Name = Name;
			options.Attributes = Attributes;
			return options;
		}

		public IIndexedItem Clone() => new LocalVM(typeSigCreatorOptions, CreateLocalOptions());

		public LocalVM Import(TypeSigCreatorOptions typeSigCreatorOptions, ModuleDef ownerModule) {
			var opts = CreateLocalOptions();
			var importer = new Importer(ownerModule, ImporterOptions.TryToUseDefs);
			opts.Type = importer.Import(opts.Type);
			return new LocalVM(typeSigCreatorOptions, opts);
		}
	}
}
