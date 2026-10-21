using OpenTK.Mathematics;
using Spacebox.Game.Rendering;

namespace Spacebox.Tests.Game;

public class CrackVariantsTests
{
    [Fact]
    public void AllVariantsAppearInASmallArea()
    {
        var seen = new HashSet<int>();
        for (int x = 0; x < 4; x++)
        for (int y = 0; y < 4; y++)
        for (int z = 0; z < 4; z++)
            seen.Add(CrackVariants.Variant(new Vector3i(x, y, z)));

        Assert.Equal(CrackVariants.Count, seen.Count);
    }

    [Fact]
    public void NeighboursUsuallyDiffer()
    {
        int same = 0;
        int total = 0;
        for (int x = -8; x < 8; x++)
        for (int y = -8; y < 8; y++)
        {
            total++;
            if (CrackVariants.Variant(new Vector3i(x, y, 0)) == CrackVariants.Variant(new Vector3i(x + 1, y, 0))) same++;
        }

        Assert.True(same < total / 4, $"{same} of {total} neighbours share a variant");
    }

    [Fact]
    public void ValuesStayInRangeForNegativeCells()
    {
        for (int i = -40; i < 40; i++)
        {
            var cell = new Vector3i(i, -i * 3, i * 7);
            Assert.InRange(CrackVariants.Variant(cell), 0, CrackVariants.Count - 1);
            Assert.InRange(CrackVariants.Orientation(cell, 5), 0, CrackVariants.Orientations - 1);
        }
    }
}
