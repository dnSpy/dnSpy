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

using System.Collections.Generic;
using System.Linq;

namespace dnSpy.Contracts.Documents {
	sealed class AnnotationsImpl : IAnnotations {
		public T AddAnnotation<T>(T annotation) where T : class {
			if (annotation != null)
				list.Add(annotation);
			return annotation;
		}

		public T Annotation<T>() where T : class => (T)list.FirstOrDefault(a => a is T);
		public IEnumerable<T> Annotations<T>() where T : class => list.OfType<T>();

		public void RemoveAnnotations<T>() where T : class {
			for (int i = list.Count - 1; i >= 0; i--) {
				if (list[i] is T)
					list.RemoveAt(i);
			}
		}
		readonly List<object> list = new List<object>();
	}
}
