// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Debugger.MetaData;
using Debugger.Interop.CorDebug;

namespace Debugger
{
	public enum EvalState {
		Evaluating,
		EvaluatedSuccessfully,
		EvaluatedException,
		EvaluatedNoResult,
		EvaluatedTimeOut,
	};
	
	/// <summary>
	/// This class holds information about function evaluation.
	/// </summary>
	public class Eval: DebuggerObject
	{
		delegate void EvalStarter(Eval eval);
		
		AppDomain     appDomain;
		Process       process;
		
		string        description;
		ICorDebugEval corEval;
		Thread        thread;
		Value         result;
		EvalState     state;
		
		public AppDomain AppDomain {
			get { return appDomain; }
		}
		
		public Process Process {
			get { return process; }
		}
		
		public string Description {
			get { return description; }
		}
		
		public ICorDebugEval CorEval {
			get { return corEval; }
		}
		
		public ICorDebugEval2 CorEval2 {
			get { return (ICorDebugEval2)corEval; }
		}

	    /// <exception cref="GetValueException">Evaluating...</exception>
	    public Value Result {
			get {
				switch(this.State) {
					case EvalState.Evaluating:            throw new GetValueException("Evaluating...");
					case EvalState.EvaluatedSuccessfully: return result;
					case EvalState.EvaluatedException:    return result;
					case EvalState.EvaluatedNoResult:     return null;
					case EvalState.EvaluatedTimeOut:      throw new GetValueException("Timeout");
					default: throw new DebuggerException("Unknown state");
				}
			}
		}
		
		public EvalState State {
			get { return state; }
		}
		
		public bool Evaluated {
			get {
				return state == EvalState.EvaluatedSuccessfully ||
				       state == EvalState.EvaluatedException ||
				       state == EvalState.EvaluatedNoResult ||
				       state == EvalState.EvaluatedTimeOut;
			}
		}
		
		Eval(AppDomain appDomain, string description, EvalStarter evalStarter)
		{
			this.appDomain = appDomain;
			this.process = appDomain.Process;
			this.description = description;
			this.state = EvalState.Evaluating;
			this.thread = GetEvaluationThread(appDomain);
			this.corEval = thread.CorThread.CreateEval();
			
			try {
				evalStarter(this);
			} catch (COMException e) {
				if ((uint)e.ErrorCode == 0x80131C26) {
					throw new GetValueException("Can not evaluate in optimized code");
				} else if ((uint)e.ErrorCode == 0x80131C28) {
					throw new GetValueException("Object is in wrong AppDomain");
				} else if ((uint)e.ErrorCode == 0x8013130A) {
					// Happens on getting of Sytem.Threading.Thread.ManagedThreadId; See SD2-1116
					throw new GetValueException("Function does not have IL code");
				} else if ((uint)e.ErrorCode == 0x80131C23) {
					// The operation failed because it is a GC unsafe point. (Exception from HRESULT: 0x80131C23)
					// This can probably happen when we break and the thread is in native code
					throw new GetValueException("Thread is in GC unsafe point");
				} else if ((uint)e.ErrorCode == 0x80131C22) {
					// The operation is illegal because of a stack overflow.
					throw new GetValueException("Can not evaluate after stack overflow");
				} else if ((uint)e.ErrorCode == 0x80131313) {
					// Func eval cannot work. Bad starting point.
					// Reproduction circumstancess are unknown
					throw new GetValueException("Func eval cannot work. Bad starting point.");
				} else {
					#if DEBUG
						throw; // Expose for more diagnostics
					#else
						throw new GetValueException(e.Message);
					#endif
				}
			}
			
			appDomain.Process.ActiveEvals.Add(this);
			
			if (appDomain.Process.Options.SuspendOtherThreads) {
				appDomain.Process.AsyncContinue(DebuggeeStateAction.Keep, new Thread[] { thread }, CorDebugThreadState.THREAD_SUSPEND);
			} else {
				appDomain.Process.AsyncContinue(DebuggeeStateAction.Keep, this.Process.UnsuspendedThreads, CorDebugThreadState.THREAD_RUN);
			}
		}
		
		static Thread GetEvaluationThread(AppDomain appDomain)
		{
			appDomain.Process.AssertPaused();
			
			Thread st = appDomain.Process.SelectedThread;
			if (st != null && !st.Suspended && !st.IsInNativeCode && st.IsAtSafePoint && st.CorThread.GetAppDomain().GetID() == appDomain.ID) {
				return st;
			}
			
			foreach(Thread t in appDomain.Process.Threads) {
				if (!t.Suspended && !t.IsInNativeCode && t.IsAtSafePoint && t.CorThread.GetAppDomain().GetID() == appDomain.ID) {
					return t;
				}
			}
			
			throw new GetValueException("No suitable thread for evaluation");
		}

		internal bool IsCorEval(ICorDebugEval corEval)
		{
			return this.corEval == corEval;
		}

	    /// <exception cref="DebuggerException">Evaluation can not be stopped</exception>
	    /// <exception cref="GetValueException">Process exited</exception>
	    Value WaitForResult()
		{
			// Note that aborting is not supported for suspended threads
			try {
				process.WaitForPause(TimeSpan.FromMilliseconds(500));
				if (!Evaluated) {
					process.TraceMessage("Aborting eval: " + Description);
					this.CorEval.Abort();
					process.WaitForPause(TimeSpan.FromMilliseconds(2500));
					if (!Evaluated) {
						process.TraceMessage("Rude aborting eval: " + Description);
						this.CorEval2.RudeAbort();
						process.WaitForPause(TimeSpan.FromMilliseconds(5000));
						if (!Evaluated) {
							throw new DebuggerException("Evaluation can not be stopped");
						}
					}
					// Note that this sets Evaluated to true
					state = EvalState.EvaluatedTimeOut;
				}
				process.AssertPaused();
				return this.Result;
			} catch (ProcessExitedException) {
				throw new GetValueException("Process exited");
			}
		}
		
		internal void NotifyEvaluationComplete(bool successful) 
		{
			// Eval result should be ICorDebugHandleValue so it should survive Continue()
			if (state == EvalState.EvaluatedTimeOut) {
				return;
			}
			if (corEval.GetResult() == null) {
				state = EvalState.EvaluatedNoResult;
			} else {
				if (successful) {
					state = EvalState.EvaluatedSuccessfully;
				} else {
					state = EvalState.EvaluatedException;
				}
				result = new Value(AppDomain, corEval.GetResult());
			}
		}
		
		/// <summary> Synchronously calls a function and returns its return value </summary>
		public static Value InvokeMethod(DebugMethodInfo method, Value thisValue, Value[] args)
		{
			if (method.BackingField != null) {
				method.Process.TraceMessage("Using backing field for " + method.FullName);
				return Value.GetMemberValue(thisValue, method.BackingField, args);
			}
			return AsyncInvokeMethod(method, thisValue, args).WaitForResult();
		}
		
		public static Eval AsyncInvokeMethod(DebugMethodInfo method, Value thisValue, Value[] args)
		{
			return new Eval(
				method.AppDomain,
				"Function call: " + method.FullName,
				delegate(Eval eval) {
					MethodInvokeStarter(eval, method, thisValue, args);
				}
			);
		}

		/// <exception cref="GetValueException"><c>GetValueException</c>.</exception>
		static void MethodInvokeStarter(Eval eval, DebugMethodInfo method, Value thisValue, Value[] args)
		{
			List<ICorDebugValue> corArgs = new List<ICorDebugValue>();
			args = args ?? new Value[0];
			if (args.Length != method.ParameterCount) {
				throw new GetValueException("Invalid parameter count");
			}
			if (!method.IsStatic) {
				if (thisValue == null)
					throw new GetValueException("'this' is null");
				if (thisValue.IsNull)
					throw new GetValueException("Null reference");
				// if (!(thisValue.IsObject)) // eg Can evaluate on array
				if (!method.DeclaringType.IsInstanceOfType(thisValue)) {
					throw new GetValueException(
						"Can not evaluate because the object is not of proper type.  " + 
						"Expected: " + method.DeclaringType.FullName + "  Seen: " + thisValue.Type.FullName
					);
				}
				corArgs.Add(thisValue.CorValue);
			}
			for(int i = 0; i < args.Length; i++) {
				Value arg = args[i];
				DebugType paramType = (DebugType)method.GetParameters()[i].ParameterType;
				if (!arg.Type.CanImplicitelyConvertTo(paramType))
					throw new GetValueException("Inncorrect parameter type");
				// Implicitely convert to correct primitve type
				if (paramType.IsPrimitive && args[i].Type != paramType) {
					object oldPrimVal = arg.PrimitiveValue;
					object newPrimVal = Convert.ChangeType(oldPrimVal, paramType.PrimitiveType);
					arg = CreateValue(method.AppDomain, newPrimVal);
				}
				// It is importatnt to pass the parameted in the correct form (boxed/unboxed)
				if (paramType.IsValueType) {
					corArgs.Add(arg.CorGenericValue);
				} else {
					if (args[i].Type.IsValueType) {
						corArgs.Add(arg.Box().CorValue);
					} else {
						corArgs.Add(arg.CorValue);
					}
				}
			}
			
			ICorDebugType[] genericArgs = ((DebugType)method.DeclaringType).GenericArgumentsAsCorDebugType;
			eval.CorEval2.CallParameterizedFunction(
				method.CorFunction,
				(uint)genericArgs.Length, genericArgs,
				(uint)corArgs.Count, corArgs.ToArray()
			);
		}
	    
	    public static Value CreateValue(AppDomain appDomain, object value)
	    {
	    	if (value == null) {
				ICorDebugClass corClass = appDomain.ObjectType.CorType.GetClass();
				Thread thread = GetEvaluationThread(appDomain);
				ICorDebugEval corEval = thread.CorThread.CreateEval();
				ICorDebugValue corValue = corEval.CreateValue((uint)CorElementType.CLASS, corClass);
				return new Value(appDomain, corValue);
			} else if (value is string) {
	    		return Eval.NewString(appDomain, (string)value);
			} else {
	    		if (!value.GetType().IsPrimitive)
	    			throw new DebuggerException("Value must be primitve type.  Seen " + value.GetType());
				Value val = Eval.NewObjectNoConstructor(DebugType.CreateFromType(appDomain.Mscorlib, value.GetType()));
				val.PrimitiveValue = value;
				return val;
			}
	    }
		
	    /*
		// The following function create values only for the purpuse of evalutaion
		// They actually do not allocate memory on the managed heap
		// The advantage is that it does not continue the process
	    /// <exception cref="DebuggerException">Can not create string this way</exception>
	    public static Value CreateValue(Process process, object value)
		{
			if (value is string) throw new DebuggerException("Can not create string this way");
			CorElementType corElemType;
			ICorDebugClass corClass = null;
			if (value != null) {
				corElemType = DebugType.TypeNameToCorElementType(value.GetType().FullName);
			} else {
				corElemType = CorElementType.CLASS;
				corClass = DebugType.Create(process, null, typeof(object).FullName).CorType.Class;
			}
			ICorDebugEval corEval = CreateCorEval(process);
			ICorDebugValue corValue = corEval.CreateValue((uint)corElemType, corClass);
			Value v = new Value(process, new Expressions.PrimitiveExpression(value), corValue);
			if (value != null) {
				v.PrimitiveValue = value;
			}
			return v;
		}
		*/
		
		#region Convenience methods
		
		public static Value NewString(AppDomain appDomain, string textToCreate)
		{
			return AsyncNewString(appDomain, textToCreate).WaitForResult();
		}
		
		#endregion
		
		public static Eval AsyncNewString(AppDomain appDomain, string textToCreate)
		{
			return new Eval(
				appDomain,
				"New string: " + textToCreate,
				delegate(Eval eval) {
					eval.CorEval2.NewStringWithLength(textToCreate, (uint)textToCreate.Length);
				}
			);
		}
		
		#region Convenience methods
		
		public static Value NewArray(DebugType type, uint length, uint? lowerBound)
		{
			return AsyncNewArray(type, length, lowerBound).WaitForResult();
		}
		
		#endregion
		
		public static Eval AsyncNewArray(DebugType type, uint length, uint? lowerBound)
		{
			lowerBound = lowerBound ?? 0;
			return new Eval(
				type.AppDomain,
				"New array: " + type + "[" + length + "]",
				delegate(Eval eval) {
					// Multi-dimensional arrays not supported in .NET 2.0
					eval.CorEval2.NewParameterizedArray(type.CorType, 1, new uint[] { length }, new uint[] { lowerBound.Value });
				}
			);
		}
		
		#region Convenience methods
		
		public static Value NewObject(DebugMethodInfo constructor, Value[] constructorArguments)
		{
			return AsyncNewObject(constructor, constructorArguments).WaitForResult();
		}
		
		#endregion
		
		public static Eval AsyncNewObject(DebugMethodInfo constructor, Value[] constructorArguments)
		{
			ICorDebugValue[] constructorArgsCorDebug = ValuesAsCorDebug(constructorArguments);
			return new Eval(
				constructor.AppDomain,
				"New object: " + constructor.FullName,
				delegate(Eval eval) {
					eval.CorEval2.NewParameterizedObject(
						constructor.CorFunction,
						(uint)constructor.DeclaringType.GetGenericArguments().Length, ((DebugType)constructor.DeclaringType).GenericArgumentsAsCorDebugType,
						(uint)constructorArgsCorDebug.Length, constructorArgsCorDebug);
				}
			);
		}
		
		#region Convenience methods
		
		public static Value NewObjectNoConstructor(DebugType debugType)
		{
			return AsyncNewObjectNoConstructor(debugType).WaitForResult();
		}
		
		#endregion
		
		public static Eval AsyncNewObjectNoConstructor(DebugType debugType)
		{
			return new Eval(
				debugType.AppDomain,
				"New object: " + debugType.FullName,
				delegate(Eval eval) {
					eval.CorEval2.NewParameterizedObjectNoConstructor(debugType.CorType.GetClass(), (uint)debugType.GetGenericArguments().Length, debugType.GenericArgumentsAsCorDebugType);
				}
			);
		}
		
		static ICorDebugValue[] ValuesAsCorDebug(Value[] values)
		{
			ICorDebugValue[] valuesAsCorDebug = new ICorDebugValue[values.Length];
			for(int i = 0; i < values.Length; i++) {
				valuesAsCorDebug[i] = values[i].CorValue;
			}
			return valuesAsCorDebug;
		}
	}
}
