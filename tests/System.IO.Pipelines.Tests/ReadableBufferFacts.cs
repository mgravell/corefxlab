// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO.Pipelines.Testing;
using System.Linq;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public abstract class ReadableBufferFacts
    {
        public class Array: ReadableBufferFacts
        {
            public Array() : base(TestBufferFactory.Array) { }
        }

        public class SingleSegment: ReadableBufferFacts
        {
            public SingleSegment() : base(TestBufferFactory.SingleSegment) { }
        }

        public class SegmentPerByte: ReadableBufferFacts
        {
            public SegmentPerByte() : base(TestBufferFactory.SegmentPerByte) { }
        }

        internal TestBufferFactory Factory { get; }

        internal ReadableBufferFacts(TestBufferFactory factory)
        {
            Factory = factory;
        }

        [Fact]
        public void EmptyIsCorrect()
        {
            var buffer = Factory.CreateOfSize(0);
            Assert.Equal(0, buffer.Length);
            Assert.True(buffer.IsEmpty);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(8)]
        public void LengthIsCorrect(int length)
        {
            var buffer = Factory.CreateOfSize(length);
            Assert.Equal(length, buffer.Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(8)]
        public void ToArrayIsCorrect(int length)
        {
            var data = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();
            var buffer = Factory.CreateWithContent(data);
            Assert.Equal(length, buffer.Length);
            Assert.Equal(data, buffer.ToArray());
        }

        [Theory]
        [MemberData(nameof(OutOfRangeSliceCases))]
        public void ReadableBufferDoesNotAllowSlicingOutOfRange(Action<ReadableBuffer> fail)
        {
            var buffer = Factory.CreateOfSize(100);
            var ex = Assert.Throws<InvalidOperationException>(() => fail(buffer));
        }

        [Fact]
        public void ReadableBufferMove_MovesReadCursor()
        {
            var buffer = Factory.CreateOfSize(100);
            var cursor = buffer.Move(buffer.Start, 65);
            Assert.Equal(buffer.Slice(65).Start, cursor);
        }

        [Fact]
        public void ReadableBufferMove_ChecksBounds()
        {
            var buffer = Factory.CreateOfSize(100);
            Assert.Throws<InvalidOperationException>(() => buffer.Move(buffer.Start, 101));
        }

        [Fact]
        public void ReadableBufferMove_DoesNotAlowNegative()
        {
            var data = new byte[20];
            var buffer = ReadableBuffer.Create(data);
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Move(buffer.Start, -1));
        }

        [Fact]
        public void ReadCursorSeekChecksEndIfNotTrustingEnd()
        {
            var buffer = BufferUtilities.CreateBuffer(1, 1, 1);
            var buffer2 = BufferUtilities.CreateBuffer(1, 1, 1);
            Assert.Throws<InvalidOperationException>(() => buffer.Start.Seek(2, buffer2.End, true));
        }

        [Fact]
        public void ReadCursorSeekDoesNotCheckEndIfTrustingEnd()
        {
            var buffer = BufferUtilities.CreateBuffer(1, 1, 1);
            var buffer2 = BufferUtilities.CreateBuffer(1, 1, 1);
            buffer.Start.Seek(2, buffer2.End, false);
        }

        [Fact]
        public void SegmentStartIsConsideredInBoundsCheck()
        {
            // 0               50           100    0             50             100
            // [                ##############] -> [##############                ]
            //                         ^c1            ^c2
            var bufferSegment1 = new BufferSegment();
            bufferSegment1.SetMemory(new OwnedArray<byte>(new byte[100]), default, 50, 99);

            var bufferSegment2 = new BufferSegment();
            bufferSegment2.SetMemory(new OwnedArray<byte>(new byte[100]), default, 0, 50);
            bufferSegment1.SetNext(bufferSegment2);

            var readableBuffer = new ReadableBuffer(new ReadCursor(bufferSegment1, 50), new ReadCursor(bufferSegment2, 50));

            var c1 = readableBuffer.Move(readableBuffer.Start, 25); // segment 1 index 75
            var c2 = readableBuffer.Move(readableBuffer.Start, 55); // segment 2 index 5

            var sliced = readableBuffer.Slice(c1, c2);

            Assert.Equal(30, sliced.Length);
        }

        public static TheoryData<Action<ReadableBuffer>> OutOfRangeSliceCases => new TheoryData<Action<ReadableBuffer>>
        {
            b => b.Slice(101),
            b => b.Slice(0, 101),
            b => b.Slice(b.Start, 101),
            b => b.Slice(0, 70).Slice(b.End, b.End),
            b => b.Slice(0, 70).Slice(b.Start, b.End),
            b => b.Slice(0, 70).Slice(0, b.End),
            b => b.Slice(70, b.Start)
        };
    }
}
