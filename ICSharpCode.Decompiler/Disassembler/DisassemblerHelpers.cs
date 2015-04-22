// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.Disassembler
{
	public enum ILNameSyntax
	{
		/// <summary>
		/// class/valuetype + TypeName (built-in types use keyword syntax)
		/// </summary>
		Signature,
		/// <summary>
		/// Like signature, but always refers to type parameters using their position
		/// </summary>
		SignatureNoNamedTypeParameters,
		/// <summary>
		/// [assembly]Full.Type.Name (even for built-in types)
		/// </summary>
		TypeName,
		/// <summary>
		/// Name (but built-in types use keyword syntax)
		/// </summary>
		ShortTypeName
	}
	
	public static class DisassemblerHelpers
	{
		public static void WriteOffsetReference(ITextOutput writer, Instruction instruction)
		{
			writer.WriteReference(CecilExtensions.OffsetToString(instruction.Offset), instruction);
		}
		
		public static void WriteTo(this ExceptionHandler exceptionHandler, ITextOutput writer)
		{
			writer.Write("Try ");
			WriteOffsetReference(writer, exceptionHandler.TryStart);
			writer.Write('-');
			WriteOffsetReference(writer, exceptionHandler.TryEnd);
			writer.Write(' ');
			writer.Write(exceptionHandler.HandlerType.ToString());
			if (exceptionHandler.FilterStart != null) {
				writer.Write(' ');
				WriteOffsetReference(writer, exceptionHandler.FilterStart);
				writer.Write(" handler ");
			}
			if (exceptionHandler.CatchType != null) {
				writer.Write(' ');
				exceptionHandler.CatchType.WriteTo(writer);
			}
			writer.Write(' ');
			WriteOffsetReference(writer, exceptionHandler.HandlerStart);
			writer.Write('-');
			WriteOffsetReference(writer, exceptionHandler.HandlerEnd);
		}
		
		public static void WriteTo(this Instruction instruction, ITextOutput writer)
		{
			writer.WriteDefinition(CecilExtensions.OffsetToString(instruction.Offset), instruction);
			writer.Write(": ");
			writer.WriteReference(instruction.OpCode.Name, instruction.OpCode);
			if (instruction.Operand != null) {
				writer.Write(' ');
				if (instruction.OpCode == OpCodes.Ldtoken) {
					if (instruction.Operand is MethodReference)
						writer.Write("method ");
					else if (instruction.Operand is FieldReference)
						writer.Write("field ");
				}
				WriteOperand(writer, instruction.Operand);
			}
		}
		
		static void WriteLabelList(ITextOutput writer, Instruction[] instructions)
		{
			writer.Write("(");
			for(int i = 0; i < instructions.Length; i++) {
				if(i != 0) writer.Write(", ");
				WriteOffsetReference(writer, instructions[i]);
			}
			writer.Write(")");
		}
		
		static string ToInvariantCultureString(object value)
		{
			IConvertible convertible = value as IConvertible;
			return(null != convertible)
				? convertible.ToString(System.Globalization.CultureInfo.InvariantCulture)
				: value.ToString();
		}
		
		public static void WriteTo(this MethodReference method, ITextOutput writer)
		{
			if (method.ExplicitThis) {
				writer.Write("instance explicit ");
			}
			else if (method.HasThis) {
				writer.Write("instance ");
			}
			method.ReturnType.WriteTo(writer, ILNameSyntax.SignatureNoNamedTypeParameters);
			writer.Write(' ');
			if (method.DeclaringType != null) {
				method.DeclaringType.WriteTo(writer, ILNameSyntax.TypeName);
				writer.Write("::");
			}
			MethodDefinition md = method as MethodDefinition;
			if (md != null && md.IsCompilerControlled) {
				writer.WriteReference(Escape(method.Name + "$PST" + method.MetadataToken.ToInt32().ToString("X8")), method);
			} else {
				writer.WriteReference(Escape(method.Name), method);
			}
			GenericInstanceMethod gim = method as GenericInstanceMethod;
			if (gim != null) {
				writer.Write('<');
				for (int i = 0; i < gim.GenericArguments.Count; i++) {
					if (i > 0)
						writer.Write(", ");
					gim.GenericArguments[i].WriteTo(writer);
				}
				writer.Write('>');
			}
			writer.Write("(");
			var parameters = method.Parameters;
			for(int i = 0; i < parameters.Count; ++i) {
				if (i > 0) writer.Write(", ");
				parameters[i].ParameterType.WriteTo(writer, ILNameSyntax.SignatureNoNamedTypeParameters);
			}
			writer.Write(")");
		}
		
		static void WriteTo(this FieldReference field, ITextOutput writer)
		{
			field.FieldType.WriteTo(writer, ILNameSyntax.SignatureNoNamedTypeParameters);
			writer.Write(' ');
			field.DeclaringType.WriteTo(writer, ILNameSyntax.TypeName);
			writer.Write("::");
			writer.WriteReference(Escape(field.Name), field);
		}
		
		static bool IsValidIdentifierCharacter(char c)
		{
			return c == '_' || c == '$' || c == '@' || c == '?' || c == '`';
		}
		
		static bool IsValidIdentifier(string identifier)
		{
			if (string.IsNullOrEmpty(identifier))
				return false;
			if (!(char.IsLetter(identifier[0]) || IsValidIdentifierCharacter(identifier[0]))) {
				// As a special case, .ctor and .cctor are valid despite starting with a dot
				return identifier == ".ctor" || identifier == ".cctor";
			}
			for (int i = 1; i < identifier.Length; i++) {
				if (!(char.IsLetterOrDigit(identifier[i]) || IsValidIdentifierCharacter(identifier[i]) || identifier[i] == '.'))
					return false;
			}
			return true;
		}
		
		static readonly HashSet<string> ilKeywords = BuildKeywordList(
			"abstract", "algorithm", "alignment", "ansi", "any", "arglist",
			"array", "as", "assembly", "assert", "at", "auto", "autochar", "beforefieldinit",
			"blob", "blob_object", "bool", "brnull", "brnull.s", "brzero", "brzero.s", "bstr",
			"bytearray", "byvalstr", "callmostderived", "carray", "catch", "cdecl", "cf",
			"char", "cil", "class", "clsid", "const", "currency", "custom", "date", "decimal",
			"default", "demand", "deny", "endmac", "enum", "error", "explicit", "extends", "extern",
			"false", "famandassem", "family", "famorassem", "fastcall", "fault", "field", "filetime",
			"filter", "final", "finally", "fixed", "float", "float32", "float64", "forwardref",
			"fromunmanaged", "handler", "hidebysig", "hresult", "idispatch", "il", "illegal",
			"implements", "implicitcom", "implicitres", "import", "in", "inheritcheck", "init",
			"initonly", "instance", "int", "int16", "int32", "int64", "int8", "interface", "internalcall",
			"iunknown", "lasterr", "lcid", "linkcheck", "literal", "localloc", "lpstr", "lpstruct", "lptstr",
			"lpvoid", "lpwstr", "managed", "marshal", "method", "modopt", "modreq", "native", "nested",
			"newslot", "noappdomain", "noinlining", "nomachine", "nomangle", "nometadata", "noncasdemand",
			"noncasinheritance", "noncaslinkdemand", "noprocess", "not", "not_in_gc_heap", "notremotable",
			"notserialized", "null", "nullref", "object", "objectref", "opt", "optil", "out",
			"permitonly", "pinned", "pinvokeimpl", "prefix1", "prefix2", "prefix3", "prefix4", "prefix5", "prefix6",
			"prefix7", "prefixref", "prejitdeny", "prejitgrant", "preservesig", "private", "privatescope", "protected",
			"public", "record", "refany", "reqmin", "reqopt", "reqrefuse", "reqsecobj", "request", "retval",
			"rtspecialname", "runtime", "safearray", "sealed", "sequential", "serializable", "special", "specialname",
			"static", "stdcall", "storage", "stored_object", "stream", "streamed_object", "string", "struct",
			"synchronized", "syschar", "sysstring", "tbstr", "thiscall", "tls", "to", "true", "typedref",
			"unicode", "unmanaged", "unmanagedexp", "unsigned", "unused", "userdefined", "value", "valuetype",
			"vararg", "variant", "vector", "virtual", "void", "wchar", "winapi", "with", "wrapper",
			
			// These are not listed as keywords in spec, but ILAsm treats them as such
			"property", "type", "flags", "callconv", "strict"
		);
		
		static HashSet<string> BuildKeywordList(params string[] keywords)
		{
			HashSet<string> s = new HashSet<string>(keywords);
			foreach (var field in typeof(OpCodes).GetFields()) {
				s.Add(((OpCode)field.GetValue(null)).Name);
			}
			return s;
		}
		
		public static string Escape(string identifier)
		{
			if (IsValidIdentifier(identifier) && !ilKeywords.Contains(identifier)) {
				return identifier;
			} else {
				// The ECMA specification says that ' inside SQString should be ecaped using an octal escape sequence,
				// but we follow Microsoft's ILDasm and use \'.
				return "'" + NRefactory.CSharp.TextWriterTokenWriter.ConvertString(identifier).Replace("'", "\\'") + "'";
			}
		}
		
		public static void WriteTo(this TypeReference type, ITextOutput writer, ILNameSyntax syntax = ILNameSyntax.Signature)
		{
			ILNameSyntax syntaxForElementTypes = syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature;
			if (type is PinnedType) {
				((PinnedType)type).ElementType.WriteTo(writer, syntaxForElementTypes);
				writer.Write(" pinned");
			} else if (type is ArrayType) {
				ArrayType at = (ArrayType)type;
				at.ElementType.WriteTo(writer, syntaxForElementTypes);
				writer.Write('[');
				writer.Write(string.Join(", ", at.Dimensions));
				writer.Write(']');
			} else if (type is GenericParameter) {
				writer.Write('!');
				if (((GenericParameter)type).Owner.GenericParameterType == GenericParameterType.Method)
					writer.Write('!');
				if (string.IsNullOrEmpty(type.Name) || type.Name[0] == '!' || syntax == ILNameSyntax.SignatureNoNamedTypeParameters)
					writer.Write(((GenericParameter)type).Position.ToString());
				else
					writer.Write(Escape(type.Name));
			} else if (type is ByReferenceType) {
				((ByReferenceType)type).ElementType.WriteTo(writer, syntaxForElementTypes);
				writer.Write('&');
			} else if (type is PointerType) {
				((PointerType)type).ElementType.WriteTo(writer, syntaxForElementTypes);
				writer.Write('*');
			} else if (type is GenericInstanceType) {
				type.GetElementType().WriteTo(writer, syntaxForElementTypes);
				writer.Write('<');
				var arguments = ((GenericInstanceType)type).GenericArguments;
				for (int i = 0; i < arguments.Count; i++) {
					if (i > 0)
						writer.Write(", ");
					arguments[i].WriteTo(writer, syntaxForElementTypes);
				}
				writer.Write('>');
			} else if (type is OptionalModifierType) {
				((OptionalModifierType)type).ElementType.WriteTo(writer, syntax);
				writer.Write(" modopt(");
				((OptionalModifierType)type).ModifierType.WriteTo(writer, ILNameSyntax.TypeName);
				writer.Write(") ");
			} else if (type is RequiredModifierType) {
				((RequiredModifierType)type).ElementType.WriteTo(writer, syntax);
				writer.Write(" modreq(");
				((RequiredModifierType)type).ModifierType.WriteTo(writer, ILNameSyntax.TypeName);
				writer.Write(") ");
			} else {
				string name = PrimitiveTypeName(type.FullName);
				if (syntax == ILNameSyntax.ShortTypeName) {
					if (name != null)
						writer.Write(name);
					else
						writer.WriteReference(Escape(type.Name), type);
				} else if ((syntax == ILNameSyntax.Signature || syntax == ILNameSyntax.SignatureNoNamedTypeParameters) && name != null) {
					writer.Write(name);
				} else {
					if (syntax == ILNameSyntax.Signature || syntax == ILNameSyntax.SignatureNoNamedTypeParameters)
						writer.Write(type.IsValueType ? "valuetype " : "class ");
					
					if (type.DeclaringType != null) {
						type.DeclaringType.WriteTo(writer, ILNameSyntax.TypeName);
						writer.Write('/');
						writer.WriteReference(Escape(type.Name), type);
					} else {
						if (!type.IsDefinition && type.Scope != null && !(type is TypeSpecification))
							writer.Write("[{0}]", Escape(type.Scope.Name));
						writer.WriteReference(Escape(type.FullName), type);
					}
				}
			}
		}
		
		public static void WriteOperand(ITextOutput writer, object operand)
		{
			if (operand == null)
				throw new ArgumentNullException("operand");
			
			Instruction targetInstruction = operand as Instruction;
			if (targetInstruction != null) {
				WriteOffsetReference(writer, targetInstruction);
				return;
			}
			
			Instruction[] targetInstructions = operand as Instruction[];
			if (targetInstructions != null) {
				WriteLabelList(writer, targetInstructions);
				return;
			}
			
			VariableReference variableRef = operand as VariableReference;
			if (variableRef != null) {
				if (string.IsNullOrEmpty(variableRef.Name))
					writer.WriteReference(variableRef.Index.ToString(), variableRef);
				else
					writer.WriteReference(Escape(variableRef.Name), variableRef);
				return;
			}
			
			ParameterReference paramRef = operand as ParameterReference;
			if (paramRef != null) {
				if (string.IsNullOrEmpty(paramRef.Name))
					writer.WriteReference(paramRef.Index.ToString(), paramRef);
				else
					writer.WriteReference(Escape(paramRef.Name), paramRef);
				return;
			}
			
			MethodReference methodRef = operand as MethodReference;
			if (methodRef != null) {
				methodRef.WriteTo(writer);
				return;
			}
			
			TypeReference typeRef = operand as TypeReference;
			if (typeRef != null) {
				typeRef.WriteTo(writer, ILNameSyntax.TypeName);
				return;
			}
			
			FieldReference fieldRef = operand as FieldReference;
			if (fieldRef != null) {
				fieldRef.WriteTo(writer);
				return;
			}
			
			string s = operand as string;
			if (s != null) {
				writer.Write("\"" + NRefactory.CSharp.TextWriterTokenWriter.ConvertString(s) + "\"");
			} else if (operand is char) {
				writer.Write(((int)(char)operand).ToString());
			} else if (operand is float) {
				float val = (float)operand;
				if (val == 0) {
					if (1 / val == float.NegativeInfinity) {
						// negative zero is a special case
						writer.Write('-');
					}
					writer.Write("0.0");
				} else if (float.IsInfinity(val) || float.IsNaN(val)) {
					byte[] data = BitConverter.GetBytes(val);
					writer.Write('(');
					for (int i = 0; i < data.Length; i++) {
						if (i > 0)
							writer.Write(' ');
						writer.Write(data[i].ToString("X2"));
					}
					writer.Write(')');
				} else {
					writer.Write(val.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
				}
			} else if (operand is double) {
				double val = (double)operand;
				if (val == 0) {
					if (1 / val == double.NegativeInfinity) {
						// negative zero is a special case
						writer.Write('-');
					}
					writer.Write("0.0");
				} else if (double.IsInfinity(val) || double.IsNaN(val)) {
					byte[] data = BitConverter.GetBytes(val);
					writer.Write('(');
					for (int i = 0; i < data.Length; i++) {
						if (i > 0)
							writer.Write(' ');
						writer.Write(data[i].ToString("X2"));
					}
					writer.Write(')');
				} else {
					writer.Write(val.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
				}
			} else if (operand is bool) {
				writer.Write((bool)operand ? "true" : "false");
			} else {
				s = ToInvariantCultureString(operand);
				writer.Write(s);
			}
		}
		
		public static string PrimitiveTypeName(string fullName)
		{
			switch (fullName) {
				case "System.SByte":
					return "int8";
				case "System.Int16":
					return "int16";
				case "System.Int32":
					return "int32";
				case "System.Int64":
					return "int64";
				case "System.Byte":
					return "uint8";
				case "System.UInt16":
					return "uint16";
				case "System.UInt32":
					return "uint32";
				case "System.UInt64":
					return "uint64";
				case "System.Single":
					return "float32";
				case "System.Double":
					return "float64";
				case "System.Void":
					return "void";
				case "System.Boolean":
					return "bool";
				case "System.String":
					return "string";
				case "System.Char":
					return "char";
				case "System.Object":
					return "object";
				case "System.IntPtr":
					return "native int";
				default:
					return null;
			}
		}
	}
}
