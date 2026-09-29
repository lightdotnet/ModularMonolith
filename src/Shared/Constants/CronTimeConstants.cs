namespace StarterKit.Shared.Constants;

public abstract class CronTimeConstants
{
    public static readonly string Every5Minutes = EveryMins(5);

    public static readonly string Every15Minutes = EveryMins(15);

    public static readonly string Every30Minutes = EveryMins(30);

    public const string Every1Hour = "0 * * * *";

    public static readonly string Every3Hours = EveryHours(3);

    public static readonly string EveryDayAt0Utc7 = EveryDayAt(17);

    public static readonly string EveryDayAt1Utc7 = EveryDayAt(18);

    public static readonly string EveryDayAt5Utc7 = EveryDayAt(22);

    public static readonly string EveryDayAt6Utc7 = EveryDayAt(23);

    public static string EveryMins(int mins) => $"*/{mins} * * * *";

    public static string EveryHours(int hour) => $"0 */{hour} * * *";

    public static string EveryDayAt(int utcHour) => $"00 {utcHour} * * *";
}
