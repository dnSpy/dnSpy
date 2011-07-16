// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB
{
//	public class PreprocessingDirective : AbstractSpecial
//	{
//		#region Conversion C# <-> VB
//		public static void VBToCSharp(IList<ISpecial> list)
//		{
//			for (int i = 0; i < list.Count; ++i) {
//				if (list[i] is PreprocessingDirective)
//					list[i] = VBToCSharp((PreprocessingDirective)list[i]);
//			}
//		}
//		
//		public static PreprocessingDirective VBToCSharp(PreprocessingDirective dir)
//		{
//			string cmd = dir.Cmd;
//			string arg = dir.Arg;
//			if (cmd.Equals("#End", StringComparison.InvariantCultureIgnoreCase)) {
//				if (arg.ToLowerInvariant().StartsWith("region")) {
//					cmd = "#endregion";
//					arg = "";
//				} else if ("if".Equals(arg, StringComparison.InvariantCultureIgnoreCase)) {
//					cmd = "#endif";
//					arg = "";
//				}
//			} else if (cmd.Equals("#Region", StringComparison.InvariantCultureIgnoreCase)) {
//				cmd = "#region";
//			} else if (cmd.Equals("#If", StringComparison.InvariantCultureIgnoreCase)) {
//				cmd = "#if";
//				if (arg.ToLowerInvariant().EndsWith(" then"))
//					arg = arg.Substring(0, arg.Length - 5);
//			} else if (cmd.Equals("#Else", StringComparison.InvariantCultureIgnoreCase)) {
//				if (dir.Expression != null)
//					cmd = "#elif";
//				else
//					cmd = "#else";
//			} else if (cmd.Equals("#ElseIf", StringComparison.InvariantCultureIgnoreCase)) {
//				cmd = "#elif";
//			}
//			return new PreprocessingDirective(cmd, arg, dir.StartPosition, dir.EndPosition) {
//				Expression = dir.Expression
//			};
//		}
//		
//		public static void CSharpToVB(List<ISpecial> list)
//		{
//			for (int i = 0; i < list.Count; ++i) {
//				if (list[i] is PreprocessingDirective)
//					list[i] = CSharpToVB((PreprocessingDirective)list[i]);
//			}
//		}
//		
//		public static PreprocessingDirective CSharpToVB(PreprocessingDirective dir)
//		{
//			string cmd = dir.Cmd;
//			string arg = dir.Arg;
//			switch (cmd) {
//				case "#region":
//					cmd = "#Region";
//					if (!arg.StartsWith("\"")) {
//						arg = "\"" + arg.Trim() + "\"";
//					}
//					break;
//				case "#endregion":
//					cmd = "#End";
//					arg = "Region";
//					break;
//				case "#endif":
//					cmd = "#End";
//					arg = "If";
//					break;
//				case "#if":
//					arg += " Then";
//					break;
//			}
//			if (cmd.Length > 1) {
//				cmd = cmd.Substring(0, 2).ToUpperInvariant() + cmd.Substring(2);
//			}
//			return new PreprocessingDirective(cmd, arg, dir.StartPosition, dir.EndPosition) {
//				Expression = dir.Expression
//			};
//		}
//		#endregion
//		
//		string cmd;
//		string arg;
//		Ast.Expression expression = Ast.Expression.Null;
//		
//		/// <summary>
//		/// Gets the directive name, including '#'.
//		/// </summary>
//		public string Cmd {
//			get {
//				return cmd;
//			}
//			set {
//				cmd = value ?? string.Empty;
//			}
//		}
//		
//		/// <summary>
//		/// Gets the directive argument.
//		/// </summary>
//		public string Arg {
//			get {
//				return arg;
//			}
//			set {
//				arg = value ?? string.Empty;
//			}
//		}
//		
//		/// <summary>
//		/// Gets/sets the expression (for directives that take an expression, e.g. #if and #elif).
//		/// </summary>
//		public Ast.Expression Expression {
//			get { return expression; }
//			set { expression = value ?? Ast.Expression.Null; }
//		}
//		
//		/// <value>
//		/// The end position of the pre processor directive line.
//		/// May be != EndPosition.
//		/// </value>
//		public Location LastLineEnd {
//			get;
//			set;
//		}
//		
//				
//		public override string ToString()
//		{
//			return String.Format("[PreProcessingDirective: Cmd = {0}, Arg = {1}]",
//			                     Cmd,
//			                     Arg);
//		}
//		
//		public PreprocessingDirective(string cmd, string arg, Location start, Location end)
//			: base(start, end)
//		{
//			this.Cmd = cmd;
//			this.Arg = arg;
//		}
//		
//		public override object AcceptVisitor(ISpecialVisitor visitor, object data)
//		{
//			return visitor.Visit(this, data);
//		}
//	}
}
