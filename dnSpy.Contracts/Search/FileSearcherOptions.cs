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

using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.Contracts.Search {
	/// <summary>
	/// <see cref="IFileSearcher"/> options
	/// </summary>
	public sealed class FileSearcherOptions {
		/// <summary>
		/// Default number of results to return
		/// </summary>
		public static readonly int DEFAULT_MAX_RESULTS = 1000;

		/// <summary>
		/// Max results to return
		/// </summary>
		public int MaxResults { get; set; }

		/// <summary>
		/// Gets the <see cref="ISearchComparer"/> instance
		/// </summary>
		public ISearchComparer SearchComparer { get; set; }

		/// <summary>
		/// Filter
		/// </summary>
		public IFileTreeNodeFilter Filter { get; set; }

		/// <summary>
		/// Searches decompiled data, eg. decompiled XAML
		/// </summary>
		public bool SearchDecompiledData { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public FileSearcherOptions() {
			this.MaxResults = DEFAULT_MAX_RESULTS;
			this.SearchDecompiledData = true;
		}

		/// <summary>
		/// Clones this instance
		/// </summary>
		/// <returns></returns>
		public FileSearcherOptions Clone() {
			return CopyTo(new FileSearcherOptions());
		}

		/// <summary>
		/// Copies this instance to <paramref name="other"/> and returns it
		/// </summary>
		/// <param name="other">Destination</param>
		/// <returns></returns>
		public FileSearcherOptions CopyTo(FileSearcherOptions other) {
			other.MaxResults = MaxResults;
			other.SearchComparer = SearchComparer;
			other.Filter = Filter;
			other.SearchDecompiledData = SearchDecompiledData;
			return other;
		}
	}
}
