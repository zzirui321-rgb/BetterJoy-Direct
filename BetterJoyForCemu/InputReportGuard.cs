using System;
using System.Threading;

namespace BetterJoyForCemu {
    public enum InputReportDisposition { Invalid, Duplicate, Process }

    // Command acknowledgements and truncated reports must not reach input parsing.
    public sealed class InputReportGuard {
        private long firstReport = -1;
        private byte lastTimestamp;
        private bool haveTimestamp;
        private byte[] previousReport;
        private volatile bool armed, hasNewMotionSamples;
        private int validReports, rejectedReports, duplicateReports, repeatedTimestamps;
        private int lastReportId = -1, lastReportLength;
        public bool IsArmed { get { return armed; } }
        public bool HasNewMotionSamples { get { return hasNewMotionSamples; } }

        public static bool IsInputReport(byte[] report, int length, bool hasImu) {
            return report != null && length <= report.Length &&
                length >= (hasImu ? 49 : 12) && report[0] == 0x30;
        }

        public bool Accept(byte[] report, int length, bool hasImu, long nowMilliseconds) {
            return Observe(report, length, hasImu, nowMilliseconds) == InputReportDisposition.Process;
        }

        public InputReportDisposition Observe(byte[] report, int length, bool hasImu, long nowMilliseconds) {
            Interlocked.Exchange(ref lastReportLength, length);
            Interlocked.Exchange(ref lastReportId, report != null && report.Length > 0 && length > 0 ? report[0] : -1);
            if (!IsInputReport(report, length, hasImu)) {
                hasNewMotionSamples = false;
                Interlocked.Increment(ref rejectedReports);
                return InputReportDisposition.Invalid;
            }
            hasNewMotionSamples = hasImu && (previousReport == null || previousReport.Length < 49 || report[1] != previousReport[1]);
            if (hasImu && !hasNewMotionSamples)
                for (int i = 13; i < 49 && !hasNewMotionSamples; i++)
                    hasNewMotionSamples = previousReport[i] != report[i];
            Interlocked.Increment(ref validReports);
            if (hasImu && haveTimestamp && report[1] == lastTimestamp)
                Interlocked.Increment(ref repeatedTimestamps);
            haveTimestamp = true;
            lastTimestamp = report[1];

            bool wasArmed = armed;
            if (firstReport < 0) firstReport = nowMilliseconds;
            // Bits 6-7 of the shared byte are transport/status flags, not buttons.
            if (!armed && nowMilliseconds - firstReport >= 300 &&
                report[3] == 0 && (report[4] & 0x3F) == 0 && report[5] == 0) armed = true;

            // Some receivers keep a fixed counter while sending changing input.
            // Identical valid frames still prove liveness and advance neutral settling.
            bool duplicate = hasImu && previousReport != null && previousReport.Length == length;
            for (int i = 0; duplicate && i < length; i++)
                duplicate = previousReport[i] == report[i];
            if (duplicate && wasArmed == armed) {
                Interlocked.Increment(ref duplicateReports);
                return InputReportDisposition.Duplicate;
            }
            if (previousReport == null || previousReport.Length != length)
                previousReport = new byte[length];
            Array.Copy(report, previousReport, length);
            return InputReportDisposition.Process;
        }

        public string Snapshot() {
            int id = Volatile.Read(ref lastReportId);
            return String.Format("validReports={0} rejectedReports={1} duplicateReports={2} repeatedTimestamps={3} lastReportId={4} lastReportLength={5}",
                Volatile.Read(ref validReports), Volatile.Read(ref rejectedReports),
                Volatile.Read(ref duplicateReports), Volatile.Read(ref repeatedTimestamps),
                id < 0 ? "none" : "0x" + id.ToString("X2"), Volatile.Read(ref lastReportLength));
        }
    }
}
