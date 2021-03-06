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
	using FoundationDB.Client.Converters;
	using System;

	/// <summary>Very simple serializer that uses FdbConverters to convert values of type <typeparamref name="T"/> from/to Slice</summary>
	/// <typeparam name="T">Type of the value to serialize/deserialize</typeparam>
	public sealed class FdbGenericSliceSerializer<T> : ISliceSerializer<T>
	{
		/// <summary>Singleton that can convert between <typeparam name="T"> and Slice</summary>
		public static readonly ISliceSerializer<T> Default = new FdbGenericSliceSerializer<T>();

		private FdbGenericSliceSerializer()
		{ }

		public Slice Serialize(T value)
		{
			return FdbConverters.Convert<T, Slice>(value);
		}

		public T Deserialize(Slice slice, T missing)
		{
			if (slice.IsNullOrEmpty) return missing;
			return FdbConverters.Convert<Slice, T>(slice);
		}
	}

	/// <summary>Very simple serializer that uses FdbConverters to convert values of type <typeparamref name="T"/> from/to Slice</summary>
	/// <typeparam name="T">Type of the value to serialize/deserialize</typeparam>
	public sealed class FdbSliceSerializer<T> : ISliceSerializer<T>
	{

		private Func<T, Slice> Serializer { get; set; }

		private Func<Slice, T> Deserializer { get; set; }

		public FdbSliceSerializer(Func<T, Slice> serialize, Func<Slice, T> deserialize)
		{
			this.Serializer = serialize;
			this.Deserializer = deserialize;
		}

		public Slice Serialize(T value)
		{
			return value == null ? Slice.Nil : this.Serializer(value);
		}

		public T Deserialize(Slice slice, T missing)
		{
			if (slice.IsNullOrEmpty) return missing;
			return this.Deserializer(slice);
		}
	}

}
