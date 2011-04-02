// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB.Parser
{
	internal class ParamModifierList
	{
		ParameterModifiers cur;
		VBParser   parser;
		
		public ParameterModifiers Modifier {
			get {
				return cur;
			}
		}
		
		public ParamModifierList(VBParser parser)
		{
			this.parser = parser;
			cur         = ParameterModifiers.None;
		}
		
		public bool isNone { get { return cur == ParameterModifiers.None; } }
		
		public void Add(ParameterModifiers m) 
		{
			if ((cur & m) == 0) {
				cur |= m;
			} else {
				parser.Error("param modifier " + m + " already defined");
			}
		}
		
		public void Add(ParamModifierList m)
		{
			Add(m.cur);
		}
		
		public void Check()
		{
			if((cur & ParameterModifiers.In) != 0 && 
			   (cur & ParameterModifiers.Ref) != 0) {
				parser.Error("ByRef and ByVal are not allowed at the same time.");
			}
		}
	}
}
