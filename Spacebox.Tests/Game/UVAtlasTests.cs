using OpenTK.Mathematics;
using Spacebox.Game.Resources;

namespace Spacebox.Tests
{
    public class UVAtlasTests
    {
        public static IEnumerable<object[]> GetUVs_Vector2_TestData()
        {
            // Test cases: { Vector2 input, sideInBlocks, expected UVs }
            // In-bounds
            yield return new object[]
            {
                new Vector2(2f, 3f),
                8,
                new Vector2[]
                {
                    new Vector2(2f / 8, 3f / 8),
                    new Vector2((2f + 1) / 8, 3f / 8),
                    new Vector2((2f + 1) / 8, (3f + 1) / 8),
                    new Vector2(2f / 8, (3f + 1) / 8)
                }
            };

            yield return new object[]
            {
                new Vector2(15f, 15f),
                16,
                new Vector2[]
                {
                    new Vector2(15f / 16, 15f / 16),
                    new Vector2((15f + 1) / 16, 15f / 16),
                    new Vector2((15f + 1) / 16, (15f + 1) / 16),
                    new Vector2(15f / 16, (15f + 1) / 16)
                }
            };

            // Out-of-bounds
            yield return new object[]
            {
                new Vector2(-1f, 9f),
                8,
                new Vector2[]
                {
                    new Vector2(0f / 8, 0f / 8),
                    new Vector2((0f + 1) / 8, 0f / 8),
                    new Vector2((0f + 1) / 8, (0f + 1) / 8),
                    new Vector2(0f / 8, (0f + 1) / 8)
                }
            };

            yield return new object[]
            {
                new Vector2(17f, 17f),
                16,
                new Vector2[]
                {
                    new Vector2(0f / 16, 0f / 16),
                    new Vector2((0f + 1) / 16, 0f / 16),
                    new Vector2((0f + 1) / 16, (0f + 1) / 16),
                    new Vector2(0f / 16, (0f + 1) / 16)
                }
            };
        }

        [Theory]
        [MemberData(nameof(GetUVs_Vector2_TestData))]
        public void GetUVs_Vector2_ReturnsCorrectUVs(Vector2 input, int sideInBlocks, Vector2[] expectedUVs)
        {
            var result = UVAtlas.GetUVs(input, sideInBlocks);
            Assert.Equal(expectedUVs.Length, result.Length);
            for (int i = 0; i < expectedUVs.Length; i++)
            {
                Assert.Equal(expectedUVs[i], result[i]);
            }
        }

        public static IEnumerable<object[]> GetUVs_Vector2Byte_TestData()
        {
            // Test cases: { Vector2Byte input, sideInBlocks, expected UVs }
            // In-bounds
            yield return new object[]
            {
                new Vector2Byte(4, 5),
                8,
                new Vector2[]
                {
                    new Vector2(4f / 8, 5f / 8),
                    new Vector2((4f + 1) / 8, 5f / 8),
                    new Vector2((4f + 1) / 8, (5f + 1) / 8),
                    new Vector2(4f / 8, (5f + 1) / 8)
                }
            };

            yield return new object[]
            {
                new Vector2Byte(15, 15),
                16,
                new Vector2[]
                {
                    new Vector2(15f / 16, 15f / 16),
                    new Vector2((15f + 1) / 16, 15f / 16),
                    new Vector2((15f + 1) / 16, (15f + 1) / 16),
                    new Vector2(15f / 16, (15f + 1) / 16)
                }
            };

            // Out-of-bounds
            yield return new object[]
            {
                new Vector2Byte(9, 9),
                8,
                new Vector2[]
                {
                    new Vector2(0f / 8, 0f / 8),
                    new Vector2((0f + 1) / 8, 0f / 8),
                    new Vector2((0f + 1) / 8, (0f + 1) / 8),
                    new Vector2(0f / 8, (0f + 1) / 8)
                }
            };

            yield return new object[]
            {
                new Vector2Byte(17, 17),
                16,
                new Vector2[]
                {
                    new Vector2(0f / 16, 0f / 16),
                    new Vector2((0f + 1) / 16, 0f / 16),
                    new Vector2((0f + 1) / 16, (0f + 1) / 16),
                    new Vector2(0f / 16, (0f + 1) / 16)
                }
            };
        }

        [Theory]
        [MemberData(nameof(GetUVs_Vector2Byte_TestData))]
        public void GetUVs_Vector2Byte_ReturnsCorrectUVs(Vector2Byte input, int sideInBlocks, Vector2[] expectedUVs)
        {
            var result = UVAtlas.GetUVs(input, sideInBlocks);
            Assert.Equal(expectedUVs.Length, result.Length);
            for (int i = 0; i < expectedUVs.Length; i++)
            {
                Assert.Equal(expectedUVs[i], result[i]);
            }
        }

        public static IEnumerable<object[]> GetUVs_Ints_TestData()
        {
            // Test cases: { x, y, sideInBlocks, expected UVs }
            // In-bounds
            yield return new object[]
            {
                4, 5, 8,
                new Vector2[]
                {
                    new Vector2(4f / 8, 5f / 8),
                    new Vector2((4f + 1) / 8, 5f / 8),
                    new Vector2((4f + 1) / 8, (5f + 1) / 8),
                    new Vector2(4f / 8, (5f + 1) / 8)
                }
            };

            yield return new object[]
            {
                15, 15, 16,
                new Vector2[]
                {
                    new Vector2(15f / 16, 15f / 16),
                    new Vector2((15f + 1) / 16, 15f / 16),
                    new Vector2((15f + 1) / 16, (15f + 1) / 16),
                    new Vector2(15f / 16, (15f + 1) / 16)
                }
            };

            // Out-of-bounds
            yield return new object[]
            {
                -1, 8, 8,
                new Vector2[]
                {
                    new Vector2(0f / 8, 0f / 8),
                    new Vector2((0f + 1) / 8, 0f / 8),
                    new Vector2((0f + 1) / 8, (0f + 1) / 8),
                    new Vector2(0f / 8, (0f + 1) / 8)
                }
            };

            yield return new object[]
            {
                16, 16, 16,
                new Vector2[]
                {
                    new Vector2(0f / 16, 0f / 16),
                    new Vector2((0f + 1) / 16, 0f / 16),
                    new Vector2((0f + 1) / 16, (0f + 1) / 16),
                    new Vector2(0f / 16, (0f + 1) / 16)
                }
            };
        }

        [Theory]
        [MemberData(nameof(GetUVs_Ints_TestData))]
        public void GetUVs_Ints_ReturnsCorrectUVs(int x, int y, int sideInBlocks, Vector2[] expectedUVs)
        {
            var result = UVAtlas.GetUVs(x, y, sideInBlocks);
            Assert.Equal(expectedUVs.Length, result.Length);
            for (int i = 0; i < expectedUVs.Length; i++)
            {
                Assert.Equal(expectedUVs[i], result[i]);
            }
        }
    }
}
