using System;

namespace BetterJoyForCemu {
    // Only full standard input reports have the layout consumed by Joycon.
    // Command acknowledgements and simple HID reports must never become input.
    public sealed class InputReportGuard {
        private long firstReport = -1;
        private byte lastTimestamp;
        private bool haveTimestamp;
        private bool armed;
        public bool IsArmed { get { return armed; } }

        public static bool IsInputReport(byte[] report, int length, bool hasImu) {
            return report != null && length <= report.Length &&
                length >= (hasImu ? 49 : 12) && report[0] == 0x30;
        }

        public bool Accept(byte[] report, int length, bool hasImu, long nowMilliseconds) {
            if (!IsInputReport(report, length, hasImu)) return false;
            if (hasImu && haveTimestamp && report[1] == lastTimestamp) return false;
            haveTimestamp = true;
            lastTimestamp = report[1];
            if (firstReport < 0) firstReport = nowMilliseconds;
            // A wake-up button must be released before it can trigger an action.
            if (!armed && nowMilliseconds - firstReport >= 300 &&
                report[3] == 0 && report[4] == 0 && report[5] == 0) armed = true;
            return true;
        }
    }
}
