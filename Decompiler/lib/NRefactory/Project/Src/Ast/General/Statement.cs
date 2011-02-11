// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.Ast
{
	public abstract class Statement : AbstractNode, INullable
	{
		public static Statement Null {
			get {
				return NullStatement.Instance;
			}
		}
		
		public virtual bool IsNull {
			get {
				return false;
			}
		}
		
		public static Statement CheckNull(Statement statement)
		{
			return statement ?? NullStatement.Instance;
		}
	}
	
	public abstract class StatementWithEmbeddedStatement : Statement
	{
		Statement embeddedStatement;
		
		public Statement EmbeddedStatement {
			get {
				return embeddedStatement;
			}
			set {
				embeddedStatement = Statement.CheckNull(value);
				if (value != null)
					value.Parent = this;
			}
		}
	}
	
	internal sealed class NullStatement : Statement
	{
		public static readonly NullStatement Instance = new NullStatement();
		
		public override bool IsNull {
			get { return true; }
		}
		
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return data;
		}
		
		public override string ToString()
		{
			return String.Format("[NullStatement]");
		}
	}
}
