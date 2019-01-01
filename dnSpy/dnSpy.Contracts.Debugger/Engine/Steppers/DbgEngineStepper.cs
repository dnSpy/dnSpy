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

namespace dnSpy.Contracts.Debugger.Engine.Steppers {
	/// <summary>
	/// Steps into, over or out of a method. When closed, any non-completed <see cref="Step(object, DbgEngineStepKind)"/> call must be canceled.
	/// </summary>
	public abstract class DbgEngineStepper : DbgObject {
		/// <summary>
		/// Raised when the step is complete
		/// </summary>
		public abstract event EventHandler<DbgEngineStepCompleteEventArgs> StepComplete;

		/// <summary>
		/// Steps once. This method can be called again once <see cref="StepComplete"/> is raised.
		/// This method is only called if the engine is paused.
		/// </summary>
		/// <param name="tag">This value must be used when raising <see cref="StepComplete"/></param>
		/// <param name="step">Step kind</param>
		public abstract void Step(object tag, DbgEngineStepKind step);

		/// <summary>
		/// Cancels the step, but does not raise <see cref="StepComplete"/>
		/// </summary>
		/// <param name="tag">Same value that was passed to <see cref="Step(object, DbgEngineStepKind)"/></param>
		public abstract void Cancel(object tag);
	}

	/// <summary>
	/// Step complete event args
	/// </summary>
	public readonly struct DbgEngineStepCompleteEventArgs {
		/// <summary>
		/// Gets the thread or null to use the default thread that was used to create the stepper
		/// </summary>
		public DbgThread Thread { get; }

		/// <summary>
		/// Gets the tag
		/// </summary>
		public object Tag { get; }

		/// <summary>
		/// Gets the error message or null if none
		/// </summary>
		public string Error { get; }

		/// <summary>
		/// true if the stepper was canceled by the engine
		/// </summary>
		public bool ForciblyCanceled { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="thread">Thread or null to use the default thread that was used to create the stepper</param>
		/// <param name="tag">Same value that was passed to <see cref="DbgEngineStepper.Step(object, DbgEngineStepKind)"/></param>
		/// <param name="error">Error message or null if none</param>
		/// <param name="forciblyCanceled">true if the stepper was canceled by the engine</param>
		public DbgEngineStepCompleteEventArgs(DbgThread thread, object tag, string error, bool forciblyCanceled) {
			Thread = thread;
			Tag = tag;
			Error = error;
			ForciblyCanceled = forciblyCanceled;
		}
	}
}
