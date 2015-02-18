// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using System.Reflection;

using Debugger;
using Debugger.Interop.CorDebug;
using Debugger.MetaData;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.ILSpy.Debugger.Services.Debugger
{
	static class DebuggerHelpers
	{
		/// <summary>
		/// Creates an expression which, when evaluated, creates a List&lt;T&gt; in the debugee
		/// filled with contents of IEnumerable&lt;T&gt; from the debugee.
		/// </summary>
		/// <param name="iEnumerableVariable">Expression for IEnumerable variable in the debugee.</param>
		/// <param name="itemType">
		/// The generic argument of IEnumerable&lt;T&gt; that <paramref name="iEnumerableVariable"/> implements.</param>
		public static Expression CreateDebugListExpression(Expression iEnumerableVariable, DebugType itemType, out DebugType listType)
		{
			// is using itemType.AppDomain ok?
			listType = DebugType.CreateFromType(itemType.AppDomain, typeof(System.Collections.Generic.List<>), itemType);
			var iEnumerableType = DebugType.CreateFromType(itemType.AppDomain, typeof(IEnumerable<>), itemType);
			// explicitely cast the variable to IEnumerable<T>, where T is itemType
			Expression iEnumerableVariableExplicitCast = new CastExpression { Expression = iEnumerableVariable.Clone() , Type = iEnumerableType.GetTypeReference() };
			
			var obj = new ObjectCreateExpression() { Type = listType.GetTypeReference() };			
			obj.Arguments.AddRange(iEnumerableVariableExplicitCast.ToList());
			
			return obj;
		}
		
		/// <summary>
		/// Gets underlying address of object in the debuggee.
		/// </summary>
		public static ulong GetObjectAddress(this Value val)
		{
			if (val.IsNull) return 0;
			ICorDebugReferenceValue refVal = val.CorReferenceValue;
			return refVal.GetValue();
		}
		
		/// <summary>
		/// Returns true if this type is enum.
		/// </summary>
		public static bool IsEnum(this DebugType type)
		{
			return (type.BaseType != null) && (type.BaseType.FullName == "System.Enum");
		}
		
		/// <summary>
		/// Returns true is this type is just System.Object.
		/// </summary>
		public static bool IsSystemDotObject(this DebugType type)
		{
			return type.FullName == "System.Object";
		}
		
		/// <summary>
		/// Evaluates expression and gets underlying address of object in the debuggee.
		/// </summary>
		public static ulong GetObjectAddress(this Expression expr)
		{
			return expr.Evaluate(WindowsDebugger.CurrentProcess).GetObjectAddress();
		}
		
		/// <summary>
		/// System.Runtime.CompilerServices.GetHashCode method, for obtaining non-overriden hash codes from debuggee.
		/// </summary>
		private static DebugMethodInfo hashCodeMethod;
		
		/// <summary>
		/// Invokes RuntimeHelpers.GetHashCode on given value, that is a default hashCode ignoring user overrides.
		/// </summary>
		/// <param name="value">Valid value.</param>
		/// <returns>Hash code of the object in the debugee.</returns>
		public static int InvokeDefaultGetHashCode(this Value value)
		{
			if (hashCodeMethod == null || hashCodeMethod.Process.HasExited) {
				DebugType typeRuntimeHelpers = DebugType.CreateFromType(value.AppDomain, typeof(System.Runtime.CompilerServices.RuntimeHelpers));
				hashCodeMethod = (DebugMethodInfo)typeRuntimeHelpers.GetMethod("GetHashCode", BindingFlags.Public | BindingFlags.Static);
				if (hashCodeMethod == null) {
					throw new DebuggerException("Cannot obtain method System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode");
				}
			}
			// David: I had hard time finding out how to invoke static method.
			// value.InvokeMethod is nice for instance methods.
			// what about MethodInfo.Invoke() ?
			// also, there could be an overload that takes 1 parameter instead of array
			Value defaultHashCode = Eval.InvokeMethod(DebuggerHelpers.hashCodeMethod, null, new Value[]{value});
			
			//MethodInfo method = value.Type.GetMember("GetHashCode", BindingFlags.Method | BindingFlags.IncludeSuperType) as MethodInfo;
			//string hashCode = value.InvokeMethod(method, null).AsString;
			return (int)defaultHashCode.PrimitiveValue;
		}
		
		public static Value EvalPermanentReference(this Expression expr)
		{
			return expr.Evaluate(WindowsDebugger.CurrentProcess).GetPermanentReference();
		}
		
		public static bool IsNull(this Expression expr)
		{
			return expr.Evaluate(WindowsDebugger.CurrentProcess).IsNull;
		}
		
		public static DebugType GetType(this Expression expr)
		{
			return expr.Evaluate(WindowsDebugger.CurrentProcess).Type;
		}
	}
}
