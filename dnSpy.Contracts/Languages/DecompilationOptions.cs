/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Diagnostics;
using System.Threading;
using ICSharpCode.Decompiler;

namespace dnSpy.Contracts.Languages {
	/// <summary>
	/// Decompilation options
	/// </summary>
	public class DecompilationOptions {
		/// <summary />
		public ProjectOptions ProjectOptions { get; set; }

		/// <summary />
		public CancellationToken CancellationToken { get; set; }

		/// <summary />
		public DecompilerSettings DecompilerSettings { get; set; }

		/// <summary>
		/// true to catch decompiler exceptions and show an error message
		/// </summary>
		public bool DontShowCreateMethodBodyExceptions { get; set; }

		// *******************************************************************
		// NOTE: Don't add new props here without also updating the decompilation cache code
		// *******************************************************************

		/// <summary>
		/// Constructor
		/// </summary>
		public DecompilationOptions() {
			this.ProjectOptions = new ProjectOptions();
			this.CancellationToken = CancellationToken.None;
			this.DecompilerSettings = new DecompilerSettings();
			this.DontShowCreateMethodBodyExceptions = true;
		}

		/// <summary>
		/// Set a <see cref="DecompilerSettings"/> factory
		/// </summary>
		/// <param name="factory">Factory</param>
		public static void SetDecompilerSettingsFactory(Func<DecompilerSettings> factory) {
			if (factory == null)
				throw new ArgumentNullException();
			if (DecompilationOptions.factory != null)
				throw new InvalidOperationException();
			DecompilationOptions.factory = factory;
		}
		static Func<DecompilerSettings> factory;

		/// <summary>
		/// Makes a copy of the global <see cref="DecompilerSettings"/>. DON'T CALL IT.
		/// </summary>
		/// <returns></returns>
		public static DecompilerSettings _DONT_CALL_CreateDecompilerSettings() {//TODO: Remove it and SetDecompilerSettingsFactory(). Only used by older C#/VB code in Languages that accessed a global
			if (factory != null) {
				var d = factory();
				if (d == null)
					throw new InvalidOperationException();
				return d;
			}
			Debug.Fail("DecompilerSettings factory hasn't been initialized");
			return new DecompilerSettings();
		}
	}
}
