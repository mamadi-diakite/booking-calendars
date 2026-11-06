namespace BookingCalendars.Domain;

public readonly record struct WeeklyHoursSettings(DayOfWeek Day, TimeOnly Start, TimeOnly End);

public static class WeeklyHoursRules
{
    /// <returns>One message per problem, empty when the ranges can be saved.</returns>
    public static IReadOnlyList<string> Check(IEnumerable<WeeklyHoursSettings> ranges)
    {
        ArgumentNullException.ThrowIfNull(ranges);

        var errors = new List<string>();
        var valid = new List<WeeklyHoursSettings>();
        foreach (var range in ranges)
        {
            if (!Enum.IsDefined(range.Day))
            {
                errors.Add("Day is not valid.");
            }
            else if (range.End <= range.Start)
            {
                errors.Add($"{range.Day} {Format(range)}: a range must end after it starts.");
            }
            else
            {
                valid.Add(range);
            }
        }

        foreach (var day in valid.GroupBy(r => r.Day))
        {
            var sorted = day.OrderBy(r => r.Start).ToList();
            for (var i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].Start < sorted[i - 1].End)
                {
                    errors.Add($"{day.Key}: {Format(sorted[i - 1])} and {Format(sorted[i])} overlap.");
                }
            }
        }

        return errors;
    }

    private static string Format(WeeklyHoursSettings range) => $"{range.Start:HH\\:mm}-{range.End:HH\\:mm}";
}
