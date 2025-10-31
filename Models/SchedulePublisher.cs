using System;

namespace OpenIDApp.Models
{
    public class SchedulePublisher
    {
        private readonly SlotPlan _plan;
        private readonly int _durationMinutes;

        public SchedulePublisher(SlotPlan plan, int durationMinutes)
        {
            _plan = plan;
            _durationMinutes = durationMinutes;
        }

        public DateTime ToStartUtc(int dayIndex, int slotId)
            => _plan.GetSlotStartUtc(dayIndex, slotId);

        public DateTime ToEndUtc(int dayIndex, int slotId)
            => _plan.GetSlotEndUtc(dayIndex, slotId);
    }
}
