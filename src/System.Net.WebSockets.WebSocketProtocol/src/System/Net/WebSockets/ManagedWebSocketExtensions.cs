// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal static class ManagedWebSocketExtensions
    {
        internal static unsafe string GetString(this UTF8Encoding encoding, Span<byte> bytes)
        {
            fixed (byte* b = &MemoryMarshal.GetReference(bytes))
            {
                return encoding.GetString(b, bytes.Length);
            }
        }

        internal static Task<int> ReadAsync(this Stream stream, Memory<byte> destination, CancellationToken cancellationToken)
        {
            if (destination.TryGetArray(out ArraySegment<byte> array))
            {
                return stream.ReadAsync(array.Array, array.Offset, array.Count, cancellationToken);
            }
            else
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(destination.Length);
                return FinishReadAsync(stream.ReadAsync(buffer, 0, destination.Length, cancellationToken), buffer, destination);

                async Task<int> FinishReadAsync(Task<int> readTask, byte[] localBuffer, Memory<byte> localDestination)
                {
                    try
                    {
                        int result = await readTask.ConfigureAwait(false);
                        new Span<byte>(localBuffer, 0, result).CopyTo(localDestination.Span);
                        return result;
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(localBuffer);
                    }
                }
            }
        }
    }

    internal static class BitConverter
    {
        internal static unsafe int ToInt32(ReadOnlySpan<byte> value)
        {
            Debug.Assert(value.Length >= sizeof(int));
            return Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(value));
        }
    }
}
