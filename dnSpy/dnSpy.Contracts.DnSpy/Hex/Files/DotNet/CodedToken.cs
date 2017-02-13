/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

// from dnlib

using System;
using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// Contains all possible coded token classes
	/// </summary>
	public sealed class CodedToken {
		/// <summary>TypeDefOrRef coded token</summary>
		public static readonly CodedToken TypeDefOrRef = new CodedToken(2, new Table[3] {
			Table.TypeDef, Table.TypeRef, Table.TypeSpec,
		});

		/// <summary>HasConstant coded token</summary>
		public static readonly CodedToken HasConstant = new CodedToken(2, new Table[3] {
			Table.Field, Table.Param, Table.Property,
		});

		/// <summary>HasCustomAttribute coded token</summary>
		public static readonly CodedToken HasCustomAttribute = new CodedToken(5, new Table[24] {
			Table.Method, Table.Field, Table.TypeRef, Table.TypeDef,
			Table.Param, Table.InterfaceImpl, Table.MemberRef, Table.Module,
			Table.DeclSecurity, Table.Property, Table.Event, Table.StandAloneSig,
			Table.ModuleRef, Table.TypeSpec, Table.Assembly, Table.AssemblyRef,
			Table.File, Table.ExportedType, Table.ManifestResource, Table.GenericParam,
			Table.GenericParamConstraint, Table.MethodSpec, 0, 0,
		});

		/// <summary>HasFieldMarshal coded token</summary>
		public static readonly CodedToken HasFieldMarshal = new CodedToken(1, new Table[2] {
			Table.Field, Table.Param,
		});

		/// <summary>HasDeclSecurity coded token</summary>
		public static readonly CodedToken HasDeclSecurity = new CodedToken(2, new Table[3] {
			Table.TypeDef, Table.Method, Table.Assembly,
		});

		/// <summary>MemberRefParent coded token</summary>
		public static readonly CodedToken MemberRefParent = new CodedToken(3, new Table[5] {
			Table.TypeDef, Table.TypeRef, Table.ModuleRef, Table.Method,
			Table.TypeSpec,
		});

		/// <summary>HasSemantic coded token</summary>
		public static readonly CodedToken HasSemantic = new CodedToken(1, new Table[2] {
			Table.Event, Table.Property,
		});

		/// <summary>MethodDefOrRef coded token</summary>
		public static readonly CodedToken MethodDefOrRef = new CodedToken(1, new Table[2] {
			Table.Method, Table.MemberRef,
		});

		/// <summary>MemberForwarded coded token</summary>
		public static readonly CodedToken MemberForwarded = new CodedToken(1, new Table[2] {
			Table.Field, Table.Method,
		});

		/// <summary>Implementation coded token</summary>
		public static readonly CodedToken Implementation = new CodedToken(2, new Table[3] {
			Table.File, Table.AssemblyRef, Table.ExportedType,
		});

		/// <summary>CustomAttributeType coded token</summary>
		public static readonly CodedToken CustomAttributeType = new CodedToken(3, new Table[4] {
			0, 0, Table.Method, Table.MemberRef,
		});

		/// <summary>ResolutionScope coded token</summary>
		public static readonly CodedToken ResolutionScope = new CodedToken(2, new Table[4] {
			Table.Module, Table.ModuleRef, Table.AssemblyRef, Table.TypeRef,
		});

		/// <summary>TypeOrMethodDef coded token</summary>
		public static readonly CodedToken TypeOrMethodDef = new CodedToken(1, new Table[2] {
			Table.TypeDef, Table.Method,
		});

		/// <summary>HasCustomDebugInformation coded token</summary>
		public static readonly CodedToken HasCustomDebugInformation = new CodedToken(5, new Table[27] {
			Table.Method, Table.Field, Table.TypeRef, Table.TypeDef,
			Table.Param, Table.InterfaceImpl, Table.MemberRef, Table.Module,
			Table.DeclSecurity, Table.Property, Table.Event, Table.StandAloneSig,
			Table.ModuleRef, Table.TypeSpec, Table.Assembly, Table.AssemblyRef,
			Table.File, Table.ExportedType, Table.ManifestResource, Table.GenericParam,
			Table.GenericParamConstraint, Table.MethodSpec, Table.Document, Table.LocalScope,
			Table.LocalVariable, Table.LocalConstant, Table.ImportScope,
		});

		readonly int mask;

		/// <summary>
		/// Returns all types of tables
		/// </summary>
		public ReadOnlyCollection<Table> TableTypes { get; }

		/// <summary>
		/// Returns the number of bits that is used to encode table type
		/// </summary>
		public int Bits { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bits">Number of bits used to encode token type</param>
		/// <param name="tableTypes">All table types</param>
		public CodedToken(int bits, Table[] tableTypes) {
			if (tableTypes == null)
				throw new ArgumentNullException(nameof(tableTypes));
			Bits = bits;
			mask = (1 << bits) - 1;
			TableTypes = new ReadOnlyCollection<Table>(tableTypes);
		}

		/// <summary>
		/// Encodes a token
		/// </summary>
		/// <param name="token">The token</param>
		/// <returns>Coded token</returns>
		/// <seealso cref="Encode(MDToken,out uint)"/>
		public uint Encode(MDToken token) {
			return Encode(token.Raw);
		}

		/// <summary>
		/// Encodes a token
		/// </summary>
		/// <param name="token">The token</param>
		/// <returns>Coded token</returns>
		/// <seealso cref="Encode(uint,out uint)"/>
		public uint Encode(uint token) {
			Encode(token, out uint codedToken);
			return codedToken;
		}

		/// <summary>
		/// Encodes a token
		/// </summary>
		/// <param name="token">The token</param>
		/// <param name="codedToken">Coded token</param>
		/// <returns><c>true</c> if successful</returns>
		public bool Encode(MDToken token, out uint codedToken) {
			return Encode(token.Raw, out codedToken);
		}

		/// <summary>
		/// Encodes a token
		/// </summary>
		/// <param name="token">The token</param>
		/// <param name="codedToken">Coded token</param>
		/// <returns><c>true</c> if successful</returns>
		public bool Encode(uint token, out uint codedToken) {
			int index = TableTypes.IndexOf(MDToken.ToTable(token));
			if (index < 0) {
				codedToken = uint.MaxValue;
				return false;
			}
			// This shift can never overflow a uint since bits < 8 (it's at most 5), and
			// ToRid() returns an integer <= 0x00FFFFFF.
			codedToken = (MDToken.ToRID(token) << Bits) | (uint)index;
			return true;
		}

		/// <summary>
		/// Decodes a coded token
		/// </summary>
		/// <param name="codedToken">The coded token</param>
		/// <returns>Decoded token or 0 on failure</returns>
		/// <seealso cref="Decode(uint,out MDToken)"/>
		public MDToken Decode2(uint codedToken) {
			Decode(codedToken, out uint token);
			return new MDToken(token);
		}

		/// <summary>
		/// Decodes a coded token
		/// </summary>
		/// <param name="codedToken">The coded token</param>
		/// <returns>Decoded token or 0 on failure</returns>
		/// <seealso cref="Decode(uint,out uint)"/>
		public uint Decode(uint codedToken) {
			Decode(codedToken, out uint token);
			return token;
		}

		/// <summary>
		/// Decodes a coded token
		/// </summary>
		/// <param name="codedToken">The coded token</param>
		/// <param name="token">Decoded token</param>
		/// <returns><c>true</c> if successful</returns>
		public bool Decode(uint codedToken, out MDToken token) {
			bool result = Decode(codedToken, out uint decodedToken);
			token = new MDToken(decodedToken);
			return result;
		}

		/// <summary>
		/// Decodes a coded token
		/// </summary>
		/// <param name="codedToken">The coded token</param>
		/// <param name="token">Decoded token</param>
		/// <returns><c>true</c> if successful</returns>
		public bool Decode(uint codedToken, out uint token) {
			uint rid = codedToken >> Bits;
			int index = (int)(codedToken & mask);
			if (rid > MDToken.RID_MAX || index >= TableTypes.Count) {
				token = 0;
				return false;
			}

			token = ((uint)TableTypes[index] << MDToken.TABLE_SHIFT) | rid;
			return true;
		}
	}
}
