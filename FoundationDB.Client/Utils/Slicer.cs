﻿#region BSD Licence
/* Copyright (c) 2013, Doxense SARL
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
	* Redistributions of source code must retain the above copyright
	  notice, this list of conditions and the following disclaimer.
	* Redistributions in binary form must reproduce the above copyright
	  notice, this list of conditions and the following disclaimer in the
	  documentation and/or other materials provided with the distribution.
	* Neither the name of Doxense nor the
	  names of its contributors may be used to endorse or promote products
	  derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion

namespace FoundationDB.Client
{
	using System;

	/// <summary>Helper class that holds the internal state used to parse tuples from slices</summary>
	public struct Slicer
	{

		/// <summary>Buffer containing the tuple being parsed</summary>
		public readonly Slice Buffer;

		/// <summary>Current position inside the buffer</summary>
		public int Position;

		public Slicer(Slice buffer)
		{
			this.Buffer = buffer;
			this.Position = 0;
		}

		/// <summary>Returns true if there are more bytes to parse</summary>
		public bool HasMore { get { return this.Position < this.Buffer.Count; } }

		/// <summary>Ensure that there are at least <paramref name="count"/> bytes remaining in the buffer</summary>
		public void EnsureBytes(int count)
		{
			if (count < 0 || count > this.Buffer.Count - this.Position) throw new ArgumentOutOfRangeException("count");
		}

		/// <summary>Return the value of the next byte in the buffer, or -1 if we reached the end</summary>
		public int PeekByte()
		{
			int p = this.Position;
			return p < this.Buffer.Count ? this.Buffer[p] : -1;
		}

		/// <summary>Skip the next <paramref name="count"/> bytes of the buffer</summary>
		public void Skip(int count)
		{
			EnsureBytes(count);

			this.Position += count;
		}

		/// <summary>Read the next byte from the buffer</summary>
		public byte ReadByte()
		{
			EnsureBytes(1);

			int p = this.Position;
			byte b = this.Buffer[p];
			this.Position = p + 1;
			return b;
		}

		/// <summary>Read the next <paramref name="count"/> bytes from the buffer</summary>
		public Slice ReadBytes(int count)
		{
			EnsureBytes(count);

			int p = this.Position;
			this.Position = p + count;
			return this.Buffer.Substring(p, count);
		}

		/// <summary>Read an encoded nul-terminated byte array from the buffer</summary>
		public Slice ReadByteString()
		{
			var buffer = this.Buffer.Array;
			int start = this.Buffer.Offset + this.Position;
			int p = start;
			int end = this.Buffer.Offset + this.Buffer.Count;

			while (p < end)
			{
				byte b = buffer[p++];
				if (b == 0)
				{
					//TODO: decode \0\xFF ?
					if (p < end && buffer[p] == 0xFF)
					{
						// skip the next byte and continue
						p++;
						continue;
					}

					this.Position = p - this.Buffer.Offset;
					return new Slice(buffer, start, p - start);
				}
			}

			throw new FormatException("Truncated byte string (expected terminal NUL not found)");
		}

	}

}
