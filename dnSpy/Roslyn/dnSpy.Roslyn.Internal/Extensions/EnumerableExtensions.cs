// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace dnSpy.Roslyn.Internal.Extensions {
	static class EnumerableExtensions2 {
        private static readonly Func<object, bool> s_notNullTest = x => x != null;

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source)
            where T : class
        {
            if (source == null)
            {
                return Array.Empty<T>();
            }

            return source.Where((Func<T, bool>)s_notNullTest);
        }
	}
}
