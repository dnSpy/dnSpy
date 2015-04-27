//
// linq.cs: support for query expressions
//
// Authors: Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2007-2008 Novell, Inc
// Copyright 2011 Xamarin Inc
//

using System;
using System.Collections.Generic;

namespace Mono.CSharp.Linq
{
	public class QueryExpression : AQueryClause
	{
		public QueryExpression (AQueryClause start)
			: base (null, null, start.Location)
		{
			this.next = start;
		}

		public override Expression BuildQueryClause (ResolveContext ec, Expression lSide, Parameter parentParameter)
		{
			return next.BuildQueryClause (ec, lSide, parentParameter);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			int counter = QueryBlock.TransparentParameter.Counter;

			Expression e = BuildQueryClause (ec, null, null);
			if (e != null)
				e = e.Resolve (ec);

			//
			// Reset counter in probing mode to ensure that all transparent
			// identifier anonymous types are created only once
			//
			if (ec.IsInProbingMode)
				QueryBlock.TransparentParameter.Counter = counter;

			return e;
		}

		protected override string MethodName {
			get { throw new NotSupportedException (); }
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public abstract class AQueryClause : ShimExpression
	{
		protected class QueryExpressionAccess : MemberAccess
		{
			public QueryExpressionAccess (Expression expr, string methodName, Location loc)
				: base (expr, methodName, loc)
			{
			}

			public QueryExpressionAccess (Expression expr, string methodName, TypeArguments typeArguments, Location loc)
				: base (expr, methodName, typeArguments, loc)
			{
			}

			protected override void Error_TypeDoesNotContainDefinition (ResolveContext ec, TypeSpec type, string name)
			{
				ec.Report.Error (1935, loc, "An implementation of `{0}' query expression pattern could not be found. " +
					"Are you missing `System.Linq' using directive or `System.Core.dll' assembly reference?",
					name);
			}
		}

		protected class QueryExpressionInvocation : Invocation, OverloadResolver.IErrorHandler
		{
			public QueryExpressionInvocation (QueryExpressionAccess expr, Arguments arguments)
				: base (expr, arguments)
			{
			}

			protected override MethodGroupExpr DoResolveOverload (ResolveContext ec)
			{
				MethodGroupExpr rmg = mg.OverloadResolve (ec, ref arguments, this, OverloadResolver.Restrictions.None);
				return rmg;
			}

			protected override Expression DoResolveDynamic (ResolveContext ec, Expression memberExpr)
			{
				ec.Report.Error (1979, loc,
					"Query expressions with a source or join sequence of type `dynamic' are not allowed");
				return null;
			}

			#region IErrorHandler Members

			bool OverloadResolver.IErrorHandler.AmbiguousCandidates (ResolveContext ec, MemberSpec best, MemberSpec ambiguous)
			{
				ec.Report.SymbolRelatedToPreviousError (best);
				ec.Report.SymbolRelatedToPreviousError (ambiguous);
				ec.Report.Error (1940, loc, "Ambiguous implementation of the query pattern `{0}' for source type `{1}'",
					best.Name, mg.InstanceExpression.GetSignatureForError ());
				return true;
			}

			bool OverloadResolver.IErrorHandler.ArgumentMismatch (ResolveContext rc, MemberSpec best, Argument arg, int index)
			{
				return false;
			}

			bool OverloadResolver.IErrorHandler.NoArgumentMatch (ResolveContext rc, MemberSpec best)
			{
				return false;
			}

			bool OverloadResolver.IErrorHandler.TypeInferenceFailed (ResolveContext rc, MemberSpec best)
			{
				var ms = (MethodSpec) best;
				TypeSpec source_type = ms.Parameters.ExtensionMethodType;
				if (source_type != null) {
					Argument a = arguments[0];

					if (TypeManager.IsGenericType (source_type) && InflatedTypeSpec.ContainsTypeParameter (source_type)) {
						TypeInferenceContext tic = new TypeInferenceContext (source_type.TypeArguments);
						tic.OutputTypeInference (rc, a.Expr, source_type);
						if (tic.FixAllTypes (rc)) {
							source_type = source_type.GetDefinition ().MakeGenericType (rc, tic.InferredTypeArguments);
						}
					}

					if (!Convert.ImplicitConversionExists (rc, a.Expr, source_type)) {
						rc.Report.Error (1936, loc, "An implementation of `{0}' query expression pattern for source type `{1}' could not be found",
							best.Name, a.Type.GetSignatureForError ());
						return true;
					}
				}

				if (best.Name == "SelectMany") {
					rc.Report.Error (1943, loc,
						"An expression type is incorrect in a subsequent `from' clause in a query expression with source type `{0}'",
						arguments[0].GetSignatureForError ());
				} else {
					rc.Report.Error (1942, loc,
						"An expression type in `{0}' clause is incorrect. Type inference failed in the call to `{1}'",
						best.Name.ToLowerInvariant (), best.Name);
				}

				return true;
			}

			#endregion
		}

		public AQueryClause next;
		public QueryBlock block;

		protected AQueryClause (QueryBlock block, Expression expr, Location loc)
			 : base (expr)
		{
			this.block = block;
			this.loc = loc;
		}
		
		protected override void CloneTo (CloneContext clonectx, Expression target)
		{
			base.CloneTo (clonectx, target);

			AQueryClause t = (AQueryClause) target;

			if (block != null)
				t.block = (QueryBlock) clonectx.LookupBlock (block);

			if (next != null)
				t.next = (AQueryClause) next.Clone (clonectx);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			return expr.Resolve (ec);
		}

		public virtual Expression BuildQueryClause (ResolveContext ec, Expression lSide, Parameter parameter)
		{
			Arguments args = null;
			CreateArguments (ec, parameter, ref args);
			lSide = CreateQueryExpression (lSide, args);
			if (next != null) {
				parameter = CreateChildrenParameters (parameter);

				Select s = next as Select;
				if (s == null || s.IsRequired (parameter))
					return next.BuildQueryClause (ec, lSide, parameter);
					
				// Skip transparent select clause if any clause follows
				if (next.next != null)
					return next.next.BuildQueryClause (ec, lSide, parameter);
			}

			return lSide;
		}

		protected virtual Parameter CreateChildrenParameters (Parameter parameter)
		{
			// Have to clone the parameter for any children use, it carries block sensitive data
			return parameter.Clone ();
		}

		protected virtual void CreateArguments (ResolveContext ec, Parameter parameter, ref Arguments args)
		{
			args = new Arguments (2);

			LambdaExpression selector = new LambdaExpression (loc);

			block.SetParameter (parameter);
			selector.Block = block;
			selector.Block.AddStatement (new ContextualReturn (expr));

			args.Add (new Argument (selector));
		}

		protected Invocation CreateQueryExpression (Expression lSide, Arguments arguments)
		{
			return new QueryExpressionInvocation (
				new QueryExpressionAccess (lSide, MethodName, loc), arguments);
		}

		protected abstract string MethodName { get; }

		public AQueryClause Next {
			set {
				next = value;
			}
		}

		public AQueryClause Tail {
			get {
				return next == null ? this : next.Tail;
			}
		}
	}

	//
	// A query clause with an identifier (range variable)
	//
	public abstract class ARangeVariableQueryClause : AQueryClause
	{
		sealed class RangeAnonymousTypeParameter : AnonymousTypeParameter
		{
			public RangeAnonymousTypeParameter (Expression initializer, RangeVariable parameter)
				: base (initializer, parameter.Name, parameter.Location)
			{
			}

			protected override void Error_InvalidInitializer (ResolveContext ec, string initializer)
			{
				ec.Report.Error (1932, loc, "A range variable `{0}' cannot be initialized with `{1}'",
					Name, initializer);
			}
		}

		class RangeParameterReference : ParameterReference
		{
			Parameter parameter;

			public RangeParameterReference (Parameter p)
				: base (null, p.Location)
			{
				this.parameter = p;
			}

			protected override Expression DoResolve (ResolveContext ec)
			{
				pi = ec.CurrentBlock.ParametersBlock.GetParameterInfo (parameter);
				return base.DoResolve (ec);
			}
		}

		protected RangeVariable identifier;
		
		public RangeVariable  IntoVariable {
			get {
				return identifier;
			}
		}
		
		protected ARangeVariableQueryClause (QueryBlock block, RangeVariable identifier, Expression expr, Location loc)
			: base (block, expr, loc)
		{
			this.identifier = identifier;
		}

		public RangeVariable Identifier {
			get {
				return identifier;
			}
		}

		public FullNamedExpression IdentifierType { get; set; }

		protected Invocation CreateCastExpression (Expression lSide)
		{
			return new QueryExpressionInvocation (
				new QueryExpressionAccess (lSide, "Cast", new TypeArguments (IdentifierType), loc), null);
		}

		protected override Parameter CreateChildrenParameters (Parameter parameter)
		{
			return new QueryBlock.TransparentParameter (parameter.Clone (), GetIntoVariable ());
		}

		protected static Expression CreateRangeVariableType (ResolveContext rc, Parameter parameter, RangeVariable name, Expression init)
		{
			var args = new List<AnonymousTypeParameter> (2);

			//
			// The first argument is the reference to the parameter
			//
			args.Add (new AnonymousTypeParameter (new RangeParameterReference (parameter), parameter.Name, parameter.Location));

			//
			// The second argument is the linq expression
			//
			args.Add (new RangeAnonymousTypeParameter (init, name));

			//
			// Create unique anonymous type
			//
			return new NewAnonymousType (args, rc.MemberContext.CurrentMemberDefinition.Parent, name.Location);
		}

		protected virtual RangeVariable GetIntoVariable ()
		{
			return identifier;
		}
	}

	public sealed class RangeVariable : INamedBlockVariable
	{
		Block block;

		public RangeVariable (string name, Location loc)
		{
			Name = name;
			Location = loc;
		}

		#region Properties

		public Block Block {
			get {
				return block;
			}
			set {
				block = value;
			}
		}

		public bool IsDeclared {
			get {
				return true;
			}
		}

		public bool IsParameter {
			get {
				return false;
			}
		}

		public Location Location { get; private set; }

		public string Name { get; private set; }

		#endregion

		public Expression CreateReferenceExpression (ResolveContext rc, Location loc)
		{
			// 
			// We know the variable name is somewhere in the scope. This generates
			// an access expression from current block
			//
			var pb = rc.CurrentBlock.ParametersBlock;
			while (true) {
				if (pb is QueryBlock) {
					for (int i = pb.Parameters.Count - 1; i >= 0; --i) {
						var p = pb.Parameters[i];
						if (p.Name == Name)
							return pb.GetParameterReference (i, loc);

						Expression expr = null;
						var tp = p as QueryBlock.TransparentParameter;
						while (tp != null) {
							if (expr == null)
								expr = pb.GetParameterReference (i, loc);
							else
								expr = new TransparentMemberAccess (expr, tp.Name);

							if (tp.Identifier == Name)
								return new TransparentMemberAccess (expr, Name);

							if (tp.Parent.Name == Name)
								return new TransparentMemberAccess (expr, Name);

							tp = tp.Parent as QueryBlock.TransparentParameter;
						}
					}
				}

				if (pb == block)
					return null;

				pb = pb.Parent.ParametersBlock;
			}
		}
	}

	public class QueryStartClause : ARangeVariableQueryClause
	{
		public QueryStartClause (QueryBlock block, Expression expr, RangeVariable identifier, Location loc)
			: base (block, identifier, expr, loc)
		{
			block.AddRangeVariable (identifier);
		}

		public override Expression BuildQueryClause (ResolveContext ec, Expression lSide, Parameter parameter)
		{
			if (IdentifierType != null)
				expr = CreateCastExpression (expr);

			if (parameter == null)
				lSide = expr;

			return next.BuildQueryClause (ec, lSide, new ImplicitLambdaParameter (identifier.Name, identifier.Location));
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			Expression e = BuildQueryClause (ec, null, null);
			return e.Resolve (ec);
		}

		protected override string MethodName {
			get { throw new NotSupportedException (); }
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}


	public class GroupBy : AQueryClause
	{
		Expression element_selector;
		QueryBlock element_block;

		public Expression ElementSelector {
			get { return this.element_selector; }
		}

		public GroupBy (QueryBlock block, Expression elementSelector, QueryBlock elementBlock, Expression keySelector, Location loc)
			: base (block, keySelector, loc)
		{
			//
			// Optimizes clauses like `group A by A'
			//
			if (!elementSelector.Equals (keySelector)) {
				this.element_selector = elementSelector;
				this.element_block = elementBlock;
			}
		}

		public Expression SelectorExpression {
			get {
 				return element_selector;
			}
		}

		protected override void CreateArguments (ResolveContext ec, Parameter parameter, ref Arguments args)
		{
			base.CreateArguments (ec, parameter, ref args);

			if (element_selector != null) {
				LambdaExpression lambda = new LambdaExpression (element_selector.Location);

				element_block.SetParameter (parameter.Clone ());
				lambda.Block = element_block;
				lambda.Block.AddStatement (new ContextualReturn (element_selector));
				args.Add (new Argument (lambda));
			}
		}

		protected override void CloneTo (CloneContext clonectx, Expression target)
		{
			GroupBy t = (GroupBy) target;
			if (element_selector != null) {
				t.element_selector = element_selector.Clone (clonectx);
				t.element_block = (QueryBlock) element_block.Clone (clonectx);
			}

			base.CloneTo (clonectx, t);
		}

		protected override string MethodName {
			get { return "GroupBy"; }
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class Join : SelectMany
	{
		QueryBlock inner_selector, outer_selector;

		public RangeVariable JoinVariable {
			get { return this.GetIntoVariable (); }
		}
		
		public Join (QueryBlock block, RangeVariable lt, Expression inner, QueryBlock outerSelector, QueryBlock innerSelector, Location loc)
			: base (block, lt, inner, loc)
		{
			this.outer_selector = outerSelector;
			this.inner_selector = innerSelector;
		}

		public QueryBlock InnerSelector {
			get {
				return inner_selector;
			}
		}
		
		public QueryBlock OuterSelector {
			get {
				return outer_selector;
			}
		}

		protected override void CreateArguments (ResolveContext ec, Parameter parameter, ref Arguments args)
		{
			args = new Arguments (4);

			if (IdentifierType != null)
				expr = CreateCastExpression (expr);

			args.Add (new Argument (expr));

			outer_selector.SetParameter (parameter.Clone ());
			var lambda = new LambdaExpression (outer_selector.StartLocation);
			lambda.Block = outer_selector;
			args.Add (new Argument (lambda));

			inner_selector.SetParameter (new ImplicitLambdaParameter (identifier.Name, identifier.Location));
			lambda = new LambdaExpression (inner_selector.StartLocation);
			lambda.Block = inner_selector;
			args.Add (new Argument (lambda));

			base.CreateArguments (ec, parameter, ref args);
		}

		protected override void CloneTo (CloneContext clonectx, Expression target)
		{
			Join t = (Join) target;
			t.inner_selector = (QueryBlock) inner_selector.Clone (clonectx);
			t.outer_selector = (QueryBlock) outer_selector.Clone (clonectx);
			base.CloneTo (clonectx, t);
		}	

		protected override string MethodName {
			get { return "Join"; }
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class GroupJoin : Join
	{
		readonly RangeVariable into;

		public GroupJoin (QueryBlock block, RangeVariable lt, Expression inner,
			QueryBlock outerSelector, QueryBlock innerSelector, RangeVariable into, Location loc)
			: base (block, lt, inner, outerSelector, innerSelector, loc)
		{
			this.into = into;
		}

		protected override RangeVariable GetIntoVariable ()
		{
			return into;
		}

		protected override string MethodName {
			get { return "GroupJoin"; }
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class Let : ARangeVariableQueryClause
	{
		public Let (QueryBlock block, RangeVariable identifier, Expression expr, Location loc)
			: base (block, identifier, expr, loc)
		{
		}

		protected override void CreateArguments (ResolveContext ec, Parameter parameter, ref Arguments args)
		{
			expr = CreateRangeVariableType (ec, parameter, identifier, expr);
			base.CreateArguments (ec, parameter, ref args);
		}

		protected override string MethodName {
			get { return "Select"; }
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class Select : AQueryClause
	{
		public Select (QueryBlock block, Expression expr, Location loc)
			: base (block, expr, loc)
		{
		}
		
		//
		// For queries like `from a orderby a select a'
		// the projection is transparent and select clause can be safely removed 
		//
		public bool IsRequired (Parameter parameter)
		{
			SimpleName sn = expr as SimpleName;
			if (sn == null)
				return true;

			return sn.Name != parameter.Name;
		}

		protected override string MethodName {
			get { return "Select"; }
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

	}

	public class SelectMany : ARangeVariableQueryClause
	{
		public SelectMany (QueryBlock block, RangeVariable identifier, Expression expr, Location loc)
			: base (block, identifier, expr, loc)
		{
		}

		protected override void CreateArguments (ResolveContext ec, Parameter parameter, ref Arguments args)
		{
			if (args == null) {
				if (IdentifierType != null)
					expr = CreateCastExpression (expr);

				base.CreateArguments (ec, parameter.Clone (), ref args);
			}

			Expression result_selector_expr;
			QueryBlock result_block;

			var target = GetIntoVariable ();
			var target_param = new ImplicitLambdaParameter (target.Name, target.Location);

			//
			// When select follows use it as a result selector
			//
			if (next is Select) {
				result_selector_expr = next.Expr;

				result_block = next.block;
				result_block.SetParameters (parameter, target_param);

				next = next.next;
			} else {
				result_selector_expr = CreateRangeVariableType (ec, parameter, target, new SimpleName (target.Name, target.Location));

				result_block = new QueryBlock (block.Parent, block.StartLocation);
				result_block.SetParameters (parameter, target_param);
			}

			LambdaExpression result_selector = new LambdaExpression (Location);
			result_selector.Block = result_block;
			result_selector.Block.AddStatement (new ContextualReturn (result_selector_expr));

			args.Add (new Argument (result_selector));
		}

		protected override string MethodName {
			get { return "SelectMany"; }
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class Where : AQueryClause
	{
		public Where (QueryBlock block, Expression expr, Location loc)
			: base (block, expr, loc)
		{
		}

		protected override string MethodName {
			get { return "Where"; }
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class OrderByAscending : AQueryClause
	{
		public OrderByAscending (QueryBlock block, Expression expr)
			: base (block, expr, expr.Location)
		{
		}

		protected override string MethodName {
			get { return "OrderBy"; }
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class OrderByDescending : AQueryClause
	{
		public OrderByDescending (QueryBlock block, Expression expr)
			: base (block, expr, expr.Location)
		{
		}

		protected override string MethodName {
			get { return "OrderByDescending"; }
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class ThenByAscending : OrderByAscending
	{
		public ThenByAscending (QueryBlock block, Expression expr)
			: base (block, expr)
		{
		}

		protected override string MethodName {
			get { return "ThenBy"; }
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class ThenByDescending : OrderByDescending
	{
		public ThenByDescending (QueryBlock block, Expression expr)
			: base (block, expr)
		{
		}

		protected override string MethodName {
			get { return "ThenByDescending"; }
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	//
	// Implicit query block
	//
	public class QueryBlock : ParametersBlock
	{
		//
		// Transparent parameters are used to package up the intermediate results
		// and pass them onto next clause
		//
		public sealed class TransparentParameter : ImplicitLambdaParameter
		{
			public static int Counter;
			const string ParameterNamePrefix = "<>__TranspIdent";

			public readonly Parameter Parent;
			public readonly string Identifier;

			public TransparentParameter (Parameter parent, RangeVariable identifier)
				: base (ParameterNamePrefix + Counter++, identifier.Location)
			{
				Parent = parent;
				Identifier = identifier.Name;
			}

			public static void Reset ()
			{
				Counter = 0;
			}
		}

		public QueryBlock (Block parent, Location start)
			: base (parent, ParametersCompiled.EmptyReadOnlyParameters, start, Flags.CompilerGenerated)
		{
		}

		public void AddRangeVariable (RangeVariable variable)
		{
			variable.Block = this;
			TopBlock.AddLocalName (variable.Name, variable, true);
		}

		public override void Error_AlreadyDeclared (string name, INamedBlockVariable variable, string reason)
		{
			TopBlock.Report.Error (1931, variable.Location,
				"A range variable `{0}' conflicts with a previous declaration of `{0}'",
				name);
		}

		public override void Error_AlreadyDeclared (string name, INamedBlockVariable variable)
		{
			TopBlock.Report.Error (1930, variable.Location,
				"A range variable `{0}' has already been declared in this scope",
				name);		
		}

		public override void Error_AlreadyDeclaredTypeParameter (string name, Location loc)
		{
			TopBlock.Report.Error (1948, loc,
				"A range variable `{0}' conflicts with a method type parameter",
				name);
		}

		public void SetParameter (Parameter parameter)
		{
			base.parameters = new ParametersCompiled (parameter);
			base.parameter_info = new ParameterInfo[] {
				new ParameterInfo (this, 0)
			};
		}

		public void SetParameters (Parameter first, Parameter second)
		{
			base.parameters = new ParametersCompiled (first, second);
			base.parameter_info = new ParameterInfo[] {
				new ParameterInfo (this, 0),
				new ParameterInfo (this, 1)
			};
		}
	}

	sealed class TransparentMemberAccess : MemberAccess
	{
		public TransparentMemberAccess (Expression expr, string name)
			: base (expr, name)
		{
		}

		public override Expression DoResolveLValue (ResolveContext rc, Expression right_side)
		{
			rc.Report.Error (1947, loc,
				"A range variable `{0}' cannot be assigned to. Consider using `let' clause to store the value",
				Name);

			return null;
		}
	}
}
