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

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// #Pdb heap header
	/// </summary>
	public abstract class PdbStreamHeaderData : StructureData {
		const string NAME = "PortablePdb";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected PdbStreamHeaderData(HexBufferSpan span)
			: base(NAME, span) {
		}

		/// <summary>PDB id</summary>
		public abstract StructField<PortablePdbIdData> PdbId { get; }
		/// <summary>EntryPoint</summary>
		public abstract StructField<TokenData> EntryPoint { get; }
		/// <summary>ReferencedTypeSystemTables</summary>
		public abstract StructField<UInt64FlagsData> ReferencedTypeSystemTables { get; }
		/// <summary>TypeSystemTableRows</summary>
		public abstract StructField<ArrayData<UInt32Data>> TypeSystemTableRows { get; }
	}
}
