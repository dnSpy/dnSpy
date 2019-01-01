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
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Debugger;

namespace dnSpy.Debugger.DbgUI {
	sealed class StartDebuggingOptionsMru {
		const int MRU_SIZE = 10;
		readonly List<Info> list;
		(StartDebuggingOptions options, Guid pageGuid)? lastOptions;

		sealed class Info {
			public string Filename { get; }
			public StartDebuggingOptions Options { get; set; }
			public Guid PageGuid { get; set; }
			public Info(string filename, StartDebuggingOptions options, Guid pageGuid) {
				Filename = filename ?? throw new ArgumentNullException(nameof(filename));
				Options = options ?? throw new ArgumentNullException(nameof(options));
				PageGuid = pageGuid;
			}
		}

		public StartDebuggingOptionsMru() => list = new List<Info>(MRU_SIZE);

		Info Find(string filename) {
			foreach (var info in list) {
				if (StringComparer.Ordinal.Equals(info.Filename, filename))
					return info;
			}
			return null;
		}

		public void Add(string filename, StartDebuggingOptions options, Guid pageGuid) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			lastOptions = ((StartDebuggingOptions)options.Clone(), pageGuid);
			if (filename == null)
				return;
			var info = Find(filename);
			if (info != null) {
				bool b = list.Remove(info);
				Debug.Assert(b);
				list.Add(info);
				info.Options = options;
				info.PageGuid = pageGuid;
			}
			else {
				if (list.Count == MRU_SIZE)
					list.RemoveAt(0);
				list.Add(new Info(filename, options, pageGuid));
			}
		}

		public (StartDebuggingOptions options, Guid pageGuid)? TryGetOptions(string filename) {
			if (filename == null)
				throw new ArgumentNullException(nameof(filename));
			var info = Find(filename);
			if (info == null)
				return null;
			return (info.Options, info.PageGuid);
		}

		public (StartDebuggingOptions options, Guid pageGuid)? TryGetLastOptions() => lastOptions;
	}
}
