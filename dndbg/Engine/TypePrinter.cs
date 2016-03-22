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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dndbg.Engine {
	public enum TypeColor {
		Unknown,
		Space,
		IPType,
		Operator,
		NativeFrame,
		InternalFrame,
		UnknownFrame,
		Number,
		Error,
		Module,
		Token,
		NamespacePart,
		InstanceProperty,
		StaticProperty,
		InstanceEvent,
		StaticEvent,
		Type,
		StaticType,
		Delegate,
		Enum,
		Interface,
		ValueType,
		Comment,
		StaticMethod,
		ExtensionMethod,
		InstanceMethod,
		TypeKeyword,
		TypeGenericParameter,
		MethodGenericParameter,
		Keyword,
		Parameter,
		String,
		Char,
		InstanceField,
		EnumField,
		LiteralField,
		StaticField,
		TypeStringBrace,
		ToStringBrace,
		ToStringResult,
	}

	public interface ITypeOutput {
		void Write(string s, TypeColor type);
	}

	public sealed class StringBuilderTypeOutput : ITypeOutput {
		readonly StringBuilder sb = new StringBuilder();

		public void Write(string s, TypeColor type) {
			sb.Append(s);
		}

		public override string ToString() {
			return sb.ToString();
		}
	}

	[Flags]
	public enum TypePrinterFlags {
		ShowModuleNames				= 0x00000001,
		ShowParameterTypes			= 0x00000002,
		ShowParameterNames			= 0x00000004,
		ShowParameterValues			= 0x00000008,
		ShowOwnerTypes				= 0x00000010,
		ShowReturnTypes				= 0x00000020,
		ShowNamespaces				= 0x00000040,
		ShowTypeKeywords			= 0x00000080,
		UseDecimal					= 0x00000100,
		ShowTokens					= 0x00000200,
		ShowIP						= 0x00000400,
		ShowArrayValueSizes			= 0x00000800,
		ShowFieldLiteralValues		= 0x00001000,
		ShowParameterLiteralValues	= 0x00002000,

		Default =
			ShowModuleNames |
			ShowParameterTypes |
			ShowParameterNames |
			ShowOwnerTypes |
			ShowNamespaces |
			ShowTypeKeywords |
			ShowArrayValueSizes |
			ShowFieldLiteralValues,
	}

	struct TypePrinter {
		const int MAX_RECURSION = 200;
		const int MAX_OUTPUT_LEN = 1024 * 4;
		int recursionCounter;
		int lineLength;
		bool outputLengthExceeded;
		bool forceWrite;
		readonly Func<DnEval> getEval;

		readonly ITypeOutput output;
		readonly TypePrinterFlags flags;
		Dictionary<CorModule, IMetaDataImport> dictMetaDataImport;

		bool ShowModuleNames {
			get { return (flags & TypePrinterFlags.ShowModuleNames) != 0; }
		}

		bool ShowParameterTypes {
			get { return (flags & TypePrinterFlags.ShowParameterTypes) != 0; }
		}

		bool ShowParameterNames {
			get { return (flags & TypePrinterFlags.ShowParameterNames) != 0; }
		}

		bool ShowParameterValues {
			get { return (flags & TypePrinterFlags.ShowParameterValues) != 0; }
		}

		bool ShowOwnerTypes {
			get { return (flags & TypePrinterFlags.ShowOwnerTypes) != 0; }
		}

		bool ShowReturnTypes {
			get { return (flags & TypePrinterFlags.ShowReturnTypes) != 0; }
		}

		bool ShowNamespaces {
			get { return (flags & TypePrinterFlags.ShowNamespaces) != 0; }
		}

		bool ShowTypeKeywords {
			get { return (flags & TypePrinterFlags.ShowTypeKeywords) != 0; }
		}

		bool UseDecimal {
			get { return (flags & TypePrinterFlags.UseDecimal) != 0; }
		}

		bool ShowTokens {
			get { return (flags & TypePrinterFlags.ShowTokens) != 0; }
		}

		bool ShowIP {
			get { return (flags & TypePrinterFlags.ShowIP) != 0; }
		}

		bool ShowArrayValueSizes {
			get { return (flags & TypePrinterFlags.ShowArrayValueSizes) != 0; }
		}

		bool ShowFieldLiteralValues {
			get { return (flags & TypePrinterFlags.ShowFieldLiteralValues) != 0; }
		}

		bool ShowParameterLiteralValues {
			get { return (flags & TypePrinterFlags.ShowParameterLiteralValues) != 0; }
		}

		public TypePrinter(ITypeOutput output, TypePrinterFlags flags, Func<DnEval> getEval = null) {
			this.output = output;
			this.flags = flags;
			this.dictMetaDataImport = null;
			this.recursionCounter = 0;
			this.lineLength = 0;
			this.outputLengthExceeded = false;
			this.forceWrite = false;
			this.getEval = getEval;
		}

		static string FilterName(string s) {
			const int MAX_NAME_LEN = 0x100;
			if (s == null)
				return "<<NULL>>";

			var sb = new StringBuilder(s.Length);

			foreach (var c in s) {
				if (sb.Length >= MAX_NAME_LEN)
					break;
				if (c >= ' ')
					sb.Append(c);
				else
					sb.Append(string.Format("\\u{0:X4}", (ushort)c));
			}

			if (sb.Length > MAX_NAME_LEN)
				sb.Length = MAX_NAME_LEN;
			return sb.ToString();
		}

		static readonly HashSet<string> isKeyword = new HashSet<string>(StringComparer.Ordinal) {
			"abstract", "as", "base", "bool", "break", "byte", "case", "catch",
			"char", "checked", "class", "const", "continue", "decimal", "default", "delegate",
			"do", "double", "else", "enum", "event", "explicit", "extern", "false",
			"finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
			"in", "int", "interface", "internal", "is", "lock", "long", "namespace",
			"new", "null", "object", "operator", "out", "override", "params", "private",
			"protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
			"sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
			"true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
			"using", "virtual", "void", "volatile", "while",
		};

		void WriteIdentifier(string id, TypeColor color) {
			if (isKeyword.Contains(id))
				OutputWrite("@", TypeColor.Operator);
			OutputWrite(IdentifierEscaper.Escape(id), color);
		}

		static string RemoveGenericTick(string s) {
			int index = s.LastIndexOf('`');
			if (index < 0)
				return s;
			if (s[0] == '<')	// check if compiler generated name
				return s;
			return s.Substring(0, index);
		}

		static string GetFileName(string s) {
			// Don't use Path.GetFileName() since it can throw if input contains invalid chars
			int index = Math.Max(s.LastIndexOf('/'), s.LastIndexOf('\\'));
			if (index < 0)
				return s;
			return s.Substring(index + 1);
		}

		void OutputWrite(string s, TypeColor color) {
			if (!forceWrite) {
				if (outputLengthExceeded)
					return;
				if (lineLength + s.Length > MAX_OUTPUT_LEN) {
					s = s.Substring(0, MAX_OUTPUT_LEN - lineLength);
					s += "[…]";
					outputLengthExceeded = true;
				}
			}
			output.Write(s, color);
			lineLength += s.Length;
		}

		void WriteSpace() {
			OutputWrite(" ", TypeColor.Space);
		}

		void WriteCommaSpace() {
			OutputWrite(",", TypeColor.Operator);
			WriteSpace();
		}

		void Write(CorModule module) {
			try {
				if (recursionCounter++ >= MAX_RECURSION)
					return;
				if (module == null) {
					OutputWrite("null module", TypeColor.Error);
					return;
				}

				var name = GetFileName(module.UniquerName);
				OutputWrite(FilterName(name), TypeColor.Module);
			}
			finally {
				recursionCounter--;
			}
		}

		IMetaDataImport GetMetaDataImport(CorModule module) {
			if (module == null)
				return null;

			if (dictMetaDataImport == null)
				dictMetaDataImport = new Dictionary<CorModule, IMetaDataImport>();

			IMetaDataImport mdi;
			if (dictMetaDataImport.TryGetValue(module, out mdi))
				return mdi;

			mdi = module.GetMetaDataInterface<IMetaDataImport>();
			if (mdi == null)
				return null;

			dictMetaDataImport.Add(module, mdi);
			return mdi;
		}

		public void Write(CorClass cls) {
			try {
				if (recursionCounter++ >= MAX_RECURSION)
					return;
				if (cls == null) {
					OutputWrite("null class", TypeColor.Error);
					return;
				}

				var mdi = GetMetaDataImport(cls.Module);
				if (mdi == null) {
					WriteDefault(cls);
					return;
				}

				WriteTypeDef(mdi, cls.Token);
			}
			finally {
				recursionCounter--;
			}
		}

		void WriteClassOrValueType(CorClass cls) {
			if (cls == null) {
				Write(cls);
				return;
			}
			var type = cls.GetParameterizedType(CorElementType.Class);
			if (type == null)
				Write(cls);
			else
				WriteClassOrValueType(type, cls);
		}

		// If anything fails, it calls Write(CorClass). Must not call Write(CorType)
		void WriteClassOrValueType(CorType type, CorClass cls) {
			if (type == null || cls == null) {
				Write(cls);
				return;
			}

			var mod = cls.Module;
			var mdi = GetMetaDataImport(mod);
			if (mdi == null) {
				Write(cls);
				return;
			}

			var types = GetEnclosingTypesAndSelf(type, cls, mod, mdi);
			if (types == null) {
				Write(cls);
				return;
			}

			for (int i = 0; i < types.Count; i++) {
				if (i > 0)
					OutputWrite(".", TypeColor.Operator);

				uint token = types[i].Item2.Token;
				var fullName = MDAPI.GetTypeDefName(mdi, token);

				var typeKeyword = !ShowTypeKeywords || i != 0 ? null : GetTypeKeyword(fullName);
				if (typeKeyword != null)
					OutputWrite(typeKeyword, TypeColor.TypeKeyword);
				else
					WriteTypeName(fullName, token, GetTypeColor(types[i].Item1, types[i].Item2));
			}
		}

		TypeColor GetTypeColor(CorType type, CorClass cls) {
			var attrs = cls.GetTypeAttributes();

			if ((attrs & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface)
				return TypeColor.Interface;
			if (type.IsEnum)
				return TypeColor.Enum;
			if (type.IsValueType)
				return TypeColor.ValueType;

			var baseType = type.Base;
			if (IsDelegate(baseType, attrs))
				return TypeColor.Delegate;

			if (baseType != null &&
				(attrs & (TypeAttributes.Sealed | TypeAttributes.Abstract)) == (TypeAttributes.Sealed | TypeAttributes.Abstract) &&
				baseType.IsSystemObject) {
				return TypeColor.StaticType;
			}

			return TypeColor.Type;
		}

		static bool IsDelegate(CorType baseType, TypeAttributes attrs) {
			if (baseType == null)
				return false;
			if ((attrs & (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.ClassSemanticsMask)) != (TypeAttributes.Sealed | TypeAttributes.Class))
				return false;
			return baseType.IsSystem("MulticastDelegate");
		}

		List<Tuple<CorType, CorClass>> GetEnclosingTypesAndSelf(CorType type, CorClass cls, CorModule module, IMetaDataImport mdi) {
			var list = new List<Tuple<CorType, CorClass>>();
			list.Add(Tuple.Create(type, cls));
			uint token = cls.Token;
			if (token == 0)
				return null;
			for (;;) {
				token = MDAPI.GetTypeDefEnclosingType(mdi, token);
				if (token == 0)
					break;

				cls = module.GetClassFromToken(token);
				if (cls == null)
					return null;
				type = cls.GetParameterizedType(CorElementType.Class);
				if (type == null)
					return null;
				list.Add(Tuple.Create(type, cls));
			}

			list.Reverse();
			return list;
		}

		void WriteTypeDef(IMetaDataImport mdi, uint token) {
			var list = MetaDataUtils.GetTypeDefFullNames(mdi, token);
			WriteTypeList(list, token);
		}

		void WriteTypeRef(IMetaDataImport mdi, uint token) {
			var list = MetaDataUtils.GetTypeRefFullNames(mdi, token);
			WriteTypeList(list, token);
		}

		void WriteTypeList(IList<TokenAndName> list, uint token) {
			if (list.Count == 0) {
				WriteDefaultType(token);
				return;
			}
			for (int i = 0; i < list.Count; i++) {
				if (i > 0)
					OutputWrite(".", TypeColor.Operator);

				var typeKeyword = !ShowTypeKeywords ? null : GetTypeKeyword(list, i);
				if (typeKeyword != null)
					OutputWrite(typeKeyword, TypeColor.TypeKeyword);
				else {
					var info = list[i];
					WriteTypeName(info.Name, info.Token, TypeColor.Type);
				}
			}
		}

		void WriteTypeSpec(IMetaDataImport mdi, uint token) {
			// This code should be unreachable
			Debug.Fail("WriteTypeSpec() should be unreachable");
			OutputWrite(string.Format("type_{0:X8}", token), TypeColor.Error);
		}

		static string GetTypeKeyword(IList<TokenAndName> list, int index) {
			if (list.Count != 1)
				return null;
			return GetTypeKeyword(list[0].Name);
		}

		static string GetTypeKeyword(string name) {
			switch (name) {
			case "System.Void":		return "void";
			case "System.Boolean":	return "bool";
			case "System.Byte":		return "byte";
			case "System.Char":		return "char";
			case "System.Decimal":	return "decimal";
			case "System.Double":	return "double";
			case "System.Int16":	return "short";
			case "System.Int32":	return "int";
			case "System.Int64":	return "long";
			case "System.Object":	return "object";
			case "System.SByte":	return "sbyte";
			case "System.Single":	return "float";
			case "System.String":	return "string";
			case "System.UInt16":	return "ushort";
			case "System.UInt32":	return "uint";
			case "System.UInt64":	return "ulong";
			default: return null;
			}
		}

		void WriteTypeName(string name, uint token, TypeColor typeColor) {
			var parts = name.Split(dot);
			if (ShowNamespaces) {
				for (int i = 1; i < parts.Length; i++) {
					WriteIdentifier(parts[i - 1], TypeColor.NamespacePart);
					OutputWrite(".", TypeColor.Operator);
				}
			}
			WriteIdentifier(RemoveGenericTick(parts[parts.Length - 1]), typeColor);
			WriteTokenComment(token);
		}
		static readonly char[] dot = new char[1] { '.' };

		void WriteDefault(CorClass cls) {
			Debug.Assert(cls != null);
			WriteDefaultType(cls.Token);
		}

		void WriteDefaultType(uint token) {
			OutputWrite("Type", TypeColor.Unknown);
			OutputWrite("[", TypeColor.Operator);
			WriteToken(token);
			OutputWrite("]", TypeColor.Operator);
		}

		public void Write(CorType type) {
			Write(type, null);
		}

		void Write(CorType type, CorValue value) {
			try {
				if (recursionCounter++ >= MAX_RECURSION)
					return;
				if (type == null) {
					OutputWrite("null type", TypeColor.Error);
					return;
				}

				if (value != null && value.IsReference && (value.Type == CorElementType.SZArray || value.Type == CorElementType.Array))
					value = value.NeuterCheckDereferencedValue ?? value;

				// It's shown reverse in C# so need to collect all array types here
				List<Tuple<CorType, CorValue>> list = null;
				while (type != null && (type.ElementType == CorElementType.SZArray || type.ElementType == CorElementType.Array)) {
					if (list == null)
						list = new List<Tuple<CorType, CorValue>>();
					list.Add(Tuple.Create(type, value));
					value = value == null ? null : value.NeuterCheckDereferencedValue;
					type = type.FirstTypeParameter;
				}
				if (list != null) {
					var t = list[list.Count - 1];
					Write(t.Item1.FirstTypeParameter, t.Item2 == null ? null : t.Item2.NeuterCheckDereferencedValue);
					foreach (var tuple in list) {
						var aryType = tuple.Item1;
						var aryValue = tuple.Item2;
						if (aryType.ElementType == CorElementType.Array) {
							OutputWrite("[", TypeColor.Operator);
							uint rank = aryType.Rank;
							if (rank == 0)
								OutputWrite("<RANK0>", TypeColor.Error);
							else {
								if (rank == 1)
									OutputWrite("*", TypeColor.Operator);
								var indexes = aryValue == null ? null : aryValue.BaseIndicies;
								var dims = aryValue == null ? null : aryValue.Dimensions;
								if (ShowArrayValueSizes && indexes != null && dims != null && (uint)indexes.Length == rank && (uint)dims.Length == rank) {
									for (uint i = 0; i < rank; i++) {
										if (i > 0) {
											OutputWrite(",", TypeColor.Operator);
											OutputWrite(" ", TypeColor.Space);
										}
										if (indexes[i] == 0)
											WriteNumber(dims[i]);
										else {
											//TODO: How does VS print these arrays?
											WriteNumber((int)indexes[i]);
											OutputWrite("..", TypeColor.Operator);
											WriteNumber((int)(indexes[i] + dims[i]));
										}
									}
								}
								else {
									for (uint i = 1; i < rank; i++)
										OutputWrite(",", TypeColor.Operator);
								}
							}
							OutputWrite("]", TypeColor.Operator);
						}
						else {
							Debug.Assert(aryType.ElementType == CorElementType.SZArray);
							OutputWrite("[", TypeColor.Operator);
							if (ShowArrayValueSizes && aryValue != null)
								WriteNumber(aryValue.ArrayCount);
							OutputWrite("]", TypeColor.Operator);
						}
					}
					return;
				}

				switch (type.ElementType) {
				case CorElementType.Void:		WriteSystemTypeKeyword("Void", "void", TypeColor.ValueType); break;
				case CorElementType.Boolean:	WriteSystemTypeKeyword("Boolean", "bool", TypeColor.ValueType); break;
				case CorElementType.Char:		WriteSystemTypeKeyword("Char", "char", TypeColor.ValueType); break;
				case CorElementType.I1:			WriteSystemTypeKeyword("SByte", "sbyte", TypeColor.ValueType); break;
				case CorElementType.U1:			WriteSystemTypeKeyword("Byte", "byte", TypeColor.ValueType); break;
				case CorElementType.I2:			WriteSystemTypeKeyword("Int16", "short", TypeColor.ValueType); break;
				case CorElementType.U2:			WriteSystemTypeKeyword("UInt16", "ushort", TypeColor.ValueType); break;
				case CorElementType.I4:			WriteSystemTypeKeyword("Int32", "int", TypeColor.ValueType); break;
				case CorElementType.U4:			WriteSystemTypeKeyword("UInt32", "uint", TypeColor.ValueType); break;
				case CorElementType.I8:			WriteSystemTypeKeyword("Int64", "long", TypeColor.ValueType); break;
				case CorElementType.U8:			WriteSystemTypeKeyword("UInt64", "ulong", TypeColor.ValueType); break;
				case CorElementType.R4:			WriteSystemTypeKeyword("Single", "float", TypeColor.ValueType); break;
				case CorElementType.R8:			WriteSystemTypeKeyword("Double", "double", TypeColor.ValueType); break;
				case CorElementType.String:		WriteSystemTypeKeyword("String", "string", TypeColor.Type); break;
				case CorElementType.Object:		WriteSystemTypeKeyword("Object", "object", TypeColor.Type); break;

				case CorElementType.TypedByRef:	WriteSystemType("TypedReference", TypeColor.ValueType); break;
				case CorElementType.I:			WriteSystemType("IntPtr", TypeColor.ValueType); break;
				case CorElementType.U:			WriteSystemType("UIntPtr", TypeColor.ValueType); break;

				case CorElementType.Ptr:
					Write(type.FirstTypeParameter, value == null ? null : value.NeuterCheckDereferencedValue);
					OutputWrite("*", TypeColor.Operator);
					break;

				case CorElementType.ByRef:
					Write(type.FirstTypeParameter, value == null ? null : value.NeuterCheckDereferencedValue);
					OutputWrite("&", TypeColor.Operator);
					break;

				case CorElementType.Class:
				case CorElementType.ValueType:
					if (type.IsSystemNullable) {
						Write(type.FirstTypeParameter);
						OutputWrite("?", TypeColor.Operator);
						break;
					}
					var cls = type.Class;
					WriteClassOrValueType(type, cls);
					if (cls != null)
						WriteGenericParameters(cls.Module, cls.Token, new List<CorType>(type.TypeParameters), emptyTokenAndNameList, false);
					break;

				case CorElementType.FnPtr:
					OutputWrite("fnptr", TypeColor.Keyword);
					break;

				case CorElementType.End:
				case CorElementType.Var:
				case CorElementType.Array:		// handled above
				case CorElementType.GenericInst:
				case CorElementType.ValueArray:
				case CorElementType.R:
				case CorElementType.SZArray:	// handled above
				case CorElementType.MVar:
				case CorElementType.CModReqd:
				case CorElementType.CModOpt:
				case CorElementType.Internal:
				case CorElementType.Module:
				case CorElementType.Sentinel:
				case CorElementType.Pinned:
				default:
					OutputWrite(string.Format("BADET[{0}]", type.ElementType), TypeColor.Error);
					break;
				}
			}
			finally {
				recursionCounter--;
			}
		}

		static T Read<T>(IList<T> list, int index) {
			if ((uint)index >= (uint)list.Count)
				return default(T);
			return list[index];
		}

		static readonly CorType[] emptyCorTypeArray = new CorType[0];
		public void Write(TypeSig type, IList<CorType> typeGenArgs = null, IList<CorType> methGenArgs = null, IList<TokenAndName> typeTokenAndNames = null, IList<TokenAndName> methTokenAndNames = null) {
			try {
				if (recursionCounter++ >= MAX_RECURSION)
					return;
				if (type == null) {
					OutputWrite("null type", TypeColor.Error);
					return;
				}

				if (typeGenArgs == null)
					typeGenArgs = emptyCorTypeArray;
				if (methGenArgs == null)
					methGenArgs = emptyCorTypeArray;
				if (typeTokenAndNames == null)
					typeTokenAndNames = emptyTokenAndNameList;
				if (methTokenAndNames == null)
					methTokenAndNames = emptyTokenAndNameList;

				// It's shown reverse in C# so need to collect all array types here
				List<ArraySigBase> list = null;
				while (type != null && (type.ElementType == ElementType.SZArray || type.ElementType == ElementType.Array)) {
					if (list == null)
						list = new List<ArraySigBase>();
					list.Add((ArraySigBase)type);
					type = type.Next;
				}
				if (list != null) {
					Write(list[list.Count - 1].Next, typeGenArgs, methGenArgs, typeTokenAndNames, methTokenAndNames);
					foreach (var aryType in list) {
						if (aryType.ElementType == ElementType.Array) {
							OutputWrite("[", TypeColor.Operator);
							uint rank = aryType.Rank;
							if (rank == 0)
								OutputWrite("<RANK0>", TypeColor.Error);
							else {
								if (rank == 1)
									OutputWrite("*", TypeColor.Operator);
								for (uint i = 1; i < rank; i++)
									OutputWrite(",", TypeColor.Operator);
							}
							OutputWrite("]", TypeColor.Operator);
						}
						else {
							Debug.Assert(aryType.ElementType == ElementType.SZArray);
							// Use two strings so we produce the exact same output as the other
							// Write() that writes CorType arrays. There's code that compares the
							// output to detect different types, so we must generate the same text.
							OutputWrite("[", TypeColor.Operator);
							OutputWrite("]", TypeColor.Operator);
						}
					}
					return;
				}

				switch (type.ElementType) {
				case ElementType.Void:			WriteSystemTypeKeyword("Void", "void", TypeColor.ValueType); break;
				case ElementType.Boolean:		WriteSystemTypeKeyword("Boolean", "bool", TypeColor.ValueType); break;
				case ElementType.Char:			WriteSystemTypeKeyword("Char", "char", TypeColor.ValueType); break;
				case ElementType.I1:			WriteSystemTypeKeyword("SByte", "sbyte", TypeColor.ValueType); break;
				case ElementType.U1:			WriteSystemTypeKeyword("Byte", "byte", TypeColor.ValueType); break;
				case ElementType.I2:			WriteSystemTypeKeyword("Int16", "short", TypeColor.ValueType); break;
				case ElementType.U2:			WriteSystemTypeKeyword("UInt16", "ushort", TypeColor.ValueType); break;
				case ElementType.I4:			WriteSystemTypeKeyword("Int32", "int", TypeColor.ValueType); break;
				case ElementType.U4:			WriteSystemTypeKeyword("UInt32", "uint", TypeColor.ValueType); break;
				case ElementType.I8:			WriteSystemTypeKeyword("Int64", "long", TypeColor.ValueType); break;
				case ElementType.U8:			WriteSystemTypeKeyword("UInt64", "ulong", TypeColor.ValueType); break;
				case ElementType.R4:			WriteSystemTypeKeyword("Single", "float", TypeColor.ValueType); break;
				case ElementType.R8:			WriteSystemTypeKeyword("Double", "double", TypeColor.ValueType); break;
				case ElementType.String:		WriteSystemTypeKeyword("String", "string", TypeColor.Type); break;
				case ElementType.Object:		WriteSystemTypeKeyword("Object", "object", TypeColor.Type); break;

				case ElementType.TypedByRef:	WriteSystemType("TypedReference", TypeColor.ValueType); break;
				case ElementType.I:				WriteSystemType("IntPtr", TypeColor.ValueType); break;
				case ElementType.U:				WriteSystemType("UIntPtr", TypeColor.ValueType); break;

				case ElementType.Ptr:
					Write(type.Next, typeGenArgs, methGenArgs, typeTokenAndNames, methTokenAndNames);
					OutputWrite("*", TypeColor.Operator);
					break;

				case ElementType.ByRef:
					Write(type.Next, typeGenArgs, methGenArgs, typeTokenAndNames, methTokenAndNames);
					OutputWrite("&", TypeColor.Operator);
					break;

				case ElementType.ValueType:
				case ElementType.Class:
					//TODO: Resolve the CorClass so we can use the correct class color
					var cvt = (TypeDefOrRefSig)type;
					var mdip = cvt.TypeDefOrRef as IMetaDataImportProvider;
					if (mdip != null)
						Write(mdip);
					else {
						//TODO:
						Debug.Fail("NYI");
					}
					break;

				case ElementType.Var:
					int varIndex = (int)((GenericSig)type).Number;
					if (typeGenArgs.Count != 0)
						Write(Read(typeGenArgs, varIndex));
					else
						Write(Read(typeTokenAndNames, varIndex), varIndex, true);
					break;

				case ElementType.MVar:
					int mvarIndex = (int)((GenericSig)type).Number;
					if (methGenArgs.Count != 0)
						Write(Read(methGenArgs, mvarIndex));
					else
						Write(Read(methTokenAndNames, mvarIndex), mvarIndex, false);
					break;

				case ElementType.GenericInst:
					var gis = (GenericInstSig)type;
					if (gis.IsSystemNullable()) {
						Write(gis.GenericArguments[0], typeGenArgs, methGenArgs, typeTokenAndNames, methTokenAndNames);
						OutputWrite("?", TypeColor.Operator);
					}
					else {
						Write(gis.GenericType, typeGenArgs, methGenArgs, typeTokenAndNames, methTokenAndNames);
						OutputWrite("<", TypeColor.Operator);
						for (int i = 0; i < gis.GenericArguments.Count; i++) {
							if (i > 0)
								WriteCommaSpace();
							Write(gis.GenericArguments[i], typeGenArgs, methGenArgs, typeTokenAndNames, methTokenAndNames);
						}
						OutputWrite(">", TypeColor.Operator);
					}
					break;

				case ElementType.FnPtr:
					OutputWrite("fnptr", TypeColor.Keyword);
					break;

				case ElementType.CModReqd:
				case ElementType.CModOpt:
				case ElementType.Pinned:
					Write(type.Next, typeGenArgs, methGenArgs, typeTokenAndNames, methTokenAndNames);
					break;

				case ElementType.End:
				case ElementType.Array:		// handled above
				case ElementType.ValueArray:
				case ElementType.R:
				case ElementType.SZArray:	// handled above
				case ElementType.Internal:
				case ElementType.Module:
				case ElementType.Sentinel:
				default:
					break;
				}
			}
			finally {
				recursionCounter--;
			}
		}

		void Write(IMetaDataImportProvider mdip) {
			if (mdip == null || mdip.MDToken.Rid == 0) {
				OutputWrite("null type", TypeColor.Error);
				return;
			}

			switch (mdip.MDToken.Table) {
			case Table.TypeDef:
				WriteTypeDef(mdip.MetaDataImport, mdip.MDToken.Raw);
				break;

			case Table.TypeRef:
				WriteTypeRef(mdip.MetaDataImport, mdip.MDToken.Raw);
				break;

			case Table.TypeSpec:
				WriteTypeSpec(mdip.MetaDataImport, mdip.MDToken.Raw);
				break;

			default:
				OutputWrite(string.Format("BADTK[{0:X8}]", mdip.MDToken.Raw), TypeColor.Error);
				break;
			}
		}

		void WriteSystemTypeKeyword(string name, string keyword, TypeColor typeColor) {
			if (ShowTypeKeywords)
				OutputWrite(keyword, TypeColor.TypeKeyword);
			else
				WriteSystemType(name, typeColor);
		}

		void WriteSystemType(string name, TypeColor typeColor) {
			if (ShowNamespaces) {
				OutputWrite("System", TypeColor.NamespacePart);
				OutputWrite(".", TypeColor.Operator);
			}
			OutputWrite(name, typeColor);
		}

		public void Write(CorFrame frame) {
			try {
				if (recursionCounter++ >= MAX_RECURSION)
					return;
				if (frame == null) {
					OutputWrite("null frame", TypeColor.Error);
					return;
				}

				if (frame.IsILFrame && frame.ILFrameIP.IsExact) {
					WriteILFrame(frame);
					return;
				}

				if (frame.IsILFrame && frame.IsNativeFrame) {
					WriteILNativeFrame(frame);
					return;
				}

				// This shouldn't be true
				if (frame.IsILFrame) {
					WriteILFrame(frame);
					return;
				}

				if (frame.IsNativeFrame) {
					WriteNativeFrame(frame);
					return;
				}

				if (frame.IsInternalFrame) {
					WriteInternalFrame(frame);
					return;
				}

				WriteUnknownFrame(frame);
			}
			finally {
				recursionCounter--;
			}
		}

		public void Write(CorField field) {
			if (field == null) {
				OutputWrite("null field", TypeColor.Error);
				return;
			}

			try {
				if (recursionCounter++ >= MAX_RECURSION)
					return;

				var sig = field.GetFieldSig();
				var cls = field.Class;
				var type = cls.GetParameterizedType(CorElementType.Class);
				bool isEnumOwner = type != null && type.IsEnum;

				var info = GetGenericInfo(null, cls, 0);
				var fieldAttrs = field.GetAttributes();

				if (!isEnumOwner || (fieldAttrs & FieldAttributes.Literal) == 0) {
					WriteSpace();
					Write(sig.Type, info.TypeGenericArguments, info.MethodGenericArguments, info.TypeTokenAndNames, info.MethodTokenAndNames);
					WriteSpace();
				}
				if (ShowOwnerTypes) {
					Write(type);
					OutputWrite(".", TypeColor.Operator);
				}
				WriteIdentifier(field.GetName(), GetTypeColor(field, type, fieldAttrs));
				WriteTokenComment(field.Token);
				if (this.ShowFieldLiteralValues) {
					object c;
					if ((fieldAttrs & FieldAttributes.Literal) != 0 && (c = field.GetConstant()) != null) {
						WriteSpace();
						OutputWrite("=", TypeColor.Operator);
						WriteSpace();
						WriteConstant(c);
					}
				}
			}
			finally {
				recursionCounter--;
			}
		}

		TypeColor GetTypeColor(CorField field, CorType type, FieldAttributes fieldAttrs) {
			if (field == null)
				return TypeColor.InstanceField;
			if (type != null && type.IsEnum)
				return TypeColor.EnumField;
			if ((fieldAttrs & FieldAttributes.Literal) != 0)
				return TypeColor.LiteralField;
			if ((fieldAttrs & FieldAttributes.Static) != 0)
				return TypeColor.StaticField;
			return TypeColor.InstanceField;
		}

		public void Write(CorProperty prop) {
			if (prop == null) {
				OutputWrite("null property", TypeColor.Error);
				return;
			}

			try {
				if (recursionCounter++ >= MAX_RECURSION)
					return;

				var getMethod = prop.GetMethod;
				var setMethod = prop.SetMethod;
				var accMeth = getMethod ?? setMethod;

				var module = prop.Class.Module;
				uint token = accMeth == null ? 0 : accMeth.Token;

				var info = GetGenericInfo(null, prop.Class, token);

				MethodSig methodSig = null;
				bool retTypeIsLastArgType = accMeth == setMethod;

				WriteModuleName(module);
				WriteReturnType(ref methodSig, retTypeIsLastArgType, module, token, info.TypeGenericArguments, info.MethodGenericArguments, info.TypeTokenAndNames, info.MethodTokenAndNames);
				if (ShowOwnerTypes) {
					Write(prop.Class.GetParameterizedType(CorElementType.Class));
					OutputWrite(".", TypeColor.Operator);
				}
				var overrides = accMeth == null ? new CorOverride[0] : accMeth.GetOverrides();
				var ovrMeth = overrides.Length == 0 ? null : overrides[0].FunctionDeclaration;
				if (IsIndexer(prop, accMeth, ovrMeth)) {
					if (ovrMeth != null) {
						WriteFuncType(ovrMeth);
						OutputWrite(".", TypeColor.Operator);
					}
					OutputWrite("this", TypeColor.Keyword);
					WriteGenericParameters(module, token, info.MethodGenericArguments, info.MethodTokenAndNames, true);
					WriteMethodParameterList(ref methodSig, retTypeIsLastArgType, module, token, info.TypeTokenAndNames, info.MethodTokenAndNames, "[", "]");
				}
				else if (ovrMeth != null && GetPropName(ovrMeth) != null) {
					WriteFuncType(ovrMeth);
					OutputWrite(".", TypeColor.Operator);
					WriteIdentifier(GetPropName(ovrMeth), GetTypeColor(accMeth, TypeColor.StaticProperty, TypeColor.InstanceProperty));
				}
				else
					WriteIdentifier(prop.GetName(), GetTypeColor(accMeth, TypeColor.StaticProperty, TypeColor.InstanceProperty));
				WriteTokenComment(prop.Token);

				WriteSpace();
				OutputWrite("{", TypeColor.Operator);
				if (getMethod != null) {
					WriteSpace();
					OutputWrite("get", TypeColor.Keyword);
					OutputWrite(";", TypeColor.Operator);
				}
				if (setMethod != null) {
					WriteSpace();
					OutputWrite("set", TypeColor.Keyword);
					OutputWrite(";", TypeColor.Operator);
				}
				WriteSpace();
				OutputWrite("}", TypeColor.Operator);
			}
			finally {
				recursionCounter--;
			}
		}

		void WriteFuncType(CorFunction func) {
			var cls = func.Class;
			var type = cls == null ? null : cls.GetParameterizedType(CorElementType.Class);
			if (type != null)
				Write(type);
			else
				Write(cls);
		}

		static string GetPropName(CorFunction method) {
			if (method == null)
				return null;
			var name = method.GetName();
			if (name.StartsWith("get_", StringComparison.Ordinal) || name.StartsWith("set_", StringComparison.Ordinal))
				return name.Substring(4);
			return null;
		}

		static bool IsIndexer(CorProperty prop, CorFunction accMeth, CorFunction ovrMeth) {
			if (prop == null || prop.GetPropertySig().GetParamCount() == 0)
				return false;

			var bp = prop;
			if (accMeth != null && ovrMeth != null) {
				foreach (var p in ovrMeth.Class.FindProperties(false)) {
					if (ovrMeth.Equals(p.GetMethod) || ovrMeth.Equals(p.SetMethod)) {
						bp = p;
						break;
					}
				}
			}
			return GetDefaultMemberName(bp.Class) == bp.GetName();
		}

		static string GetDefaultMemberName(CorClass cls) {
			if (cls == null)
				return null;

			//TODO:
			return "Item";
		}

		public void Write(CorEvent evt) {
			if (evt == null) {
				OutputWrite("null event", TypeColor.Error);
				return;
			}

			try {
				if (recursionCounter++ >= MAX_RECURSION)
					return;

				Write(evt.GetEventType());
				WriteSpace();
				if (ShowOwnerTypes) {
					Write(evt.Class.GetParameterizedType(CorElementType.Class));
					OutputWrite(".", TypeColor.Operator);
				}
				WriteIdentifier(evt.GetName(), GetTypeColor(evt));
				WriteTokenComment(evt.Token);
			}
			finally {
				recursionCounter--;
			}
		}

		TypeColor GetTypeColor(CorEvent e) {
			return GetTypeColor(e.AddMethod ?? e.RemoveMethod ?? e.FireMethod, TypeColor.StaticEvent, TypeColor.InstanceEvent);
		}

		TypeColor GetTypeColor(CorFunction func, TypeColor staticValue, TypeColor instanceValue) {
			if (func == null)
				return instanceValue;
			var attrs = func.GetAttributes();
			if ((attrs & MethodAttributes.Static) != 0)
				return staticValue;
			return instanceValue;
		}

		public void Write(CorFunction func) {
			Write(func, null);
		}

		void Write(CorFunction func, CorFrame frame) {
			if (func == null) {
				OutputWrite("null function", TypeColor.Error);
				return;
			}

			var code = func.ILCode ?? func.NativeCode;
			Write(func, code, frame);
		}

		public void Write(CorCode code) {
			if (code == null) {
				OutputWrite("null code", TypeColor.Error);
				return;
			}

			var func = code.Function;
			if (func == null) {
				OutputWrite("code has no func", TypeColor.Error);
				return;
			}

			Write(func, code, null);
		}

		struct GenericInfo {
			public List<CorType> TypeGenericArguments;
			public List<CorType> MethodGenericArguments;
			public List<TokenAndName> TypeTokenAndNames;
			public List<TokenAndName> MethodTokenAndNames;
		}

		GenericInfo GetGenericInfo(CorFrame frame, CorClass cls, uint methodToken) {
			Debug.Assert(frame != null || cls != null);
			GenericInfo info;
			if (frame != null) {
				frame.GetTypeAndMethodGenericParameters(out info.TypeGenericArguments, out info.MethodGenericArguments);
				info.TypeTokenAndNames = emptyTokenAndNameList;
				info.MethodTokenAndNames = emptyTokenAndNameList;
			}
			else {
				info.MethodGenericArguments = emptyCorTypeList;
				info.TypeGenericArguments = emptyCorTypeList;
				var mdi = GetMetaDataImport(cls == null ? null : cls.Module);
				var clsToken = cls == null ? 0 : cls.Token;
				info.TypeTokenAndNames = MetaDataUtils.GetGenericParameterNames(mdi, clsToken);
				info.MethodTokenAndNames = MetaDataUtils.GetGenericParameterNames(mdi, methodToken);
			}

			return info;
		}
		static readonly List<CorType> emptyCorTypeList = new List<CorType>();
		static readonly List<TokenAndName> emptyTokenAndNameList = new List<TokenAndName>();

		void Write(CorFunction func, CorCode code, CorFrame frame) {
			try {
				if (recursionCounter++ >= MAX_RECURSION)
					return;
				Debug.Assert(func != null);

				bool hasFrame = frame != null;
				var module = func.Module;
				uint token = func.Token;

				var info = GetGenericInfo(frame, func.Class, token);
				var args = new List<CorValue>();
				if (hasFrame)
					args.AddRange(frame.ILArguments);

				MethodSig methodSig = null;
				WriteModuleName(module);
				WriteReturnType(ref methodSig, false, module, token, info.TypeGenericArguments, info.MethodGenericArguments, info.TypeTokenAndNames, info.MethodTokenAndNames);
				WriteTypeOwner(func.Class, info.TypeGenericArguments, info.TypeTokenAndNames);
				WriteMethodName(module, token);
				WriteGenericParameters(module, token, info.MethodGenericArguments, info.MethodTokenAndNames, true);
				if (hasFrame)
					WriteMethodParameterList(ref methodSig, false, module, token, args, info.TypeGenericArguments, info.MethodGenericArguments);
				else
					WriteMethodParameterList(ref methodSig, false, module, token, info.TypeTokenAndNames, info.MethodTokenAndNames);
				WriteIP(frame, code);
			}
			finally {
				recursionCounter--;
			}
		}

		void WriteIP(CorFrame frame, CorCode code) {
			if (!ShowIP)
				return;
			if (frame == null)
				return;

			// Always show the IP even if the line is too long
			var old = forceWrite;
			try {
				forceWrite = true;

				if (frame.IsILFrame || frame.IsNativeFrame) {
					WriteSpace();
					OutputWrite("(", TypeColor.Operator);
					bool needComma = false;
					if (frame.IsILFrame) {
						var ip = frame.ILFrameIP;
						OutputWrite("IL", TypeColor.IPType);
						OutputWrite("=", TypeColor.Operator);
						if (ip.IsExact)
							WriteILOffset(ip.Offset);
						else if (ip.IsApproximate) {
							OutputWrite("~", TypeColor.Operator);
							WriteILOffset(ip.Offset);
						}
						else if (ip.IsProlog)
							OutputWrite("Prolog", TypeColor.IPType);
						else if (ip.IsEpilog)
							OutputWrite("Epilog", TypeColor.IPType);
						else
							OutputWrite("???", TypeColor.Error);
						needComma = true;
					}
					if (frame.IsNativeFrame) {
						if (needComma)
							WriteCommaSpace();
						OutputWrite("Native", TypeColor.IPType);
						OutputWrite("=", TypeColor.Operator);

						var nativeCode = code != null && !code.IsIL ? code : null;
						if (nativeCode == null) {
							var func = frame.Function;
							//TODO: This can be a random JITed method if it's a generic method
							nativeCode = func == null ? null : func.NativeCode;
						}

						uint ip = frame.NativeFrameIP;
						if (nativeCode != null && !nativeCode.IsIL)
							WriteNativeAddress(nativeCode.Address);
						else
							OutputWrite("???", TypeColor.Error);
						WriteRelativeOffset((int)ip);
					}
					OutputWrite(")", TypeColor.Operator);
				}
			}
			finally {
				forceWrite = old;
			}
		}

		bool WriteModuleName(CorModule module) {
			if (!ShowModuleNames)
				return false;

			Write(module);
			OutputWrite("!", TypeColor.Operator);
			return true;
		}

		bool WriteReturnType(ref MethodSig methodSig, bool retTypeIsLastArgType, CorModule module, uint token, IList<CorType> typeGenArgs, IList<CorType> methGenArgs, List<TokenAndName> typeTokenAndNames, List<TokenAndName> methTokenAndNames) {
			if (!ShowReturnTypes)
				return false;

			Initialize(GetMetaDataImport(module), token, ref methodSig);
			var retType = retTypeIsLastArgType ? methodSig.Params.LastOrDefault() : methodSig.GetRetType();
			Write(retType, typeGenArgs, methGenArgs, typeTokenAndNames, methTokenAndNames);
			WriteSpace();
			return true;
		}

		bool WriteTypeOwner(CorClass cls, IList<CorType> typeGenArgs, List<TokenAndName> typeTokenAndNames) {
			if (!ShowOwnerTypes)
				return false;

			WriteClassOrValueType(cls);
			WriteGenericParameters(cls == null ? null : cls.Module, cls == null ? 0 : cls.Token, typeGenArgs, typeTokenAndNames, false);
			OutputWrite(".", TypeColor.Operator);
			return true;
		}

		void WriteMethodName(CorModule module, uint token) {
			var mdi = GetMetaDataImport(module);
			if (mdi == null)
				WriteDefaultFuncName(token);
			else {
				var name = MDAPI.GetMethodName(mdi, token);
				if (name == null)
					WriteDefaultFuncName(token);
				else
					WriteMethodName(name, token, GetTypeColor(mdi, token));
			}
		}

		TypeColor GetTypeColor(IMetaDataImport mdi, uint token) {
			MethodAttributes attrs;
			MethodImplAttributes implAttrs;
			MDAPI.GetMethodAttributes(mdi, token, out attrs, out implAttrs);

			if ((attrs & MethodAttributes.Static) != 0) {
				if (MDAPI.HasAttribute(mdi, token, "System.Runtime.CompilerServices"))
					return TypeColor.ExtensionMethod;
				return TypeColor.StaticMethod;
			}

			return TypeColor.InstanceMethod;
		}

		bool WriteGenericParameters(CorModule module, uint token, IList<CorType> genArgs, List<TokenAndName> typeTokenAndNames, bool isMethod) {
			var mdi = GetMetaDataImport(module);
			var gps = MetaDataUtils.GetGenericParameterNames(mdi, token);
			if (gps.Count == 0)
				return false;

			OutputWrite("<", TypeColor.Operator);
			for (int i = 0; i < gps.Count; i++) {
				if (i > 0)
					WriteCommaSpace();
				if (i < genArgs.Count)
					Write(genArgs[i]);
				else if (i < typeTokenAndNames.Count)
					Write(typeTokenAndNames[i], i, token);
				else {
					var gp = gps[i];
					WriteGenericParameterName(gp.Name, gp.Token, isMethod);
				}
			}
			OutputWrite(">", TypeColor.Operator);
			return true;
		}

		void Write(TokenAndName info, int index, uint ownerToken) {
			Write(info, index, (ownerToken >> 24) == 2);
		}

		void Write(TokenAndName info, int index, bool isType) {
			Debug.Assert(info.Name != null);
			string name = info.Name;
			if (string.IsNullOrEmpty(name))
				name = string.Format("_T{0}_", index);
			OutputWrite(name, isType ? TypeColor.TypeGenericParameter : TypeColor.MethodGenericParameter);
		}

		void Initialize(IMetaDataImport mdi, uint token, ref MethodSig methodSig) {
			if (mdi == null || methodSig != null)
				return;
			methodSig = MetaDataUtils.GetMethodSignature(mdi, token);
		}

		void WriteMethodParameterList(ref MethodSig methodSig, bool retTypeIsLastArgType, CorModule module, uint token, IList<CorValue> args, IList<CorType> typeGenArgs, IList<CorType> methGenArgs, string leftParen = "(", string rightParen = ")") {
			if (!ShowParameterTypes && !ShowParameterNames && !ShowParameterValues)
				return;

			var mdi = GetMetaDataImport(module);
			var ps = MetaDataUtils.GetParameters(mdi, token);

			OutputWrite(leftParen, TypeColor.Operator);
			Initialize(mdi, token, ref methodSig);
			Debug.Assert(methodSig != null);
			Debug.Assert(methodSig == null || methodSig.GenParamCount == methGenArgs.Count);
			int argsCount = args.Count;
			if (retTypeIsLastArgType)
				argsCount--;
			for (int i = methodSig == null ? 0 : methodSig.HasThis ? 1 : 0, mi = 0; i < argsCount; i++, mi++) {
				if (mi > 0)
					WriteCommaSpace();
				var arg = args[i];

				var ma = methodSig == null ? null : mi >= methodSig.Params.Count ? null : methodSig.Params[mi];
				var paramInfo = ps.Get((uint)mi + 1);
				bool isCSharpOut = paramInfo != null && !paramInfo.Value.IsIn && paramInfo.Value.IsOut;

				bool needSpace = false;
				if (ShowParameterTypes) {
					if (ma != null) {
						if (ma.RemovePinnedAndModifiers().GetElementType() == ElementType.ByRef) {
							OutputWrite(isCSharpOut ? "out" : "ref", TypeColor.Keyword);
							WriteSpace();
							ma = ma.RemovePinnedAndModifiers().GetNext();
						}
						Write(ma, typeGenArgs, methGenArgs, emptyTokenAndNameList, emptyTokenAndNameList);
					}
					else {
						var type = arg.ExactType;
						if (type != null) {
							if (type.ElementType == CorElementType.ByRef) {
								OutputWrite(isCSharpOut ? "out" : "ref", TypeColor.Keyword);
								WriteSpace();
								type = type.FirstTypeParameter;
							}
							Write(type);
						}
						else {
							var cls = arg.Class;
							if (cls != null)
								Write(cls);
							else
								OutputWrite("???", TypeColor.Error);
						}
					}

					needSpace = true;
				}

				if (ShowParameterNames) {
					if (needSpace)
						WriteSpace();

					WriteIdentifier(paramInfo == null ? string.Format("A_{0}", mi) : paramInfo.Value.Name, TypeColor.Parameter);
					needSpace = true;
				}

				if (ShowParameterValues) {
					if (needSpace) {
						WriteSpace();
						OutputWrite("=", TypeColor.Operator);
						WriteSpace();
					}

					Write(arg);
					needSpace = true;
				}
			}
			OutputWrite(rightParen, TypeColor.Operator);
		}

		void WriteMethodParameterList(ref MethodSig methodSig, bool retTypeIsLastArgType, CorModule module, uint token, List<TokenAndName> typeTokenAndNames, List<TokenAndName> methTokenAndNames, string leftParen = "(", string rightParen = ")") {
			if (!ShowParameterTypes && !ShowParameterNames)
				return;

			var mdi = GetMetaDataImport(module);
			var ps = MetaDataUtils.GetParameters(mdi, token);

			OutputWrite(leftParen, TypeColor.Operator);
			Initialize(mdi, token, ref methodSig);
			Debug.Assert(methodSig != null);
			var sigParams = methodSig == null ? (IList<TypeSig>)new TypeSig[0] : methodSig.Params;
			int paramsCount = sigParams.Count;
			if (retTypeIsLastArgType)
				paramsCount--;
			for (int i = 0; i < paramsCount; i++) {
				if (i > 0)
					WriteCommaSpace();

				var ma = sigParams[i];
				var paramInfo = ps.Get((uint)i + 1);
				bool isCSharpOut = paramInfo != null && !paramInfo.Value.IsIn && paramInfo.Value.IsOut;

				bool needSpace = false;
				if (ShowParameterTypes) {
					needSpace = true;

					if (paramInfo != null && MDAPI.HasAttribute(mdi, paramInfo.Value.Token, "System.ParamArrayAttribute")) {
						OutputWrite("params", TypeColor.Keyword);
						WriteSpace();
					}

					if (ma.RemovePinnedAndModifiers().GetElementType() == ElementType.ByRef) {
						OutputWrite(isCSharpOut ? "out" : "ref", TypeColor.Keyword);
						WriteSpace();
						ma = ma.RemovePinnedAndModifiers().GetNext();
					}
					Write(ma, null, null, typeTokenAndNames, methTokenAndNames);
				}

				if (ShowParameterNames) {
					if (needSpace)
						WriteSpace();
					needSpace = true;

					WriteIdentifier(paramInfo == null ? string.Format("A_{0}", i) : paramInfo.Value.Name, TypeColor.Parameter);
				}

				if (ShowParameterLiteralValues && paramInfo != null) {
					CorElementType etype;
					var c = MDAPI.GetParamConstant(mdi, paramInfo.Value.Token, out etype);
					if (etype != CorElementType.End) {
						if (needSpace) {
							WriteSpace();
							OutputWrite("=", TypeColor.Operator);
							WriteSpace();
						}
						needSpace = true;

						var t = ma.RemovePinnedAndModifiers();
						if (t.GetElementType() == ElementType.ByRef)
							t = t.Next;
						if (c == null && t != null && t.IsValueType) {
							OutputWrite("default", TypeColor.Keyword);
							OutputWrite("(", TypeColor.Operator);
							Write(t, null, null, typeTokenAndNames, methTokenAndNames);
							OutputWrite(")", TypeColor.Operator);
						}
						else
							WriteConstant(c);
					}
				}
			}
			OutputWrite(rightParen, TypeColor.Operator);
		}

		void WriteGenericParameterName(string name, uint token, bool isMethod) {
			WriteIdentifier(name, isMethod ? TypeColor.MethodGenericParameter : TypeColor.TypeGenericParameter);
			WriteTokenComment(token);
		}

		void WriteDefaultFuncName(uint token) {
			WriteMethodName(string.Format("meth_{0:X8}", token), token, TypeColor.InstanceMethod);
		}

		void WriteMethodName(string name, uint token, TypeColor typeColor) {
			WriteIdentifier(name, typeColor);
			WriteTokenComment(token);
		}

		void WriteILFrame(CorFrame frame) {
			Debug.Assert(frame != null && frame.IsILFrame);

			Write(frame.Function, frame);
		}

		void WriteILNativeFrame(CorFrame frame) {
			Debug.Assert(frame != null && frame.IsILFrame && frame.IsNativeFrame);

			Write(frame.Function, frame);
		}

		void WriteNativeFrame(CorFrame frame) {
			Debug.Assert(frame != null && frame.IsNativeFrame);

			OutputWrite("[", TypeColor.Operator);
			OutputWrite("Native Frame ", TypeColor.NativeFrame);
			WriteNativeAddress(frame.NativeFrameIP);
			OutputWrite("]", TypeColor.Operator);
		}

		void WriteInternalFrame(CorFrame frame) {
			Debug.Assert(frame != null && frame.IsInternalFrame);

			OutputWrite("[", TypeColor.Operator);
			switch (frame.InternalFrameType) {
			case CorDebugInternalFrameType.STUBFRAME_M2U:
				OutputWrite("Managed to Native Transition", TypeColor.InternalFrame);
				break;

			case CorDebugInternalFrameType.STUBFRAME_U2M:
				OutputWrite("Native to Managed Transition", TypeColor.InternalFrame);
				break;

			case CorDebugInternalFrameType.STUBFRAME_APPDOMAIN_TRANSITION:
				OutputWrite("Appdomain Transition", TypeColor.InternalFrame);
				break;

			case CorDebugInternalFrameType.STUBFRAME_LIGHTWEIGHT_FUNCTION:
				OutputWrite("Lightweight Function", TypeColor.InternalFrame);
				break;

			case CorDebugInternalFrameType.STUBFRAME_FUNC_EVAL:
				OutputWrite("Function Evaluation", TypeColor.InternalFrame);
				break;

			case CorDebugInternalFrameType.STUBFRAME_INTERNALCALL:
				OutputWrite("Internal Call", TypeColor.InternalFrame);
				break;

			case CorDebugInternalFrameType.STUBFRAME_CLASS_INIT:
				OutputWrite("Class Init", TypeColor.InternalFrame);
				break;

			case CorDebugInternalFrameType.STUBFRAME_EXCEPTION:
				OutputWrite("Exception", TypeColor.InternalFrame);
				break;

			case CorDebugInternalFrameType.STUBFRAME_SECURITY:
				OutputWrite("Security", TypeColor.InternalFrame);
				break;

			case CorDebugInternalFrameType.STUBFRAME_JIT_COMPILATION:
				OutputWrite("JIT Compilation", TypeColor.InternalFrame);
				break;

			case CorDebugInternalFrameType.STUBFRAME_NONE:
			default:
				OutputWrite(string.Format("Internal Frame {0}", ConvertNumberToString((int)frame.InternalFrameType)), TypeColor.InternalFrame);
				break;
			}
			OutputWrite("]", TypeColor.Operator);
		}

		void WriteUnknownFrame(CorFrame frame) {
			Debug.Assert(frame != null);

			OutputWrite("[", TypeColor.Operator);
			OutputWrite("Unknown Frame", TypeColor.UnknownFrame);
			OutputWrite("]", TypeColor.Operator);
		}

		void WriteTokenComment(uint token) {
			if (!ShowTokens)
				return;
			OutputWrite("/*", TypeColor.Comment);
			OutputWrite(ConvertTokenToString(token), TypeColor.Comment);
			OutputWrite("*/", TypeColor.Comment);
		}

		void WriteToken(uint token) {
			OutputWrite(ConvertTokenToString(token), TypeColor.Token);
		}

		void WriteNumber(object value) {
			OutputWrite(ConvertNumberToString(value), TypeColor.Number);
		}

		public void Write(CorValue value) {
			if (value == null) {
				OutputWrite("???", TypeColor.Error);
				return;
			}

			Write(value, value.Value);
		}

		public void Write(CorValue value, CorValueResult result) {
			if (result.IsValid) {
				var et = value == null ? null : value.ExactType;
				if (et != null && et.IsEnum)
					WriteEnum(et, result.Value);
				else
					WriteSimpleValue(result.Value);
			}
			else {
				if (value == null) {
					OutputWrite("???", TypeColor.Error);
					return;
				}

				if (getEval == null) {
					WriteTypeOfValue(value);
					return;
				}

				CorValue nullableValue;
				if (value.GetNullableValue(out nullableValue) && nullableValue != null)
					value = nullableValue;

				//TODO: Support DebuggerDisplayAttribute

				var info = FindToStringMethodIfOverridden(value);
				if (info == null) {
					WriteTypeOfValue(value);
					return;
				}

				var func = GetFunction(info.OwnerType, info.Token);
				if (func == null) {
					WriteTypeOfValue(value);
					return;
				}

				WriteToStringData(value, func);
			}
		}

		static CorFunction GetFunction(CorType type, uint token) {
			var cls = type.Class;
			var mod = cls == null ? null : cls.Module;
			return mod == null ? null : mod.GetFunctionFromToken(token);
		}

		CorMethodInfo FindToStringMethodIfOverridden(CorValue value) {
			var et = value.ExactType;
			if (et == null)
				return null;
			var ts = et.GetToStringMethod();
			if (ts == null)
				return null;
			if (ts.OwnerType.IsSystemObject || ts.OwnerType.IsSystemValueType)
				return null;
			return et.GetSystemObjectToStringMethod();
		}

		void WriteToStringData(CorValue value, CorFunction func) {
			Debug.Assert(value != null && func != null && getEval != null);

			try {
				var eval = getEval();
				if (eval == null) {
					WriteTypeOfValue(value);
					return;
				}

				using (eval) {
					var v = value;
					if (v != null && v.IsReference && v.Type == CorElementType.ByRef)
						v = v.NeuterCheckDereferencedValue;
					if (v != null && v.IsGeneric && !v.IsHeap && v.ExactType.IsValueType)
						v = eval.Box(v);
					if (v == null) {
						WriteTypeOfValue(value);
						return;
					}
					int hr;
					var res = eval.Call(func, null, new CorValue[1] { v }, out hr);
					if (res == null || CordbgErrors.IsCantEvaluateError(hr) || res.Value.WasException) {
						WriteTypeOfValue(value);
						return;
					}
					var rv = res.Value.ResultOrException;
					if (rv != null && rv.IsNull) {
						WriteTypeOfValue(value);
						return;
					}
					if (rv != null && rv.IsReference)
						rv = rv.NeuterCheckDereferencedValue;
					if (rv == null || !rv.IsString) {
						WriteToStringFailed("return value isn't a string");
						return;
					}
					if (rv.IsNull) {
						WriteTypeOfValue(value);
						return;
					}
					OutputWrite("{", TypeColor.ToStringBrace);
					OutputWrite(CleanUpEvaluatedToStringString(rv.String), TypeColor.ToStringResult);
					OutputWrite("}", TypeColor.ToStringBrace);
				}
			}
			catch (TimeoutException) {
				WriteToStringFailed("timed out!");
			}
			catch (Exception ex) {
				WriteToStringFailed(ex.Message);
			}
		}

		static string CleanUpEvaluatedToStringString(string s) {
			// VS calls wsprintf("{%s}") and if the string has a zero in it, anything
			// after that isn't shown, including the final '}'. We'll show the full string.
			return s;
		}

		void WriteToStringFailed(string msg) {
			OutputWrite(string.Format("{{ToString() failed: {0}}}", msg), TypeColor.Error);
		}

		void WriteTypeOfValue(CorValue value) {
			if (value == null) {
				OutputWrite("???", TypeColor.Error);
				return;
			}

			OutputWrite("{", TypeColor.TypeStringBrace);
			if (value.IsReference && value.Type == CorElementType.ByRef)
				value = value.NeuterCheckDereferencedValue ?? value;
			var type = value.ExactType;
			if (type != null)
				Write(type, value);
			else {
				var cls = value.Class;
				if (cls != null)
					Write(cls);
				else
					OutputWrite("???", TypeColor.Error);
			}
			OutputWrite("}", TypeColor.TypeStringBrace);
		}

		public void WriteConstant(TypeSig type, object c) {
			if (!TryWriteEnum(type, c))
				WriteConstant(c);
		}

		bool TryWriteEnum(TypeSig type, object c) {
			if (type == null)
				return false;
			var cts = type.RemovePinnedAndModifiers() as ClassOrValueTypeSig;
			var mdip = cts == null ? null : cts.TypeDefOrRef as IMetaDataImportProvider;
			if (mdip == null || mdip.MDToken.Table == Table.TypeSpec)
				return false;

			var mdi = mdip.MetaDataImport;
			uint token = mdip.MDToken.Raw;

			if (!MetaDataUtils.IsEnum(mdi, token))
				return false;

			WriteEnum(mdi, token, c, MetaDataUtils.GetFieldInfos(mdi, token));
			return true;
		}

		public void WriteConstant(object c) {
			if (c == null)
				OutputWrite("null", TypeColor.Keyword);
			else if (c is bool)
				OutputWrite((bool)c ? "true" : "false", TypeColor.Keyword);
			else if (c is char)
				WriteCharValue((char)c);
			else if (c is string)
				WriteStringValue((string)c);
			else
				WriteNumber(c);
		}

		void WriteEnum(CorType type, object value) {
			if (type == null || value == null) {
				OutputWrite("???", TypeColor.Error);
				return;
			}
			Debug.Assert(type.IsEnum);

			uint token;
			var mdi = type.GetMetaDataImport(out token);
			WriteEnum(mdi, token, value, MetaDataUtils.GetFieldInfos(type, false));
		}

		void WriteEnum(IMetaDataImport mdi, uint token, object value, IEnumerable<CorFieldInfo> typeFields) {
			bool hasFlagsAttr = MDAPI.HasAttribute(mdi, token, "System.FlagsAttribute");
			var fields = typeFields.Where(a => (a.Attributes & (FieldAttributes.Literal | FieldAttributes.Static)) == (FieldAttributes.Literal | FieldAttributes.Static) && a.Constant != null);
			var input = Utils.IntegerToUInt64ZeroExtend(value);
			if (hasFlagsAttr && input != null && input.Value != 0) {
				ulong f = input.Value;
				Debug.Assert(f != 0);
				bool needSep = false;
				foreach (var field in fields) {
					var flag = Utils.IntegerToUInt64ZeroExtend(field.Constant);
					if (flag == null || flag.Value == 0)
						continue;
					if ((f & flag) == 0)
						continue;
					if (needSep)
						WriteEnumSeperator();
					needSep = true;
					WriteEnumField(mdi, token, field.Name);
					f &= ~flag.Value;
					if (f == 0)
						break;
				}
				if (f != 0) {
					if (needSep)
						WriteEnumSeperator();
					WriteSimpleValue(Utils.ConvertValue(f, value.GetType()));
				}
			}
			else {
				bool printed = false;
				foreach (var field in fields) {
					if (field.Constant.Equals(value)) {
						WriteEnumField(mdi, token, field.Name);
						printed = true;
						break;
					}
				}
				if (!printed)
					WriteSimpleValue(value);
			}
		}

		void WriteEnumSeperator() {
			OutputWrite(" ", TypeColor.Space);
			OutputWrite("|", TypeColor.Operator);
			OutputWrite(" ", TypeColor.Space);
		}

		void WriteEnumField(IMetaDataImport mdi, uint token, string name) {
			WriteIdentifier(name, TypeColor.EnumField);
		}

		void WriteSimpleValue(object value) {
			if (value == null) {
				OutputWrite("null", TypeColor.Keyword);
				return;
			}

			switch (Type.GetTypeCode(value.GetType())) {
			case TypeCode.Boolean:
				WriteBooleanValue((bool)value);
				return;

			case TypeCode.Char:
				WriteCharValue((char)value);
				return;

			case TypeCode.String:
				WriteStringValue((string)value);
				return;

			case TypeCode.SByte:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
			case TypeCode.Byte:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
			case TypeCode.Single:
			case TypeCode.Double:
			case TypeCode.Decimal:
				WriteNumber(value);
				return;
			}

			if (value is IntPtr || value is UIntPtr) {
				WriteNumber(value);
				return;
			}

			OutputWrite(value.ToString(), TypeColor.Unknown);
		}

		void WriteBooleanValue(bool b) {
			OutputWrite(b ? "true" : "false", TypeColor.Keyword);
		}

		void WriteCharValue(char c) {
			OutputWrite(ToCSharpChar(c), TypeColor.Char);
		}

		static string ToCSharpChar(char value) {
			var sb = new StringBuilder(8);
			sb.Append('\'');
			switch (value) {
			case '\a': sb.Append(@"\a"); break;
			case '\b': sb.Append(@"\b"); break;
			case '\f': sb.Append(@"\f"); break;
			case '\n': sb.Append(@"\n"); break;
			case '\r': sb.Append(@"\r"); break;
			case '\t': sb.Append(@"\t"); break;
			case '\v': sb.Append(@"\v"); break;
			case '\\': sb.Append(@"\\"); break;
			case '\0': sb.Append(@"\0"); break;
			case '\'': sb.Append(@"\'"); break;
			default:
				if (char.IsControl(value))
					sb.Append(string.Format(@"\u{0:X4}", (ushort)value));
				else
					sb.Append(value);
				break;
			}
			sb.Append('\'');
			return sb.ToString();
		}

		void WriteStringValue(string s) {
			if (s == null) {
				OutputWrite("null", TypeColor.Keyword);
				return;
			}

			OutputWrite(ToCSharpString(s), TypeColor.String);
		}

		internal static string ToCSharpString(string s, bool useQuotes = true) {
			if (s == null)
				return string.Empty;

			var sb = new StringBuilder(s.Length + 10);
			if (useQuotes)
				sb.Append('"');
			foreach (var c in s) {
				switch (c) {
				case '\a': sb.Append(@"\a"); break;
				case '\b': sb.Append(@"\b"); break;
				case '\f': sb.Append(@"\f"); break;
				case '\n': sb.Append(@"\n"); break;
				case '\r': sb.Append(@"\r"); break;
				case '\t': sb.Append(@"\t"); break;
				case '\v': sb.Append(@"\v"); break;
				case '\\': sb.Append(@"\\"); break;
				case '\0': sb.Append(@"\0"); break;
				case '"':  sb.Append("\\\""); break;
				default:
					if (char.IsControl(c))
						sb.Append(string.Format(@"\u{0:X4}", (ushort)c));
					else
						sb.Append(c);
					break;
				}
			}
			if (useQuotes)
				sb.Append('"');
			return sb.ToString();
		}

		static string ConvertTokenToString(uint token) {
			// Tokens are always in hex
			return string.Format("0x{0:X8}", token);
		}

		void WriteRelativeOffset(int offset) {
			long offset2 = offset;
			if (offset2 < 0) {
				offset2 = -offset2;
				OutputWrite("-", TypeColor.Operator);
			}
			else
				OutputWrite("+", TypeColor.Operator);
			if (UseDecimal)
				WriteNumber(offset2);
			else
				OutputWrite(string.Format("0x{0:X}", offset2), TypeColor.Number);
		}

		static object ConvertRelativeOffset(long offset) {
			if (sbyte.MinValue <= offset && offset <= sbyte.MaxValue)
				return (sbyte)offset;
			if (short.MinValue <= offset && offset <= short.MaxValue)
				return (short)offset;
			if (int.MinValue <= offset && offset <= int.MaxValue)
				return (int)offset;
			return offset;
		}

		void WriteILOffset(uint offset) {
			if (offset <= ushort.MaxValue)
				OutputWrite(string.Format("0x{0:X4}", offset), TypeColor.Number);
			else
				OutputWrite(string.Format("0x{0:X8}", offset), TypeColor.Number);
		}

		static object ConvertAddressToDebuggeeIntPtr(ulong addr) {
			return Utils.IsDebuggee32Bit ? (object)(uint)addr : addr;
		}

		void WriteNativeAddress(ulong address) {
			if (Utils.IsDebuggee32Bit)
				OutputWrite(string.Format("0x{0:X8}", address), TypeColor.Number);
			else
				OutputWrite(string.Format("0x{0:X16}", address), TypeColor.Number);
		}

		string ConvertNumberToString(object value) {
			if (value == null)
				return string.Empty;
			if (!UseDecimal) {
				switch (Type.GetTypeCode(value.GetType())) {
				case TypeCode.SByte:	return string.Format("0x{0:X2}", value);
				case TypeCode.Int16:	return string.Format("0x{0:X4}", value);
				case TypeCode.Int32:	return string.Format("0x{0:X8}", value);
				case TypeCode.Int64:	return string.Format("0x{0:X16}", value);
				case TypeCode.Byte:		return string.Format("0x{0:X2}", value);
				case TypeCode.UInt16:	return string.Format("0x{0:X4}", value);
				case TypeCode.UInt32:	return string.Format("0x{0:X8}", value);
				case TypeCode.UInt64:	return string.Format("0x{0:X16}", value);
				}
				if (value is IntPtr)
					return string.Format("0x{0:X}", ((IntPtr)value).ToInt64());
				if (value is UIntPtr)
					return string.Format("0x{0:X}", ((UIntPtr)value).ToUInt64());
			}
			return value.ToString();
		}
	}
}
