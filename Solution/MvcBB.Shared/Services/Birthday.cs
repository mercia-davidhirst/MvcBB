using System.Globalization;

namespace MvcBB.Shared.Services
{
    /// <summary>
    /// A member's birthday as they gave it on their profile: day and month
    /// together, the year optional (some members don't want their age shown).
    /// The one place that checks and describes them, for the API, the profile
    /// page and mods (the Calendar shows them each year).
    /// </summary>
    public static class Birthday
    {
        public const int EarliestYear = 1900;

        /// <summary>
        /// Checks a birthday. All three blank is fine (no birthday). Returns
        /// the message to show, or null when it's fine. 29 February is allowed
        /// with no year, or with a leap year.
        /// </summary>
        public static string? Validate(int? day, int? month, int? year, DateTime todayUtc)
        {
            if (day == null && month == null && year == null) return null;
            if (day == null || month == null) return "Please give both the day and the month of your birthday, or leave all three blank.";
            if (month is < 1 or > 12) return "That month isn't valid.";

            // A leap year when none is given, so 29 February counts.
            var daysInMonth = DateTime.DaysInMonth(year is >= EarliestYear and <= 9999 ? year.Value : 2000, month.Value);
            if (day < 1 || day > daysInMonth) return "That day isn't in that month.";

            if (year != null)
            {
                if (year < EarliestYear || year > todayUtc.Year) return $"The year must be between {EarliestYear} and {todayUtc.Year}.";
                if (new DateTime(year.Value, month.Value, day.Value) > todayUtc.Date) return "Your birthday can't be in the future.";
            }

            return null;
        }

        /// <summary>
        /// When the birthday falls in <paramref name="year"/>. 29 February is
        /// celebrated on 28 February in other years.
        /// </summary>
        public static DateTime InYear(int day, int month, int year) =>
            new(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));

        /// <summary>The age turned on the birthday in <paramref name="year"/>, or null when no birth year was given.</summary>
        public static int? AgeTurning(int? birthYear, int year) =>
            birthYear is int born && year >= born ? year - born : null;

        /// <summary>"14 March", or "14 March 1990" when the year was given. Empty when there's no birthday.</summary>
        public static string Describe(int? day, int? month, int? year)
        {
            if (day is not int d || month is not int m || m < 1 || m > 12) return string.Empty;
            var text = d.ToString(CultureInfo.InvariantCulture) + " " + CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m);
            return year is int y ? text + " " + y.ToString(CultureInfo.InvariantCulture) : text;
        }
    }
}
