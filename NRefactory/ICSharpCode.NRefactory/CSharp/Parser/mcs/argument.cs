//
// argument.cs: Argument expressions
//
// Author:
//   Miguel de Icaza (miguel@ximain.com)
//   Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
// Copyright 2003-2008 Novell, Inc.
//

using System;
using System.Collections.Generic;

#if STATIC
using IKVM.Reflection.Emit;
#else
using System.Reflection.Emit;
#endif

namespace Mono.CSharp
{
	//
	// Argument expression used for invocation
	//
	public class Argument
	{
		public enum AType : byte
		{
			None = 0,
			Ref = 1,			// ref modifier used
			Out = 2,			// out modifier used
			Default = 3,		// argument created from default parameter value
			DynamicTypeName = 4,	// System.Type argument for dynamic binding
			ExtensionType = 5,	// Instance expression inserted as the first argument
		}

		public readonly AType ArgType;
		public Expression Expr;

		public Argument (Expression expr, AType type)
		{
			this.Expr = expr;
			this.ArgType = type;
		}

		public Argument (Expression expr)
		{
			if (expr == null)
				throw new ArgumentNullException ();

			this.Expr = expr;
		}

		#region Properties

		public bool IsByRef {
			get { return ArgType == AType.Ref || ArgType == AType.Out; }
		}

		public bool IsDefaultArgument {
			get { return ArgType == AType.Default; }
		}

		public Parameter.Modifier Modifier {
			get {
				switch (ArgType) {
				case AType.Out:
					return Parameter.Modifier.OUT;

				case AType.Ref:
					return Parameter.Modifier.REF;

				default:
					return Parameter.Modifier.NONE;
				}
			}
		}

		public TypeSpec Type {
			get { return Expr.Type; }
		}

		#endregion

		public Argument Clone (Expression expr)
		{
			Argument a = (Argument) MemberwiseClone ();
			a.Expr = expr;
			return a;
		}

		public Argument Clone (CloneContext clonectx)
		{
			return Clone (Expr.Clone (clonectx));
		}

		public virtual Expression CreateExpressionTree (ResolveContext ec)
		{
			if (ArgType == AType.Default)
				ec.Report.Error (854, Expr.Location, "An expression tree cannot contain an invocation which uses optional parameter");

			return Expr.CreateExpressionTree (ec);
		}

		public string GetSignatureForError ()
		{
			if (Expr.eclass == ExprClass.MethodGroup)
				return Expr.ExprClassName;

			return TypeManager.CSharpName (Expr.Type);
		}

		public bool ResolveMethodGroup (ResolveContext ec)
		{
			SimpleName sn = Expr as SimpleName;
			if (sn != null)
				Expr = sn.GetMethodGroup ();

			// FIXME: csc doesn't report any error if you try to use `ref' or
			//        `out' in a delegate creation expression.
			Expr = Expr.Resolve (ec, ResolveFlags.VariableOrValue | ResolveFlags.MethodGroup);
			if (Expr == null)
				return false;

			return true;
		}

		public void Resolve (ResolveContext ec)
		{
			if (Expr == EmptyExpression.Null)
				return;

//			using (ec.With (ResolveContext.Options.DoFlowAnalysis, true)) {
				// Verify that the argument is readable
				if (ArgType != AType.Out)
					Expr = Expr.Resolve (ec);

				// Verify that the argument is writeable
				if (Expr != null && IsByRef)
					Expr = Expr.ResolveLValue (ec, EmptyExpression.OutAccess.Instance);

				if (Expr == null)
					Expr = EmptyExpression.Null;
//			}
		}

		public virtual void Emit (EmitContext ec)
		{
			if (!IsByRef) {
				Expr.Emit (ec);
				return;
			}

			AddressOp mode = AddressOp.Store;
			if (ArgType == AType.Ref)
				mode |= AddressOp.Load;

			IMemoryLocation ml = (IMemoryLocation) Expr;
			ml.AddressOf (ec, mode);
		}
	}

	public class NamedArgument : Argument
	{
		public readonly string Name;
		readonly Location loc;
		LocalTemporary variable;

		public NamedArgument (string name, Location loc, Expression expr)
			: this (name, loc, expr, AType.None)
		{
		}

		public NamedArgument (string name, Location loc, Expression expr, AType modifier)
			: base (expr, modifier)
		{
			this.Name = name;
			this.loc = loc;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			ec.Report.Error (853, loc, "An expression tree cannot contain named argument");
			return base.CreateExpressionTree (ec);
		}

		public override void Emit (EmitContext ec)
		{
			// TODO: Should guard against multiple emits
			base.Emit (ec);

			// Release temporary variable when used
			if (variable != null)
				variable.Release (ec);
		}

		public void EmitAssign (EmitContext ec)
		{
			var type = Expr.Type;
			if (IsByRef) {
				var ml = (IMemoryLocation) Expr;
				ml.AddressOf (ec, AddressOp.Load);
				type = ReferenceContainer.MakeType (type);
			} else {
				Expr.Emit (ec);
			}

			variable = new LocalTemporary (type);
			variable.Store (ec);

			Expr = variable;
		}

		public Location Location {
			get { return loc; }
		}
	}
	
	public class Arguments
	{
		sealed class ArgumentsOrdered : Arguments
		{
			List<NamedArgument> ordered;

			public ArgumentsOrdered (Arguments args)
				: base (args.Count)
			{
				AddRange (args);
				ordered = new List<NamedArgument> ();
			}

			public void AddOrdered (NamedArgument na)
			{
				ordered.Add (na);
			}

			public override Expression[] Emit (EmitContext ec, bool dup_args)
			{
				foreach (NamedArgument na in ordered)
					na.EmitAssign (ec);

				return base.Emit (ec, dup_args);
			}
		}

		// Try not to add any more instances to this class, it's allocated a lot
		List<Argument> args;

		public Arguments (int capacity)
		{
			args = new List<Argument> (capacity);
		}

		public void Add (Argument arg)
		{
			args.Add (arg);
		}

		public void AddRange (Arguments args)
		{
			this.args.AddRange (args.args);
		}

		public ArrayInitializer CreateDynamicBinderArguments (ResolveContext rc)
		{
			Location loc = Location.Null;
			var all = new ArrayInitializer (args.Count, loc);

			MemberAccess binder = DynamicExpressionStatement.GetBinderNamespace (loc);

			foreach (Argument a in args) {
				Arguments dargs = new Arguments (2);

				// CSharpArgumentInfoFlags.None = 0
				const string info_flags_enum = "CSharpArgumentInfoFlags";
				Expression info_flags = new IntLiteral (0, loc);

				if (a.Expr is Constant) {
					info_flags = new Binary (Binary.Operator.BitwiseOr, info_flags,
						new MemberAccess (new MemberAccess (binder, info_flags_enum, loc), "Constant", loc), loc);
				} else if (a.ArgType == Argument.AType.Ref) {
					info_flags = new Binary (Binary.Operator.BitwiseOr, info_flags,
						new MemberAccess (new MemberAccess (binder, info_flags_enum, loc), "IsRef", loc), loc);
					info_flags = new Binary (Binary.Operator.BitwiseOr, info_flags,
						new MemberAccess (new MemberAccess (binder, info_flags_enum, loc), "UseCompileTimeType", loc), loc);
				} else if (a.ArgType == Argument.AType.Out) {
					info_flags = new Binary (Binary.Operator.BitwiseOr, info_flags,
						new MemberAccess (new MemberAccess (binder, info_flags_enum, loc), "IsOut", loc), loc);
					info_flags = new Binary (Binary.Operator.BitwiseOr, info_flags,
						new MemberAccess (new MemberAccess (binder, info_flags_enum, loc), "UseCompileTimeType", loc), loc);
				} else if (a.ArgType == Argument.AType.DynamicTypeName) {
					info_flags = new Binary (Binary.Operator.BitwiseOr, info_flags,
						new MemberAccess (new MemberAccess (binder, info_flags_enum, loc), "IsStaticType", loc), loc);
				}

				var arg_type = a.Expr.Type;

				if (arg_type != InternalType.Dynamic && arg_type != InternalType.Null) {
					MethodGroupExpr mg = a.Expr as MethodGroupExpr;
					if (mg != null) {
						rc.Report.Error (1976, a.Expr.Location,
							"The method group `{0}' cannot be used as an argument of dynamic operation. Consider using parentheses to invoke the method",
							mg.Name);
					} else if (arg_type == InternalType.AnonymousMethod) {
						rc.Report.Error (1977, a.Expr.Location,
							"An anonymous method or lambda expression cannot be used as an argument of dynamic operation. Consider using a cast");
					} else if (arg_type == TypeManager.void_type || arg_type == InternalType.Arglist || arg_type.IsPointer) {
						rc.Report.Error (1978, a.Expr.Location,
							"An expression of type `{0}' cannot be used as an argument of dynamic operation",
							TypeManager.CSharpName (arg_type));
					}

					info_flags = new Binary (Binary.Operator.BitwiseOr, info_flags,
						new MemberAccess (new MemberAccess (binder, info_flags_enum, loc), "UseCompileTimeType", loc), loc);
				}

				string named_value;
				NamedArgument na = a as NamedArgument;
				if (na != null) {
					info_flags = new Binary (Binary.Operator.BitwiseOr, info_flags,
						new MemberAccess (new MemberAccess (binder, info_flags_enum, loc), "NamedArgument", loc), loc);

					named_value = na.Name;
				} else {
					named_value = null;
				}

				dargs.Add (new Argument (info_flags));
				dargs.Add (new Argument (new StringLiteral (named_value, loc)));
				all.Add (new Invocation (new MemberAccess (new MemberAccess (binder, "CSharpArgumentInfo", loc), "Create", loc), dargs));
			}

			return all;
		}

		public static Arguments CreateForExpressionTree (ResolveContext ec, Arguments args, params Expression[] e)
		{
			Arguments all = new Arguments ((args == null ? 0 : args.Count) + e.Length);
			for (int i = 0; i < e.Length; ++i) {
				if (e [i] != null)
					all.Add (new Argument (e[i]));
			}

			if (args != null) {
				foreach (Argument a in args.args) {
					Expression tree_arg = a.CreateExpressionTree (ec);
					if (tree_arg != null)
						all.Add (new Argument (tree_arg));
				}
			}

			return all;
		}

		public void CheckArrayAsAttribute (CompilerContext ctx)
		{
			foreach (Argument arg in args) {
				// Type is undefined (was error 246)
				if (arg.Type == null)
					continue;

				if (arg.Type.IsArray)
					ctx.Report.Warning (3016, 1, arg.Expr.Location, "Arrays as attribute arguments are not CLS-compliant");
			}
		}

		public Arguments Clone (CloneContext ctx)
		{
			Arguments cloned = new Arguments (args.Count);
			foreach (Argument a in args)
				cloned.Add (a.Clone (ctx));

			return cloned;
		}

		public int Count {
			get { return args.Count; }
		}

		//
		// Emits a list of resolved Arguments
		// 
		public void Emit (EmitContext ec)
		{
			Emit (ec, false);
		}

		//
		// if `dup_args' is true, a copy of the arguments will be left
		// on the stack and return value will contain an array of access
		// expressions
		// NOTE: It's caller responsibility is to release temporary variables
		//
		public virtual Expression[] Emit (EmitContext ec, bool dup_args)
		{
			Expression[] temps;

			if (dup_args && Count != 0)
				temps = new Expression [Count];
			else
				temps = null;

			int i = 0;
			LocalTemporary lt;
			foreach (Argument a in args) {
				a.Emit (ec);
				if (!dup_args)
					continue;

				if (a.Expr is Constant || a.Expr is This) {
					//
					// No need to create a temporary variable for constants
					//
					temps[i] = a.Expr;
				} else {
					ec.Emit (OpCodes.Dup);
					temps[i] = lt = new LocalTemporary (a.Type);
					lt.Store (ec);
				}

				++i;
			}

			return temps;
		}

		public List<Argument>.Enumerator GetEnumerator ()
		{
			return args.GetEnumerator ();
		}

		//
		// At least one argument is of dynamic type
		//
		public bool HasDynamic {
			get {
				foreach (Argument a in args) {
					if (a.Type == InternalType.Dynamic && !a.IsByRef)
						return true;
				}
				
				return false;
			}
		}

		//
		// At least one argument is named argument
		//
		public bool HasNamed {
			get {
				foreach (Argument a in args) {
					if (a is NamedArgument)
						return true;
				}
				
				return false;
			}
		}


		public void Insert (int index, Argument arg)
		{
			args.Insert (index, arg);
		}

		public static System.Linq.Expressions.Expression[] MakeExpression (Arguments args, BuilderContext ctx)
		{
			if (args == null || args.Count == 0)
				return null;

			var exprs = new System.Linq.Expressions.Expression [args.Count];
			for (int i = 0; i < exprs.Length; ++i) {
				Argument a = args.args [i];
				exprs[i] = a.Expr.MakeExpression (ctx);
			}

			return exprs;
		}

		//
		// For named arguments when the order of execution is different
		// to order of invocation
		//
		public Arguments MarkOrderedArgument (NamedArgument a)
		{
			//
			// Constant expression have no effect on left-to-right execution
			//
			if (a.Expr is Constant)
				return this;

			ArgumentsOrdered ra = this as ArgumentsOrdered;
			if (ra == null)
				ra = new ArgumentsOrdered (this);

			ra.AddOrdered (a);
			return ra;
		}

		//
		// Returns dynamic when at least one argument is of dynamic type
		//
		public void Resolve (ResolveContext ec, out bool dynamic)
		{
			dynamic = false;
			foreach (Argument a in args) {
				a.Resolve (ec);
				if (a.Type == InternalType.Dynamic && !a.IsByRef)
					dynamic = true;
			}
		}

		public void RemoveAt (int index)
		{
			args.RemoveAt (index);
		}

		public Argument this [int index] {
			get { return args [index]; }
			set { args [index] = value; }
		}
	}
}
