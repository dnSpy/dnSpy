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

namespace dnSpy.Contracts.Text.Tagging {
	/// <summary>
	/// Tag aggregator options
	/// </summary>
	[Flags]
	public enum TagAggregatorOptions {
		/// <summary>
		/// Default behavior. The tag aggregator maps up and down through all projection buffers
		/// </summary>
		None				= 0,

		/// <summary>
		/// Map only through projection buffers that have the "projection" content type
		/// </summary>
		MapByContentType	= 1,
	}
}
