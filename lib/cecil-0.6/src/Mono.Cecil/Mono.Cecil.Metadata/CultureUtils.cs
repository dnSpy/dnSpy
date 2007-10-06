//
// CultureUtils.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace Mono.Cecil.Metadata {

	using System;
	using System.Collections;
	using System.Globalization;

	class CultureUtils {

		static IDictionary m_cultures;

		CultureUtils ()
		{
		}

		static void LoadCultures ()
		{
			if (m_cultures != null)
				return;

#if CF_1_0 || CF_2_0
			CultureInfo [] cultures = new CultureInfo [0];
#else
			CultureInfo [] cultures = CultureInfo.GetCultures (CultureTypes.AllCultures);
#endif
			m_cultures = new Hashtable (cultures.Length + 2);

			foreach (CultureInfo ci in cultures)
				if (!m_cultures.Contains (ci.Name))
					m_cultures.Add (ci.Name, ci);

			if (!m_cultures.Contains (string.Empty))
				m_cultures.Add (string.Empty, CultureInfo.InvariantCulture);

			m_cultures.Add ("neutral", CultureInfo.InvariantCulture);
		}

		public static bool IsValid (string culture)
		{
			if (culture == null)
				throw new ArgumentNullException ("culture");

			LoadCultures ();

			return m_cultures.Contains (culture);
		}

		public static CultureInfo GetCultureInfo (string culture)
		{
			if (IsValid (culture))
				return m_cultures [culture] as CultureInfo;

			return CultureInfo.InvariantCulture;
		}
	}
}
