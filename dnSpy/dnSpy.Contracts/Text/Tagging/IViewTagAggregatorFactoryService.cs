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

using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Contracts.Text.Tagging {
	/// <summary>
	/// A service that creates an <see cref="ITagAggregator{T}"/> for an <see cref="ITextView"/>
	/// </summary>
	public interface IViewTagAggregatorFactoryService {
		/// <summary>
		/// Creates a tag aggregator for the specified <see cref="ITextView"/> that aggregates tags of the given type
		/// </summary>
		/// <typeparam name="T">The type of tag to aggregate</typeparam>
		/// <param name="textView">Text view</param>
		/// <returns></returns>
		ITagAggregator<T> CreateTagAggregator<T>(ITextView textView) where T : ITag;

		/// <summary>
		/// Creates a tag aggregator for the specified <see cref="ITextView"/> and with the given options that aggregates tags of the given type
		/// </summary>
		/// <typeparam name="T">The type of tag to aggregate</typeparam>
		/// <param name="textView">Text view</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		ITagAggregator<T> CreateTagAggregator<T>(ITextView textView, TagAggregatorOptions options) where T : ITag;
	}
}
