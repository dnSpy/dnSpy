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
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class TypeSigCreatorOptions : ICloneable {
		public string? Title { get; set; }
		public bool IsLocal { get; set; }
		public bool CanAddGenericTypeVar { get; set; }
		public bool CanAddGenericMethodVar { get; set; }
		public bool NullTypeSigAllowed { get; set; }

		public TypeDef? OwnerType {
			get => ownerType ?? OwnerMethod?.DeclaringType;
			set => ownerType = value;
		}
		TypeDef? ownerType;

		public MethodDef? OwnerMethod { get; set; }

		public ModuleDef OwnerModule {
			get => module;
			private set => module = value ?? throw new ArgumentNullException(nameof(value));
		}
		ModuleDef module;

		public IDecompiler Decompiler {
			get => decompiler;
			set => decompiler = value ?? throw new ArgumentNullException(nameof(value));
		}
		IDecompiler decompiler;

		public IDecompilerService DecompilerService { get; }

		public TypeSigCreatorOptions(ModuleDef ownerModule, IDecompilerService decompilerService) {
			module = ownerModule ?? throw new ArgumentNullException(nameof(ownerModule));
			decompiler = decompilerService.Decompiler ?? throw new ArgumentNullException(nameof(decompilerService));
			DecompilerService = decompilerService;
		}

		public TypeSigCreatorOptions Clone() => (TypeSigCreatorOptions)MemberwiseClone();

		public TypeSigCreatorOptions Clone(string? title) {
			var clone = Clone();
			clone.Title = title;
			return clone;
		}

		object ICloneable.Clone() => Clone();
	}
}
