﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using ShiftIt.Http;

namespace ShiftIt.Internal.Socket
{
	/// <summary>
	/// Wrapper around a non-chunked http body stream
	/// </summary>
	public class HttpSingleResponseStream : IHttpResponseStream
	{
		Stream _source;
		long _expectedLength;
		readonly object _lock;
		long _readSoFar;

		/// <summary>
		/// Returns true if all expected data has been read.
		/// Returns false if message should have more data.
		/// 
		/// Due to frequent protocol violations, this is not 100% reliable.
		/// </summary>
		public bool Complete { get { return _readSoFar >= _expectedLength; }}

		/// <summary>
		/// Timeout for reading.
		/// </summary>
		public TimeSpan Timeout { get; set; }

		/// <summary>
		/// Wrap a non-chunked http body stream, with an expected length
		/// </summary>
		public HttpSingleResponseStream(Stream source, int expectedLength)
		{
			_lock = new Object();
			_expectedLength = expectedLength;
			_readSoFar = 0;

			Timeout = HttpClient.DefaultTimeout;
			_source = source;
		}

		/// <summary>
		/// Length that server reported for the response.
		/// Tries to give decompressed length if response is compressed.
		/// 
		/// Due to frequent protocol violations, this is not 100% reliable.
		/// </summary>
		public long ExpectedLength
		{
			get {
				return _expectedLength;
			}
		}

		/// <summary>
		/// Read string up to the declared response length.
		/// If response is chunked, this will read until an empty chunk is received.
		/// </summary>
		public string ReadStringToLength()
		{
			return Encoding.UTF8.GetString(ReadBytesToLength());
		}

		/// <summary>
		/// Read string while data is on the stream, waiting up to the timeout value for more data.
		/// If response is chunked, this will read the next chunk.
		/// </summary>
		public string ReadStringToTimeout()
		{
			return Encoding.UTF8.GetString(ReadBytesToTimeout());
		}

		/// <summary>
		/// Read raw bytes up to the declared response length.
		/// If response is chunked, this will read until an empty chunk is received.
		/// </summary>
		public byte[] ReadBytesToLength()
		{
			var ms = new MemoryStream((int)_expectedLength);
			lock (_lock)
			{
				_expectedLength = StreamTools.CopyBytesToLength(_source, ms, _expectedLength, Timeout);
			}
			_readSoFar += ms.Length;
			return ms.ToArray();
		}

		/// <summary>
		/// Read raw bytes while data is on the stream, waiting up to the timeout value for more data.
		/// </summary>
		public byte[] ReadBytesToTimeout()
		{
			var ms = new MemoryStream((int)_expectedLength);
			StreamTools.CopyBytesToTimeout(_source, ms);
			_readSoFar += ms.Length;
			return ms.ToArray();
		}

		/// <summary>
		/// Read raw bytes from the response into a buffer, returning number of bytes read.
		/// </summary>
		public int Read(byte[] buffer, int offset, int count)
		{
			return _source.Read(buffer, offset, count);
		}

		/// <summary>
		/// Dispose of the underlying stream
		/// </summary>
		~HttpSingleResponseStream()
		{
			Dispose(false);
		}

		/// <summary>
		/// Close and dispose the underlying stream
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Internal dispose
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			var sock = Interlocked.Exchange(ref _source, null);
			if (sock == null) return;
			sock.Dispose();
		}
	}
}
