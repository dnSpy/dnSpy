//
// complete.cs: Expression that are used for completion suggestions.
//
// Author:
//   Miguel de Icaza (miguel@ximian.com)
//   Marek Safar (marek.safar@gmail.com)
//
// Copyright 2001, 2002, 2003 Ximian, Inc.
// Copyright 2003-2009 Novell, Inc.
// Copyright 2011 Xamarin Inc
//
// Completion* classes derive from ExpressionStatement as this allows
// them to pass through the parser in many conditions that require
// statements even when the expression is incomplete (for example
// completing inside a lambda
//
using System.Collections.Generic;
using System.Linq;

namespace Mono.CSharp {

	//
	// A common base class for Completing expressions, it
	// is just a very simple ExpressionStatement
	//
	public abstract class CompletingExpression : ExpressionStatement
	{
		public static void AppendResults (List<string> results, string prefix, IEnumerable<string> names)
		{
			foreach (string name in names) {
				if (name == null)
					continue;

				if (prefix != null && !name.StartsWith (prefix))
					continue;

				if (results.Contains (name))
					continue;

				if (prefix != null)
					results.Add (name.Substring (prefix.Length));
				else
					results.Add (name);
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			return false;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return null;
		}

		public override void EmitStatement (EmitContext ec)
		{
			// Do nothing
		}

		public override void Emit (EmitContext ec)
		{
			// Do nothing
		}
	}
	
	public class CompletionSimpleName : CompletingExpression {
		public string Prefix;
		
		public CompletionSimpleName (string prefix, Location l)
		{
			this.loc = l;
			this.Prefix = prefix;
		}
		
		protected override Expression DoResolve (ResolveContext ec)
		{
			var results = new List<string> ();

			ec.CurrentMemberDefinition.GetCompletionStartingWith (Prefix, results);

			throw new CompletionResult (Prefix, results.Distinct ().Select (l => l.Substring (Prefix.Length)).ToArray ());
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			// Nothing
		}
	}
	
	public class CompletionMemberAccess : CompletingExpression {
		Expression expr;
		string partial_name;
		TypeArguments targs;
		
		public CompletionMemberAccess (Expression e, string partial_name, Location l)
		{
			this.expr = e;
			this.loc = l;
			this.partial_name = partial_name;
		}

		public CompletionMemberAccess (Expression e, string partial_name, TypeArguments targs, Location l)
		{
			this.expr = e;
			this.loc = l;
			this.partial_name = partial_name;
			this.targs = targs;
		}
		
		protected override Expression DoResolve (ResolveContext ec)
		{
			Expression expr_resolved = expr.Resolve (ec,
				ResolveFlags.VariableOrValue | ResolveFlags.Type);

			if (expr_resolved == null)
				return null;

			TypeSpec expr_type = expr_resolved.Type;
			if (expr_type.IsPointer || expr_type.Kind == MemberKind.Void || expr_type == InternalType.NullLiteral || expr_type == InternalType.AnonymousMethod) {
				expr_resolved.Error_OperatorCannotBeApplied (ec, loc, ".", expr_type);
				return null;
			}

			if (targs != null) {
				if (!targs.Resolve (ec))
					return null;
			}

			var results = new List<string> ();
			if (expr_resolved is Namespace){
				Namespace nexpr = expr_resolved as Namespace;
				string namespaced_partial;

				if (partial_name == null)
					namespaced_partial = nexpr.Name;
				else
					namespaced_partial = nexpr.Name + "." + partial_name;

				ec.CurrentMemberDefinition.GetCompletionStartingWith (namespaced_partial, results);
				if (partial_name != null)
					results = results.Select (l => l.Substring (partial_name.Length)).ToList ();
			} else {
				var r = MemberCache.GetCompletitionMembers (ec, expr_type, partial_name).Select (l => l.Name);
				AppendResults (results, partial_name, r);
			}

			throw new CompletionResult (partial_name == null ? "" : partial_name, results.Distinct ().ToArray ());
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			CompletionMemberAccess target = (CompletionMemberAccess) t;

			if (targs != null)
				target.targs = targs.Clone ();

			target.expr = expr.Clone (clonectx);
		}
	}

	public class CompletionElementInitializer : CompletingExpression {
		string partial_name;
		
		public CompletionElementInitializer (string partial_name, Location l)
		{
			this.partial_name = partial_name;
			this.loc = l;
		}
		
		protected override Expression DoResolve (ResolveContext ec)
		{
			var members = MemberCache.GetCompletitionMembers (ec, ec.CurrentInitializerVariable.Type, partial_name);

// TODO: Does this mean exact match only ?
//			if (partial_name != null && results.Count > 0 && result [0] == "")
//				throw new CompletionResult ("", new string [] { "=" });

			var results = members.Where (l => (l.Kind & (MemberKind.Field | MemberKind.Property)) != 0).Select (l => l.Name).ToList ();
			if (partial_name != null) {
				var temp = new List<string> ();
				AppendResults (temp, partial_name, results);
				results = temp;
			}

			throw new CompletionResult (partial_name == null ? "" : partial_name, results.Distinct ().ToArray ());
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			// Nothing
		}
	}
	
}
