namespace OpenIDApp.Models
{
    public static class TimeSlot
    {
        // 4 ca / ngày cố định
        public const int SlotsPerDay = 4;

        public static (TimeSpan start, TimeSpan end) GetSlotLocalTime(int slotId)
        {
            return slotId switch
            {
                0 => (new TimeSpan(7, 0, 0),  new TimeSpan(8, 30, 0)),  // 07:00 - 08:30
                1 => (new TimeSpan(9, 30, 0), new TimeSpan(11, 0, 0)),  // 09:30 - 11:00
                2 => (new TimeSpan(13, 0, 0), new TimeSpan(14, 30, 0)), // 13:00 - 14:30
                3 => (new TimeSpan(15, 30, 0),new TimeSpan(17, 0, 0)),  // 15:30 - 17:00
                _ => throw new ArgumentOutOfRangeException(nameof(slotId))
            };
        }
    }
}
