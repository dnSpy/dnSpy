// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace dnSpy.Roslyn.Internal
{
    internal static partial class EnumerableExtensions
    {
        public static IEnumerable<T> Do<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            // perf optimization. try to not use enumerator if possible
            var list = source as IList<T>;
            if (list != null)
            {
                for (int i = 0, count = list.Count; i < count; i++)
                {
                    action(list[i]);
                }
            }
            else
            {
                foreach (var value in source)
                {
                    action(value);
                }
            }

            return source;
        }

        public static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ReadOnlyCollection<T>(source.ToList());
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T value)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.ConcatWorker(value);
        }

        private static IEnumerable<T> ConcatWorker<T>(this IEnumerable<T> source, T value)
        {
            foreach (var v in source)
            {
                yield return v;
            }

            yield return value;
        }

        public static bool SetEquals<T>(this IEnumerable<T> source1, IEnumerable<T> source2, IEqualityComparer<T> comparer)
        {
            if (source1 == null)
            {
                throw new ArgumentNullException(nameof(source1));
            }

            if (source2 == null)
            {
                throw new ArgumentNullException(nameof(source2));
            }

            return source1.ToSet(comparer).SetEquals(source2);
        }

        public static bool SetEquals<T>(this IEnumerable<T> source1, IEnumerable<T> source2)
        {
            if (source1 == null)
            {
                throw new ArgumentNullException(nameof(source1));
            }

            if (source2 == null)
            {
                throw new ArgumentNullException(nameof(source2));
            }

            return source1.ToSet().SetEquals(source2);
        }

        public static ISet<T> ToSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new HashSet<T>(source, comparer);
        }

        public static ISet<T> ToSet<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source as ISet<T> ?? new HashSet<T>(source);
        }

        public static T? FirstOrNullable<T>(this IEnumerable<T> source)
            where T : struct
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Cast<T?>().FirstOrDefault();
        }

        public static T? FirstOrNullable<T>(this IEnumerable<T> source, Func<T, bool> predicate)
            where T : struct
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Cast<T?>().FirstOrDefault(v => predicate(v.Value));
        }

        public static T? LastOrNullable<T>(this IEnumerable<T> source)
            where T : struct
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Cast<T?>().LastOrDefault();
        }

        public static bool IsSingle<T>(this IEnumerable<T> list)
        {
            using (var enumerator = list.GetEnumerator())
            {
                return enumerator.MoveNext() && !enumerator.MoveNext();
            }
        }

        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            var readOnlyCollection = source as IReadOnlyCollection<T>;
            if (readOnlyCollection != null)
            {
                return readOnlyCollection.Count == 0;
            }

            var genericCollection = source as ICollection<T>;
            if (genericCollection != null)
            {
                return genericCollection.Count == 0;
            }

            var collection = source as ICollection;
            if (collection != null)
            {
                return collection.Count == 0;
            }

            var str = source as string;
            if (str != null)
            {
                return str.Length == 0;
            }

            foreach (var t in source)
            {
                return false;
            }

            return true;
        }

        public static bool IsEmpty<T>(this IReadOnlyCollection<T> source)
        {
            return source.Count == 0;
        }

        public static bool IsEmpty<T>(this ICollection<T> source)
        {
            return source.Count == 0;
        }

        public static bool IsEmpty(this string source)
        {
            return source.Length == 0;
        }

        /// <remarks>
        /// This method is necessary to avoid an ambiguity between <see cref="IsEmpty{T}(IReadOnlyCollection{T})"/> and <see cref="IsEmpty{T}(ICollection{T})"/>.
        /// </remarks>
        public static bool IsEmpty<T>(this T[] source)
        {
            return source.Length == 0;
        }

        /// <remarks>
        /// This method is necessary to avoid an ambiguity between <see cref="IsEmpty{T}(IReadOnlyCollection{T})"/> and <see cref="IsEmpty{T}(ICollection{T})"/>.
        /// </remarks>
        public static bool IsEmpty<T>(this List<T> source)
        {
            return source.Count == 0;
        }

        private static readonly Func<object, bool> s_notNullTest = x => x != null;

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source)
            where T : class
        {
            if (source == null)
            {
                return Enumerable.Empty<T>();
            }

            return source.Where((Func<T, bool>)s_notNullTest);
        }

        public static bool All(this IEnumerable<bool> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            foreach (var b in source)
            {
                if (!b)
                {
                    return false;
                }
            }

            return true;
        }

        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> sequence)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            return sequence.SelectMany(s => s);
        }

        public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source, IComparer<T> comparer)
        {
            return source.OrderBy(Functions<T>.Identity, comparer);
        }

        public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source, Comparison<T> compare)
        {
            return source.OrderBy(Comparer<T>.Create(compare));
        }

        public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source) where T : IComparable<T>
        {
            return source.OrderBy(Comparisons<T>.Comparer);
        }

        public static IOrderedEnumerable<T> ThenBy<T>(this IOrderedEnumerable<T> source, IComparer<T> comparer)
        {
            return source.ThenBy(Functions<T>.Identity, comparer);
        }

        public static IOrderedEnumerable<T> ThenBy<T>(this IOrderedEnumerable<T> source, Comparison<T> compare)
        {
            return source.ThenBy(Comparer<T>.Create(compare));
        }

        public static IOrderedEnumerable<T> ThenBy<T>(this IOrderedEnumerable<T> source) where T : IComparable<T>
        {
            return source.ThenBy(Comparisons<T>.Comparer);
        }

        private static class Comparisons<T> where T : IComparable<T>
        {
            public static readonly Comparison<T> CompareTo = (t1, t2) => t1.CompareTo(t2);

            public static readonly IComparer<T> Comparer = Comparer<T>.Create(CompareTo);
        }

        private static class Functions<T>
        {
            public static readonly Func<T, T> Identity = t => t;
        }

        public static bool IsSorted<T>(this IEnumerable<T> enumerable, IComparer<T> comparer)
        {
            using (var e = enumerable.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    return true;
                }

                var previous = e.Current;
                while (e.MoveNext())
                {
                    if (comparer.Compare(previous, e.Current) > 0)
                    {
                        return false;
                    }

                    previous = e.Current;
                }

                return true;
            }
        }

        public static bool SequenceEqual<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, T, bool> comparer)
        {
            Debug.Assert(comparer != null);

            if (first == second)
            {
                return true;
            }

            if (first == null || second == null)
            {
                return false;
            }

            using (var enumerator = first.GetEnumerator())
            using (var enumerator2 = second.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (!enumerator2.MoveNext() || !comparer(enumerator.Current, enumerator2.Current))
                    {
                        return false;
                    }
                }

                if (enumerator2.MoveNext())
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Contains<T>(this IEnumerable<T> sequence, Func<T, bool> predicate)
        {
            return sequence.Any(predicate);
        }

        public static bool Contains(this IEnumerable<string> sequence, string s)
        {
            foreach (var item in sequence)
            {
                if (item == s)
                {
                    return true;
                }
            }

            return false;
        }

        public static IComparer<T> ToComparer<T>(this Comparison<T> comparison)
        {
            return Comparer<T>.Create(comparison);
        }
    }
}