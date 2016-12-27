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

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// .NET embedded resource
	/// </summary>
	public abstract class DotNetEmbeddedResource : StructureData {
		const string NAME = "DotNetResource";

		/// <summary>
		/// Gets the token
		/// </summary>
		public MDToken Token { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="token">Token</param>
		protected DotNetEmbeddedResource(HexBufferSpan span, uint token)
			: base(NAME, span) {
			Token = new MDToken(token);
		}

		/// <summary>
		/// Gets the size of the resource
		/// </summary>
		public abstract StructField<UInt32Data> Size { get; }

		/// <summary>
		/// Gets the resource content
		/// </summary>
		public abstract StructField<VirtualArrayData<ByteData>> Content { get; }
	}
}
