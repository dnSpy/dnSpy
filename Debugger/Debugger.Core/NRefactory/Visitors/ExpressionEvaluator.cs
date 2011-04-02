// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Debugger;
using Debugger.MetaData;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.Visitors
{
	public class EvaluateException: GetValueException
	{
		public EvaluateException(AstNode code, string msg):base(code, msg) {}
		public EvaluateException(AstNode code, string msgFmt, params string[] msgArgs):base(code, string.Format(msgFmt, msgArgs)) {}
	}
	
	class TypedValue
	{
		Value value;
		DebugType type;
		
		public Value Value {
			get { return value; }
		}
		
		public DebugType Type {
			get { return type; }
		}
		
		public object PrimitiveValue {
			get { return value.PrimitiveValue; }
		}
		
		public TypedValue(Value value, DebugType type)
		{
			this.value = value;
			this.type = type;
		}
	}
	
	public class ExpressionEvaluator : NotImplementedAstVisitor<object, object>
	{
		StackFrame context;
		
		public StackFrame Context {
			get { return context; }
		}
		
		ExpressionEvaluator(StackFrame context)
		{
			this.context = context;
		}
		
		public static AstNode Parse(string code, SupportedLanguage language)
		{
			switch (language) {
				case SupportedLanguage.CSharp:
					var parser = new CSharpParser();
					using (var textReader = new StringReader(code)) {
						AstNode astRoot = parser.ParseExpression(textReader);
						if (parser.HasErrors) {
							throw new GetValueException("Parser errors");
						}

						return astRoot;
					}
					
				default:
					throw new ArgumentException("Unsuported language");
			}
		}
		
		/// <summary> Evaluate given expression.  If you have expression tree already, use overloads of this method.</summary>
		/// <returns> Returned value or null for statements </returns>
		public static Value Evaluate(string code, SupportedLanguage language, StackFrame context)
		{
			return Evaluate(Parse(code, language), context);
		}
		
		public static Value Evaluate(AstNode code, Process context)
		{
			if (context.SelectedStackFrame != null) {
				return Evaluate(code, context.SelectedStackFrame);
			} else if (context.SelectedThread.MostRecentStackFrame != null ) {
				return Evaluate(code, context.SelectedThread.MostRecentStackFrame);
			} else {
				// This can happen when needed 'dll' is missing.  This causes an exception dialog to be shown even before the applicaiton starts
				throw new GetValueException("Can not evaluate because the process has no managed stack frames");
			}
		}
		
		public static Value Evaluate(AstNode code, StackFrame context)
		{
			if (context == null) throw new ArgumentNullException("context");
			if (context.IsInvalid) throw new DebuggerException("The context is no longer valid");
			
			TypedValue val = new ExpressionEvaluator(context).Evaluate(code, false);
			if (val == null)
				return null;
			return val.Value;
		}
		
		/// <summary>
		/// Parses string representation of an expression (eg. "a.b[10] + 2") into NRefactory Expression tree.
		/// </summary>
		public static Expression ParseExpression(string code, SupportedLanguage language)
		{
			switch (language) {
				case SupportedLanguage.CSharp:
					var parser = new CSharpParser();
					using (var textReader = new StringReader(code)) {
						AstNode astRoot = parser.ParseExpression(textReader);
						if (parser.HasErrors) {
							throw new GetValueException("Parser errors");
						}

						return astRoot as Expression;
					}
				default:
					throw new ArgumentException("Unsuported language");
			}
		}
		
		public static string FormatValue(Value val)
		{
			if (val == null) {
				return null;
			} if (val.IsNull) {
				return "null";
			} else if (val.Type.IsArray) {
				StringBuilder sb = new StringBuilder();
				sb.Append(val.Type.Name);
				sb.Append(" {");
				bool first = true;
				foreach(Value item in val.GetArrayElements()) {
					if (!first) sb.Append(", ");
					first = false;
					sb.Append(FormatValue(item));
				}
				sb.Append("}");
				return sb.ToString();
			} else if (val.Type.GetInterface(typeof(ICollection).FullName) != null) {
				StringBuilder sb = new StringBuilder();
				sb.Append(val.Type.Name);
				sb.Append(" {");
				val = val.GetPermanentReference();
				int count = (int)val.GetMemberValue("Count").PrimitiveValue;
				for(int i = 0; i < count; i++) {
					if (i > 0) sb.Append(", ");
					DebugPropertyInfo itemProperty = (DebugPropertyInfo)val.Type.GetProperty("Item");
					Value item = val.GetPropertyValue(itemProperty, Eval.CreateValue(val.AppDomain, i));
					sb.Append(FormatValue(item));
				}
				sb.Append("}");
				return sb.ToString();
			} else if (val.Type.FullName == typeof(char).FullName) {
				return "'" + val.PrimitiveValue.ToString() + "'";
			} else if (val.Type.FullName == typeof(string).FullName) {
				return "\"" + val.PrimitiveValue.ToString() + "\"";
			} else if (val.Type.IsPrimitive) {
				return val.PrimitiveValue.ToString();
			} else {
				return val.InvokeToString();
			}
		}
		
		TypedValue Evaluate(AstNode expression)
		{
			return Evaluate(expression, true);
		}
		
		TypedValue Evaluate(AstNode expression, bool permRef)
		{
			// Try to get the value from cache
			// (the cache is cleared when the process is resumed)
			TypedValue val;
			if (context.Process.ExpressionsCache.TryGetValue(expression, out val)) {
				if (val == null || !val.Value.IsInvalid)
					return val;
			}
			
			System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
			watch.Start();
			try {
				val = (TypedValue)expression.AcceptVisitor(this, null);
				if (val != null && permRef)
					val = new TypedValue(val.Value.GetPermanentReference(), val.Type);
			} catch (GetValueException e) {
				e.Expression = expression;
				throw;
			} catch (NotImplementedException e) {
				throw new GetValueException(expression, "Language feature not implemented: " + e.Message);
			} finally {
				watch.Stop();
				context.Process.TraceMessage("Evaluated: {0} in {1} ms total", expression.PrettyPrint(), watch.ElapsedMilliseconds);
			}
			
			if (val != null && val.Value.IsInvalid)
				throw new DebuggerException("Expression \"" + expression.PrettyPrint() + "\" is invalid right after evaluation");
			
			// Add the result to cache
			context.Process.ExpressionsCache[expression] = val;
			
			return val;
		}
		
		IList<TypedValue> EvaluateAll(IEnumerable<Expression> exprs)
		{
			var vals = new List<TypedValue>(exprs.Count());
			foreach(Expression expr in exprs) {
				vals.Add(Evaluate(expr));
			}
			return vals;
		}
		
		int EvaluateAsInt(AstNode expression)
		{
			if (expression is PrimitiveExpression) {
				int? i = ((PrimitiveExpression)expression).Value as int?;
				if (i == null)
					throw new EvaluateException(expression, "Integer expected");
				return i.Value;
			} else {
				TypedValue typedVal = Evaluate(expression);
				if (typedVal.Type.CanImplicitelyConvertTo(typeof(int))) {
					int i = (int)Convert.ChangeType(typedVal.PrimitiveValue, typeof(int));
					return i;
				} else {
					throw new EvaluateException(expression, "Integer expected");
				}
			}
		}
		
		TypedValue EvaluateAs(AstNode expression, DebugType type)
		{
			TypedValue val = Evaluate(expression);
			if (val.Type == type)
				return val;
			if (!val.Type.CanImplicitelyConvertTo(type))
				throw new EvaluateException(expression, "Can not implicitely cast {0} to {1}", val.Type.FullName, type.FullName);
			if (type.IsPrimitive) {
				object oldVal = val.PrimitiveValue;
				object newVal;
				try {
					newVal = Convert.ChangeType(oldVal, type.PrimitiveType);
				} catch (InvalidCastException) {
					throw new EvaluateException(expression, "Can not cast {0} to {1}", val.GetType().FullName, type.FullName);
				} catch (OverflowException) {
					throw new EvaluateException(expression, "Overflow");
				}
				return CreateValue(newVal);
			} else {
				return new TypedValue(val.Value, type);
			}
		}
		
		Value[] GetValues(IList<TypedValue> typedVals)
		{
			List<Value> vals = new List<Value>(typedVals.Count);
			foreach(TypedValue typedVal in typedVals) {
				vals.Add(typedVal.Value);
			}
			return vals.ToArray();
		}
		
		DebugType[] GetTypes(IList<TypedValue> typedVals)
		{
			List<DebugType> types = new List<DebugType>(typedVals.Count);
			foreach(TypedValue typedVal in typedVals) {
				types.Add(typedVal.Type);
			}
			return types.ToArray();
		}
		
		TypedValue CreateValue(object primitiveValue)
		{
			Value val = Eval.CreateValue(context.AppDomain, primitiveValue);
			return new TypedValue(val, val.Type);
		}
		
		public override object VisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
		{
			BinaryOperatorType op;
			switch (assignmentExpression.Operator) {
					case AssignmentOperatorType.Add:           op = BinaryOperatorType.Add; break;
					case AssignmentOperatorType.Subtract:      op = BinaryOperatorType.Subtract; break;
					case AssignmentOperatorType.Multiply:      op = BinaryOperatorType.Multiply; break;
					case AssignmentOperatorType.Divide:        op = BinaryOperatorType.Divide; break;
					case AssignmentOperatorType.ShiftLeft:     op = BinaryOperatorType.ShiftLeft; break;
					case AssignmentOperatorType.ShiftRight:    op = BinaryOperatorType.ShiftRight; break;
					case AssignmentOperatorType.ExclusiveOr:   op = BinaryOperatorType.ExclusiveOr; break;
					case AssignmentOperatorType.Modulus:       op = BinaryOperatorType.Modulus; break;
					case AssignmentOperatorType.BitwiseAnd:    op = BinaryOperatorType.BitwiseAnd; break;
					case AssignmentOperatorType.BitwiseOr:     op = BinaryOperatorType.BitwiseOr; break;
					default: throw new GetValueException("Unknown operator " + assignmentExpression.Operator);
			}
			
			TypedValue right;
			if (op == BinaryOperatorType.Any) {
				right = Evaluate(assignmentExpression.Right);
			} else {
				BinaryOperatorExpression binOpExpr = new BinaryOperatorExpression();
				binOpExpr.Left  = assignmentExpression.Left;
				binOpExpr.Operator    = op;
				binOpExpr.Right = assignmentExpression.Right;
				right = Evaluate(binOpExpr);
			}
			
			// We can not have perfRef because we need to be able to set the value
			TypedValue left = (TypedValue)assignmentExpression.Left.AcceptVisitor(this, null);
			
			if (left == null) {
				// Can this happen?
				throw new GetValueException(string.Format("\"{0}\" can not be set", assignmentExpression.Left.PrettyPrint()));
			}
			if (!left.Value.IsReference && left.Type.FullName != right.Type.FullName) {
				throw new GetValueException(string.Format("Type {0} expected, {1} seen", left.Type.FullName, right.Type.FullName));
			}
			left.Value.SetValue(right.Value);
			return right;
		}
		
		public override object VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			foreach(var statement in blockStatement.Children) {
				Evaluate(statement);
			}
			return null;
		}
		
		public override object VisitEmptyStatement(EmptyStatement emptyStatement, object data)
		{
			return null;
		}
		
		public override object VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			Evaluate(expressionStatement.Expression);
			return null;
		}
		
		public override object VisitCastExpression(CastExpression castExpression, object data)
		{
			TypedValue val = Evaluate(castExpression.Expression);
			DebugType castTo = castExpression.Type.ResolveType(context.AppDomain);
			
			if (castTo.IsPrimitive && val.Type.IsPrimitive && castTo != val.Type) {
				object oldVal = val.PrimitiveValue;
				object newVal;
				try {
					newVal = Convert.ChangeType(oldVal, castTo.PrimitiveType);
				} catch (InvalidCastException) {
					throw new EvaluateException(castExpression, "Can not cast {0} to {1}", val.Type.FullName, castTo.FullName);
				} catch (OverflowException) {
					throw new EvaluateException(castExpression, "Overflow");
				}
				val = CreateValue(newVal);
			}
			if (!castTo.IsAssignableFrom(val.Value.Type) && !val.Value.IsNull)
				throw new GetValueException("Can not cast {0} to {1}", val.Value.Type.FullName, castTo.FullName);
			return new TypedValue(val.Value, castTo);
		}

		public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			string identifier = identifierExpression.Identifier;
			
			if (identifier == "__exception") {
				if (context.Thread.CurrentException != null) {
					return new TypedValue(
						context.Thread.CurrentException.Value,
						DebugType.CreateFromType(context.AppDomain.Mscorlib, typeof(System.Exception))
					);
				} else {
					throw new GetValueException("No current exception");
				}
			}
			
			// get parameter
			DebugParameterInfo par = context.MethodInfo.GetParameter(identifier);
			if (par != null)
				return new TypedValue(par.GetValue(context), (DebugType)par.ParameterType);
			
			//get local variables
			
//			DebugLocalVariableInfo loc = context.MethodInfo.GetLocalVariable(context.IP, identifier);
//			if (loc != null)
//				return new TypedValue(loc.GetValue(context), (DebugType)loc.LocalType);
			
			object localIndex = identifierExpression.Annotation(typeof(int[]));
			
			if (localIndex != null) {
				Value localValue = DebugMethodInfo.GetLocalVariableValue(context, ((int[])localIndex)[0]);
				return new TypedValue(localValue, localValue.Type);
			}
			
			// Instance class members
			// Note that the method might be generated instance method that represents anonymous method
			TypedValue thisValue = this.GetThisValue();
			if (thisValue != null) {
				IDebugMemberInfo instMember = (IDebugMemberInfo)thisValue.Type.GetMember<MemberInfo>(identifier, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, DebugType.IsFieldOrNonIndexedProperty);
				if (instMember != null)
					return new TypedValue(Value.GetMemberValue(thisValue.Value, (MemberInfo)instMember), instMember.MemberType);
			}
			
			// Static class members
			foreach(DebugType declaringType in ((DebugType)context.MethodInfo.DeclaringType).GetSelfAndDeclaringTypes()) {
				IDebugMemberInfo statMember = (IDebugMemberInfo)declaringType.GetMember<MemberInfo>(identifier, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, DebugType.IsFieldOrNonIndexedProperty);
				if (statMember != null)
					return new TypedValue(Value.GetMemberValue(null, (MemberInfo)statMember), statMember.MemberType);
			}
			
			throw new GetValueException("Identifier \"" + identifier + "\" not found in this context");
		}

		public override object VisitIndexerExpression(IndexerExpression indexerExpression, object data)
		{
			TypedValue target = Evaluate(indexerExpression.Target);
			
			if (target.Type.IsArray) {
				List<int> intIndexes = new List<int>();
				foreach(Expression indexExpr in indexerExpression.Arguments) {
					intIndexes.Add(EvaluateAsInt(indexExpr));
				}
				return new TypedValue(
					target.Value.GetArrayElement(intIndexes.ToArray()),
					(DebugType)target.Type.GetElementType()
				);
			} else if (target.Type.FullName == typeof(string).FullName) {
				if (indexerExpression.Arguments.Count() != 1)
					throw new GetValueException("Single index expected");
				
				int index = EvaluateAsInt(indexerExpression.Arguments.First());
				string str = (string)target.PrimitiveValue;
				if (index < 0 || index >= str.Length)
					throw new GetValueException("Index was outside the bounds of the array.");
				return CreateValue(str[index]);
			} else {
				var indexes = EvaluateAll(indexerExpression.Arguments);
				DebugPropertyInfo pi = (DebugPropertyInfo)target.Type.GetProperty("Item", GetTypes(indexes));
				if (pi == null)
					throw new GetValueException("The object does not have an indexer property");
				return new TypedValue(
					target.Value.GetPropertyValue(pi, GetValues(indexes)),
					(DebugType)pi.PropertyType
				);
			}
		}

		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			TypedValue target;
			DebugType targetType;
			string methodName;
			MemberReferenceExpression memberRef = invocationExpression.Target as MemberReferenceExpression;
			if (memberRef != null) {
				// TODO: Optimize
				try {
					// Instance
					target = Evaluate(memberRef.Target);
					targetType = target.Type;
				} catch (GetValueException) {
					// Static
					target = null;
					targetType = memberRef.Target.ResolveType(context.AppDomain);
				}
				methodName = memberRef.MemberName;
			} else {
				IdentifierExpression ident = invocationExpression.Target as IdentifierExpression;
				if (ident != null) {
					target = Evaluate(new ThisReferenceExpression());
					targetType = target.Type;
					methodName = ident.Identifier;
				} else {
					throw new GetValueException("Member reference expected for method invocation");
				}
			}
			var args = EvaluateAll(invocationExpression.Arguments);
			MethodInfo method = targetType.GetMethod(methodName, DebugType.BindingFlagsAllInScope, null, GetTypes(args), null);
			if (method == null)
				throw new GetValueException("Method " + methodName + " not found");
			Value retVal = Value.InvokeMethod(target != null ? target.Value : null, method, GetValues(args));
			if (retVal == null)
				return null;
			return new TypedValue(retVal, (DebugType)method.ReturnType);
		}

		public override object VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			if (!objectCreateExpression.Initializer.IsNull)
				throw new EvaluateException(objectCreateExpression.Initializer, "Object initializers not supported");
			
			DebugType type = objectCreateExpression.Type.ResolveType(context.AppDomain);
			var ctorArgs = EvaluateAll(objectCreateExpression.Arguments);
			ConstructorInfo ctor = type.GetConstructor(BindingFlags.Default, null, CallingConventions.Any, GetTypes(ctorArgs), null);
			if (ctor == null)
				throw new EvaluateException(objectCreateExpression, "Constructor not found");
			Value val = (Value)ctor.Invoke(GetValues(ctorArgs));
			return new TypedValue(val, type);
		}

		public override object VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
			if (arrayCreateExpression.AdditionalArraySpecifiers.Count() != 0)
				throw new EvaluateException(arrayCreateExpression, "Multi-dimensional arrays are not suppored");
			
			DebugType type = arrayCreateExpression.Type.ResolveType(context.AppDomain);
			int length = 0;
			if (arrayCreateExpression.Arguments.Count() == 1) {
				length = EvaluateAsInt(arrayCreateExpression.Arguments.First());
			} else if (!arrayCreateExpression.Initializer.IsNull) {
				length = arrayCreateExpression.Initializer.Elements.Count();
			}
			Value array = Eval.NewArray((DebugType)type.GetElementType(), (uint)length, null);
			if (!arrayCreateExpression.Initializer.IsNull) {
				var inits = arrayCreateExpression.Initializer.Elements;
				if (inits.Count() != length)
					throw new EvaluateException(arrayCreateExpression, "Incorrect initializer length");
				int i = 0;
				var enumerator = inits.GetEnumerator();
				while (enumerator.MoveNext()) {
					TypedValue init = EvaluateAs(enumerator.Current, (DebugType)type.GetElementType());
					array.SetArrayElement(new int[] { i++ }, init.Value);
				}
			}
			return new TypedValue(array, type);
		}

		public override object VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, object data)
		{
			TypedValue target;
			DebugType targetType;
			try {
				// Instance
				target = Evaluate(memberReferenceExpression.Target);
				targetType = target.Type;
			} catch (GetValueException e) {
				// Static
				target = null;
				try {
					targetType = memberReferenceExpression.Target.ResolveType(context.AppDomain);
				} catch (GetValueException) {
					throw e;  // Use the other, nicer message
				}
			}
			MemberInfo[] memberInfos = targetType.GetMember(memberReferenceExpression.MemberName, DebugType.BindingFlagsAllInScope);
			if (memberInfos.Length == 0)
				throw new GetValueException("Member \"" + memberReferenceExpression.MemberName + "\" not found");
			return new TypedValue(
				Value.GetMemberValue(target != null ? target.Value : null, memberInfos[0]),
				((IDebugMemberInfo)memberInfos[0]).MemberType
			);
		}

		public override object VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			return Evaluate(parenthesizedExpression.Expression);
		}

		public override object VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			return CreateValue(primitiveExpression.Value);
		}

		TypedValue GetThisValue()
		{
			try {
				// This is needed so that captured 'this' is supported
				return new TypedValue(context.GetThisValue(), (DebugType)context.MethodInfo.DeclaringType);
			} catch (GetValueException) {
				// static method
				return null;
			}
		}

		public override object VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, object data)
		{
			TypedValue thisValue = GetThisValue();
			if (thisValue == null)
				throw new GetValueException(context.MethodInfo.FullName + " is static method and does not have \"this\"");
			return thisValue;
		}

		#region Binary and unary expressions

		public override object VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			TypedValue value = Evaluate(unaryOperatorExpression.Expression);
			UnaryOperatorType op = unaryOperatorExpression.Operator;
			
			if (op == UnaryOperatorType.Dereference) {
				if (!value.Type.IsPointer)
					throw new GetValueException("Target object is not a pointer");
				return new TypedValue(value.Value.Dereference(), (DebugType)value.Type.GetElementType());
			}
			
			if (!value.Type.IsPrimitive)
				throw new GetValueException("Primitive value expected");
			
			if (op == UnaryOperatorType.Decrement || op == UnaryOperatorType.PostDecrement ||
			    op == UnaryOperatorType.Increment || op == UnaryOperatorType.PostIncrement)
			{
				TypedValue oldValue = value;
				TypedValue newValue = null;
				try {
					if (op == UnaryOperatorType.Decrement || op == UnaryOperatorType.PostDecrement)
						newValue = (TypedValue)VisitAssignmentExpression(new AssignmentExpression() { Right = unaryOperatorExpression.Expression, Operator = AssignmentOperatorType.Subtract }, null);
					if (op == UnaryOperatorType.Increment || op == UnaryOperatorType.PostIncrement)
						newValue = (TypedValue)VisitAssignmentExpression(new AssignmentExpression(){ Right = unaryOperatorExpression.Expression, Operator = AssignmentOperatorType.Add }, null);
				} catch (EvaluateException e) {
					throw new EvaluateException(unaryOperatorExpression, e.Message);
				}
				if (op == UnaryOperatorType.PostDecrement || op == UnaryOperatorType.PostIncrement) {
					return oldValue;
				} else {
					// Note: the old unaryOparatorExpression is still cached and still has the old value
					return newValue;
				}
			}
			
			if (op == UnaryOperatorType.Minus) {
				object val = value.PrimitiveValue;
				// Special case - it would promote the value to long otherwise
				if (val is uint && (uint)val == (uint)1 << 31)
					return CreateValue(int.MinValue);
				
				// Special case - it would overflow otherwise
				if (val is ulong && (ulong)val == (ulong)1 << 63)
					return CreateValue(long.MinValue);
			}
			
			if (op == UnaryOperatorType.Plus || op == UnaryOperatorType.Minus ||
			    op == UnaryOperatorType.BitNot || op == UnaryOperatorType.Not)
			{
				Type[] overloads;
				if (op == UnaryOperatorType.Not) {
					overloads = new Type[] { typeof(bool) };
				} else if (op == UnaryOperatorType.Minus) {
					overloads = new Type[] { typeof(int), typeof(long), typeof(ulong), typeof(float), typeof(double) };
				} else {
					overloads = new Type[] { typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double) };
				}
				foreach(Type argType in overloads) {
					if (value.Type.CanPromoteTo(argType)) {
						object a = Convert.ChangeType(value.PrimitiveValue, argType);
						object res;
						try {
							res = PerformUnaryOperation(a, op, argType);
						} catch (ArithmeticException e) {
							// Can happen for smaller int or long
							throw new EvaluateException(unaryOperatorExpression, e.Message);
						}
						if (res != null)
							return CreateValue(res);
						break; // Match only one overload
					}
				}
			}
			
			throw new EvaluateException(unaryOperatorExpression, "Can not use the unary operator {0} on type {1}", op.ToString(), value.Type.FullName);
		}

		/// <summary>
		/// Perform given arithmetic operation.
		/// The arguments must be already converted to the correct types.
		/// </summary>
		object PerformUnaryOperation(object val, UnaryOperatorType op, Type argType)
		{
			checked {
				if (argType == typeof(bool)) {
					bool a = (bool)val;
					switch (op) {
							case UnaryOperatorType.Not:    return !a;
					}
				}
				
				if (argType == typeof(float)) {
					float a = (float)val;
					switch (op) {
							case UnaryOperatorType.Minus:  return -a;
							case UnaryOperatorType.Plus:   return +a;
					}
				}
				
				if (argType == typeof(double)) {
					double a = (double)val;
					switch (op) {
							case UnaryOperatorType.Minus:  return -a;
							case UnaryOperatorType.Plus:   return +a;
					}
				}
				
				if (argType == typeof(int)) {
					int a = (int)val;
					switch (op) {
							case UnaryOperatorType.Minus:  return -a;
							case UnaryOperatorType.Plus:   return +a;
							case UnaryOperatorType.BitNot: return ~a;
					}
				}
				
				if (argType == typeof(uint)) {
					uint a = (uint)val;
					switch (op) {
							case UnaryOperatorType.Plus:   return +a;
							case UnaryOperatorType.BitNot: return ~a;
					}
				}
				
				if (argType == typeof(long)) {
					long a = (long)val;
					switch (op) {
							case UnaryOperatorType.Minus:  return -a;
							case UnaryOperatorType.Plus:   return +a;
							case UnaryOperatorType.BitNot: return ~a;
					}
				}
				
				if (argType == typeof(ulong)) {
					ulong a = (ulong)val;
					switch (op) {
							case UnaryOperatorType.Plus:   return +a;
							case UnaryOperatorType.BitNot: return ~a;
					}
				}
			}
			
			return null;
		}

		public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			BinaryOperatorType op = binaryOperatorExpression.Operator;
			
			TypedValue left = Evaluate(binaryOperatorExpression.Left);
			TypedValue right = Evaluate(binaryOperatorExpression.Right);
			
			// Try to apply any implicit binary operation
			// Be careful to do these in correct order
			
			//	==, !=
			//	bool operator ==(string x, string y);
			// 
			//	==, !=   C is not Value type; other rules apply  (must be after string)
			//	bool operator ==(C x, C y);
			
			if (op == BinaryOperatorType.Equality || op == BinaryOperatorType.InEquality) {
				if (left.Type.Is<string>() && right.Type.Is<string>()) {
					if (left.Value.IsNull || right.Value.IsNull) {
						return CreateValue(left.Value.IsNull && right.Value.IsNull);
					} else {
						return CreateValue((string)left.PrimitiveValue == (string)right.PrimitiveValue);
					}
				}
				if (!left.Type.IsValueType && !right.Type.IsValueType) {
					// Reference comparison
					if (left.Value.IsNull || right.Value.IsNull) {
						return CreateValue(left.Value.IsNull && right.Value.IsNull);
					} else {
						return CreateValue(left.Value.Address == right.Value.Address);
					}
				}
			}
			
			//	+
			//	string operator +(string x, string y);
			//	string operator +(string x, object y);
			//	string operator +(object x, string y);
			
			if (op == BinaryOperatorType.Add) {
				if (left.Type.Is<string>() || right.Type.Is<string>()) {
					string a = left.Value.IsNull ? string.Empty : left.Value.InvokeToString();
					string b = right.Value.IsNull ? string.Empty : right.Value.InvokeToString();
					return CreateValue(a + b);
				}
			}
			
			//	<<, >>
			//	int operator <<(int x, int count);
			//	uint operator <<(uint x, int count);
			//	long operator <<(long x, int count);
			//	ulong operator <<(ulong x, int count);
			
			if (op == BinaryOperatorType.ShiftLeft || op == BinaryOperatorType.ShiftRight) {
				Type[] overloads = { typeof(int), typeof(uint), typeof(long), typeof(ulong)};
				foreach(Type argType in overloads) {
					if (left.Type.CanPromoteTo(argType) && right.Type.CanPromoteTo(typeof(int))) {
						object a = Convert.ChangeType(left.PrimitiveValue, argType);
						object b = Convert.ChangeType(right.PrimitiveValue, typeof(int));
						// Shift operations never cause overflows
						object res = PerformBinaryOperation(a, b, op, argType);
						return CreateValue(res);
					}
				}
			}
			
			//	*, /, %, +, and –
			//	==, !=, <, >, <=, >=,
			//	&, ^, and |  (except float and double)
			//	int operator +(int x, int y);
			//	uint operator +(uint x, uint y);
			//	long operator +(long x, long y);
			//	ulong operator +(ulong x, ulong y);
			//	void operator +(long x, ulong y);
			//	void operator +(ulong x, long y);
			//	float operator +(float x, float y);
			//	double operator +(double x, double y);
			//
			//	&, |, ^, &&, ||
			//	==, !=
			//	bool operator &(bool x, bool y);
			
			if (op != BinaryOperatorType.ShiftLeft && op != BinaryOperatorType.ShiftRight) {
				Type[] overloads = { typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(bool) };
				foreach(Type argType in overloads) {
					if (left.Type.CanPromoteTo(argType) && right.Type.CanPromoteTo(argType)) {
						if (argType == typeof(float) || argType == typeof(double)) {
							// Invalid overloads
							if (left.Type.CanPromoteTo(typeof(long)) && right.Type.CanPromoteTo(typeof(ulong)))
								break;
							if (left.Type.CanPromoteTo(typeof(ulong)) && right.Type.CanPromoteTo(typeof(long)))
								break;
						}
						object a = Convert.ChangeType(left.PrimitiveValue, argType);
						object b = Convert.ChangeType(right.PrimitiveValue, argType);
						object res;
						try {
							res = PerformBinaryOperation(a, b, op, argType);
						} catch (ArithmeticException e) {
							throw new EvaluateException(binaryOperatorExpression, e.Message);
						}
						if (res != null)
							return CreateValue(res);
						break; // Match only one overload
					}
				}
			}
			
			throw new EvaluateException(binaryOperatorExpression, "Can not use the binary operator {0} on types {1} and {2}", op.ToString(), left.Type.FullName, right.Type.FullName);
		}

		/// <summary>
		/// Perform given arithmetic operation.
		/// The arguments must be already converted to the correct types.
		/// </summary>
		object PerformBinaryOperation(object left, object right, BinaryOperatorType op, Type argTypes)
		{
			checked {
				if (argTypes == typeof(string)) {
					string a = (string)left;
					string b = (string)right;
					switch (op) {
							case BinaryOperatorType.Equality:   return a == b;
							case BinaryOperatorType.InEquality: return a != b;
							case BinaryOperatorType.Add:        return a + b;
					}
				}
				
				if (argTypes == typeof(bool)) {
					bool a = (bool)left;
					bool b = (bool)right;
					switch (op) {
							case BinaryOperatorType.Equality:    return a == b;
							case BinaryOperatorType.InEquality:  return a != b;
							case BinaryOperatorType.ExclusiveOr: return a ^ b;
							case BinaryOperatorType.BitwiseAnd:  return a & b;
							case BinaryOperatorType.BitwiseOr:   return a | b;
							case BinaryOperatorType.ConditionalAnd:  return a && b;
							case BinaryOperatorType.ConditionalOr:   return a || b;
					}
				}
				
				if (argTypes == typeof(float)) {
					float a = (float)left;
					float b = (float)right;
					switch (op) {
							case BinaryOperatorType.GreaterThan:        return a > b;
							case BinaryOperatorType.GreaterThanOrEqual: return a >= b;
							case BinaryOperatorType.Equality:           return a == b;
							case BinaryOperatorType.InEquality:         return a != b;
							case BinaryOperatorType.LessThan:           return a < b;
							case BinaryOperatorType.LessThanOrEqual:    return a <= b;
							
							case BinaryOperatorType.Add:           return a + b;
							case BinaryOperatorType.Subtract:      return a - b;
							case BinaryOperatorType.Multiply:      return a * b;
							case BinaryOperatorType.Divide:        return a / b;
							case BinaryOperatorType.Modulus:       return a % b;
					}
				}
				
				if (argTypes == typeof(double)) {
					double a = (double)left;
					double b = (double)right;
					switch (op) {
							case BinaryOperatorType.GreaterThan:        return a > b;
							case BinaryOperatorType.GreaterThanOrEqual: return a >= b;
							case BinaryOperatorType.Equality:           return a == b;
							case BinaryOperatorType.InEquality:         return a != b;
							case BinaryOperatorType.LessThan:           return a < b;
							case BinaryOperatorType.LessThanOrEqual:    return a <= b;
							
							case BinaryOperatorType.Add:           return a + b;
							case BinaryOperatorType.Subtract:      return a - b;
							case BinaryOperatorType.Multiply:      return a * b;
							case BinaryOperatorType.Divide:        return a / b;
							case BinaryOperatorType.Modulus:       return a % b;
					}
				}
				
				if (argTypes == typeof(int)) {
					switch (op) {
							case BinaryOperatorType.ShiftLeft:     return (int)left << (int)right;
							case BinaryOperatorType.ShiftRight:    return (int)left >> (int)right;
					}
					int a = (int)left;
					int b = (int)right;
					switch (op) {
							case BinaryOperatorType.BitwiseAnd:  return a & b;
							case BinaryOperatorType.BitwiseOr:   return a | b;
							case BinaryOperatorType.ExclusiveOr: return a ^ b;
							
							case BinaryOperatorType.GreaterThan:        return a > b;
							case BinaryOperatorType.GreaterThanOrEqual: return a >= b;
							case BinaryOperatorType.Equality:           return a == b;
							case BinaryOperatorType.InEquality:         return a != b;
							case BinaryOperatorType.LessThan:           return a < b;
							case BinaryOperatorType.LessThanOrEqual:    return a <= b;
							
							case BinaryOperatorType.Add:           return a + b;
							case BinaryOperatorType.Subtract:      return a - b;
							case BinaryOperatorType.Multiply:      return a * b;
							case BinaryOperatorType.Divide:        return a / b;
							case BinaryOperatorType.Modulus:       return a % b;
					}
				}
				
				if (argTypes == typeof(uint)) {
					switch (op) {
							case BinaryOperatorType.ShiftLeft:     return (uint)left << (int)right;
							case BinaryOperatorType.ShiftRight:    return (uint)left >> (int)right;
					}
					uint a = (uint)left;
					uint b = (uint)right;
					switch (op) {
							case BinaryOperatorType.BitwiseAnd:  return a & b;
							case BinaryOperatorType.BitwiseOr:   return a | b;
							case BinaryOperatorType.ExclusiveOr: return a ^ b;
							
							case BinaryOperatorType.GreaterThan:        return a > b;
							case BinaryOperatorType.GreaterThanOrEqual: return a >= b;
							case BinaryOperatorType.Equality:           return a == b;
							case BinaryOperatorType.InEquality:         return a != b;
							case BinaryOperatorType.LessThan:           return a < b;
							case BinaryOperatorType.LessThanOrEqual:    return a <= b;
							
							case BinaryOperatorType.Add:           return a + b;
							case BinaryOperatorType.Subtract:      return a - b;
							case BinaryOperatorType.Multiply:      return a * b;
							case BinaryOperatorType.Divide:        return a / b;
							case BinaryOperatorType.Modulus:       return a % b;
					}
				}
				
				if (argTypes == typeof(long)) {
					switch (op) {
							case BinaryOperatorType.ShiftLeft:     return (long)left << (int)right;
							case BinaryOperatorType.ShiftRight:    return (long)left >> (int)right;
					}
					long a = (long)left;
					long b = (long)right;
					switch (op) {
							case BinaryOperatorType.BitwiseAnd:  return a & b;
							case BinaryOperatorType.BitwiseOr:   return a | b;
							case BinaryOperatorType.ExclusiveOr: return a ^ b;
							
							case BinaryOperatorType.GreaterThan:        return a > b;
							case BinaryOperatorType.GreaterThanOrEqual: return a >= b;
							case BinaryOperatorType.Equality:           return a == b;
							case BinaryOperatorType.InEquality:         return a != b;
							case BinaryOperatorType.LessThan:           return a < b;
							case BinaryOperatorType.LessThanOrEqual:    return a <= b;
							
							case BinaryOperatorType.Add:           return a + b;
							case BinaryOperatorType.Subtract:      return a - b;
							case BinaryOperatorType.Multiply:      return a * b;
							case BinaryOperatorType.Divide:        return a / b;
							case BinaryOperatorType.Modulus:       return a % b;
					}
				}
				
				if (argTypes == typeof(ulong)) {
					switch (op) {
							case BinaryOperatorType.ShiftLeft:     return (ulong)left << (int)right;
							case BinaryOperatorType.ShiftRight:    return (ulong)left >> (int)right;
					}
					ulong a = (ulong)left;
					ulong b = (ulong)right;
					switch (op) {
							case BinaryOperatorType.BitwiseAnd:  return a & b;
							case BinaryOperatorType.BitwiseOr:   return a | b;
							case BinaryOperatorType.ExclusiveOr: return a ^ b;
							
							case BinaryOperatorType.GreaterThan:        return a > b;
							case BinaryOperatorType.GreaterThanOrEqual: return a >= b;
							case BinaryOperatorType.Equality:           return a == b;
							case BinaryOperatorType.InEquality:         return a != b;
							case BinaryOperatorType.LessThan:           return a < b;
							case BinaryOperatorType.LessThanOrEqual:    return a <= b;
							
							case BinaryOperatorType.Add:           return a + b;
							case BinaryOperatorType.Subtract:      return a - b;
							case BinaryOperatorType.Multiply:      return a * b;
							case BinaryOperatorType.Divide:        return a / b;
							case BinaryOperatorType.Modulus:       return a % b;
					}
				}
				
				return null;
			}
		}

		#endregion
	}
}
