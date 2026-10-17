using Engine.Graphics.Atlas;

namespace Spacebox.Tests
{
    public class AtlasLayoutTests
    {
        [Fact]
        public void Pack_Empty_ReturnsOnePixelAtlas()
        {
            var placements = new AtlasRect[0];

            bool packed = AtlasLayout.Pack(new AtlasRect[0], 0, 4096, out int width, out int height, placements);

            Assert.True(packed);
            Assert.Equal(1, width);
            Assert.Equal(1, height);
        }

        [Fact]
        public void Pack_MixedSizes_NoOverlapsAndInsideBounds()
        {
            var sizes = new[]
            {
                new AtlasRect(0, 0, 64, 64),
                new AtlasRect(0, 0, 32, 32),
                new AtlasRect(0, 0, 32, 32),
                new AtlasRect(0, 0, 16, 48),
                new AtlasRect(0, 0, 128, 16),
                new AtlasRect(0, 0, 8, 8)
            };
            var placements = new AtlasRect[sizes.Length];

            bool packed = AtlasLayout.Pack(sizes, 0, 4096, out int width, out int height, placements);

            Assert.True(packed);
            for (int i = 0; i < placements.Length; i++)
            {
                Assert.Equal(sizes[i].Width, placements[i].Width);
                Assert.Equal(sizes[i].Height, placements[i].Height);
                Assert.InRange(placements[i].X, 0, width - placements[i].Width);
                Assert.InRange(placements[i].Y, 0, height - placements[i].Height);

                for (int j = i + 1; j < placements.Length; j++)
                {
                    Assert.False(placements[i].Overlaps(placements[j]), $"{i} overlaps {j}");
                }
            }
        }

        [Fact]
        public void Pack_SizeIsPowerOfTwo()
        {
            var sizes = new AtlasRect[40];
            for (int i = 0; i < sizes.Length; i++)
            {
                sizes[i] = new AtlasRect(0, 0, 32, 32);
            }
            var placements = new AtlasRect[sizes.Length];

            AtlasLayout.Pack(sizes, 0, 4096, out int width, out int height, placements);

            Assert.Equal(0, width & (width - 1));
            Assert.Equal(0, height & (height - 1));
            Assert.True(width * height >= 40 * 32 * 32);
        }

        [Fact]
        public void Pack_WithPadding_KeepsGapBetweenRegions()
        {
            const int padding = 2;
            var sizes = new[]
            {
                new AtlasRect(0, 0, 16, 16),
                new AtlasRect(0, 0, 16, 16),
                new AtlasRect(0, 0, 16, 16)
            };
            var placements = new AtlasRect[sizes.Length];

            AtlasLayout.Pack(sizes, padding, 4096, out _, out _, placements);

            for (int i = 0; i < placements.Length; i++)
            {
                var grown = new AtlasRect(placements[i].X - padding, placements[i].Y - padding,
                    placements[i].Width + padding * 2, placements[i].Height + padding * 2);

                for (int j = i + 1; j < placements.Length; j++)
                {
                    Assert.False(grown.Overlaps(placements[j]));
                }
            }
        }

        [Fact]
        public void Pack_TooLarge_ReturnsFalse()
        {
            var sizes = new[] { new AtlasRect(0, 0, 300, 300), new AtlasRect(0, 0, 300, 300) };
            var placements = new AtlasRect[sizes.Length];

            bool packed = AtlasLayout.Pack(sizes, 0, 512, out _, out _, placements);

            Assert.False(packed);
        }
    }
}
