// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision$</version>
// </file>

using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Parser.VB
{
	internal class ParamModifierList
	{
		ParameterModifiers cur;
		Parser   parser;
		
		public ParameterModifiers Modifier {
			get {
				return cur;
			}
		}
		
		public ParamModifierList(Parser parser)
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
