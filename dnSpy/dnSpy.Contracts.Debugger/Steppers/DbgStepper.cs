/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;

namespace dnSpy.Contracts.Debugger.Steppers {
	/// <summary>
	/// Steps into, over or out of a method
	/// </summary>
	public abstract class DbgStepper : DbgObject {
		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process => Thread.Process;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public DbgRuntime Runtime => Thread.Runtime;

		/// <summary>
		/// Gets the thread
		/// </summary>
		public abstract DbgThread Thread { get; }

		/// <summary>
		/// true if it's possible to call <see cref="Step(DbgStepKind, bool)"/> (eg. process must be paused)
		/// </summary>
		public abstract bool CanStep { get; }

		/// <summary>
		/// true if <see cref="Step(DbgStepKind, bool)"/> has been called but <see cref="StepComplete"/> hasn't been raised yet
		/// </summary>
		public abstract bool IsStepping { get; }

		/// <summary>
		/// Raised when the step is complete
		/// </summary>
		public abstract event EventHandler<DbgStepCompleteEventArgs> StepComplete;

		/// <summary>
		/// Steps once. This method can be called again once <see cref="StepComplete"/> is raised.
		/// The method can only be called when its process is paused.
		/// </summary>
		/// <param name="step">Step kind</param>
		/// <param name="autoClose">true to call <see cref="Close"/> once <see cref="StepComplete"/> is raised</param>
		public abstract void Step(DbgStepKind step, bool autoClose = false);

		/// <summary>
		/// Cancels the step
		/// </summary>
		public abstract void Cancel();

		/// <summary>
		/// Closes the stepper and cancels <see cref="Step(DbgStepKind, bool)"/>
		/// </summary>
		public abstract void Close();
	}

	/// <summary>
	/// Step complete event args
	/// </summary>
	public readonly struct DbgStepCompleteEventArgs {
		/// <summary>
		/// Gets the thread
		/// </summary>
		public DbgThread Thread { get; }

		/// <summary>
		/// Gets the error message or null if none
		/// </summary>
		public string Error { get; }

		/// <summary>
		/// true if there was an error
		/// </summary>
		public bool HasError => Error != null;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="thread">Thread</param>
		/// <param name="error">Error message or null if none</param>
		public DbgStepCompleteEventArgs(DbgThread thread, string error) {
			Thread = thread ?? throw new ArgumentNullException(nameof(thread));
			Error = error;
		}
	}
}
