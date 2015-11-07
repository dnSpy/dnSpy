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
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.ToolBars;

namespace dnSpy.Contracts {
	/// <summary>
	/// Globals
	/// </summary>
	public static class DnSpy {
		/// <summary>
		/// Gets the application
		/// </summary>
		public static IApp App {
			get {
				Debug.Assert(app != null);
				return app;
			}
			set {
				Debug.Assert(app == null);
				if (app != null)
					throw new InvalidOperationException();
				if (value == null)
					throw new ArgumentNullException();
				app = value;
			}
		}
		static IApp app;
	}

	/// <summary>
	/// dnSpy application interface
	/// </summary>
	public interface IApp {
		/// <summary>
		/// Gets the version number
		/// </summary>
		Version Version { get; }

		/// <summary>
		/// Gets the <see cref="IMenuManager"/> instance
		/// </summary>
		IMenuManager MenuManager { get; }

		/// <summary>
		/// Gets the <see cref="IToolBarManager"/> instance
		/// </summary>
		IToolBarManager ToolBarManager { get; }

		/// <summary>
		/// Gets the <see cref="IThemeManager"/> instance
		/// </summary>
		IThemeManager ThemeManager { get; }

		/// <summary>
		/// Gets the <see cref="IImageManager"/> instance
		/// </summary>
		IImageManager ImageManager { get; }

		/// <summary>
		/// Gets the <see cref="System.ComponentModel.Composition.Hosting.CompositionContainer"/> instance
		/// </summary>
		CompositionContainer CompositionContainer { get; }
	}
}
