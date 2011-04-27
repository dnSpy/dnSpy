//
// Utilities.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Mono.Cecil.Metadata;

namespace Mono.Cecil {

	static partial class Mixin {

		public static uint ReadCompressedUInt32 (this byte [] data, ref int position)
		{
			uint integer;
			if ((data [position] & 0x80) == 0) {
				integer = data [position];
				position++;
			} else if ((data [position] & 0x40) == 0) {
				integer = (uint) (data [position] & ~0x80) << 8;
				integer |= data [position + 1];
				position += 2;
			} else {
				integer = (uint) (data [position] & ~0xc0) << 24;
				integer |= (uint) data [position + 1] << 16;
				integer |= (uint) data [position + 2] << 8;
				integer |= (uint) data [position + 3];
				position += 4;
			}
			return integer;
		}

		public static MetadataToken GetMetadataToken (this CodedIndex self, uint data)
		{
			uint rid;
			TokenType token_type;
			switch (self) {
			case CodedIndex.TypeDefOrRef:
				rid = data >> 2;
				switch (data & 3) {
				case 0:
					token_type = TokenType.TypeDef; goto ret;
				case 1:
					token_type = TokenType.TypeRef; goto ret;
				case 2:
					token_type = TokenType.TypeSpec; goto ret;
				default:
					goto exit;
				}
			case CodedIndex.HasConstant:
				rid = data >> 2;
				switch (data & 3) {
				case 0:
					token_type = TokenType.Field; goto ret;
				case 1:
					token_type = TokenType.Param; goto ret;
				case 2:
					token_type = TokenType.Property; goto ret;
				default:
					goto exit;
				}
			case CodedIndex.HasCustomAttribute:
				rid = data >> 5;
				switch (data & 31) {
				case 0:
					token_type = TokenType.Method; goto ret;
				case 1:
					token_type = TokenType.Field; goto ret;
				case 2:
					token_type = TokenType.TypeRef; goto ret;
				case 3:
					token_type = TokenType.TypeDef; goto ret;
				case 4:
					token_type = TokenType.Param; goto ret;
				case 5:
					token_type = TokenType.InterfaceImpl; goto ret;
				case 6:
					token_type = TokenType.MemberRef; goto ret;
				case 7:
					token_type = TokenType.Module; goto ret;
				case 8:
					token_type = TokenType.Permission; goto ret;
				case 9:
					token_type = TokenType.Property; goto ret;
				case 10:
					token_type = TokenType.Event; goto ret;
				case 11:
					token_type = TokenType.Signature; goto ret;
				case 12:
					token_type = TokenType.ModuleRef; goto ret;
				case 13:
					token_type = TokenType.TypeSpec; goto ret;
				case 14:
					token_type = TokenType.Assembly; goto ret;
				case 15:
					token_type = TokenType.AssemblyRef; goto ret;
				case 16:
					token_type = TokenType.File; goto ret;
				case 17:
					token_type = TokenType.ExportedType; goto ret;
				case 18:
					token_type = TokenType.ManifestResource; goto ret;
				case 19:
					token_type = TokenType.GenericParam; goto ret;
				default:
					goto exit;
				}
			case CodedIndex.HasFieldMarshal:
				rid = data >> 1;
				switch (data & 1) {
				case 0:
					token_type = TokenType.Field; goto ret;
				case 1:
					token_type = TokenType.Param; goto ret;
				default:
					goto exit;
				}
			case CodedIndex.HasDeclSecurity:
				rid = data >> 2;
				switch (data & 3) {
				case 0:
					token_type = TokenType.TypeDef; goto ret;
				case 1:
					token_type = TokenType.Method; goto ret;
				case 2:
					token_type = TokenType.Assembly; goto ret;
				default:
					goto exit;
				}
			case CodedIndex.MemberRefParent:
				rid = data >> 3;
				switch (data & 7) {
				case 0:
					token_type = TokenType.TypeDef; goto ret;
				case 1:
					token_type = TokenType.TypeRef; goto ret;
				case 2:
					token_type = TokenType.ModuleRef; goto ret;
				case 3:
					token_type = TokenType.Method; goto ret;
				case 4:
					token_type = TokenType.TypeSpec; goto ret;
				default:
					goto exit;
				}
			case CodedIndex.HasSemantics:
				rid = data >> 1;
				switch (data & 1) {
				case 0:
					token_type = TokenType.Event; goto ret;
				case 1:
					token_type = TokenType.Property; goto ret;
				default:
					goto exit;
				}
			case CodedIndex.MethodDefOrRef:
				rid = data >> 1;
				switch (data & 1) {
				case 0:
					token_type = TokenType.Method; goto ret;
				case 1:
					token_type = TokenType.MemberRef; goto ret;
				default:
					goto exit;
				}
			case CodedIndex.MemberForwarded:
				rid = data >> 1;
				switch (data & 1) {
				case 0:
					token_type = TokenType.Field; goto ret;
				case 1:
					token_type = TokenType.Method; goto ret;
				default:
					goto exit;
				}
			case CodedIndex.Implementation:
				rid = data >> 2;
				switch (data & 3) {
				case 0:
					token_type = TokenType.File; goto ret;
				case 1:
					token_type = TokenType.AssemblyRef; goto ret;
				case 2:
					token_type = TokenType.ExportedType; goto ret;
				default:
					goto exit;
				}
			case CodedIndex.CustomAttributeType:
				rid = data >> 3;
				switch (data & 7) {
				case 2:
					token_type = TokenType.Method; goto ret;
				case 3:
					token_type = TokenType.MemberRef; goto ret;
				default:
					goto exit;
				}
			case CodedIndex.ResolutionScope:
				rid = data >> 2;
				switch (data & 3) {
				case 0:
					token_type = TokenType.Module; goto ret;
				case 1:
					token_type = TokenType.ModuleRef; goto ret;
				case 2:
					token_type = TokenType.AssemblyRef; goto ret;
				case 3:
					token_type = TokenType.TypeRef; goto ret;
				default:
					goto exit;
				}
			case CodedIndex.TypeOrMethodDef:
				rid = data >> 1;
				switch (data & 1) {
				case 0:
					token_type = TokenType.TypeDef; goto ret;
				case 1:
					token_type = TokenType.Method; goto ret;
				default: goto exit;
				}
			default:
				goto exit;
			}
		ret:
			return new MetadataToken (token_type, rid);
		exit:
			return MetadataToken.Zero;
		}

#if !READ_ONLY
		public static uint CompressMetadataToken (this CodedIndex self, MetadataToken token)
		{
			uint ret = 0;
			if (token.RID == 0)
				return ret;
			switch (self) {
			case CodedIndex.TypeDefOrRef:
				ret = token.RID << 2;
				switch (token.TokenType) {
				case TokenType.TypeDef:
					return ret | 0;
				case TokenType.TypeRef:
					return ret | 1;
				case TokenType.TypeSpec:
					return ret | 2;
				default:
					goto exit;
				}
			case CodedIndex.HasConstant:
				ret = token.RID << 2;
				switch (token.TokenType) {
				case TokenType.Field:
					return ret | 0;
				case TokenType.Param:
					return ret | 1;
				case TokenType.Property:
					return ret | 2;
				default:
					goto exit;
				}
			case CodedIndex.HasCustomAttribute:
				ret = token.RID << 5;
				switch (token.TokenType) {
				case TokenType.Method:
					return ret | 0;
				case TokenType.Field:
					return ret | 1;
				case TokenType.TypeRef:
					return ret | 2;
				case TokenType.TypeDef:
					return ret | 3;
				case TokenType.Param:
					return ret | 4;
				case TokenType.InterfaceImpl:
					return ret | 5;
				case TokenType.MemberRef:
					return ret | 6;
				case TokenType.Module:
					return ret | 7;
				case TokenType.Permission:
					return ret | 8;
				case TokenType.Property:
					return ret | 9;
				case TokenType.Event:
					return ret | 10;
				case TokenType.Signature:
					return ret | 11;
				case TokenType.ModuleRef:
					return ret | 12;
				case TokenType.TypeSpec:
					return ret | 13;
				case TokenType.Assembly:
					return ret | 14;
				case TokenType.AssemblyRef:
					return ret | 15;
				case TokenType.File:
					return ret | 16;
				case TokenType.ExportedType:
					return ret | 17;
				case TokenType.ManifestResource:
					return ret | 18;
				case TokenType.GenericParam:
					return ret | 19;
				default:
					goto exit;
				}
			case CodedIndex.HasFieldMarshal:
				ret = token.RID << 1;
				switch (token.TokenType) {
				case TokenType.Field:
					return ret | 0;
				case TokenType.Param:
					return ret | 1;
				default:
					goto exit;
				}
			case CodedIndex.HasDeclSecurity:
				ret = token.RID << 2;
				switch (token.TokenType) {
				case TokenType.TypeDef:
					return ret | 0;
				case TokenType.Method:
					return ret | 1;
				case TokenType.Assembly:
					return ret | 2;
				default:
					goto exit;
				}
			case CodedIndex.MemberRefParent:
				ret = token.RID << 3;
				switch (token.TokenType) {
				case TokenType.TypeDef:
					return ret | 0;
				case TokenType.TypeRef:
					return ret | 1;
				case TokenType.ModuleRef:
					return ret | 2;
				case TokenType.Method:
					return ret | 3;
				case TokenType.TypeSpec:
					return ret | 4;
				default:
					goto exit;
				}
			case CodedIndex.HasSemantics:
				ret = token.RID << 1;
				switch (token.TokenType) {
				case TokenType.Event:
					return ret | 0;
				case TokenType.Property:
					return ret | 1;
				default:
					goto exit;
				}
			case CodedIndex.MethodDefOrRef:
				ret = token.RID << 1;
				switch (token.TokenType) {
				case TokenType.Method:
					return ret | 0;
				case TokenType.MemberRef:
					return ret | 1;
				default:
					goto exit;
				}
			case CodedIndex.MemberForwarded:
				ret = token.RID << 1;
				switch (token.TokenType) {
				case TokenType.Field:
					return ret | 0;
				case TokenType.Method:
					return ret | 1;
				default:
					goto exit;
				}
			case CodedIndex.Implementation:
				ret = token.RID << 2;
				switch (token.TokenType) {
				case TokenType.File:
					return ret | 0;
				case TokenType.AssemblyRef:
					return ret | 1;
				case TokenType.ExportedType:
					return ret | 2;
				default:
					goto exit;
				}
			case CodedIndex.CustomAttributeType:
				ret = token.RID << 3;
				switch (token.TokenType) {
				case TokenType.Method:
					return ret | 2;
				case TokenType.MemberRef:
					return ret | 3;
				default:
					goto exit;
				}
			case CodedIndex.ResolutionScope:
				ret = token.RID << 2;
				switch (token.TokenType) {
				case TokenType.Module:
					return ret | 0;
				case TokenType.ModuleRef:
					return ret | 1;
				case TokenType.AssemblyRef:
					return ret | 2;
				case TokenType.TypeRef:
					return ret | 3;
				default:
					goto exit;
				}
			case CodedIndex.TypeOrMethodDef:
				ret = token.RID << 1;
				switch (token.TokenType) {
				case TokenType.TypeDef:
					return ret | 0;
				case TokenType.Method:
					return ret | 1;
				default:
					goto exit;
				}
			default:
				goto exit;
			}
		exit:
			throw new ArgumentException ();
		}
#endif

		public static int GetSize (this CodedIndex self, Func<Table, int> counter)
		{
			int bits;
			Table [] tables;

			switch (self) {
			case CodedIndex.TypeDefOrRef:
				bits = 2;
				tables = new [] { Table.TypeDef, Table.TypeRef, Table.TypeSpec };
				break;
			case CodedIndex.HasConstant:
				bits = 2;
				tables = new [] { Table.Field, Table.Param, Table.Property };
				break;
			case CodedIndex.HasCustomAttribute:
				bits = 5;
				tables = new [] {
					Table.Method, Table.Field, Table.TypeRef, Table.TypeDef, Table.Param, Table.InterfaceImpl, Table.MemberRef,
					Table.Module, Table.DeclSecurity, Table.Property, Table.Event, Table.StandAloneSig, Table.ModuleRef,
					Table.TypeSpec, Table.Assembly, Table.AssemblyRef, Table.File, Table.ExportedType,
					Table.ManifestResource, Table.GenericParam
				};
				break;
			case CodedIndex.HasFieldMarshal:
				bits = 1;
				tables = new [] { Table.Field, Table.Param };
				break;
			case CodedIndex.HasDeclSecurity:
				bits = 2;
				tables = new [] { Table.TypeDef, Table.Method, Table.Assembly };
				break;
			case CodedIndex.MemberRefParent:
				bits = 3;
				tables = new [] { Table.TypeDef, Table.TypeRef, Table.ModuleRef, Table.Method, Table.TypeSpec };
				break;
			case CodedIndex.HasSemantics:
				bits = 1;
				tables = new [] { Table.Event, Table.Property };
				break;
			case CodedIndex.MethodDefOrRef:
				bits = 1;
				tables = new [] { Table.Method, Table.MemberRef };
				break;
			case CodedIndex.MemberForwarded:
				bits = 1;
				tables = new [] { Table.Field, Table.Method };
				break;
			case CodedIndex.Implementation:
				bits = 2;
				tables = new [] { Table.File, Table.AssemblyRef, Table.ExportedType };
				break;
			case CodedIndex.CustomAttributeType:
				bits = 3;
				tables = new [] { Table.Method, Table.MemberRef };
				break;
			case CodedIndex.ResolutionScope:
				bits = 2;
				tables = new [] { Table.Module, Table.ModuleRef, Table.AssemblyRef, Table.TypeRef };
				break;
			case CodedIndex.TypeOrMethodDef:
				bits = 1;
				tables = new [] { Table.TypeDef, Table.Method };
				break;
			default:
				throw new ArgumentException ();
			}

			int max = 0;

			for (int i = 0; i < tables.Length; i++) {
				max = System.Math.Max (counter (tables [i]), max);
			}

			return max < (1 << (16 - bits)) ? 2 : 4;
		}
	}
}
