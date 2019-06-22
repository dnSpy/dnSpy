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

namespace dnSpy.Contracts.Debugger.Attach {
	/// <summary>
	/// Options shown in the 'attach to process' dialog box and created by <see cref="AttachProgramOptionsProvider.Create(AttachProgramOptionsProviderContext)"/>
	/// </summary>
	public abstract class AttachProgramOptions {
		/// <summary>
		/// Process id
		/// </summary>
		public abstract int ProcessId { get; }

		/// <summary>
		/// Runtime id
		/// </summary>
		public abstract RuntimeId RuntimeId { get; }

		/// <summary>
		/// Gets the runtime GUID, see <see cref="PredefinedDbgRuntimeGuids"/>
		/// </summary>
		public abstract Guid RuntimeGuid { get; }

		/// <summary>
		/// Gets the runtime kind GUID, see <see cref="PredefinedDbgRuntimeKindGuids"/>
		/// </summary>
		public abstract Guid RuntimeKindGuid { get; }

		/// <summary>
		/// Runtime name, eg. "CLR v4.0.30319"
		/// </summary>
		public abstract string RuntimeName { get; }

		/// <summary>
		/// Short process name (filename) or null to use the default value
		/// </summary>
		public virtual string? Name => null;

		/// <summary>
		/// Process title or null to use the default value
		/// </summary>
		public virtual string? Title => null;

		/// <summary>
		/// Full filename or null to use the default value
		/// </summary>
		public virtual string? Filename => null;

		/// <summary>
		/// Command line or null to use the default value
		/// </summary>
		public virtual string? CommandLine => null;

		/// <summary>
		/// Processor architecture or null to use the default value
		/// </summary>
		public virtual DbgArchitecture? Architecture => null;

		/// <summary>
		/// Operating system or null to use the default value
		/// </summary>
		public virtual DbgOperatingSystem? OperatingSystem => null;

		/// <summary>
		/// Gets all options required to attach to the process
		/// </summary>
		/// <returns></returns>
		public abstract AttachToProgramOptions GetOptions();
	}
}
