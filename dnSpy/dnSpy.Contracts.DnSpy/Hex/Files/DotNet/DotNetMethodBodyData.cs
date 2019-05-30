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
using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// <see cref="DotNetMethodBody"/> kind
	/// </summary>
	public enum DotNetMethodBodyKind {
		/// <summary>
		/// Invalid method body (<see cref="InvalidMethodBody"/>)
		/// </summary>
		Invalid,

		/// <summary>
		/// Tiny method body (<see cref="TinyMethodBody"/>)
		/// </summary>
		Tiny,

		/// <summary>
		/// Fat method body (<see cref="FatMethodBody"/>)
		/// </summary>
		Fat,
	}

	/// <summary>
	/// .NET method body
	/// </summary>
	public abstract class DotNetMethodBody : StructureData {
		/// <summary>
		/// Gets tokens of all methods that reference this method body
		/// </summary>
		public ReadOnlyCollection<uint> Tokens { get; }

		/// <summary>
		/// Gets the owner <see cref="DotNetMethodProvider"/> instance
		/// </summary>
		public DotNetMethodProvider MethodProvider { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="methodProvider">Owner</param>
		/// <param name="name">Name</param>
		/// <param name="span">Span</param>
		/// <param name="tokens">Tokens of all methods that reference this method body</param>
		protected DotNetMethodBody(DotNetMethodProvider methodProvider, string name, HexBufferSpan span, ReadOnlyCollection<uint> tokens)
			: base(name, span) {
			if (tokens is null)
				throw new ArgumentOutOfRangeException(nameof(tokens));
			if (tokens.Count == 0)
				throw new ArgumentOutOfRangeException(nameof(tokens));
			MethodProvider = methodProvider ?? throw new ArgumentNullException(nameof(methodProvider));
			Tokens = tokens;
		}

		/// <summary>
		/// Gets the kind
		/// </summary>
		public abstract DotNetMethodBodyKind Kind { get; }

		/// <summary>
		/// Gets the instruction bytes
		/// </summary>
		public abstract StructField<VirtualArrayData<ByteData>> Instructions { get; }
	}

	/// <summary>
	/// Invalid .NET method body
	/// </summary>
	public abstract class InvalidMethodBody : DotNetMethodBody {
		const string NAME = "IMAGE_COR_ILMETHOD_???";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="methodProvider">Owner</param>
		/// <param name="span">Span</param>
		/// <param name="tokens">Tokens of all methods that reference this method body</param>
		protected InvalidMethodBody(DotNetMethodProvider methodProvider, HexBufferSpan span, ReadOnlyCollection<uint> tokens)
			: base(methodProvider, NAME, span, tokens) {
		}

		/// <summary>
		/// Gets the kind
		/// </summary>
		public sealed override DotNetMethodBodyKind Kind => DotNetMethodBodyKind.Invalid;
	}

	/// <summary>
	/// Tiny .NET method body
	/// </summary>
	public abstract class TinyMethodBody : DotNetMethodBody {
		const string NAME = "IMAGE_COR_ILMETHOD_TINY";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="methodProvider">Owner</param>
		/// <param name="span">Span</param>
		/// <param name="tokens">Tokens of all methods that reference this method body</param>
		protected TinyMethodBody(DotNetMethodProvider methodProvider, HexBufferSpan span, ReadOnlyCollection<uint> tokens)
			: base(methodProvider, NAME, span, tokens) {
		}

		/// <summary>
		/// Gets the kind
		/// </summary>
		public sealed override DotNetMethodBodyKind Kind => DotNetMethodBodyKind.Tiny;

		/// <summary>IMAGE_COR_ILMETHOD_TINY.Flags_CodeSize</summary>
		public abstract StructField<ByteFlagsData> Flags_CodeSize { get; }
	}

	/// <summary>
	/// Fat .NET method body
	/// </summary>
	public abstract class FatMethodBody : DotNetMethodBody {
		const string NAME = "IMAGE_COR_ILMETHOD_FAT";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="methodProvider">Owner</param>
		/// <param name="span">Span</param>
		/// <param name="tokens">Tokens of all methods that reference this method body</param>
		protected FatMethodBody(DotNetMethodProvider methodProvider, HexBufferSpan span, ReadOnlyCollection<uint> tokens)
			: base(methodProvider, NAME, span, tokens) {
		}

		/// <summary>
		/// Gets the kind
		/// </summary>
		public sealed override DotNetMethodBodyKind Kind => DotNetMethodBodyKind.Fat;

		/// <summary>IMAGE_COR_ILMETHOD_FAT.Flags / Size</summary>
		public abstract StructField<UInt16FlagsData> Flags_Size { get; }

		/// <summary>IMAGE_COR_ILMETHOD_FAT.MaxStack</summary>
		public abstract StructField<UInt16Data> MaxStack { get; }

		/// <summary>IMAGE_COR_ILMETHOD_FAT.CodeSize</summary>
		public abstract StructField<UInt32Data> CodeSize { get; }

		/// <summary>IMAGE_COR_ILMETHOD_FAT.LocalVarSigTok</summary>
		public abstract StructField<TokenData> LocalVarSigTok { get; }

		/// <summary>
		/// Padding between <see cref="DotNetMethodBody.Instructions"/> and <see cref="EHTable"/>.
		/// It's null if <see cref="EHTable"/> isn't present.
		/// </summary>
		public abstract StructField<VirtualArrayData<ByteData>>? Padding { get; }

		/// <summary>
		/// Gets the exception handler table or null if there's none
		/// </summary>
		public abstract StructField<ExceptionHandlerTable>? EHTable { get; }
	}

	/// <summary>
	/// .NET method body section
	/// </summary>
	public abstract class DotNetMethodSection : StructureData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="span">Span</param>
		protected DotNetMethodSection(string name, HexBufferSpan span)
			: base(name, span) {
		}

		/// <summary>
		/// Returns true if this is a small section
		/// </summary>
		public abstract bool IsSmall { get; }
	}

	/// <summary>
	/// Small section
	/// </summary>
	public abstract class SmallSection : DotNetMethodSection {
		const string NAME = "IMAGE_COR_ILMETHOD_SECT_SMALL";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected SmallSection(HexBufferSpan span)
			: base(NAME, span) {
			if (span.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Returns true since this is a small section
		/// </summary>
		public override bool IsSmall => true;

		/// <summary>IMAGE_COR_ILMETHOD_SECT_SMALL.Kind</summary>
		public abstract StructField<ByteFlagsData> Kind { get; }

		/// <summary>IMAGE_COR_ILMETHOD_SECT_SMALL.DataSize</summary>
		public abstract StructField<ByteData> DataSize { get; }
	}

	/// <summary>
	/// Fat section
	/// </summary>
	public abstract class FatSection : DotNetMethodSection {
		const string NAME = "IMAGE_COR_ILMETHOD_SECT_FAT";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected FatSection(HexBufferSpan span)
			: base(NAME, span) {
			if (span.Length != 4)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Returns false since this is a fat section
		/// </summary>
		public override bool IsSmall => false;

		/// <summary>IMAGE_COR_ILMETHOD_SECT_FAT.Kind</summary>
		public abstract StructField<ByteFlagsData> Kind { get; }

		/// <summary>IMAGE_COR_ILMETHOD_SECT_FAT.DataSize</summary>
		public abstract StructField<UInt24Data> DataSize { get; }
	}

	/// <summary>
	/// Exception handler table
	/// </summary>
	public abstract class ExceptionHandlerTable : StructureData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="span">Span</param>
		protected ExceptionHandlerTable(string name, HexBufferSpan span)
			: base(name, span) {
		}

		/// <summary>
		/// true if it's a small exception handler table
		/// </summary>
		public abstract bool IsSmall { get; }
	}

	/// <summary>
	/// Small exception handler table
	/// </summary>
	public abstract class SmallExceptionHandlerTable : ExceptionHandlerTable {
		const string NAME = "IMAGE_COR_ILMETHOD_SECT_EH_SMALL";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected SmallExceptionHandlerTable(HexBufferSpan span)
			: base(NAME, span) {
		}

		/// <summary>
		/// Returns true since this is a small exception handler table
		/// </summary>
		public override bool IsSmall => true;

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_SMALL.SectSmall</summary>
		public abstract StructField<SmallSection> SectSmall { get; }

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_SMALL.Reserved</summary>
		public abstract StructField<UInt16Data> Reserved { get; }

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_SMALL.Clauses</summary>
		public abstract StructField<ArrayData<SmallExceptionClause>> Clauses { get; }
	}

	/// <summary>
	/// Fat exception handler table
	/// </summary>
	public abstract class FatExceptionHandlerTable : ExceptionHandlerTable {
		const string NAME = "IMAGE_COR_ILMETHOD_SECT_EH_FAT";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected FatExceptionHandlerTable(HexBufferSpan span)
			: base(NAME, span) {
		}

		/// <summary>
		/// Returns false since this is a fat exception handler table
		/// </summary>
		public override bool IsSmall => false;

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_FAT.SectFat</summary>
		public abstract StructField<FatSection> SectFat { get; }

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_FAT.Clauses</summary>
		public abstract StructField<ArrayData<FatExceptionClause>> Clauses { get; }
	}

	/// <summary>
	/// Exception clause
	/// </summary>
	public abstract class ExceptionClause : StructureData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="span">Span</param>
		protected ExceptionClause(string name, HexBufferSpan span)
			: base(name, span) {
		}

		/// <summary>
		/// true if it's a small exception clause
		/// </summary>
		public abstract bool IsSmall { get; }
	}

	/// <summary>
	/// Small exception clause
	/// </summary>
	public abstract class SmallExceptionClause : ExceptionClause {
		const string NAME = "IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL";

		/// <summary>
		/// Constructor
		/// </summary>
		protected SmallExceptionClause(HexBufferSpan span)
			: base(NAME, span) {
			if (span.Length != 12)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Returns true since this is a small exception clause
		/// </summary>
		public override bool IsSmall => true;

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL.Flags</summary>
		public abstract StructField<UInt16FlagsData> Flags { get; }

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL.TryOffset</summary>
		public abstract StructField<UInt16Data> TryOffset { get; }

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL.TryLength</summary>
		public abstract StructField<ByteData> TryLength { get; }

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL.HandlerOffset</summary>
		public abstract StructField<UInt16Data> HandlerOffset { get; }

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL.HandlerLength</summary>
		public abstract StructField<ByteData> HandlerLength { get; }

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL.ClassToken/FilterOffset</summary>
		public abstract StructField<UInt32Data> ClassTokenOrFilterOffset { get; }
	}

	/// <summary>
	/// Fat exception clause
	/// </summary>
	public abstract class FatExceptionClause : ExceptionClause {
		const string NAME = "IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT";

		/// <summary>
		/// Constructor
		/// </summary>
		protected FatExceptionClause(HexBufferSpan span)
			: base(NAME, span) {
			if (span.Length != 24)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Returns false since this is a fat exception clause
		/// </summary>
		public override bool IsSmall => false;

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT.Flags</summary>
		public abstract StructField<UInt32FlagsData> Flags { get; }

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT.TryOffset</summary>
		public abstract StructField<UInt32Data> TryOffset { get; }

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT.TryLength</summary>
		public abstract StructField<UInt32Data> TryLength { get; }

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT.HandlerOffset</summary>
		public abstract StructField<UInt32Data> HandlerOffset { get; }

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT.HandlerLength</summary>
		public abstract StructField<UInt32Data> HandlerLength { get; }

		/// <summary>IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT.ClassToken / FilterOffset</summary>
		public abstract StructField<UInt32Data> ClassTokenOrFilterOffset { get; }
	}
}
