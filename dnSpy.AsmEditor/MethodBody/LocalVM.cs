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
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Shared.MVVM;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class LocalVM : ViewModelBase, IIndexedItem {
		public static readonly LocalVM Null = new LocalVM();

		LocalOptions origOptions;

		public ITypeSigCreator TypeSigCreator {
			set { typeSigCreator = value; }
		}
		ITypeSigCreator typeSigCreator;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public ICommand EditTypeCommand {
			get { return new RelayCommand(a => EditType()); }
		}

		public int Index {
			get { return index; }
			set {
				if (index != value) {
					index = value;
					OnPropertyChanged("Index");
				}
			}
		}
		int index;

		public bool IsPinned {
			get { return Type is PinnedSig; }
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
			get { return type; }
			set {
				if (type != value) {
					type = value;
					OnPropertyChanged("Type");
					OnPropertyChanged("IsPinned");
				}
			}
		}
		TypeSig type;

		public string Name {
			get { return name; }
			set {
				if (name != value) {
					name = value;
					OnPropertyChanged("Name");
				}
			}
		}
		string name;

		public bool IsCompilerGenerated {
			get { return PdbAttributes == 1; }
			set { PdbAttributes = value ? 1 : 0; }
		}

		public int PdbAttributes {
			get { return pdbAttributes; }
			set {
				if (pdbAttributes != value) {
					pdbAttributes = value;
					OnPropertyChanged("PdbAttributes");
					OnPropertyChanged("IsCompilerGenerated");
				}
			}
		}
		int pdbAttributes;

		readonly TypeSigCreatorOptions typeSigCreatorOptions;

		LocalVM() {
		}

		public LocalVM(TypeSigCreatorOptions typeSigCreatorOptions, LocalOptions options) {
			this.typeSigCreatorOptions = typeSigCreatorOptions.Clone(dnSpy_AsmEditor_Resources.CreateLocalType);
			this.typeSigCreatorOptions.IsLocal = true;
			this.typeSigCreatorOptions.NullTypeSigAllowed = false;
			this.origOptions = options;

			Reinitialize();
		}

		void EditType() {
			if (typeSigCreator == null)
				throw new InvalidOperationException();

			bool canceled;
			var newType = typeSigCreator.Create(typeSigCreatorOptions, Type, out canceled);
			if (canceled)
				return;

			Type = newType;
		}

		void Reinitialize() {
			InitializeFrom(origOptions);
		}

		public LocalOptions CreateLocalOptions() {
			return CopyTo(new LocalOptions());
		}

		public void InitializeFrom(LocalOptions options) {
			this.Type = options.Type;
			this.Name = options.Name;
			this.PdbAttributes = options.PdbAttributes;
		}

		public LocalOptions CopyTo(LocalOptions options) {
			options.Type = this.Type;
			options.Name = this.Name;
			options.PdbAttributes = this.PdbAttributes;
			return options;
		}

		public IIndexedItem Clone() {
			return new LocalVM(typeSigCreatorOptions, CreateLocalOptions());
		}

		public LocalVM Import(TypeSigCreatorOptions typeSigCreatorOptions, ModuleDef ownerModule) {
			var opts = CreateLocalOptions();
			var importer = new Importer(ownerModule, ImporterOptions.TryToUseDefs);
			opts.Type = importer.Import(opts.Type);
			return new LocalVM(typeSigCreatorOptions, opts);
		}
	}
}
