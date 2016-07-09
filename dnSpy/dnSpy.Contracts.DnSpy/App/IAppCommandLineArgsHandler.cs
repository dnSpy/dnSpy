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

namespace dnSpy.Contracts.App {
	/// <summary>
	/// Gets notified when new command line arguments have been passed to dnSpy
	/// </summary>
	public interface IAppCommandLineArgsHandler {
		/// <summary>
		/// Order
		/// </summary>
		double Order { get; }

		/// <summary>
		/// Called whenever there are new command line arguments
		/// </summary>
		/// <param name="args">Command line arguments</param>
		void OnNewArgs(IAppCommandLineArgs args);
	}
}
