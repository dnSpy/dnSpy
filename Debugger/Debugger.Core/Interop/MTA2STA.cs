// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Debugger
{
	public delegate T MethodInvokerWithReturnValue<T>();
	
	public enum CallMethod {DirectCall, Manual, HiddenForm, HiddenFormWithTimeout};
	
	public class MTA2STA
	{
		Form hiddenForm;
		IntPtr hiddenFormHandle;
		
		System.Threading.Thread targetThread;
		CallMethod callMethod = CallMethod.HiddenFormWithTimeout;
		
		Queue<MethodInvoker> pendingCalls = new Queue<MethodInvoker>();
		ManualResetEvent pendingCallsNotEmpty = new ManualResetEvent(false);
		
		WaitHandle EnqueueCall(MethodInvoker callDelegate)
		{
			lock (pendingCalls) {
				ManualResetEvent callDone = new ManualResetEvent(false);
				pendingCalls.Enqueue(delegate{
				                     	callDelegate();
				                     	callDone.Set();
				                     });
				pendingCallsNotEmpty.Set();
				return callDone;
			}
		}
		
		/// <summary>
		/// Wait until a call a made
		/// </summary>
		public void WaitForCall()
		{
			pendingCallsNotEmpty.WaitOne();
		}
		
		public void WaitForCall(TimeSpan timeout)
		{
			pendingCallsNotEmpty.WaitOne(timeout, false);
		}
		
		/// <summary>
		/// Performs all waiting calls on the current thread
		/// </summary>
		public void PerformAllCalls()
		{
			while (true) {
				if (!PerformCall()) {
					return;
				}
			}
		}
		
		/// <summary>
		/// Performs all waiting calls on the current thread
		/// </summary>
		/// <remarks>
		/// Private - user should always drain the queue - otherwise Prcoess.RaisePausedEvents might fail
		/// </remarks>
		bool PerformCall()
		{
			MethodInvoker nextMethod;
			lock (pendingCalls) {
				if (pendingCalls.Count > 0) {
					nextMethod = pendingCalls.Dequeue();
				} else {
					pendingCallsNotEmpty.Reset();
					return false;
				}
			}
			nextMethod();
			return true;
		}
		
		public CallMethod CallMethod {
			get {
				return callMethod;
			}
			set {
				callMethod = value;
			}
		}
		
		public MTA2STA()
		{
			targetThread = System.Threading.Thread.CurrentThread;
			
			hiddenForm = new Form();
			// Force handle creation
			hiddenFormHandle = hiddenForm.Handle;
		}
		
		/// <summary>
		/// SoftWait waits for any of the given WaitHandles and allows processing of calls during the wait
		/// </summary>
		public int SoftWait(params WaitHandle[] waitFor)
		{
			List<WaitHandle> waits = new List<WaitHandle> (waitFor);
			waits.Add(pendingCallsNotEmpty);
			while(true) {
				int i = WaitHandle.WaitAny(waits.ToArray());
				PerformAllCalls();
				if (i < waits.Count - 1) { // If not pendingCallsNotEmpty
					return i;
				}
			}
		}
		
		/// <summary>
		/// Schedules invocation of method and returns immediately
		/// </summary>
		public WaitHandle AsyncCall(MethodInvoker callDelegate)
		{
			WaitHandle callDone = EnqueueCall(callDelegate);
			TriggerInvoke();
			return callDone;
		}
		
		public T Call<T>(MethodInvokerWithReturnValue<T> callDelegate)
		{
			T returnValue = default(T);
			Call(delegate { returnValue = callDelegate(); }, true);
			return returnValue;
		}
		
		public void Call(MethodInvoker callDelegate)
		{
			Call(callDelegate, false);
		}
		
		void Call(MethodInvoker callDelegate, bool hasReturnValue)
		{
			// Enqueue the call
			WaitHandle callDone = EnqueueCall(callDelegate);
			
			if (targetThread == System.Threading.Thread.CurrentThread) {
				PerformAllCalls();
				return;
			}
			
			// We have the call waiting in queue, we need to call it (not waiting for it to finish)
			TriggerInvoke();
			
			// Wait for the call to finish
			if (!hasReturnValue && callMethod == CallMethod.HiddenFormWithTimeout) {
				// Give it 5 seconds to run
				if (!callDone.WaitOne(5000, true)) {
					System.Console.WriteLine("Call time out! (continuing)");
					System.Console.WriteLine(new System.Diagnostics.StackTrace(true).ToString());
				}
			} else {
				callDone.WaitOne();
			}
		}
		
		void TriggerInvoke()
		{
			switch (callMethod) {
				case CallMethod.DirectCall:
					PerformAllCalls();
					break;
				case CallMethod.Manual:
					// Nothing we can do - someone else must call SoftWait or Pulse
					break;
				case CallMethod.HiddenForm:
				case CallMethod.HiddenFormWithTimeout:
					hiddenForm.BeginInvoke((MethodInvoker)PerformAllCalls);
					break;
			}
		}
		
		public static object MarshalParamTo(object param, Type outputType)
		{
			if (param is IntPtr) {
				return MarshalIntPtrTo((IntPtr)param, outputType);
			} else {
				return param;
			}
		}
		
		public static T MarshalIntPtrTo<T>(IntPtr param)
		{
			return (T)MarshalIntPtrTo(param, typeof(T));
		}
		
		public static object MarshalIntPtrTo(IntPtr param, Type outputType)
		{
			// IntPtr requested as output (must be before the null check so that we pass IntPtr.Zero)
			if (outputType == typeof(IntPtr)) {
				return param;
			}
			// The parameter is null pointer
			if ((IntPtr)param == IntPtr.Zero) {
				return null;
			}
			// String requested as output
			if (outputType == typeof(string)) {
				return Marshal.PtrToStringAuto((IntPtr)param);
			}
			// Marshal a COM object
			object comObject = Marshal.GetObjectForIUnknown(param);
			Debugger.Interop.TrackedComObjects.Track(comObject);
			return comObject;
		}
		
		/// <summary>
		/// Uses reflection to call method. Automaticaly marshals parameters.
		/// </summary>
		/// <param name="targetObject">Targed object which contains the method. In case of static mehod pass the Type</param>
		/// <param name="functionName">The name of the function to call</param>
		/// <param name="functionParameters">Parameters which should be send to the function. Parameters will be marshaled to proper type.</param>
		/// <returns>Return value of the called function</returns>
		public static object InvokeMethod(object targetObject, string functionName, object[] functionParameters)
		{
			System.Reflection.MethodInfo method;
			if (targetObject is Type) {
				method = ((Type)targetObject).GetMethod(functionName);
			} else {
				method = targetObject.GetType().GetMethod(functionName);
			}
			
			ParameterInfo[] methodParamsInfo = method.GetParameters();
			object[] convertedParams = new object[methodParamsInfo.Length];
			
			for (int i = 0; i < convertedParams.Length; i++) {
				convertedParams[i] = MarshalParamTo(functionParameters[i], methodParamsInfo[i].ParameterType);
			}
			
			try {
				if (targetObject is Type) {
					return method.Invoke(null, convertedParams);
				} else {
					return method.Invoke(targetObject, convertedParams);
				}
			} catch (System.Exception exception) {
				throw new Debugger.DebuggerException("Invoke of " + functionName + " failed.", exception);
			}
		}
	}
}
