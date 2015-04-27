//
// support.cs: Support routines to work around the fact that System.Reflection.Emit
// can not introspect types that are being constructed
//
// Author:
//   Miguel de Icaza (miguel@ximian.com)
//   Marek Safar (marek.safar@gmail.com)
//
// Copyright 2001 Ximian, Inc (http://www.ximian.com)
// Copyright 2003-2009 Novell, Inc
// Copyright 2011 Xamarin Inc
//

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Mono.CSharp {

	sealed class ReferenceEquality<T> : IEqualityComparer<T> where T : class
	{
		public static readonly IEqualityComparer<T> Default = new ReferenceEquality<T> ();

		private ReferenceEquality ()
		{
		}

		public bool Equals (T x, T y)
		{
			return ReferenceEquals (x, y);
		}

		public int GetHashCode (T obj)
		{
			return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode (obj);
		}
	}
#if !NET_4_0 && !MOBILE_DYNAMIC
	public class Tuple<T1, T2> : IEquatable<Tuple<T1, T2>>
	{
		public Tuple (T1 item1, T2 item2)
		{
			Item1 = item1;
			Item2 = item2;
		}

		public T1 Item1 { get; private set; }
		public T2 Item2 { get; private set; }

		public override int GetHashCode ()
		{
			return ((object)Item1 ?? 0) .GetHashCode () ^ ((object)Item2 ?? 0).GetHashCode ();
		}

		#region IEquatable<Tuple<T1,T2>> Members

		public bool Equals (Tuple<T1, T2> other)
		{
			return EqualityComparer<T1>.Default.Equals (Item1, other.Item1) &&
				EqualityComparer<T2>.Default.Equals (Item2, other.Item2);
		}

		#endregion
	}

	public class Tuple<T1, T2, T3> : IEquatable<Tuple<T1, T2, T3>>
	{
		public Tuple (T1 item1, T2 item2, T3 item3)
		{
			Item1 = item1;
			Item2 = item2;
			Item3 = item3;
		}

		public T1 Item1 { get; private set; }
		public T2 Item2 { get; private set; }
		public T3 Item3 { get; private set; }

		public override int GetHashCode ()
		{
			return Item1.GetHashCode () ^ Item2.GetHashCode () ^ Item3.GetHashCode ();
		}

		#region IEquatable<Tuple<T1,T2>> Members

		public bool Equals (Tuple<T1, T2, T3> other)
		{
			return EqualityComparer<T1>.Default.Equals (Item1, other.Item1) &&
				EqualityComparer<T2>.Default.Equals (Item2, other.Item2) &&
				EqualityComparer<T3>.Default.Equals (Item3, other.Item3);
		}

		#endregion
	}

	static class Tuple
	{
		public static Tuple<T1, T2> Create<T1, T2> (T1 item1, T2 item2)
		{
			return new Tuple<T1, T2> (item1, item2);
		}

		public static Tuple<T1, T2, T3> Create<T1, T2, T3> (T1 item1, T2 item2, T3 item3)
		{
			return new Tuple<T1, T2, T3> (item1, item2, item3);
		}
	}
#endif

	static class ArrayComparer
	{
		public static bool IsEqual<T> (T[] array1, T[] array2)
		{
			if (array1 == null || array2 == null)
				return array1 == array2;

			var eq = EqualityComparer<T>.Default;

			for (int i = 0; i < array1.Length; ++i) {
				if (!eq.Equals (array1[i], array2[i])) {
					return false;
				}
			}

			return true;
		}
	}
#if !FULL_AST
	/// <summary>
	///   This is an arbitrarily seekable StreamReader wrapper.
	///
	///   It uses a self-tuning buffer to cache the seekable data,
	///   but if the seek is too far, it may read the underly
	///   stream all over from the beginning.
	/// </summary>
	public class SeekableStreamReader : IDisposable
	{
		public const int DefaultReadAheadSize =
			4096 / 2;

		StreamReader reader;
		Stream stream;

		char[] buffer;
		int read_ahead_length;	// the length of read buffer
		int buffer_start;       // in chars
		int char_count;         // count of filled characters in buffer[]
		int pos;                // index into buffer[]

		public SeekableStreamReader (Stream stream, Encoding encoding, char[] sharedBuffer = null)
		{
			this.stream = stream;
			this.buffer = sharedBuffer;

			InitializeStream (DefaultReadAheadSize);
			reader = new StreamReader (stream, encoding, true);
		}

		public void Dispose ()
		{
			// Needed to release stream reader buffers
			reader.Dispose ();
		}

		void InitializeStream (int read_length_inc)
		{
			read_ahead_length += read_length_inc;

			int required_buffer_size = read_ahead_length * 2;

			if (buffer == null || buffer.Length < required_buffer_size)
				buffer = new char [required_buffer_size];

			stream.Position = 0;
			buffer_start = char_count = pos = 0;
		}

		/// <remarks>
		///   This value corresponds to the current position in a stream of characters.
		///   The StreamReader hides its manipulation of the underlying byte stream and all
		///   character set/decoding issues.  Thus, we cannot use this position to guess at
		///   the corresponding position in the underlying byte stream even though there is
		///   a correlation between them.
		/// </remarks>
		public int Position {
			get {
				return buffer_start + pos;
			}

			set {
				//
				// If the lookahead was too small, re-read from the beginning. Increase the buffer size while we're at it
				// This should never happen until we are parsing some weird source code
				//
				if (value < buffer_start) {
					InitializeStream (read_ahead_length);

					//
					// Discard buffer data after underlying stream changed position
					// Cannot use handy reader.DiscardBufferedData () because it for
					// some strange reason resets encoding as well
					//
					reader = new StreamReader (stream, reader.CurrentEncoding, true);
				}

				while (value > buffer_start + char_count) {
					pos = char_count;
					if (!ReadBuffer ())
						throw new InternalErrorException ("Seek beyond end of file: " + (buffer_start + char_count - value));
				}

				pos = value - buffer_start;
			}
		}

		bool ReadBuffer ()
		{
			int slack = buffer.Length - char_count;

			//
			// read_ahead_length is only half of the buffer to deal with
			// reads ahead and moves back without re-reading whole buffer
			//
			if (slack <= read_ahead_length) {
				//
				// shift the buffer to make room for read_ahead_length number of characters
				//
				int shift = read_ahead_length - slack;
				Array.Copy (buffer, shift, buffer, 0, char_count - shift);

				// Update all counters
				pos -= shift;
				char_count -= shift;
				buffer_start += shift;
				slack += shift;
			}

			char_count += reader.Read (buffer, char_count, slack);

			return pos < char_count;
		}
		
		public char GetChar (int position)
		{
			if (buffer_start <= position && position < buffer.Length)
				return buffer[position];
			return '\0';
		}
		
		public char[] ReadChars (int fromPosition, int toPosition)
		{
			char[] chars = new char[toPosition - fromPosition];
			if (buffer_start <= fromPosition && toPosition <= buffer_start + buffer.Length) {
				Array.Copy (buffer, fromPosition - buffer_start, chars, 0, chars.Length);
			} else {
				throw new NotImplementedException ();
			}

			return chars;
		}

		public int Peek ()
		{
			if ((pos >= char_count) && !ReadBuffer ())
				return -1;

			return buffer [pos];
		}

		public int Read ()
		{
			if ((pos >= char_count) && !ReadBuffer ())
				return -1;

			return buffer [pos++];
		}
	}
#endif

	public class UnixUtils {
		[System.Runtime.InteropServices.DllImport ("libc", EntryPoint="isatty")]
		extern static int _isatty (int fd);
			
		public static bool isatty (int fd)
		{
			try {
				return _isatty (fd) == 1;
			} catch {
				return false;
			}
		}
	}

	/// <summary>
	///   An exception used to terminate the compiler resolution phase and provide completions
	/// </summary>
	/// <remarks>
	///   This is thrown when we want to return the completions or
	///   terminate the completion process by AST nodes used in
	///   the completion process.
	/// </remarks>
	public class CompletionResult : Exception {
		string [] result;
		string base_text;
		
		public CompletionResult (string base_text, string [] res)
		{
			if (base_text == null)
				throw new ArgumentNullException ("base_text");
			this.base_text = base_text;

			result = res;
			Array.Sort (result);
		}

		public string [] Result {
			get {
				return result;
			}
		}

		public string BaseText {
			get {
				return base_text;
			}
		}
	}

	struct TypeNameParser
	{
		internal static string Escape(string name)
		{
			if (name == null) {
				return null;
			}
			StringBuilder sb = null;
			for (int pos = 0; pos < name.Length; pos++) {
				char c = name[pos];
				switch (c) {
					case '\\':
					case '+':
					case ',':
					case '[':
					case ']':
					case '*':
					case '&':
						if (sb == null) {
							sb = new StringBuilder(name, 0, pos, name.Length + 3);
						}
						sb.Append("\\").Append(c);
						break;
					default:
						if (sb != null) {
							sb.Append(c);
						}
						break;
				}
			}
			return sb != null ? sb.ToString() : name;
		}
	}
}
