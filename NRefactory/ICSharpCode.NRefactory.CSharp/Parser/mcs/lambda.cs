//
// lambda.cs: support for lambda expressions
//
// Authors: Miguel de Icaza (miguel@gnu.org)
//          Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2007-2008 Novell, Inc
// Copyright 2011 Xamarin Inc
//

#if STATIC
using IKVM.Reflection.Emit;
#else
using System.Reflection.Emit;
#endif

namespace Mono.CSharp {
	public class LambdaExpression : AnonymousMethodExpression
	{
		//
		// The parameters can either be:
		//    A list of Parameters (explicitly typed parameters)
		//    An ImplicitLambdaParameter
		//
		public LambdaExpression (Location loc)
			: base (loc)
		{
		}

		protected override Expression CreateExpressionTree (ResolveContext ec, TypeSpec delegate_type)
		{
			if (ec.IsInProbingMode)
				return this;

			BlockContext bc = new BlockContext (ec.MemberContext, ec.ConstructorBlock, ec.BuiltinTypes.Void) {
				CurrentAnonymousMethod = ec.CurrentAnonymousMethod
			};

			Expression args = Parameters.CreateExpressionTree (bc, loc);
			Expression expr = Block.CreateExpressionTree (ec);
			if (expr == null)
				return null;

			Arguments arguments = new Arguments (2);
			arguments.Add (new Argument (expr));
			arguments.Add (new Argument (args));
			return CreateExpressionFactoryCall (ec, "Lambda",
				new TypeArguments (new TypeExpression (delegate_type, loc)),
				arguments);
		}

		public override bool HasExplicitParameters {
			get {
				return Parameters.Count > 0 && !(Parameters.FixedParameters [0] is ImplicitLambdaParameter);
			}
		}

		protected override ParametersCompiled ResolveParameters (ResolveContext ec, TypeInferenceContext tic, TypeSpec delegateType)
		{
			if (!delegateType.IsDelegate)
				return null;

			AParametersCollection d_params = Delegate.GetParameters (delegateType);

			if (HasExplicitParameters) {
				if (!VerifyExplicitParameters (ec, tic, delegateType, d_params))
					return null;

				return Parameters;
			}

			//
			// If L has an implicitly typed parameter list we make implicit parameters explicit
			// Set each parameter of L is given the type of the corresponding parameter in D
			//
			if (!VerifyParameterCompatibility (ec, tic, delegateType, d_params, ec.IsInProbingMode))
				return null;

			TypeSpec [] ptypes = new TypeSpec [Parameters.Count];
			for (int i = 0; i < d_params.Count; i++) {
				// D has no ref or out parameters
				if ((d_params.FixedParameters[i].ModFlags & Parameter.Modifier.RefOutMask) != 0)
					return null;

				TypeSpec d_param = d_params.Types [i];

				//
				// When type inference context exists try to apply inferred type arguments
				//
				if (tic != null) {
					d_param = tic.InflateGenericArgument (ec, d_param);
				}

				ptypes [i] = d_param;
				ImplicitLambdaParameter ilp = (ImplicitLambdaParameter) Parameters.FixedParameters [i];
				ilp.SetParameterType (d_param);
				ilp.Resolve (null, i);
			}

			Parameters.Types = ptypes;
			return Parameters;
		}

		protected override AnonymousMethodBody CompatibleMethodFactory (TypeSpec returnType, TypeSpec delegateType, ParametersCompiled p, ParametersBlock b)
		{
			return new LambdaMethod (p, b, returnType, delegateType, loc);
		}

		protected override bool DoResolveParameters (ResolveContext rc)
		{
			//
			// Only explicit parameters can be resolved at this point
			//
			if (HasExplicitParameters) {
				return Parameters.Resolve (rc);
			}

			return true;
		}

		public override string GetSignatureForError ()
		{
			return "lambda expression";
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	class LambdaMethod : AnonymousMethodBody
	{
		public LambdaMethod (ParametersCompiled parameters,
					ParametersBlock block, TypeSpec return_type, TypeSpec delegate_type,
					Location loc)
			: base (parameters, block, return_type, delegate_type, loc)
		{
		}

		#region Properties

		public override string ContainerType {
			get {
				return "lambda expression";
			}
		}

		#endregion

		protected override void CloneTo (CloneContext clonectx, Expression target)
		{
			// TODO: nothing ??
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			BlockContext bc = new BlockContext (ec.MemberContext, Block, ReturnType);
			Expression args = parameters.CreateExpressionTree (bc, loc);
			Expression expr = Block.CreateExpressionTree (ec);
			if (expr == null)
				return null;

			Arguments arguments = new Arguments (2);
			arguments.Add (new Argument (expr));
			arguments.Add (new Argument (args));
			return CreateExpressionFactoryCall (ec, "Lambda",
				new TypeArguments (new TypeExpression (type, loc)),
				arguments);
		}
	}

	//
	// This is a return statement that is prepended lambda expression bodies that happen
	// to be expressions.  Depending on the return type of the delegate this will behave
	// as either { expr (); return (); } or { return expr (); }
	//
	public class ContextualReturn : Return
	{
		ExpressionStatement statement;

		public ContextualReturn (Expression expr)
			: base (expr, expr.StartLocation)
		{
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return Expr.CreateExpressionTree (ec);
		}

		protected override void DoEmit (EmitContext ec)
		{
			if (statement != null) {
				statement.EmitStatement (ec);
				if (unwind_protect)
					ec.Emit (OpCodes.Leave, ec.CreateReturnLabel ());
				else {
					ec.Emit (OpCodes.Ret);
				}
				return;
			}

			base.DoEmit (ec);
		}

		protected override bool DoResolve (BlockContext ec)
		{
			//
			// When delegate returns void, only expression statements can be used
			//
			if (ec.ReturnType.Kind == MemberKind.Void) {
				Expr = Expr.Resolve (ec);
				if (Expr == null)
					return false;

				statement = Expr as ExpressionStatement;
				if (statement == null)
					Expr.Error_InvalidExpressionStatement (ec);

				return true;
			}

			return base.DoResolve (ec);
		}
	}
}
