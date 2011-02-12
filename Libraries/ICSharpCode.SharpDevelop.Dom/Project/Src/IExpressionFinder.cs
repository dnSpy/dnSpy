// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom
{
	public interface IExpressionFinder
	{
		/// <summary>
		/// Finds an expression before the current offset.
		/// </summary>
		ExpressionResult FindExpression(string text, int offset);
		
		/// <summary>
		/// Finds an expression around the current offset.
		/// </summary>
		ExpressionResult FindFullExpression(string text, int offset);
		
		/// <summary>
		/// Removed the last part of the expression.
		/// </summary>
		/// <example>
		/// "arr[i]" => "arr"
		/// "obj.Field" => "obj"
		/// "obj.Method(args,...)" => "obj.Method"
		/// </example>
		string RemoveLastPart(string expression);
	}
	
	/// <summary>
	/// Structure containing the result of a call to an expression finder.
	/// </summary>
	public struct ExpressionResult
	{
		public static readonly ExpressionResult Empty = new ExpressionResult(null);
		
		/// <summary>The expression that has been found at the specified offset.</summary>
		public string Expression;
		/// <summary>The exact source code location of the expression.</summary>
		public DomRegion Region;
		/// <summary>Specifies the context in which the expression was found.</summary>
		public ExpressionContext Context;
		/// <summary>An object carrying additional language-dependend data.</summary>
		public object Tag;
		
		public ExpressionResult(string expression) : this(expression, DomRegion.Empty, ExpressionContext.Default, null) {}
		public ExpressionResult(string expression, ExpressionContext context) : this(expression, DomRegion.Empty, context, null) {}
		
		public ExpressionResult(string expression, DomRegion region, ExpressionContext context, object tag)
		{
			this.Expression = expression;
			this.Region = region;
			this.Context = context;
			this.Tag = tag;
		}
		
		public override string ToString()
		{
			if (Context == ExpressionContext.Default)
				return "<" + Expression + ">";
			else
				return "<" + Expression + "> (" + Context.ToString() + ")";
		}
	}
}
