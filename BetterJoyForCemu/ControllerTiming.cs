using System;
using System.Diagnostics;
using System.Globalization;

namespace BetterJoyForCemu {
    public sealed class ControllerTiming {
        private readonly object sync = new object();
        private bool hasInput;
        private long previousInput;
        private long inputCount;
        private long maxInputGap;
        private long outputCount;
        private double outputTotal;
        private long maxOutput;
        private long rumbleCount;
        private long maxRumble;

        public void RecordInput(long timestampTicks) {
            lock (sync) {
                inputCount++;
                if (!hasInput) {
                    previousInput = timestampTicks;
                    hasInput = true;
                } else if (timestampTicks > previousInput) {
                    maxInputGap = Math.Max(maxInputGap, timestampTicks - previousInput);
                    previousInput = timestampTicks;
                }
            }
        }

        public void RecordOutput(long elapsedTicks) {
            elapsedTicks = Math.Max(0, elapsedTicks);
            lock (sync) {
                outputCount++;
                outputTotal += elapsedTicks;
                maxOutput = Math.Max(maxOutput, elapsedTicks);
            }
        }

        public void RecordRumble(long elapsedTicks) {
            elapsedTicks = Math.Max(0, elapsedTicks);
            lock (sync) {
                rumbleCount++;
                maxRumble = Math.Max(maxRumble, elapsedTicks);
            }
        }

        public string Snapshot() {
            lock (sync) {
                double millisecondsPerTick = 1000.0 / Stopwatch.Frequency;
                return String.Format(CultureInfo.InvariantCulture,
                    "hidReports={0} maxGapMs={1:F3} outputCount={2} outputMeanMs={3:F3} outputMaxMs={4:F3} rumbleWrites={5} rumbleMaxMs={6:F3}",
                    inputCount, maxInputGap * millisecondsPerTick, outputCount,
                    outputCount == 0 ? 0 : outputTotal / outputCount * millisecondsPerTick,
                    maxOutput * millisecondsPerTick, rumbleCount, maxRumble * millisecondsPerTick);
            }
        }
    }
}
