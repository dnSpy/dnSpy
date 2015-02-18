// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.CSharp;

namespace Debugger
{
	public class GetValueException: DebuggerException
	{
		AstNode expression;
		string error;
		
		/// <summary> Expression that has caused this exception to occur </summary>
		public AstNode Expression {
			get { return expression; }
			set { expression = value; }
		}
		
		public string Error {
			get { return error; }
		}
		
		public override string Message {
			get {
				if (expression == null) {
					return error;
				} else {
					return error;
					// return String.Format("Error evaluating \"{0}\": {1}", expression.PrettyPrint(), error);
				}
			}
		}
		
		public GetValueException(AstNode expression, string error):base(error)
		{
			this.expression = expression;
			this.error = error;
		}
		
		public GetValueException(string error, System.Exception inner):base(error, inner)
		{
			this.error = error;
		}
		
		public GetValueException(string errorFmt, params object[] args):base(string.Format(errorFmt, args))
		{
			this.error = string.Format(errorFmt, args);
		}
		
		public GetValueException(string error):base(error)
		{
			this.error = error;
		}
	}
}
