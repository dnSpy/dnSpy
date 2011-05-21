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
		/// Name (even for built-in types)
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
			if(null != instruction.Operand) {
				writer.Write(' ');
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
			if (method.HasThis)
				writer.Write("instance ");
			method.ReturnType.WriteTo(writer, ILNameSyntax.SignatureNoNamedTypeParameters);
			writer.Write(' ');
			if (method.DeclaringType != null) {
				method.DeclaringType.WriteTo(writer, ILNameSyntax.TypeName);
				writer.Write("::");
			}
			writer.WriteReference(Escape(method.Name), method);
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
		
		static bool IsValidIdentifier(string identifier)
		{
			if (string.IsNullOrEmpty(identifier))
				return false;
			if (!(char.IsLetter(identifier[0]) || identifier[0] == '_' || identifier[0] == '.'))
				return false;
			for (int i = 1; i < identifier.Length; i++) {
				if (!(char.IsLetterOrDigit(identifier[i]) || identifier[i] == '_' || identifier[i] == '.' || identifier[i] == '`'))
					return false;
			}
			return true;
		}
		
		public static string Escape(string identifier)
		{
			if (IsValidIdentifier(identifier) && identifier != "value")
				return identifier;
			else
				return "'" + identifier + "'";
		}
		
		public static void WriteTo(this TypeReference type, ITextOutput writer, ILNameSyntax syntax = ILNameSyntax.Signature)
		{
			if (type is PinnedType) {
				writer.Write("pinned ");
				((PinnedType)type).ElementType.WriteTo(writer, syntax);
			} else if (type is ArrayType) {
				ArrayType at = (ArrayType)type;
				at.ElementType.WriteTo(writer, syntax);
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
				((ByReferenceType)type).ElementType.WriteTo(writer, syntax);
				writer.Write('&');
			} else if (type is PointerType) {
				((PointerType)type).ElementType.WriteTo(writer, syntax);
				writer.Write('*');
			} else if (type is GenericInstanceType) {
				type.GetElementType().WriteTo(writer, syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature);
				writer.Write('<');
				var arguments = ((GenericInstanceType)type).GenericArguments;
				for (int i = 0; i < arguments.Count; i++) {
					if (i > 0)
						writer.Write(", ");
					arguments[i].WriteTo(writer, syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature);
				}
				writer.Write('>');
			} else if (type is OptionalModifierType) {
				writer.Write("modopt(");
				((OptionalModifierType)type).ModifierType.WriteTo(writer, ILNameSyntax.TypeName);
				writer.Write(") ");
				((OptionalModifierType)type).ElementType.WriteTo(writer, syntax);
			} else if (type is RequiredModifierType) {
				writer.Write("modreq(");
				((RequiredModifierType)type).ModifierType.WriteTo(writer, ILNameSyntax.TypeName);
				writer.Write(") ");
				((RequiredModifierType)type).ElementType.WriteTo(writer, syntax);
			} else {
				string name = PrimitiveTypeName(type);
				if (syntax == ILNameSyntax.ShortTypeName) {
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
				writer.Write("\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
				return;
			}
			
			if (operand is float) {
				float val = (float)operand;
				if (val == 0) {
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
				return;
			} else if (operand is double) {
				double val = (double)operand;
				if (val == 0) {
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
				return;
			}
			s = ToInvariantCultureString(operand);
			writer.Write(s);
		}
		
		public static string PrimitiveTypeName(this TypeReference type)
		{
			switch (type.FullName) {
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
