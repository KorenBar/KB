using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;

namespace KB.Communication
{
    /// <summary>
    /// Will keep your serial port open if was closed unexpectedly.
    /// </summary>
    public class SafeSerialPort : SerialPort
    {
        public event EventHandler Opened;
        public event EventHandler Closed;

        public double ReOpenInterval
        {
            get => reOpenTimer.Interval;
            set => reOpenTimer.Interval = value;
        }

        private System.Timers.Timer reOpenTimer = new System.Timers.Timer(1000) { AutoReset = true, Enabled = false };
        private bool isOpen = false;
        private void ReOpenTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!base.IsOpen) try { base.Open(); } catch { }
            bool bIsOpen = base.IsOpen;
            if (this.isOpen != bIsOpen)
                if (bIsOpen) this.Opened?.Invoke(this, new EventArgs());
                else this.Closed?.Invoke(this, new EventArgs());
            this.isOpen = bIsOpen;
        }

        /// <summary>
        /// Once the Open method was called the timer will start running to make sure that serial port will stay opened.
        /// </summary>
        public new void Open()
        {
            try { base.Open(); }
            catch (Exception ex) { throw ex; }
            finally
            {
                isOpen = base.IsOpen;
                if (isOpen) this.Opened?.Invoke(this, new EventArgs());
                if (!reOpenTimer.Enabled) // Was not opened already
                    reOpenTimer.Elapsed += ReOpenTimer_Elapsed;
                reOpenTimer.Enabled = true;
            }
        }

        /// <summary>
        /// Will close the serial port and stop the reconnect timer.
        /// </summary>
        public new void Close()
        {
            try { base.Close(); }
            catch (Exception ex) { throw ex; }
            finally
            {
                isOpen = base.IsOpen;
                if (!isOpen) this.Closed?.Invoke(this, new EventArgs());
                if (reOpenTimer.Enabled) // Was not closed already
                    reOpenTimer.Elapsed -= ReOpenTimer_Elapsed;
                reOpenTimer.Enabled = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            reOpenTimer.Enabled = false;
            reOpenTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
