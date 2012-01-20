using System;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Cecil.Tests {

	public static class Formatter {

		public static string FormatInstruction (Instruction instruction)
		{
			var writer = new StringWriter ();
			WriteInstruction (writer, instruction);
			return writer.ToString ();
		}

		public static string FormatMethodBody (MethodDefinition method)
		{
			var writer = new StringWriter ();
			WriteMethodBody (writer, method);
			return writer.ToString ();
		}

		public static void WriteMethodBody (TextWriter writer, MethodDefinition method)
		{
			var body = method.Body;

			WriteVariables (writer, body);

			foreach (Instruction instruction in body.Instructions) {
				var sequence_point = instruction.SequencePoint;
				if (sequence_point != null) {
					writer.Write ('\t');
					WriteSequencePoint (writer, sequence_point);
					writer.WriteLine ();
				}

				writer.Write ('\t');
				WriteInstruction (writer, instruction);
				writer.WriteLine ();
			}

			WriteExceptionHandlers (writer, body);
		}

		static void WriteVariables (TextWriter writer, MethodBody body)
		{
			var variables = body.Variables;

			writer.Write ('\t');
			writer.Write (".locals {0}(", body.InitLocals ? "init " : string.Empty);

			for (int i = 0; i < variables.Count; i++) {
				if (i > 0)
					writer.Write (", ");

				var variable = variables [i];

				writer.Write ("{0} {1}", variable.VariableType, variable);
			}
			writer.WriteLine (")");
		}

		static void WriteInstruction (TextWriter writer, Instruction instruction)
		{
			writer.Write (FormatLabel (instruction.Offset));
			writer.Write (": ");
			writer.Write (instruction.OpCode.Name);
			if (null != instruction.Operand) {
				writer.Write (' ');
				WriteOperand (writer, instruction.Operand);
			}
		}

		static void WriteSequencePoint (TextWriter writer, SequencePoint sequence_point)
		{
			writer.Write (".line {0},{1}:{2},{3} '{4}'",
				sequence_point.StartLine,
				sequence_point.EndLine,
				sequence_point.StartColumn,
				sequence_point.EndColumn,
				sequence_point.Document.Url);
		}

		static string FormatLabel (int offset)
		{
			string label = "000" + offset.ToString ("x");
			return "IL_" + label.Substring (label.Length - 4);
		}

		static string FormatLabel (Instruction instruction)
		{
			return FormatLabel (instruction.Offset);
		}

		static void WriteOperand (TextWriter writer, object operand)
		{
			if (null == operand) throw new ArgumentNullException ("operand");

			var target = operand as Instruction;
			if (null != target) {
				writer.Write (FormatLabel (target.Offset));
				return;
			}

			var targets = operand as Instruction [];
			if (null != targets) {
				WriteLabelList (writer, targets);
				return;
			}

			string s = operand as string;
			if (null != s) {
				writer.Write ("\"" + s + "\"");
				return;
			}

			var parameter = operand as ParameterDefinition;
			if (parameter != null) {
				writer.Write (ToInvariantCultureString (parameter.Sequence));
				return;
			}

			s = ToInvariantCultureString (operand);
			writer.Write (s);
		}

		static void WriteLabelList (TextWriter writer, Instruction [] instructions)
		{
			writer.Write ("(");

			for (int i = 0; i < instructions.Length; i++) {
				if (i != 0) writer.Write (", ");
				writer.Write (FormatLabel (instructions [i].Offset));
			}

			writer.Write (")");
		}

		static void WriteExceptionHandlers (TextWriter writer, MethodBody body)
		{
			if (!body.HasExceptionHandlers)
				return;

			foreach (var handler in body.ExceptionHandlers) {
				writer.Write ("\t");
				writer.WriteLine (".try {0} to {1} {2} handler {3} to {4}",
					FormatLabel (handler.TryStart),
					FormatLabel (handler.TryEnd),
					FormatHandlerType (handler),
					FormatLabel (handler.HandlerStart),
					FormatLabel (handler.HandlerEnd));
			}
		}

		static string FormatHandlerType (ExceptionHandler handler)
		{
			var handler_type = handler.HandlerType;
			var type = handler_type.ToString ().ToLowerInvariant ();

			switch (handler_type) {
			case ExceptionHandlerType.Catch:
				return string.Format ("{0} {1}", type, handler.CatchType.FullName);
			case ExceptionHandlerType.Filter:
				throw new NotImplementedException ();
			default:
				return type;
			}
		}

		public static string ToInvariantCultureString (object value)
		{
			var convertible = value as IConvertible;
			return (null != convertible)
				? convertible.ToString (System.Globalization.CultureInfo.InvariantCulture)
				: value.ToString ();
		}
	}
}
