//
// BlobReader.cs
//
// Author:
//       Daniel Grunwald <daniel@danielgrunwald.de>
//
// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	sealed class BlobReader
	{
		internal static int GetBlobHashCode(byte[] blob)
		{
			unchecked {
				int hash = 0;
				foreach (byte b in blob) {
					hash *= 257;
					hash += b;
				}
				return hash;
			}
		}
		
		internal static bool BlobEquals(byte[] a, byte[] b)
		{
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}
		
		byte[] buffer;
		int position;
		readonly IAssembly currentResolvedAssembly;

		public BlobReader(byte[] buffer, IAssembly currentResolvedAssembly)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			this.buffer = buffer;
			this.currentResolvedAssembly = currentResolvedAssembly;
		}
		
		public byte ReadByte()
		{
			return buffer[position++];
		}

		public sbyte ReadSByte()
		{
			unchecked {
				return(sbyte) ReadByte();
			}
		}
		
		public byte[] ReadBytes(int length)
		{
			var bytes = new byte[length];
			Buffer.BlockCopy(buffer, position, bytes, 0, length);
			position += length;
			return bytes;
		}

		public ushort ReadUInt16()
		{
			unchecked {
				ushort value =(ushort)(buffer[position]
				                       |(buffer[position + 1] << 8));
				position += 2;
				return value;
			}
		}

		public short ReadInt16()
		{
			unchecked {
				return(short) ReadUInt16();
			}
		}

		public uint ReadUInt32()
		{
			unchecked {
				uint value =(uint)(buffer[position]
				                   |(buffer[position + 1] << 8)
				                   |(buffer[position + 2] << 16)
				                   |(buffer[position + 3] << 24));
				position += 4;
				return value;
			}
		}

		public int ReadInt32()
		{
			unchecked {
				return(int) ReadUInt32();
			}
		}

		public ulong ReadUInt64()
		{
			unchecked {
				uint low = ReadUInt32();
				uint high = ReadUInt32();

				return(((ulong) high) << 32) | low;
			}
		}

		public long ReadInt64()
		{
			unchecked {
				return(long) ReadUInt64();
			}
		}

		public uint ReadCompressedUInt32()
		{
			unchecked {
				byte first = ReadByte();
				if((first & 0x80) == 0)
					return first;

				if((first & 0x40) == 0)
					return((uint)(first & ~0x80) << 8)
						| ReadByte();

				return((uint)(first & ~0xc0) << 24)
					|(uint) ReadByte() << 16
					|(uint) ReadByte() << 8
					| ReadByte();
			}
		}

		public float ReadSingle()
		{
			unchecked {
				if(!BitConverter.IsLittleEndian) {
					var bytes = ReadBytes(4);
					Array.Reverse(bytes);
					return BitConverter.ToSingle(bytes, 0);
				}

				float value = BitConverter.ToSingle(buffer, position);
				position += 4;
				return value;
			}
		}

		public double ReadDouble()
		{
			unchecked {
				if(!BitConverter.IsLittleEndian) {
					var bytes = ReadBytes(8);
					Array.Reverse(bytes);
					return BitConverter.ToDouble(bytes, 0);
				}

				double value = BitConverter.ToDouble(buffer, position);
				position += 8;
				return value;
			}
		}
		
		public ResolveResult ReadFixedArg(IType argType)
		{
			if (argType.Kind == TypeKind.Array) {
				if (((ArrayType)argType).Dimensions != 1) {
					// Only single-dimensional arrays are supported
					return ErrorResolveResult.UnknownError;
				}
				IType elementType = ((ArrayType)argType).ElementType;
				uint numElem = ReadUInt32();
				if (numElem == 0xffffffff) {
					// null reference
					return new ConstantResolveResult(argType, null);
				} else {
					ResolveResult[] elements = new ResolveResult[numElem];
					for (int i = 0; i < elements.Length; i++) {
						elements[i] = ReadElem(elementType);
						// Stop decoding when encountering an error:
						if (elements[i].IsError)
							return ErrorResolveResult.UnknownError;
					}
					IType int32 = currentResolvedAssembly.Compilation.FindType(KnownTypeCode.Int32);
					ResolveResult[] sizeArgs = { new ConstantResolveResult(int32, elements.Length) };
					return new ArrayCreateResolveResult(argType, sizeArgs, elements);
				}
			} else {
				return ReadElem(argType);
			}
		}
		
		public ResolveResult ReadElem(IType elementType)
		{
			ITypeDefinition underlyingType;
			if (elementType.Kind == TypeKind.Enum) {
				underlyingType = elementType.GetDefinition().EnumUnderlyingType.GetDefinition();
			} else {
				underlyingType = elementType.GetDefinition();
			}
			if (underlyingType == null)
				return ErrorResolveResult.UnknownError;
			KnownTypeCode typeCode = underlyingType.KnownTypeCode;
			if (typeCode == KnownTypeCode.Object) {
				// boxed value type
				IType boxedTyped = ReadCustomAttributeFieldOrPropType();
				ResolveResult elem = ReadElem(boxedTyped);
				if (elem.IsCompileTimeConstant && elem.ConstantValue == null)
					return new ConstantResolveResult(elementType, null);
				else
					return new ConversionResolveResult(elementType, elem, Conversion.BoxingConversion);
			} else if (typeCode == KnownTypeCode.Type) {
				return new TypeOfResolveResult(underlyingType, ReadType());
			} else {
				return new ConstantResolveResult(elementType, ReadElemValue(typeCode));
			}
		}
		
		object ReadElemValue(KnownTypeCode typeCode)
		{
			switch (typeCode) {
				case KnownTypeCode.Boolean:
					return ReadByte() != 0;
				case KnownTypeCode.Char:
					return (char)ReadUInt16();
				case KnownTypeCode.SByte:
					return ReadSByte();
				case KnownTypeCode.Byte:
					return ReadByte();
				case KnownTypeCode.Int16:
					return ReadInt16();
				case KnownTypeCode.UInt16:
					return ReadUInt16();
				case KnownTypeCode.Int32:
					return ReadInt32();
				case KnownTypeCode.UInt32:
					return ReadUInt32();
				case KnownTypeCode.Int64:
					return ReadInt64();
				case KnownTypeCode.UInt64:
					return ReadUInt64();
				case KnownTypeCode.Single:
					return ReadSingle();
				case KnownTypeCode.Double:
					return ReadDouble();
				case KnownTypeCode.String:
					return ReadSerString();
				default:
					throw new NotSupportedException();
			}
		}
		
		public string ReadSerString ()
		{
			if (buffer [position] == 0xff) {
				position++;
				return null;
			}

			int length = (int) ReadCompressedUInt32();
			if (length == 0)
				return string.Empty;

			string @string = System.Text.Encoding.UTF8.GetString(
				buffer, position,
				buffer [position + length - 1] == 0 ? length - 1 : length);

			position += length;
			return @string;
		}
		
		public KeyValuePair<IMember, ResolveResult> ReadNamedArg(IType attributeType)
		{
			SymbolKind memberType;
			var b = ReadByte();
			switch (b) {
				case 0x53:
					memberType = SymbolKind.Field;
					break;
				case 0x54:
					memberType = SymbolKind.Property;
					break;
				default:
					throw new NotSupportedException(string.Format("Custom member type 0x{0:x} is not supported.", b));
			}
			IType type = ReadCustomAttributeFieldOrPropType();
			string name = ReadSerString();
			ResolveResult val = ReadFixedArg(type);
			IMember member = null;
			// Use last matching member, as GetMembers() returns members from base types first.
			foreach (IMember m in attributeType.GetMembers(m => m.SymbolKind == memberType && m.Name == name)) {
				if (m.ReturnType.Equals(type))
					member = m;
			}
			return new KeyValuePair<IMember, ResolveResult>(member, val);
		}

		IType ReadCustomAttributeFieldOrPropType()
		{
			ICompilation compilation = currentResolvedAssembly.Compilation;
			var b = ReadByte();
			switch (b) {
				case 0x02:
					return compilation.FindType(KnownTypeCode.Boolean);
				case 0x03:
					return compilation.FindType(KnownTypeCode.Char);
				case 0x04:
					return compilation.FindType(KnownTypeCode.SByte);
				case 0x05:
					return compilation.FindType(KnownTypeCode.Byte);
				case 0x06:
					return compilation.FindType(KnownTypeCode.Int16);
				case 0x07:
					return compilation.FindType(KnownTypeCode.UInt16);
				case 0x08:
					return compilation.FindType(KnownTypeCode.Int32);
				case 0x09:
					return compilation.FindType(KnownTypeCode.UInt32);
				case 0x0a:
					return compilation.FindType(KnownTypeCode.Int64);
				case 0x0b:
					return compilation.FindType(KnownTypeCode.UInt64);
				case 0x0c:
					return compilation.FindType(KnownTypeCode.Single);
				case 0x0d:
					return compilation.FindType(KnownTypeCode.Double);
				case 0x0e:
					return compilation.FindType(KnownTypeCode.String);
				case 0x1d:
					return new ArrayType(compilation, ReadCustomAttributeFieldOrPropType());
				case 0x50:
					return compilation.FindType(KnownTypeCode.Type);
				case 0x51: // boxed value type
					return compilation.FindType(KnownTypeCode.Object);
				case 0x55: // enum
					return ReadType();
				default:
					throw new NotSupportedException(string.Format("Custom attribute type 0x{0:x} is not supported.", b));
			}
		}
		
		IType ReadType()
		{
			string typeName = ReadSerString();
			ITypeReference typeReference = ReflectionHelper.ParseReflectionName(typeName);
			IType typeInCurrentAssembly = typeReference.Resolve(new SimpleTypeResolveContext(currentResolvedAssembly));
			if (typeInCurrentAssembly.Kind != TypeKind.Unknown)
				return typeInCurrentAssembly;
			
			// look for the type in mscorlib
			ITypeDefinition systemObject = currentResolvedAssembly.Compilation.FindType(KnownTypeCode.Object).GetDefinition();
			if (systemObject != null) {
				return typeReference.Resolve(new SimpleTypeResolveContext(systemObject.ParentAssembly));
			} else {
				// couldn't find corlib - return the unknown IType for the current assembly
				return typeInCurrentAssembly;
			}
		}
	}
}
