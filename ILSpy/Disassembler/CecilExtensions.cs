// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.ILSpy.Disassembler
{
	static class CecilExtensions
	{
		#region Debug output(ToString helpers)
		public static string OffsetToString(int offset)
		{
			return string.Format("IL_{0:x4}", offset);
		}
		
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
			writer.Write(OffsetToString(instruction.Offset));
			writer.Write(": ");
			writer.Write(instruction.OpCode.Name);
			if(null != instruction.Operand) {
				writer.Write(' ');
				WriteOperand(writer, instruction.Operand);
			}
		}
		
		public static void WriteOperand(ITextOutput writer, object operand)
		{
			if(null == operand) throw new ArgumentNullException("operand");
			
			Instruction targetInstruction = operand as Instruction;
			if(null != targetInstruction) {
				writer.Write(OffsetToString(targetInstruction.Offset));
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
		
		static void WriteLabelList(ITextOutput writer, Instruction[] instructions)
		{
			writer.Write("(");
			for(int i = 0; i < instructions.Length; i++) {
				if(i != 0) writer.Write(", ");
				writer.Write(OffsetToString(instructions [i].Offset));
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
		#endregion
		
		#region GetPushDelta / GetPopDelta
		public static int GetPushDelta(this Instruction instruction)
		{
			OpCode code = instruction.OpCode;
			switch (code.StackBehaviourPush) {
				case StackBehaviour.Push0:
					return 0;

				case StackBehaviour.Push1:
				case StackBehaviour.Pushi:
				case StackBehaviour.Pushi8:
				case StackBehaviour.Pushr4:
				case StackBehaviour.Pushr8:
				case StackBehaviour.Pushref:
					return 1;

				case StackBehaviour.Push1_push1:
					return 2;

				case StackBehaviour.Varpush:
					if (code.FlowControl != FlowControl.Call)
						break;

					IMethodSignature method = (IMethodSignature) instruction.Operand;
					return IsVoid (method.ReturnType) ? 0 : 1;
			}

			throw new NotSupportedException ();
		}
		
		public static int GetPopDelta(this Instruction instruction, MethodDefinition current, int currentStackSize)
		{
			OpCode code = instruction.OpCode;
			switch (code.StackBehaviourPop) {
				case StackBehaviour.Pop0:
					return 0;
				case StackBehaviour.Popi:
				case StackBehaviour.Popref:
				case StackBehaviour.Pop1:
					return 1;

				case StackBehaviour.Pop1_pop1:
				case StackBehaviour.Popi_pop1:
				case StackBehaviour.Popi_popi:
				case StackBehaviour.Popi_popi8:
				case StackBehaviour.Popi_popr4:
				case StackBehaviour.Popi_popr8:
				case StackBehaviour.Popref_pop1:
				case StackBehaviour.Popref_popi:
					return 2;

				case StackBehaviour.Popi_popi_popi:
				case StackBehaviour.Popref_popi_popi:
				case StackBehaviour.Popref_popi_popi8:
				case StackBehaviour.Popref_popi_popr4:
				case StackBehaviour.Popref_popi_popr8:
				case StackBehaviour.Popref_popi_popref:
					return 3;

				case StackBehaviour.PopAll:
					return currentStackSize;

				case StackBehaviour.Varpop:
					if (code == OpCodes.Ret)
						return IsVoid (current.ReturnType) ? 0 : 1;

					if (code.FlowControl != FlowControl.Call)
						break;

					IMethodSignature method = (IMethodSignature) instruction.Operand;
					int count = method.HasParameters ? method.Parameters.Count : 0;
					if (method.HasThis && code != OpCodes.Newobj)
						++count;

					return count;
			}

			throw new NotSupportedException ();
		}
		
		public static bool IsVoid(this TypeReference type)
		{
			return type.FullName == "System.Void" && !(type is TypeSpecification);
		}
		
		public static bool IsValueTypeOrVoid(this TypeReference type)
		{
			while (type is OptionalModifierType || type is RequiredModifierType)
				type = ((TypeSpecification)type).ElementType;
			if (type is ArrayType)
				return false;
			return type.IsValueType || type.IsVoid();
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
