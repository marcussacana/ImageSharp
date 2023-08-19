// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#if SUPPORTS_RUNTIME_INTRINSICS
using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SixLabors.ImageSharp.Formats.Jpeg.Components
{
    internal static partial class ZigZag
    {
#pragma warning disable SA1309 // naming rules violation warnings
        /// <summary>
        /// Special byte value to zero out elements during Sse/Avx shuffle intrinsics.
        /// </summary>
        private const byte _ = 0xff;
#pragma warning restore SA1309

        /// <summary>
        /// Gets shuffle vectors for <see cref="ApplyTransposingZigZagOrderingSsse3"/>
        /// zig zag implementation.
        /// </summary>
        private static ReadOnlySpan<byte> SseShuffleMasks => new byte[]
        {
#pragma warning disable SA1515
            /* row0 - A0 B0 A1 A2 B1 C0 D0 C1 */
            // A
            0, 1, _, _, 2, 3, 4, 5, _, _, _, _, _, _, _, _,
            // B
            _, _, 0, 1, _, _, _, _, 2, 3, _, _, _, _, _, _,
            // C
            _, _, _, _, _, _, _, _, _, _, 0, 1, _, _, 2, 3,

            /* row1 - B2 A3 A4 B3 C2 D1 E0 F0 */
            // A
            _, _, 6, 7, 8, 9, _, _, _, _, _, _, _, _, _, _,
            // B
            4, 5, _, _, _, _, 6, 7, _, _, _, _, _, _, _, _,

            /* row2 - E1 D2 C3 B4 A5 A6 B5 C4 */
            // A
            _, _, _, _, _, _, _, _, 10, 11, 12, 13,  _,  _, _, _,
            // B
            _, _, _, _, _, _, 8, 9,  _,  _,  _,  _, 10, 11, _, _,
            // C
            _, _, _, _, 6, 7, _, _,  _,  _,  _,  _,  _,  _, 8, 9,

            /* row3 - D3 E2 F1 G0 H0 G1 F2 E3 */
            // E
            _, _, 4, 5, _, _, _, _, _, _, _, _, _, _, 6, 7,
            // F
            _, _, _, _, 2, 3, _, _, _, _, _, _, 4, 5, _, _,
            // G
            _, _, _, _, _, _, 0, 1, _, _, 2, 3, _, _, _, _,

            /* row4 - D4 C5 B6 A7 B7 C6 D5 E4 */
            // B
            _, _,  _,  _, 12, 13, _, _, 14, 15,  _,  _,  _,  _, _, _,
            // C
            _, _, 10, 11,  _,  _, _, _,  _,  _, 12, 13,  _,  _, _, _,
            // D
            8, 9,  _,  _,  _,  _, _, _,  _,  _,  _,  _, 10, 11, _, _,

            /* row5 - F3 G2 H1 H2 G3 F4 E5 D6 */
            // F
            6, 7, _, _, _, _, _, _, _, _, 8, 9, _, _, _, _,
            // G
            _, _, 4, 5, _, _, _, _, 6, 7, _, _, _, _, _, _,
            // H
            _, _, _, _, 2, 3, 4, 5, _, _, _, _, _, _, _, _,

            /* row6 - C7 D7 E6 F5 G4 H3 H4 G5 */
            // G
            _, _, _, _, _, _, _, _, 8, 9, _, _, _, _, 10, 11,
            // H
            _, _, _, _, _, _, _, _, _, _, 6, 7, 8, 9,  _,  _,

            /* row7 - F6 E7 F7 G6 H5 H6 G7 H7 */
            // F
            12, 13, _, _, 14, 15,  _,  _,  _,  _,  _,  _,  _,  _, _, _,
            // G
            _,  _, _, _,  _,  _, 12, 13,  _,  _,  _,  _, 14, 15, _, _,
            // H
            _,  _, _, _,  _,  _,  _,  _, 10, 11, 12, 13,  _,  _, 14, 15,
#pragma warning restore SA1515
        };

        /// <summary>
        /// Gets shuffle vectors for <see cref="ApplyTransposingZigZagOrderingAvx2"/>
        /// zig zag implementation.
        /// </summary>
        private static ReadOnlySpan<byte> AvxShuffleMasks => new byte[]
        {
#pragma warning disable SA1515
            /* 01 */
            // [cr] crln_01_AB_CD
            0, 0, 0, 0,   1, 0, 0, 0,   4, 0, 0, 0,   _, _, _, _,   1, 0, 0, 0,   2, 0, 0, 0,   4, 0, 0, 0,   5, 0, 0, 0,
            // (in) AB
            0, 1, 8, 9,   2, 3, 4, 5,   10, 11, _, _,   _, _, _, _,   12, 13, 2, 3,   4, 5, 14, 15,   _, _, _, _,   _, _, _, _,
            // (in) CD
            _, _, _, _,   _, _, _, _,   _, _, 0, 1,   8, 9, 2, 3,   _, _, _, _,   _, _, _, _,   0, 1, 10, 11,   _, _, _, _,
            // [cr] crln_01_23_EF_23_CD
            0, 0, 0, 0,   1, 0, 0, 0,   2, 0, 0, 0,   5, 0, 0, 0,   0, 0, 0, 0,   1, 0, 0, 0,   4, 0, 0, 0,   5, 0, 0, 0,
            // (in) EF
            _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,   0, 1, 8, 9,

            /* 23 */
            // [cr] crln_23_AB_23_45_GH
            2, 0, 0, 0,   3, 0, 0, 0,   6, 0, 0, 0,   7, 0, 0, 0,   0, 0, 0, 0,   1, 0, 0, 0,   4, 0, 0, 0,   5, 0, 0, 0,
            // (in) AB
            _, _, _, _,   _, _, 8, 9,   2, 3, 4, 5,   10, 11, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,
            // (in) CDe
            _, _, 12, 13,   6, 7, _, _,   _, _, _, _,   _, _, 8, 9,   14, 15, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,
            // (in) EF
            2, 3, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, 4, 5,   10, 11, _, _,   _, _, _, _,   12, 13, 6, 7,
            // (in) GH
            _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, 0, 1,   8, 9, 2, 3,   _, _, _, _,

            /* 45 */
            // (in) AB
            _, _, _, _,   12, 13, 6, 7,   14, 15, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,
            // [cr] crln_45_67_CD_45_EF
            2, 0, 0, 0,   3, 0, 0, 0,   6, 0, 0, 0,   7, 0, 0, 0,   2, 0, 0, 0,   5, 0, 0, 0,   6, 0, 0, 0,   7, 0, 0, 0,
            // (in) CD
            8, 9, 2, 3,   _, _, _, _,   _, _, 4, 5,   10, 11, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, 12, 13,
            // (in) EF
            _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, 0, 1,   6, 7, _, _,   _, _, _, _,   _, _, 8, 9,   2, 3, _, _,
            // (in) GH
            _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, 4, 5,   10, 11, 12, 13,   6, 7, _, _,   _, _, _, _,

            /* 67 */
            // (in) CD
            6, 7, 14, 15,   _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,   _, _, _, _,
            // [cr] crln_67_EF_67_GH
            2, 0, 0, 0,   3, 0, 0, 0,   5, 0, 0, 0,   6, 0, 0, 0,   3, 0, 0, 0,   6, 0, 0, 0,   7, 0, 0, 0,   _, _, _, _,
            // (in) EF
            _, _, _, _,   4, 5, 14, 15,   _, _, _, _,   _, _, _, _,   8, 9, 2, 3,   10, 11, _, _,   _, _, _, _,   _, _, _, _,
            // (in) GH
            _, _, _, _,   _, _, _, _,   0, 1, 10, 11,   12, 13, 2, 3,   _, _, _, _,   _, _, 0, 1,   6, 7, 8, 9,   2, 3, 10, 11,
#pragma warning restore SA1515
        };

        /// <summary>
        /// Applies zig zag ordering for given 8x8 matrix using SSE cpu intrinsics.
        /// </summary>
        /// <param name="block">Input matrix.</param>
        public static unsafe void ApplyTransposingZigZagOrderingSsse3(ref Block8x8 block)
        {
            DebugGuard.IsTrue(Ssse3.IsSupported, "Ssse3 support is required to run this operation!");

            fixed (byte* shuffleVectorsPtr = &MemoryMarshal.GetReference(SseShuffleMasks))
            {
                Vector128<byte> rowA = block.V0.AsByte();
                Vector128<byte> rowB = block.V1.AsByte();
                Vector128<byte> rowC = block.V2.AsByte();
                Vector128<byte> rowD = block.V3.AsByte();
                Vector128<byte> rowE = block.V4.AsByte();
                Vector128<byte> rowF = block.V5.AsByte();
                Vector128<byte> rowG = block.V6.AsByte();
                Vector128<byte> rowH = block.V7.AsByte();

                // row0 - A0 B0 A1 A2 B1 C0 D0 C1
                Vector128<short> row0_A = Ssse3.Shuffle(rowA, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 0))).AsInt16();
                Vector128<short> row0_B = Ssse3.Shuffle(rowB, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 1))).AsInt16();
                Vector128<short> row0_C = Ssse3.Shuffle(rowC, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 2))).AsInt16();
                Vector128<short> row0 = Sse2.Or(Sse2.Or(row0_A, row0_B), row0_C);
                row0 = Sse2.Insert(row0.AsUInt16(), Sse2.Extract(rowD.AsUInt16(), 0), 6).AsInt16();

                // row1 - B2 A3 A4 B3 C2 D1 E0 F0
                Vector128<short> row1_A = Ssse3.Shuffle(rowA, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 3))).AsInt16();
                Vector128<short> row1_B = Ssse3.Shuffle(rowB, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 4))).AsInt16();
                Vector128<short> row1 = Sse2.Or(row1_A, row1_B);
                row1 = Sse2.Insert(row1.AsUInt16(), Sse2.Extract(rowC.AsUInt16(), 2), 4).AsInt16();
                row1 = Sse2.Insert(row1.AsUInt16(), Sse2.Extract(rowD.AsUInt16(), 1), 5).AsInt16();
                row1 = Sse2.Insert(row1.AsUInt16(), Sse2.Extract(rowE.AsUInt16(), 0), 6).AsInt16();
                row1 = Sse2.Insert(row1.AsUInt16(), Sse2.Extract(rowF.AsUInt16(), 0), 7).AsInt16();

                // row2 - E1 D2 C3 B4 A5 A6 B5 C4
                Vector128<short> row2_A = Ssse3.Shuffle(rowA, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 5))).AsInt16();
                Vector128<short> row2_B = Ssse3.Shuffle(rowB, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 6))).AsInt16();
                Vector128<short> row2_C = Ssse3.Shuffle(rowC, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 7))).AsInt16();
                Vector128<short> row2 = Sse2.Or(Sse2.Or(row2_A, row2_B), row2_C);
                row2 = Sse2.Insert(row2.AsUInt16(), Sse2.Extract(rowD.AsUInt16(), 2), 1).AsInt16();
                row2 = Sse2.Insert(row2.AsUInt16(), Sse2.Extract(rowE.AsUInt16(), 1), 0).AsInt16();

                // row3 - D3 E2 F1 G0 H0 G1 F2 E3
                Vector128<short> row3_E = Ssse3.Shuffle(rowE, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 8))).AsInt16();
                Vector128<short> row3_F = Ssse3.Shuffle(rowF, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 9))).AsInt16();
                Vector128<short> row3_G = Ssse3.Shuffle(rowG, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 10))).AsInt16();
                Vector128<short> row3 = Sse2.Or(Sse2.Or(row3_E, row3_F), row3_G);
                row3 = Sse2.Insert(row3.AsUInt16(), Sse2.Extract(rowD.AsUInt16(), 3), 0).AsInt16();
                row3 = Sse2.Insert(row3.AsUInt16(), Sse2.Extract(rowH.AsUInt16(), 0), 4).AsInt16();

                // row4 - D4 C5 B6 A7 B7 C6 D5 E4
                Vector128<short> row4_B = Ssse3.Shuffle(rowB, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 11))).AsInt16();
                Vector128<short> row4_C = Ssse3.Shuffle(rowC, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 12))).AsInt16();
                Vector128<short> row4_D = Ssse3.Shuffle(rowD, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 13))).AsInt16();
                Vector128<short> row4 = Sse2.Or(Sse2.Or(row4_B, row4_C), row4_D);
                row4 = Sse2.Insert(row4.AsUInt16(), Sse2.Extract(rowA.AsUInt16(), 7), 3).AsInt16();
                row4 = Sse2.Insert(row4.AsUInt16(), Sse2.Extract(rowE.AsUInt16(), 4), 7).AsInt16();

                // row5 - F3 G2 H1 H2 G3 F4 E5 D6
                Vector128<short> row5_F = Ssse3.Shuffle(rowF, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 14))).AsInt16();
                Vector128<short> row5_G = Ssse3.Shuffle(rowG, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 15))).AsInt16();
                Vector128<short> row5_H = Ssse3.Shuffle(rowH, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 16))).AsInt16();
                Vector128<short> row5 = Sse2.Or(Sse2.Or(row5_F, row5_G), row5_H);
                row5 = Sse2.Insert(row5.AsUInt16(), Sse2.Extract(rowD.AsUInt16(), 6), 7).AsInt16();
                row5 = Sse2.Insert(row5.AsUInt16(), Sse2.Extract(rowE.AsUInt16(), 5), 6).AsInt16();

                // row6 - C7 D7 E6 F5 G4 H3 H4 G5
                Vector128<short> row6_G = Ssse3.Shuffle(rowG, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 17))).AsInt16();
                Vector128<short> row6_H = Ssse3.Shuffle(rowH, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 18))).AsInt16();
                Vector128<short> row6 = Sse2.Or(row6_G, row6_H);
                row6 = Sse2.Insert(row6.AsUInt16(), Sse2.Extract(rowC.AsUInt16(), 7), 0).AsInt16();
                row6 = Sse2.Insert(row6.AsUInt16(), Sse2.Extract(rowD.AsUInt16(), 7), 1).AsInt16();
                row6 = Sse2.Insert(row6.AsUInt16(), Sse2.Extract(rowE.AsUInt16(), 6), 2).AsInt16();
                row6 = Sse2.Insert(row6.AsUInt16(), Sse2.Extract(rowF.AsUInt16(), 5), 3).AsInt16();

                // row7 - F6 E7 F7 G6 H5 H6 G7 H7
                Vector128<short> row7_F = Ssse3.Shuffle(rowF, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 19))).AsInt16();
                Vector128<short> row7_G = Ssse3.Shuffle(rowG, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 20))).AsInt16();
                Vector128<short> row7_H = Ssse3.Shuffle(rowH, Sse2.LoadVector128(shuffleVectorsPtr + (16 * 21))).AsInt16();
                Vector128<short> row7 = Sse2.Or(Sse2.Or(row7_F, row7_G), row7_H);
                row7 = Sse2.Insert(row7.AsUInt16(), Sse2.Extract(rowE.AsUInt16(), 7), 1).AsInt16();

                block.V0 = row0;
                block.V1 = row1;
                block.V2 = row2;
                block.V3 = row3;
                block.V4 = row4;
                block.V5 = row5;
                block.V6 = row6;
                block.V7 = row7;
            }
        }

        /// <summary>
        /// Applies zig zag ordering for given 8x8 matrix using AVX cpu intrinsics.
        /// </summary>
        /// <param name="block">Input matrix.</param>
        public static unsafe void ApplyTransposingZigZagOrderingAvx2(ref Block8x8 block)
        {
            DebugGuard.IsTrue(Avx2.IsSupported, "Avx2 support is required to run this operation!");

            fixed (byte* shuffleVectorsPtr = &MemoryMarshal.GetReference(AvxShuffleMasks))
            {
                Vector256<byte> rowAB = block.V01.AsByte();
                Vector256<byte> rowCD = block.V23.AsByte();
                Vector256<byte> rowEF = block.V45.AsByte();
                Vector256<byte> rowGH = block.V67.AsByte();

                /* row01 - A0 B0 A1 A2 B1 C0 D0 C1 | B2 A3 A4 B3 C2 D1 E0 F0 */
                Vector256<int> crln_01_AB_CD = Avx.LoadVector256(shuffleVectorsPtr + (0 * 32)).AsInt32();
                Vector256<byte> row01_AB = Avx2.PermuteVar8x32(rowAB.AsInt32(), crln_01_AB_CD).AsByte();
                row01_AB = Avx2.Shuffle(row01_AB, Avx.LoadVector256(shuffleVectorsPtr + (1 * 32))).AsByte();
                Vector256<byte> row01_CD = Avx2.PermuteVar8x32(rowCD.AsInt32(), crln_01_AB_CD).AsByte();
                row01_CD = Avx2.Shuffle(row01_CD, Avx.LoadVector256(shuffleVectorsPtr + (2 * 32))).AsByte();
                Vector256<int> crln_01_23_EF_23_CD = Avx.LoadVector256(shuffleVectorsPtr + (3 * 32)).AsInt32();
                Vector256<byte> row01_23_EF = Avx2.PermuteVar8x32(rowEF.AsInt32(), crln_01_23_EF_23_CD).AsByte();
                Vector256<byte> row01_EF = Avx2.Shuffle(row01_23_EF, Avx.LoadVector256(shuffleVectorsPtr + (4 * 32))).AsByte();

                Vector256<byte> row01 = Avx2.Or(row01_AB, Avx2.Or(row01_CD, row01_EF));

                /* row23 - E1 D2 C3 B4 A5 A6 B5 C4 | D3 E2 F1 G0 H0 G1 F2 E3 */
                Vector256<int> crln_23_AB_23_45_GH = Avx.LoadVector256(shuffleVectorsPtr + (5 * 32)).AsInt32();
                Vector256<byte> row23_45_AB = Avx2.PermuteVar8x32(rowAB.AsInt32(), crln_23_AB_23_45_GH).AsByte();
                Vector256<byte> row23_AB = Avx2.Shuffle(row23_45_AB, Avx.LoadVector256(shuffleVectorsPtr + (6 * 32))).AsByte();
                Vector256<byte> row23_CD = Avx2.PermuteVar8x32(rowCD.AsInt32(), crln_01_23_EF_23_CD).AsByte();
                row23_CD = Avx2.Shuffle(row23_CD, Avx.LoadVector256(shuffleVectorsPtr + (7 * 32))).AsByte();
                Vector256<byte> row23_EF = Avx2.Shuffle(row01_23_EF, Avx.LoadVector256(shuffleVectorsPtr + (8 * 32))).AsByte();
                Vector256<byte> row23_45_GH = Avx2.PermuteVar8x32(rowGH.AsInt32(), crln_23_AB_23_45_GH).AsByte();
                Vector256<byte> row23_GH = Avx2.Shuffle(row23_45_GH, Avx.LoadVector256(shuffleVectorsPtr + (9 * 32))).AsByte();

                Vector256<byte> row23 = Avx2.Or(Avx2.Or(row23_AB, row23_CD), Avx2.Or(row23_EF, row23_GH));

                /* row45 - D4 C5 B6 A7 B7 C6 D5 E4 | F3 G2 H1 H2 G3 F4 E5 D6 */
                Vector256<byte> row45_AB = Avx2.Shuffle(row23_45_AB, Avx.LoadVector256(shuffleVectorsPtr + (10 * 32))).AsByte();
                Vector256<int> crln_45_67_CD_45_EF = Avx.LoadVector256(shuffleVectorsPtr + (11 * 32)).AsInt32();
                Vector256<byte> row45_67_CD = Avx2.PermuteVar8x32(rowCD.AsInt32(), crln_45_67_CD_45_EF).AsByte();
                Vector256<byte> row45_CD = Avx2.Shuffle(row45_67_CD, Avx.LoadVector256(shuffleVectorsPtr + (12 * 32))).AsByte();
                Vector256<byte> row45_EF = Avx2.PermuteVar8x32(rowEF.AsInt32(), crln_45_67_CD_45_EF).AsByte();
                row45_EF = Avx2.Shuffle(row45_EF, Avx.LoadVector256(shuffleVectorsPtr + (13 * 32))).AsByte();
                Vector256<byte> row45_GH = Avx2.Shuffle(row23_45_GH, Avx.LoadVector256(shuffleVectorsPtr + (14 * 32))).AsByte();

                Vector256<byte> row45 = Avx2.Or(Avx2.Or(row45_AB, row45_CD), Avx2.Or(row45_EF, row45_GH));

                /* row67 - C7 D7 E6 F5 G4 H3 H4 G5 | F6 E7 F7 G6 H5 H6 G7 H7 */
                Vector256<byte> row67_CD = Avx2.Shuffle(row45_67_CD, Avx.LoadVector256(shuffleVectorsPtr + (15 * 32))).AsByte();
                Vector256<int> crln_67_EF_67_GH = Avx.LoadVector256(shuffleVectorsPtr + (16 * 32)).AsInt32();
                Vector256<byte> row67_EF = Avx2.PermuteVar8x32(rowEF.AsInt32(), crln_67_EF_67_GH).AsByte();
                row67_EF = Avx2.Shuffle(row67_EF, Avx.LoadVector256(shuffleVectorsPtr + (17 * 32))).AsByte();
                Vector256<byte> row67_GH = Avx2.PermuteVar8x32(rowGH.AsInt32(), crln_67_EF_67_GH).AsByte();
                row67_GH = Avx2.Shuffle(row67_GH, Avx.LoadVector256(shuffleVectorsPtr + (18 * 32))).AsByte();

                Vector256<byte> row67 = Avx2.Or(row67_CD, Avx2.Or(row67_EF, row67_GH));

                block.V01 = row01.AsInt16();
                block.V23 = row23.AsInt16();
                block.V45 = row45.AsInt16();
                block.V67 = row67.AsInt16();
            }
        }
    }
}
#endif
