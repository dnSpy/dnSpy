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

namespace dnSpy.Decompiler.MSBuild {
	interface IMSBuildProgressListener {
		/// <summary>
		/// Called to initialize max progress
		/// </summary>
		/// <param name="maxProgress">Max progress</param>
		void SetMaxProgress(int maxProgress);

		/// <summary>
		/// Sets current progress. Since it can be called from multiple threads at the same time,
		/// the callee must use the largest <paramref name="progress"/> value it receives and ignore
		/// the other values.
		/// </summary>
		/// <param name="progress">Current progress. Must be ignored by the callee if it's lower
		/// than the value of a previous call to this method.</param>
		void SetProgress(int progress);
	}

	sealed class NoMSBuildProgressListener : IMSBuildProgressListener {
		public static readonly NoMSBuildProgressListener Instance = new NoMSBuildProgressListener();

		public void SetMaxProgress(int maxProgress) { }
		public void SetProgress(int progress) { }
	}
}
