/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

namespace dnSpy.Contracts.Debugger.UI {
	/// <summary>
	/// Options shown in the 'attach to process' dialog box and created by <see cref="AttachProgramOptionsProvider.Create(AttachProgramOptionsProviderContext)"/>
	/// </summary>
	public abstract class AttachProgramOptions {
		/// <summary>
		/// Process Id
		/// </summary>
		public abstract int Id { get; }

		/// <summary>
		/// Runtime type, see <see cref="PredefinedRuntimeTypes"/>
		/// </summary>
		public abstract string RuntimeType { get; }

		/// <summary>
		/// Runtime name, eg. "v4.0.30319"
		/// </summary>
		public abstract string RuntimeName { get; }

		/// <summary>
		/// Short process name (filename) or null to use the default value
		/// </summary>
		public virtual string Name => null;

		/// <summary>
		/// Process title or null to use the default value
		/// </summary>
		public virtual string Title => null;

		/// <summary>
		/// Full filename or null to use the default value
		/// </summary>
		public virtual string Filename => null;

		/// <summary>
		/// Processor architecture (eg. <see cref="PredefinedArchitectureNames.X86"/>) or null to use the default value
		/// </summary>
		public virtual string Architecture => null;

		/// <summary>
		/// Gets all options required to attach to the process
		/// </summary>
		/// <returns></returns>
		public abstract StartDebuggingOptions GetOptions();
	}
}
