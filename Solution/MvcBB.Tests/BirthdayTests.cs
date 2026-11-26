using MvcBB.Shared.Services;

namespace MvcBB.Tests;

public class BirthdayTests
{
    private static readonly DateTime Today = new(2026, 11, 26);

    [Fact]
    public void Validate_AcceptsNone_DayAndMonth_OrAFullDate()
    {
        Assert.Null(Birthday.Validate(null, null, null, Today));
        Assert.Null(Birthday.Validate(14, 3, null, Today));
        Assert.Null(Birthday.Validate(14, 3, 1990, Today));
        Assert.Null(Birthday.Validate(29, 2, null, Today));
        Assert.Null(Birthday.Validate(29, 2, 2000, Today));
    }

    [Fact]
    public void Validate_RefusesPartsAndImpossibleDates()
    {
        Assert.NotNull(Birthday.Validate(14, null, null, Today));
        Assert.NotNull(Birthday.Validate(null, null, 1990, Today));
        Assert.NotNull(Birthday.Validate(31, 4, null, Today));
        Assert.NotNull(Birthday.Validate(29, 2, 2001, Today));
        Assert.NotNull(Birthday.Validate(1, 13, null, Today));
        Assert.NotNull(Birthday.Validate(1, 1, 1899, Today));
        Assert.NotNull(Birthday.Validate(27, 11, 2026, Today));
    }

    [Fact]
    public void InYear_MovesLeapDayBirthdays_AndAgeNeedsAYear()
    {
        Assert.Equal(new DateTime(2027, 2, 28), Birthday.InYear(29, 2, 2027));
        Assert.Equal(new DateTime(2028, 2, 29), Birthday.InYear(29, 2, 2028));
        Assert.Equal(36, Birthday.AgeTurning(1990, 2026));
        Assert.Null(Birthday.AgeTurning(null, 2026));
        Assert.Equal("14 March 1990", Birthday.Describe(14, 3, 1990));
        Assert.Equal("14 March", Birthday.Describe(14, 3, null));
        Assert.Equal(string.Empty, Birthday.Describe(null, 3, null));
    }
}
