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

using System.Collections.Generic;
using dnlib.DotNet;
using Emit = dnlib.DotNet.Emit;

namespace dnSpy.AsmEditor.Compiler {
	abstract class ImportedType {
		/// <summary>
		/// New or existing type in target module
		/// </summary>
		public TypeDef TargetType { get; protected set; }
	}

	/// <summary>
	/// This is a new type that got imported into the target module
	/// </summary>
	sealed class NewImportedType : ImportedType {
		public NewImportedType(TypeDef targetType) {
			TargetType = targetType;
		}
	}

	/// <summary>
	/// An edited method body
	/// </summary>
	struct EditedMethodBody {
		/// <summary>
		/// Original method
		/// </summary>
		public MethodDef OriginalMethod { get; }

		/// <summary>
		/// New method body
		/// </summary>
		public Emit.MethodBody NewBody { get; }

		/// <summary>
		/// New <see cref="MethodImplAttributes"/> value
		/// </summary>
		public MethodImplAttributes ImplAttributes { get; }

		public EditedMethodBody(MethodDef originalMethod, Emit.MethodBody newBody, MethodImplAttributes implAttributes) {
			OriginalMethod = originalMethod;
			NewBody = newBody;
			ImplAttributes = implAttributes;
		}
	}

	/// <summary>
	/// This is a type that gets updated with more members. The user or the compiler added
	/// more members that must be included in the existing type
	/// </summary>
	sealed class MergedImportedType : ImportedType {
		internal bool RenameDuplicates { get; }

		public bool IsEmpty =>
			NewNestedTypes.Count == 0 &&
			NewProperties.Count == 0 &&
			NewEvents.Count == 0 &&
			NewMethods.Count == 0 &&
			NewFields.Count == 0 &&
			EditedMethodBodies.Count == 0;

		/// <summary>
		/// New nested types that must be added to <see cref="ImportedType.TargetType"/>
		/// </summary>
		public List<ImportedType> NewNestedTypes { get; } = new List<ImportedType>();

		/// <summary>
		/// New properties that must be added to <see cref="ImportedType.TargetType"/>
		/// </summary>
		public List<PropertyDef> NewProperties { get; } = new List<PropertyDef>();

		/// <summary>
		/// New events that must be added to <see cref="ImportedType.TargetType"/>
		/// </summary>
		public List<EventDef> NewEvents { get; } = new List<EventDef>();

		/// <summary>
		/// New methods that must be added to <see cref="ImportedType.TargetType"/>
		/// </summary>
		public List<MethodDef> NewMethods { get; } = new List<MethodDef>();

		/// <summary>
		/// New fields that must be added to <see cref="ImportedType.TargetType"/>
		/// </summary>
		public List<FieldDef> NewFields { get; } = new List<FieldDef>();

		/// <summary>
		/// New method bodies that must be updated
		/// </summary>
		public List<EditedMethodBody> EditedMethodBodies { get; } = new List<EditedMethodBody>();

		public MergedImportedType(TypeDef targetType, bool renameDuplicates) {
			TargetType = targetType;
			RenameDuplicates = renameDuplicates;
		}
	}
}
