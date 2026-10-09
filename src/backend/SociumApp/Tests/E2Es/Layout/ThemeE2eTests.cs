using Microsoft.Playwright;

namespace Tests.E2Es.Layout;

/// <summary>
/// Переключение светлой и тёмной темы в верхней панели и сохранение выбора между открытиями.
/// </summary>
[Collection(E2eCollection.Name)]
[Trait("Category", "E2E")]
public sealed class ThemeE2eTests(E2eAppFixture fixture)
{
    [Fact]
    public async Task Theme_Toggle_WithLightTheme_ShouldApplyDarkAndKeepAfterReload()
    {
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");
        var root = page.Locator("html");
        var banner = page.GetByRole(AriaRole.Banner);

        await Assertions.Expect(root).ToHaveAttributeAsync("data-theme", "light");
        await banner.GetByRole(AriaRole.Button, new() { Name = "Включить тёмную тему" }).ClickAsync();
        await Assertions.Expect(root).ToHaveAttributeAsync("data-theme", "dark");

        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Assertions.Expect(root).ToHaveAttributeAsync("data-theme", "dark");
        var background = await page.EvaluateAsync<string>("() => getComputedStyle(document.body).backgroundColor");
        Assert.Equal("rgb(33, 34, 38)", background);

        await banner.GetByRole(AriaRole.Button, new() { Name = "Включить светлую тему" }).ClickAsync();
        await Assertions.Expect(root).ToHaveAttributeAsync("data-theme", "light");
    }
}
