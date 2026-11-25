using System.Globalization;
using MvcBB.Shared.Models.AdminEmail;

namespace MvcBB.App.Models
{
    /// <summary>
    /// The geometry of the Sending activity chart, worked out here so the view
    /// only draws it: stacked bars per kind, the host limit "ruler", the line
    /// where admin emails stop (the limit less the share kept for site
    /// emails), the rolling 60-minute line (24-hour view), and the y axis.
    /// Drawn in a fixed viewBox and scaled by CSS.
    /// </summary>
    public class EmailActivityChart
    {
        public const double Width = 800;
        public const double Height = 300;
        public const double Left = 48;
        public const double Right = 16;
        public const double Top = 16;
        public const double Bottom = 36;

        public static double PlotWidth => Width - Left - Right;
        public static double PlotHeight => Height - Top - Bottom;
        public static double PlotBottom => Height - Bottom;

        /// <summary>The order kinds are stacked in, bottom up, with their CSS class and legend name.</summary>
        public static readonly IReadOnlyList<(EmailKind Kind, string CssClass, string Name)> Kinds = new[]
        {
            (EmailKind.AdminEmail, "email-chart-admin", "Email Users"),
            (EmailKind.Notification, "email-chart-notification", "Notifications"),
            (EmailKind.PasswordReset, "email-chart-reset", "Password resets"),
            (EmailKind.Registration, "email-chart-registration", "Registration"),
            (EmailKind.Other, "email-chart-other", "Other")
        };

        public EmailActivity Activity { get; }

        /// <summary>The host limit for one bucket (per hour on the 24-hour view, per day otherwise), 0 for none.</summary>
        public int Limit { get; }

        /// <summary>Where admin emails stop (the limit less the reserve), 0 for no limit.</summary>
        public int AdminShare { get; }

        /// <summary>The top of the y axis.</summary>
        public int YMax { get; }

        public List<Bar> Bars { get; } = new();
        public List<(double Y, int Value)> Ticks { get; } = new();

        /// <summary>The rolling 60-minute line's points (24-hour view only), and whether each is over the limit.</summary>
        public List<(double X, double Y, int Count, DateTime AtUtc, bool Over)> RollingPoints { get; } = new();

        public double? LimitY => Limit > 0 ? Y(Limit) : null;
        public double? AdminShareY => AdminShare > 0 && AdminShare < Limit ? Y(AdminShare) : null;

        public string RollingPolyline => string.Join(" ", RollingPoints.Select(p => $"{F(p.X)},{F(p.Y)}"));

        public EmailActivityChart(EmailActivity activity, IReadOnlyList<string> labels)
        {
            Activity = activity;
            var usage = activity.Usage;
            Limit = activity.Hourly ? usage.LimitPerHour : usage.LimitPerDay;
            AdminShare = Limit > 0 ? Limit - (activity.Hourly ? usage.ReservePerHour : usage.ReservePerDay) : 0;

            var highest = Math.Max(Limit, activity.Buckets.Select(b => b.Total).DefaultIfEmpty(0).Max());
            if (activity.Hourly)
                highest = Math.Max(highest, activity.RollingHour.Select(p => p.Count).DefaultIfEmpty(0).Max());
            YMax = NiceCeiling(highest);

            // Five steps: every YMax NiceCeiling picks (10, 20, 25, 50...) divides by 5.
            for (var i = 0; i <= 5; i++)
            {
                var value = YMax * i / 5;
                Ticks.Add((Y(value), value));
            }

            var count = Math.Max(1, activity.Buckets.Count);
            var slot = PlotWidth / count;
            var barWidth = Math.Max(2, slot * 0.7);
            var labelEvery = count <= 8 ? 1 : count <= 24 ? 3 : 5;
            for (var i = 0; i < activity.Buckets.Count; i++)
            {
                var bucket = activity.Buckets[i];
                var bar = new Bar
                {
                    Bucket = bucket,
                    X = Left + slot * i + (slot - barWidth) / 2,
                    Width = barWidth,
                    CenterX = Left + slot * i + slot / 2,
                    Label = i < labels.Count ? labels[i] : string.Empty,
                    ShowLabel = (count - 1 - i) % labelEvery == 0,
                    Over = Limit > 0 && bucket.Total > Limit
                };

                var running = 0;
                foreach (var (kind, cssClass, _) in Kinds)
                {
                    var value = bucket.CountOf(kind);
                    if (value == 0)
                        continue;
                    bar.Segments.Add((Y(running + value), Y(running) - Y(running + value), cssClass));
                    running += value;
                }

                if (bar.Over)
                    bar.OverSegment = (Y(bucket.Total), Y(Limit) - Y(bucket.Total));

                Bars.Add(bar);
            }

            if (activity.Hourly && activity.Buckets.Count > 0)
            {
                var start = activity.Buckets[0].StartUtc;
                var span = (activity.Buckets[^1].EndUtc - start).TotalMinutes;
                foreach (var point in activity.RollingHour)
                {
                    var x = Left + PlotWidth * Math.Clamp((point.AtUtc - start).TotalMinutes / span, 0, 1);
                    RollingPoints.Add((x, Y(point.Count), point.Count, point.AtUtc, Limit > 0 && point.Count > Limit));
                }
            }
        }

        public double Y(int value) => PlotBottom - PlotHeight * value / Math.Max(1, YMax);

        /// <summary>Invariant-culture number for SVG attributes.</summary>
        public static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        // 1, 2 or 5 times a power of ten at or above value (at least 10), so the axis reads cleanly.
        private static int NiceCeiling(int value)
        {
            if (value <= 10)
                return 10;
            var target = value * 1.1;
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(target)));
            foreach (var step in new[] { 1, 2, 2.5, 5, 10 })
            {
                if (step * magnitude >= target)
                    return (int)(step * magnitude);
            }
            return (int)(10 * magnitude);
        }

        public class Bar
        {
            public EmailActivityBucket Bucket { get; set; } = new();
            public double X { get; set; }
            public double Width { get; set; }
            public double CenterX { get; set; }
            public string Label { get; set; } = string.Empty;
            public bool ShowLabel { get; set; }
            public bool Over { get; set; }
            public List<(double Y, double Height, string CssClass)> Segments { get; } = new();

            /// <summary>The part above the limit, drawn in the danger colour.</summary>
            public (double Y, double Height)? OverSegment { get; set; }

            /// <summary>"120 emails: 100 Email Users, 15 password resets..." for the bar's title and aria-label.</summary>
            public string Describe(string period)
            {
                var parts = Kinds
                    .Where(k => Bucket.CountOf(k.Kind) > 0)
                    .Select(k => $"{Bucket.CountOf(k.Kind)} {k.Name.ToLowerInvariant()}");
                var detail = string.Join(", ", parts);
                return $"{period}: {Bucket.Total} email(s){(detail.Length > 0 ? $" ({detail})" : string.Empty)}{(Over ? ", over the host limit" : string.Empty)}";
            }
        }
    }
}
