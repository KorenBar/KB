using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KB.Threading
{
    public class ActionScheduler
    {
        /// <summary>
        /// Get the next DateTime for weekly schedule.
        /// </summary>
        /// <param name="days">Days to invoke</param>
        /// <param name="timeOfDay">Time of day to invoke</param>
        /// <returns>The next DateTime from now</returns>
        public static DateTime GetNextDateTime(DayOfWeek[] days, TimeSpan timeOfDay)
        {
            if (days.Length == 0) throw new ArgumentException("Must be at least one DayOfWeek.", "days");
            DateTime now = DateTime.Now;
            DateTime nextTime = now.Date.Add(timeOfDay);
            if (nextTime < now)
                nextTime = nextTime.AddDays(1);
            while (!days.Contains(nextTime.DayOfWeek)) // Based on that "days" can't be empty (else it will be infinity loop).
                nextTime = nextTime.AddDays(1);
            return nextTime;
        }

        private List<DateTime> triggers = new List<DateTime>(); // Invoked triggers will be saved here.
        public DateTime[] Triggers => this.triggers.ToArray();
        public DateTime[] InvokedTriggers => this.triggers.Where(t => t <= DateTime.Now).ToArray();
        public DateTime[] ActiveTriggers => this.triggers.Where(t => t > DateTime.Now).ToArray();

        private bool multipleTriggers = false;
        public bool MultipleTriggers
        {
            get { return this.multipleTriggers; }
            set
            {
                this.multipleTriggers = value;
                if (!this.multipleTriggers) // If MultipleTriggers is false all of the active triggers will be removed except the last one.
                    for (int i = 0; i < this.triggers.Count - 1; i++) // Save the last trigger
                        if (this.triggers[i] > DateTime.Now)
                            this.triggers.RemoveAt(i); // TODO: Check it
            }
        }

        public bool Enabled { get; set; } = true;

        public Action Action { get; set; }

        public ActionScheduler() : this(null) { }
        public ActionScheduler(Action action) => this.Action = action;

        public async Task<bool> Invoke(DateTime trigger)
        {
            if (trigger <= DateTime.Now)
                throw new ArgumentException(
                    trigger.Date < DateTime.Now.Date
                        ? "The date has passed."
                        : "The time has passed.",
                    "trigger");

            if (!this.MultipleTriggers)
                this.triggers.RemoveAll(t => t > DateTime.Now);
            this.triggers.Add(trigger);

            while (trigger > DateTime.Now)
                if (this.triggers.Contains(trigger))
                    await Task.Delay(1000);
                else return false;

            if (this.Action == null) return false;
            if (!this.Enabled)
            {
                Console.WriteLine("Trigger is Disabled");
                return false;
            }

            this.Action();
            return true;
        }

        public int Revoke() => this.triggers.RemoveAll(t => t > DateTime.Now);

        public int Revoke(DateTime trigger) => this.triggers.RemoveAll(t => t == trigger && t > DateTime.Now);
    }
}
