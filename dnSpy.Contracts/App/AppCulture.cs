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

using System;
using System.Globalization;
using System.Threading;

namespace dnSpy.Contracts.App {
	/// <summary>
	/// Initializes culture and UI culture
	/// </summary>
	public static class AppCulture {
		/// <summary>
		/// Culture
		/// </summary>
		public static CultureInfo Culture {
			get { return culture; }
		}
		static CultureInfo culture;

		/// <summary>
		/// UI Culture
		/// </summary>
		public static CultureInfo UICulture {
			get { return uiCulture; }
		}
		static CultureInfo uiCulture;

		static AppCulture() {
			__Initialize(Thread.CurrentThread.CurrentCulture, Thread.CurrentThread.CurrentUICulture);
		}

		/// <summary>
		/// Initializes the culture. Don't call this method.
		/// </summary>
		/// <param name="culture">Culture</param>
		/// <param name="uiCulture">UI culture</param>
		public static void __Initialize(CultureInfo culture, CultureInfo uiCulture) {
			if (culture == null || uiCulture == null)
				throw new ArgumentNullException();
			AppCulture.culture = culture;
			AppCulture.uiCulture = uiCulture;
		}

		/// <summary>
		/// Updates current thread's culture and UI culture
		/// </summary>
		public static void InitializeCulture() {
			Thread.CurrentThread.CurrentCulture = Culture;
			Thread.CurrentThread.CurrentUICulture = UICulture;
		}
	}
}
