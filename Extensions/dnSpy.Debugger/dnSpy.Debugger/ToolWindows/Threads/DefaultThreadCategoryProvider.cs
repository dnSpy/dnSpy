/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Images;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.Threads {
	[ExportThreadCategoryProvider]
	sealed class DefaultThreadCategoryProvider : ThreadCategoryProvider {
		public override ThreadCategoryInfo? GetCategory(string kind) {
			switch (kind) {
			case PredefinedThreadKinds.Unknown:
				return new ThreadCategoryInfo(DsImages.QuestionMark, dnSpy_Debugger_Resources.ThreadType_Unknown);
			case PredefinedThreadKinds.Main:
				return new ThreadCategoryInfo(DsImages.Thread, dnSpy_Debugger_Resources.ThreadType_Main);
			case PredefinedThreadKinds.ThreadPool:
				return new ThreadCategoryInfo(DsImages.Process, dnSpy_Debugger_Resources.ThreadType_ThreadPool);
			case PredefinedThreadKinds.WorkerThread:
				return new ThreadCategoryInfo(DsImages.Process, dnSpy_Debugger_Resources.ThreadType_Worker);
			case PredefinedThreadKinds.Terminated:
				return new ThreadCategoryInfo(DsImages.QuestionMark, dnSpy_Debugger_Resources.ThreadType_Terminated);
			case PredefinedThreadKinds.GC:
				return new ThreadCategoryInfo(DsImages.Process, "GC");// No need to localize it
			case PredefinedThreadKinds.Finalizer:
				return new ThreadCategoryInfo(DsImages.Process, "Finalizer");// No need to localize it
			}
			return null;
		}
	}
}
