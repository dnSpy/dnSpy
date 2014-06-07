// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.ILSpy.TextView;

namespace ICSharpCode.ILSpy
{
	public static class TaskHelper
	{
		public static readonly Task CompletedTask = FromResult<object>(null);
		
		public static Task<T> FromResult<T>(T result)
		{
			TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
			tcs.SetResult(result);
			return tcs.Task;
		}
		
		public static Task<T> FromException<T>(Exception ex)
		{
			var tcs = new TaskCompletionSource<T>();
			tcs.SetException(ex);
			return tcs.Task;
		}

		public static Task<T> FromCancellation<T>()
		{
			var tcs = new TaskCompletionSource<T>();
			tcs.SetCanceled();
			return tcs.Task;
		}

		/// <summary>
		/// Sets the result of the TaskCompletionSource based on the result of the finished task.
		/// </summary>
		public static void SetFromTask<T>(this TaskCompletionSource<T> tcs, Task<T> task)
		{
			switch (task.Status) {
				case TaskStatus.RanToCompletion:
					tcs.SetResult(task.Result);
					break;
				case TaskStatus.Canceled:
					tcs.SetCanceled();
					break;
				case TaskStatus.Faulted:
					tcs.SetException(task.Exception.InnerExceptions);
					break;
				default:
					throw new InvalidOperationException("The input task must have already finished");
			}
		}
		
		/// <summary>
		/// Sets the result of the TaskCompletionSource based on the result of the finished task.
		/// </summary>
		public static void SetFromTask(this TaskCompletionSource<object> tcs, Task task)
		{
			switch (task.Status) {
				case TaskStatus.RanToCompletion:
					tcs.SetResult(null);
					break;
				case TaskStatus.Canceled:
					tcs.SetCanceled();
					break;
				case TaskStatus.Faulted:
					tcs.SetException(task.Exception.InnerExceptions);
					break;
				default:
					throw new InvalidOperationException("The input task must have already finished");
			}
		}
		
		public static Task Then<T>(this Task<T> task, Action<T> action)
		{
			if (action == null)
				throw new ArgumentNullException("action");
			return task.ContinueWith(t => action(t.Result), CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public static Task<U> Then<T, U>(this Task<T> task, Func<T, U> func)
		{
			if (func == null)
				throw new ArgumentNullException("func");
			return task.ContinueWith(t => func(t.Result), CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public static Task Then<T>(this Task<T> task, Func<T, Task> asyncFunc)
		{
			if (asyncFunc == null)
				throw new ArgumentNullException("asyncFunc");
			return task.ContinueWith(t => asyncFunc(t.Result), CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap();
		}

		public static Task<U> Then<T, U>(this Task<T> task, Func<T, Task<U>> asyncFunc)
		{
			if (asyncFunc == null)
				throw new ArgumentNullException("asyncFunc");
			return task.ContinueWith(t => asyncFunc(t.Result), CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap();
		}

		public static Task Then(this Task task, Action action)
		{
			if (action == null)
				throw new ArgumentNullException("action");
			return task.ContinueWith(t => {
				t.Wait();
				action();
			}, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public static Task<U> Then<U>(this Task task, Func<U> func)
		{
			if (func == null)
				throw new ArgumentNullException("func");
			return task.ContinueWith(t => {
				t.Wait();
				return func();
			}, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public static Task Then(this Task task, Func<Task> asyncAction)
		{
			if (asyncAction == null)
				throw new ArgumentNullException("asyncAction");
			return task.ContinueWith(t => {
				t.Wait();
				return asyncAction();
			}, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap();
		}

		public static Task<U> Then<U>(this Task task, Func<Task<U>> asyncFunc)
		{
			if (asyncFunc == null)
				throw new ArgumentNullException("asyncFunc");
			return task.ContinueWith(t => {
				t.Wait();
				return asyncFunc();
			}, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap();
		}

		/// <summary>
		/// If the input task fails, calls the action to handle the error.
		/// </summary>
		/// <returns>
		/// Returns a task that finishes successfully when error handling has completed.
		/// If the input task ran successfully, the returned task completes successfully.
		/// If the input task was cancelled, the returned task is cancelled as well.
		/// </returns>
		public static Task Catch<TException>(this Task task, Action<TException> action) where TException : Exception
		{
			if (action == null)
				throw new ArgumentNullException("action");
			return task.ContinueWith(t => {
				if (t.IsFaulted) {
					Exception ex = t.Exception;
					while (ex is AggregateException)
						ex = ex.InnerException;
					if (ex is TException)
						action((TException)ex);
					else
						throw t.Exception;
				}
			}, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());
		}
		
		/// <summary>
		/// Ignore exceptions thrown by the task.
		/// </summary>
		public static void IgnoreExceptions(this Task task)
		{
		}
		
		/// <summary>
		/// Handle exceptions by displaying the error message in the text view.
		/// </summary>
		public static void HandleExceptions(this Task task)
		{
			task.Catch<Exception>(exception => MainWindow.Instance.Dispatcher.BeginInvoke(new Action(delegate {
				AvalonEditTextOutput output = new AvalonEditTextOutput();
				output.Write(exception.ToString());
				MainWindow.Instance.TextView.ShowText(output);
			}))).IgnoreExceptions();
		}
	}
}
