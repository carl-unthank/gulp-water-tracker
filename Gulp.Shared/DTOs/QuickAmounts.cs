namespace Gulp.Shared.DTOs;

public static class QuickAmounts
{
    public static readonly List<QuickAmountDto> DefaultAmounts = new()
    {
        new() { AmountMl = 250, Label = "Cup", Icon = "🥤" },
        new() { AmountMl = 330, Label = "Can", Icon = "🥫" },
        new() { AmountMl = 500, Label = "Bottle", Icon = "🍼" },
        new() { AmountMl = 750, Label = "Large Bottle", Icon = "🍾" },
        new() { AmountMl = 1000, Label = "Liter", Icon = "💧" }
    };
}
