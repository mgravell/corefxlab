// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines.Testing;
using System.Linq;
using System.Text;

namespace System.IO.Pipelines.Tests
{
    internal abstract class TestBufferFactory
    {
        public static TestBufferFactory Array { get; } = new ArrayTestBufferFactory();
        public static TestBufferFactory OwnedMemory { get; } = new OwnedMemoryTestBufferFactory();
        public static TestBufferFactory SingleSegment { get; } = new SingleSegmentTestBufferFactory();
        public static TestBufferFactory SegmentPerByte { get; } = new BytePerSegmentTestBufferFactory();

        public abstract ReadOnlyBuffer CreateOfSize(int size);
        public abstract ReadOnlyBuffer CreateWithContent(byte[] data);

        public ReadOnlyBuffer CreateWithContent(string data)
        {
            return CreateWithContent(Encoding.ASCII.GetBytes(data));
        }

        internal class ArrayTestBufferFactory : TestBufferFactory
        {
            public override ReadOnlyBuffer CreateOfSize(int size)
            {
                return new ReadOnlyBuffer(new byte[size + 20], 10, size);
            }

            public override ReadOnlyBuffer CreateWithContent(byte[] data)
            {
                var startSegment = new byte[data.Length + 20];
                System.Array.Copy(data, 0, startSegment, 10, data.Length);
                return new ReadOnlyBuffer(startSegment, 10, data.Length);
            }
        }

        internal class OwnedMemoryTestBufferFactory : TestBufferFactory
        {
            public override ReadOnlyBuffer CreateOfSize(int size)
            {
                return new ReadOnlyBuffer(new OwnedArray<byte>(size + 20), 10, size);
            }

            public override ReadOnlyBuffer CreateWithContent(byte[] data)
            {
                var startSegment = new byte[data.Length + 20];
                System.Array.Copy(data, 0, startSegment, 10, data.Length);
                return new ReadOnlyBuffer(new OwnedArray<byte>(startSegment), 10, data.Length);
            }
        }

        internal class SingleSegmentTestBufferFactory: TestBufferFactory
        {
            public override ReadOnlyBuffer CreateOfSize(int size)
            {
                return BufferUtilities.CreateBuffer(size);
            }

            public override ReadOnlyBuffer CreateWithContent(byte[] data)
            {
                return BufferUtilities.CreateBuffer(data);
            }
        }

        internal class BytePerSegmentTestBufferFactory: TestBufferFactory
        {
            public override ReadOnlyBuffer CreateOfSize(int size)
            {
                return CreateWithContent(new byte[size]);
            }

            public override ReadOnlyBuffer CreateWithContent(byte[] data)
            {
                var segments = new List<byte[]>();

                segments.Add(System.Array.Empty<byte>());
                foreach (var b in data)
                {
                    segments.Add(new [] { b });
                    segments.Add(System.Array.Empty<byte>());
                }

                return BufferUtilities.CreateBuffer(segments.ToArray());
            }
        }
    }
}
