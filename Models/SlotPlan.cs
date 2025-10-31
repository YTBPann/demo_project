using System;

namespace OpenIDApp.Models
{
    public class SlotPlan
    {
        public DateTime BaseDateUtc { get; }        // ví dụ: ngày đầu kỳ thi (UTC)
        public int NumDays { get; }                 // 7..10 ngày
        public TimeZoneInfo TimeZone { get; }       // "Asia/Ho_Chi_Minh" nếu cần

        public SlotPlan(DateTime baseDateUtc, int numDays, TimeZoneInfo tz)
        {
            BaseDateUtc = baseDateUtc.Date;
            NumDays = numDays;
            TimeZone = tz;
        }

        public DateTime GetSlotStartUtc(int dayIndex, int slotId)
        {
            if (dayIndex < 0 || dayIndex >= NumDays) throw new ArgumentOutOfRangeException(nameof(dayIndex));
            var (start, _) = TimeSlot.GetSlotLocalTime(slotId);

            // base local date -> utc
            var localDate = TimeZoneInfo.ConvertTimeFromUtc(BaseDateUtc, TimeZone).Date.AddDays(dayIndex);
            var localStart = localDate.Add(start);
            return TimeZoneInfo.ConvertTimeToUtc(localStart, TimeZone);
        }

        public DateTime GetSlotEndUtc(int dayIndex, int slotId)
        {
            var (_, end) = TimeSlot.GetSlotLocalTime(slotId);
            var startUtc = GetSlotStartUtc(dayIndex, slotId);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(
                TimeZoneInfo.ConvertTimeFromUtc(startUtc, TimeZone).Date.Add(end),
                TimeZone
            );
            return endUtc;
        }

        public int GetDayIndexFromUtc(DateTime utc)
        {
            var day0Local = TimeZoneInfo.ConvertTimeFromUtc(BaseDateUtc, TimeZone).Date;
            var dayLocal  = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone).Date;
            return (int)(dayLocal - day0Local).TotalDays;
        }
    }
}
