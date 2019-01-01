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
	/// A process that can be attached to
	/// </summary>
	public abstract class AttachableProcess {
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
		/// Short process name (filename)
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Process title
		/// </summary>
		public abstract string Title { get; }

		/// <summary>
		/// Full filename
		/// </summary>
		public abstract string Filename { get; }

		/// <summary>
		/// Processor architecture
		/// </summary>
		public abstract DbgArchitecture Architecture { get; }

		/// <summary>
		/// Operating system
		/// </summary>
		public abstract DbgOperatingSystem OperatingSystem { get; }

		/// <summary>
		/// Gets all options required to attach to the process
		/// </summary>
		/// <returns></returns>
		public abstract AttachToProgramOptions GetOptions();

		/// <summary>
		/// Attaches to the process
		/// </summary>
		public abstract void Attach();
	}
}
