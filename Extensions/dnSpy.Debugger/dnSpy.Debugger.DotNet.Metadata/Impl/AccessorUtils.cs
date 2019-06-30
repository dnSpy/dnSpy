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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	static class AccessorUtils {
		public static DmdMethodInfo? FilterAccessor(DmdGetAccessorOptions options, DmdMethodInfo method) {
			if ((options & DmdGetAccessorOptions.All) != 0)
				return method;
			if (method is null)
				return null;
			if (method.IsPrivate && (object?)method.DeclaringType != method.ReflectedType)
				return null;
			if (method.IsPublic || (options & DmdGetAccessorOptions.NonPublic) != 0)
				return method;
			return null;
		}
	}
}
