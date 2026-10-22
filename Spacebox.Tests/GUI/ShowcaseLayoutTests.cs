using Engine.CanvasUI;
using Engine.CanvasUI.Validation;
using Spacebox.Game.GUI.Showcase;

namespace Spacebox.Tests.GUI;

public class ShowcaseLayoutTests
{
    [Fact]
    public void Font_IsTheRealPixelFont()
    {
        var font = ShowcaseFixture.Assets.Font;
        Assert.IsType<SkiaCanvasFont>(font);
        Assert.True(font.MeasureWidth("INVENTORY", 12f) > 30f);
        Assert.True(ShowcaseFixture.Assets.Items.Count > 10);
    }

    [Fact]
    public void Variants_IncludeFiveInterfaces()
    {
        var names = ShowcaseVariants.CreateAll().Select(v => v.Name).ToArray();
        Assert.Equal(5, names.Length);
        Assert.Equal(names.Length, names.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(ShowcaseFixture.VariantIndices), MemberType = typeof(ShowcaseFixture))]
    public void Variant_LaysOutCleanlyOnEveryScreen(int variant)
    {
        var fixture = new ShowcaseFixture(variant);
        var report = LayoutHarness.ValidateAll(fixture.Root, ScreenProfiles.All);
        Assert.True(report.IsClean, report.Format());
    }

    [Theory]
    [MemberData(nameof(ShowcaseFixture.VariantIndices), MemberType = typeof(ShowcaseFixture))]
    public void Variant_HudAloneLaysOutCleanlyOnEveryScreen(int variant)
    {
        var fixture = new ShowcaseFixture(variant);
        fixture.Host.ToggleWindows();
        var report = LayoutHarness.ValidateAll(fixture.Root, ScreenProfiles.All);
        Assert.True(report.IsClean, report.Format());
    }

    [Theory]
    [MemberData(nameof(ShowcaseFixture.VariantIndices), MemberType = typeof(ShowcaseFixture))]
    public void Variant_TooltipsStayOnScreen(int variant)
    {
        var fixture = new ShowcaseFixture(variant);
        var reports = ScreenProfiles.Quick.Select(profile => LayoutHarness.ValidateTooltips(fixture.Root, profile));
        var report = LayoutReport.Merge(reports);
        Assert.True(report.IsClean, report.Format());
    }

    [Theory]
    [MemberData(nameof(ShowcaseFixture.VariantIndices), MemberType = typeof(ShowcaseFixture))]
    public void Variant_HelpModalBlocksTheInterfaceAndFits(int variant)
    {
        var fixture = new ShowcaseFixture(variant);
        fixture.Host.ToggleHelp();

        foreach (var profile in ScreenProfiles.All)
        {
            var report = LayoutHarness.Validate(fixture.Root, profile);
            Assert.True(report.IsClean, report.Format());

            var tab = fixture.Host.Tabs[0];
            Assert.True(fixture.Root.IsBlockedByModal(tab));
            Assert.NotSame(tab, fixture.Root.HitTest(tab.Rect.Center));
        }

        fixture.Host.ToggleHelp();
        LayoutHarness.Apply(fixture.Root, ScreenProfiles.Widescreen[0]);
        Assert.False(fixture.Root.IsBlockedByModal(fixture.Host.Tabs[0]));
    }

    [Fact]
    public void Sidebar_ItemDetailsPopupFitsBesideEverySlot()
    {
        int sidebar = ShowcaseVariants.CreateAll().ToList().FindIndex(v => v is SidebarVariant);
        var fixture = new ShowcaseFixture(sidebar);
        var popup = ShowcaseFixture.Descendants<ItemDetailsPopup>(fixture.Root.Content).Single();

        foreach (var profile in ScreenProfiles.Quick.Concat(ScreenProfiles.Ultrawide).Concat(ScreenProfiles.Portrait))
        {
            LayoutHarness.Apply(fixture.Root, profile);
            var slots = ShowcaseFixture.Descendants<SlotControl>(fixture.Host.Windows).Concat(ShowcaseFixture.Descendants<SlotControl>(fixture.Host.Hud))
                .Where(s => s.Slot != null).ToList();
            Assert.NotEmpty(slots);

            foreach (var slot in slots)
            {
                popup.Hide();
                LayoutHarness.Click(fixture.Root, slot.Rect.Center, right: true);
                Assert.True(popup.Visible);
                Assert.Same(slot, popup.Target);
                var report = LayoutValidator.Validate(fixture.Root, null, profile + " slot " + slot.Rect);
                Assert.True(report.IsClean, report.Format());
            }

            LayoutHarness.Click(fixture.Root, popup.CloseButton.Rect.Center);
            Assert.False(popup.Visible);
        }
    }

    [Fact]
    public void Showcase_UiScaleIsCappedSoTheMinimumViewportStillFits()
    {
        var fixture = new ShowcaseFixture();
        LayoutHarness.Apply(fixture.Root, new ScreenProfile("HD", 1280, 720, 3f));

        Assert.True(fixture.Root.ViewportSize.X >= ShowcaseHost.MinimumViewport.X - 0.01f);
        Assert.True(fixture.Root.ViewportSize.Y >= ShowcaseHost.MinimumViewport.Y - 0.01f);
    }

    [Fact]
    public void Showcase_UltrawideKeepsTheInterfaceInACentredSafeArea()
    {
        var fixture = new ShowcaseFixture();
        LayoutHarness.Apply(fixture.Root, new ScreenProfile("32:9", 5120, 1440));

        var safe = fixture.Root.SafeRect;
        Assert.Equal(ShowcaseHost.MaxSafeAspect, safe.Width / safe.Height, 2);
        Assert.Equal(fixture.Root.ViewportSize.X * 0.5f, safe.Center.X, 0);
        Assert.True(safe.Encloses(fixture.Host.Bar.Rect, 0.5f));
    }
}
