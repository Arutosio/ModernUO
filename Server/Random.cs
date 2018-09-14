/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

namespace Server {
	/// <summary>
	/// Handles random number generation.
	/// </summary>
	public static class RandomImpl {
		private static readonly IRandomImpl _Random;

		static RandomImpl() {
			if (Core.Unix && File.Exists("rdrand.so")) {
				_Random = new RDRandUnix();
			} else if (Core.Is64Bit && File.Exists("rdrand64.dll")) {
				_Random = new RDRand64();
			} else if (File.Exists("rdrand.dll")) {
				_Random = new RDRand32();
			} else {
				_Random = new CSPRandom();
			}

			if (_Random is IHardwareRNG rng && !rng.IsSupported()) {
				_Random = new CSPRandom();
			}
		}

		public static bool IsHardwareRNG  => _Random is IHardwareRNG;

		public static Type Type  => _Random.GetType();

		public static int Next(int c) {
			return _Random.Next(c);
		}

		public static bool NextBool() {
			return _Random.NextBool();
		}

		public static void NextBytes(byte[] b) {
			_Random.NextBytes(b);
		}

		public static double NextDouble() {
			return _Random.NextDouble();
		}
	}

	public interface IRandomImpl {
		int Next(int c);
		bool NextBool();
		void NextBytes(byte[] b);
		double NextDouble();
	}

	public interface IHardwareRNG {
		bool IsSupported();
	}

	public sealed class SimpleRandom : IRandomImpl {
		private Random m_Random = new Random();

		public SimpleRandom() {
		}

		public int Next(int c) {
			int r;
			lock (m_Random)
				r = m_Random.Next(c);
			return r;
		}

		public bool NextBool() {
			return NextDouble() >= .5;
		}

		public void NextBytes(byte[] b) {
			lock (m_Random)
				m_Random.NextBytes(b);
		}

		public double NextDouble() {
			double r;
			lock (m_Random)
				r = m_Random.NextDouble();
			return r;
		}
	}

	public sealed class CSPRandom : IRandomImpl {
		private RNGCryptoServiceProvider _CSP = new RNGCryptoServiceProvider();

		private static int BUFFER_SIZE = 0x4000;
		private static int LARGE_REQUEST = 0x40;

		private byte[] _Working = new byte[BUFFER_SIZE];
		private byte[] _Buffer = new byte[BUFFER_SIZE];

		private int _Index = 0;

		private object _sync = new object();

		private ManualResetEvent _filled = new ManualResetEvent(false);

		public CSPRandom() {
			_CSP.GetBytes(_Working);
			ThreadPool.QueueUserWorkItem(Fill);
		}

		private void CheckSwap(int c) {
			if (_Index + c < BUFFER_SIZE)
				return;

			_filled.WaitOne();

			byte[] b = _Working;
			_Working = _Buffer;
			_Buffer = b;
			_Index = 0;

			_filled.Reset();

			ThreadPool.QueueUserWorkItem(Fill);
		}

		private void Fill(object o) {
			lock (_CSP)
				_CSP.GetBytes(_Buffer);

			_filled.Set();
		}

		private void _GetBytes(byte[] b) {
			int c = b.Length;

			lock (_sync) {
				CheckSwap(c);
				Buffer.BlockCopy(_Working, _Index, b, 0, c);
				_Index += c;
			}
		}

		private void _GetBytes(byte[] b, int offset, int count) {
			lock (_sync) {
				CheckSwap(count);
				Buffer.BlockCopy(_Working, _Index, b, offset, count);
				_Index += count;
			}
		}

		public int Next(int c) {
			return (int)(c * NextDouble());
		}

		public bool NextBool() {
			return (NextByte() & 1) == 1;
		}

		private byte NextByte() {
			lock (_sync) {
				CheckSwap(1);
				return _Working[_Index++];
			}
		}

		public void NextBytes(byte[] b) {
			int c = b.Length;

			if (c >= LARGE_REQUEST) {
				lock (_CSP)
					_CSP.GetBytes(b);
				return;
			}
			_GetBytes(b);
		}

		public unsafe double NextDouble() {
			byte[] b = new byte[8];

			if (BitConverter.IsLittleEndian) {
				b[7] = 0;
				_GetBytes(b, 0, 7);
			} else {
				b[0] = 0;
				_GetBytes(b, 1, 7);
			}

			ulong r = 0;
			fixed(byte* buf = b)
				r = *(ulong*)(&buf[0]) >> 3;

			/* double: 53 bits of significand precision
			 * ulong.MaxValue >> 11 = 9007199254740991
			 * 2^53 = 9007199254740992
			 */

			return (double)r / 9007199254740992;
		}
	}

	public sealed class RDRandUnix : IRandomImpl, IHardwareRNG
	{
		internal class SafeNativeMethods
		{
			[DllImport("rdrand.so")]
			internal static extern RDRandError rdrand_32(ref uint rand, bool retry);

			[DllImport("rdrand.so")]
			internal static extern RDRandError rdrand_get_bytes(int n, byte[] buffer);
		}

		private static int BUFFER_SIZE = 0x10000;
		private static int LARGE_REQUEST = 0x40;

		private byte[] _Working = new byte[BUFFER_SIZE];
		private byte[] _Buffer = new byte[BUFFER_SIZE];

		private int _Index = 0;

		private object _sync = new object();

		private ManualResetEvent _filled = new ManualResetEvent(false);

		public RDRandUnix()
		{
			SafeNativeMethods.rdrand_get_bytes(BUFFER_SIZE, _Working);
			ThreadPool.QueueUserWorkItem(Fill);
		}

		public bool IsSupported()
		{
			uint r = 0;
			return SafeNativeMethods.rdrand_32(ref r, true) == RDRandError.Success;
		}

		private void CheckSwap(int c)
		{
			if (_Index + c < BUFFER_SIZE)
				return;

			_filled.WaitOne();

			byte[] b = _Working;
			_Working = _Buffer;
			_Buffer = b;
			_Index = 0;

			_filled.Reset();

			ThreadPool.QueueUserWorkItem(Fill);
		}

		private void Fill(object o)
		{
			SafeNativeMethods.rdrand_get_bytes(BUFFER_SIZE, _Buffer);
			_filled.Set();
		}

		private void _GetBytes(byte[] b)
		{
			int c = b.Length;

			lock (_sync)
			{
				CheckSwap(c);
				Buffer.BlockCopy(_Working, _Index, b, 0, c);
				_Index += c;
			}
		}

		private void _GetBytes(byte[] b, int offset, int count)
		{
			lock (_sync)
			{
				CheckSwap(count);
				Buffer.BlockCopy(_Working, _Index, b, offset, count);
				_Index += count;
			}
		}

		public int Next(int c)
		{
			return (int)(c * NextDouble());
		}

		public bool NextBool()
		{
			return (NextByte() & 1) == 1;
		}

		private byte NextByte()
		{
			lock (_sync)
			{
				CheckSwap(1);
				return _Working[_Index++];
			}
		}

		public void NextBytes(byte[] b)
		{
			int c = b.Length;

			if (c >= LARGE_REQUEST)
			{
				SafeNativeMethods.rdrand_get_bytes(c, b);
				return;
			}
			_GetBytes(b);
		}

		public unsafe double NextDouble()
		{
			byte[] b = new byte[8];

			if (BitConverter.IsLittleEndian)
			{
				b[7] = 0;
				_GetBytes(b, 0, 7);
			}
			else
			{
				b[0] = 0;
				_GetBytes(b, 1, 7);
			}

			ulong r = 0;
			fixed (byte* buf = b)
				r = *(ulong*)(&buf[0]) >> 3;

			/* double: 53 bits of significand precision
			 * ulong.MaxValue >> 11 = 9007199254740991
			 * 2^53 = 9007199254740992
			 */

			return (double)r / 9007199254740992;
		}
	}

	public sealed class RDRand32 : IRandomImpl, IHardwareRNG {
		internal class SafeNativeMethods
		{
			[DllImport("rdrand32")]
			internal static extern RDRandError rdrand_32(ref uint rand, bool retry);

			[DllImport("rdrand32")]
			internal static extern RDRandError rdrand_get_bytes(int n, byte[] buffer);
		}

		private static int BUFFER_SIZE = 0x10000;
		private static int LARGE_REQUEST = 0x40;

		private byte[] _Working = new byte[BUFFER_SIZE];
		private byte[] _Buffer = new byte[BUFFER_SIZE];

		private int _Index = 0;

		private object _sync = new object();

		private ManualResetEvent _filled = new ManualResetEvent(false);

		public RDRand32() {
			SafeNativeMethods.rdrand_get_bytes(BUFFER_SIZE, _Working);
			ThreadPool.QueueUserWorkItem(Fill);
		}

		public bool IsSupported() {
			uint r = 0;
			return SafeNativeMethods.rdrand_32(ref r, true) == RDRandError.Success;
		}

		private void CheckSwap(int c) {
			if (_Index + c < BUFFER_SIZE)
				return;

			_filled.WaitOne();

			byte[] b = _Working;
			_Working = _Buffer;
			_Buffer = b;
			_Index = 0;

			_filled.Reset();

			ThreadPool.QueueUserWorkItem(Fill);
		}

		private void Fill(object o) {
			SafeNativeMethods.rdrand_get_bytes(BUFFER_SIZE, _Buffer);
			_filled.Set();
		}

		private void _GetBytes(byte[] b) {
			int c = b.Length;

			lock (_sync) {
				CheckSwap(c);
				Buffer.BlockCopy(_Working, _Index, b, 0, c);
				_Index += c;
			}
		}

		private void _GetBytes(byte[] b, int offset, int count) {
			lock (_sync) {
				CheckSwap(count);
				Buffer.BlockCopy(_Working, _Index, b, offset, count);
				_Index += count;
			}
		}

		public int Next(int c) {
			return (int)(c * NextDouble());
		}

		public bool NextBool() {
			return (NextByte() & 1) == 1;
		}

		private byte NextByte() {
			lock (_sync) {
				CheckSwap(1);
				return _Working[_Index++];
			}
		}

		public void NextBytes(byte[] b) {
			int c = b.Length;

			if (c >= LARGE_REQUEST) {
				SafeNativeMethods.rdrand_get_bytes(c, b);
				return;
			}
			_GetBytes(b);
		}

		public unsafe double NextDouble() {
			byte[] b = new byte[8];

			if (BitConverter.IsLittleEndian) {
				b[7] = 0;
				_GetBytes(b, 0, 7);
			} else {
				b[0] = 0;
				_GetBytes(b, 1, 7);
			}

			ulong r = 0;
			fixed(byte* buf = b)
				r = *(ulong*)(&buf[0]) >> 3;

			/* double: 53 bits of significand precision
			 * ulong.MaxValue >> 11 = 9007199254740991
			 * 2^53 = 9007199254740992
			 */

			return (double)r / 9007199254740992;
		}
	}

	public sealed class RDRand64 : IRandomImpl, IHardwareRNG {
		internal class SafeNativeMethods {
			[DllImport("rdrand64")]
			internal static extern RDRandError rdrand_64(ref ulong rand, bool retry);

			[DllImport("rdrand64")]
			internal static extern RDRandError rdrand_get_bytes(int n, byte[] buffer);
		}

		private static int BUFFER_SIZE = 0x10000;
		private static int LARGE_REQUEST = 0x40;

		private byte[] _Working = new byte[BUFFER_SIZE];
		private byte[] _Buffer = new byte[BUFFER_SIZE];

		private int _Index = 0;

		private object _sync = new object();

		private ManualResetEvent _filled = new ManualResetEvent(false);

		public RDRand64() {
			SafeNativeMethods.rdrand_get_bytes(BUFFER_SIZE, _Working);
			ThreadPool.QueueUserWorkItem(Fill);
		}

		public bool IsSupported() {
			ulong r = 0;
			return SafeNativeMethods.rdrand_64(ref r, true) == RDRandError.Success;
		}

		private void CheckSwap(int c) {
			if (_Index + c < BUFFER_SIZE)
				return;

			_filled.WaitOne();

			byte[] b = _Working;
			_Working = _Buffer;
			_Buffer = b;
			_Index = 0;

			_filled.Reset();

			ThreadPool.QueueUserWorkItem(Fill);
		}

		private void Fill(object o) {
			SafeNativeMethods.rdrand_get_bytes(BUFFER_SIZE, _Buffer);
			_filled.Set();
		}

		private void _GetBytes(byte[] b) {
			int c = b.Length;

			lock (_sync) {
				CheckSwap(c);
				Buffer.BlockCopy(_Working, _Index, b, 0, c);
				_Index += c;
			}
		}

		private void _GetBytes(byte[] b, int offset, int count) {
			lock (_sync) {
				CheckSwap(count);
				Buffer.BlockCopy(_Working, _Index, b, offset, count);
				_Index += count;
			}
		}

		public int Next(int c) {
			return (int)(c * NextDouble());
		}

		public bool NextBool() {
			return (NextByte() & 1) == 1;
		}

		private byte NextByte() {
			lock (_sync) {
				CheckSwap(1);
				return _Working[_Index++];
			}
		}

		public void NextBytes(byte[] b) {
			int c = b.Length;

			if (c >= LARGE_REQUEST) {
				SafeNativeMethods.rdrand_get_bytes(c, b);
				return;
			}
			_GetBytes(b);
		}

		public unsafe double NextDouble() {
			byte[] b = new byte[8];

			if (BitConverter.IsLittleEndian) {
				b[7] = 0;
				_GetBytes(b, 0, 7);
			} else {
				b[0] = 0;
				_GetBytes(b, 1, 7);
			}

			ulong r = 0;
			fixed(byte* buf = b)
				r = *(ulong*)(&buf[0]) >> 3;

			/* double: 53 bits of significand precision
			 * ulong.MaxValue >> 11 = 9007199254740991
			 * 2^53 = 9007199254740992
			 */

			return (double)r / 9007199254740992;
		}
	}

	public enum RDRandError : int {
		Unknown = -4,
		Unsupported = -3,
		Supported = -2,
		NotReady = -1,

		Failure = 0,

		Success = 1,
	}
}
