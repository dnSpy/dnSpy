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

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Target environment
	/// </summary>
	public sealed class DbgEnvironment {
		/// <summary>
		/// Gets the environment keys and values
		/// </summary>
		public KeyValuePair<string, string>[] Environment => environment.ToArray();

		readonly List<KeyValuePair<string, string>> environment;

		/// <summary>
		/// Constructor
		/// </summary>
		public DbgEnvironment() {
			environment = new List<KeyValuePair<string, string>>();
			var e = System.Environment.GetEnvironmentVariables().GetEnumerator();
			while (e.MoveNext()) {
				if (e.Key is string k && e.Value is string v)
					environment.Add(new KeyValuePair<string, string>(k, v));
			}
		}

		/// <summary>
		/// Copy constructor
		/// </summary>
		/// <param name="other">Other instance</param>
		public DbgEnvironment(DbgEnvironment other) =>
			environment = new List<KeyValuePair<string, string>>(other.environment);

		/// <summary>
		/// Clears the environment
		/// </summary>
		public void Clear() => environment.Clear();

		/// <summary>
		/// Removes a key from the environment
		/// </summary>
		/// <param name="key">Key</param>
		public void Remove(string key) {
			if (key is null)
				throw new ArgumentNullException(nameof(key));
			var env = environment;
			for (int i = env.Count - 1; i >= 0; i--) {
				if (env[i].Key == key)
					env.RemoveAt(i);
			}
		}

		/// <summary>
		/// Adds a key and value to the environment
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		public void Add(string key, string value) {
			if (key is null)
				throw new ArgumentNullException(nameof(key));
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			Remove(key);
			environment.Add(new KeyValuePair<string, string>(key, value));
		}

		/// <summary>
		/// Adds values to the environment
		/// </summary>
		/// <param name="environment">Environment</param>
		public void AddRange(IEnumerable<KeyValuePair<string, string>> environment) {
			if (environment is null)
				throw new ArgumentNullException(nameof(environment));
			this.environment.AddRange(environment);
		}
	}
}
