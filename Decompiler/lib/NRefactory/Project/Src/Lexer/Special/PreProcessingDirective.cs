// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory
{
	public class PreprocessingDirective : AbstractSpecial
	{
		public static void VBToCSharp(IList<ISpecial> list)
		{
			for (int i = 0; i < list.Count; ++i) {
				if (list[i] is PreprocessingDirective)
					list[i] = VBToCSharp((PreprocessingDirective)list[i]);
			}
		}
		
		public static PreprocessingDirective VBToCSharp(PreprocessingDirective dir)
		{
			string cmd = dir.Cmd;
			string arg = dir.Arg;
			if (cmd.Equals("#End", StringComparison.InvariantCultureIgnoreCase)) {
				if (arg.ToLowerInvariant().StartsWith("region")) {
					cmd = "#endregion";
					arg = "";
				} else if ("if".Equals(arg, StringComparison.InvariantCultureIgnoreCase)) {
					cmd = "#endif";
					arg = "";
				}
			} else if (cmd.Equals("#Region", StringComparison.InvariantCultureIgnoreCase)) {
				cmd = "#region";
			} else if (cmd.Equals("#If", StringComparison.InvariantCultureIgnoreCase)) {
				cmd = "#if";
				if (arg.ToLowerInvariant().EndsWith(" then"))
					arg = arg.Substring(0, arg.Length - 5);
			}
			return new PreprocessingDirective(cmd, arg, dir.StartPosition, dir.EndPosition);
		}
		
		public static void CSharpToVB(List<ISpecial> list)
		{
			for (int i = 0; i < list.Count; ++i) {
				if (list[i] is PreprocessingDirective)
					list[i] = CSharpToVB((PreprocessingDirective)list[i]);
			}
		}
		
		public static PreprocessingDirective CSharpToVB(PreprocessingDirective dir)
		{
			string cmd = dir.Cmd;
			string arg = dir.Arg;
			switch (cmd) {
				case "#region":
					cmd = "#Region";
					if (!arg.StartsWith("\"")) {
						arg = "\"" + arg.Trim() + "\"";
					}
					break;
				case "#endregion":
					cmd = "#End";
					arg = "Region";
					break;
				case "#endif":
					cmd = "#End";
					arg = "If";
					break;
				case "#if":
					arg += " Then";
					break;
			}
			if (cmd.Length > 1) {
				cmd = cmd.Substring(0, 2).ToUpperInvariant() + cmd.Substring(2);
			}
			return new PreprocessingDirective(cmd, arg, dir.StartPosition, dir.EndPosition);
		}
		
		string cmd;
		string arg;
		
		public string Cmd {
			get {
				return cmd;
			}
			set {
				cmd = value ?? string.Empty;
			}
		}
		
		public string Arg {
			get {
				return arg;
			}
			set {
				arg = value ?? string.Empty;
			}
		}
		
		public override string ToString()
		{
			return String.Format("[PreProcessingDirective: Cmd = {0}, Arg = {1}]",
			                     Cmd,
			                     Arg);
		}
		
		public PreprocessingDirective(string cmd, string arg, Location start, Location end)
			: base(start, end)
		{
			this.Cmd = cmd;
			this.Arg = arg;
		}
		
		public override object AcceptVisitor(ISpecialVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
