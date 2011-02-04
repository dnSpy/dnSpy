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
using ICSharpCode.Decompiler;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.ILSpy.Disassembler
{
	static class DisassemblerHelpers
	{
		#region Debug output(ToString helpers)
		public static void WriteTo(this ExceptionHandler exceptionHandler, ITextOutput writer)
		{
			writer.Write("Try IL_{0:x4}-IL_{1:x4} ", exceptionHandler.TryStart.Offset, exceptionHandler.TryEnd.Offset);
			writer.Write(exceptionHandler.HandlerType.ToString());
			if (exceptionHandler.FilterStart != null) {
				writer.Write(" IL_{0:x4}-IL_{1:x4} handler ", exceptionHandler.FilterStart.Offset, exceptionHandler.FilterEnd.Offset);
			}
			writer.Write(" IL_{0:x4}-IL_{1:x4} ", exceptionHandler.HandlerStart.Offset, exceptionHandler.HandlerEnd.Offset);
		}
		
		public static void WriteTo(this Instruction instruction, ITextOutput writer)
		{
			writer.Write(CecilExtensions.OffsetToString(instruction.Offset));
			writer.Write(": ");
			writer.Write(instruction.OpCode.Name);
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
				writer.Write(CecilExtensions.OffsetToString(instructions [i].Offset));
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
		
		static void WriteMethodReference(ITextOutput writer, MethodReference method)
		{
			writer.Write(FormatTypeReference(method.ReturnType));
			writer.Write(' ');
			writer.Write(FormatTypeReference(method.DeclaringType));
			writer.Write("::");
			writer.Write(method.Name);
			writer.Write("(");
			var parameters = method.Parameters;
			for(int i=0; i < parameters.Count; ++i) {
				if(i > 0) writer.Write(", ");
				writer.Write(FormatTypeReference(parameters [i].ParameterType));
			}
			writer.Write(")");
		}
		
		static string FormatTypeReference(TypeReference type)
		{
			string typeName = type.FullName;
			switch(typeName) {
					case "System.Void": return "void";
					case "System.String": return "string";
					case "System.Int32": return "int32";
					case "System.Long": return "int64";
					case "System.Boolean": return "bool";
					case "System.Single": return "float32";
					case "System.Double": return "float64";
			}
			return typeName;
		}
		
		public static void WriteOperand(ITextOutput writer, object operand)
		{
			if(null == operand) throw new ArgumentNullException("operand");
			
			Instruction targetInstruction = operand as Instruction;
			if(null != targetInstruction) {
				writer.Write(CecilExtensions.OffsetToString(targetInstruction.Offset));
				return;
			}
			
			Instruction [] targetInstructions = operand as Instruction [];
			if(null != targetInstructions) {
				WriteLabelList(writer, targetInstructions);
				return;
			}
			
			VariableReference variableRef = operand as VariableReference;
			if(null != variableRef) {
				writer.Write(variableRef.Index.ToString());
				return;
			}
			
			MethodReference methodRef = operand as MethodReference;
			if(null != methodRef) {
				WriteMethodReference(writer, methodRef);
				return;
			}
			
			string s = operand as string;
			if(null != s) {
				writer.Write("\"" + s + "\"");
				return;
			}
			
			s = ToInvariantCultureString(operand);
			writer.Write(s);
		}
		#endregion
		
		public static string ShortTypeName(this TypeReference type)
		{
			switch (type.FullName) {
				case "System.Int16":
					return "short";
				case "System.Int32":
					return "int";
				case "System.Int64":
					return "long";
				case "System.UInt16":
					return "ushort";
				case "System.UInt32":
					return "uint";
				case "System.UInt64":
					return "ulong";
				case "System.Single":
					return "float";
				case "System.Double":
					return "double";
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
				default:
					string name = type.Name;
					int pos = name.LastIndexOf('`');
					if (pos >= 0)
						return name.Substring(0, pos);
					else
						return name;
			}
		}
	}
}
