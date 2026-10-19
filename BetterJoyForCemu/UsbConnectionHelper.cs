using System;

namespace BetterJoyForCemu {
    public static class UsbConnectionHelper {
        public const string LegacyUsbSerial = "000000000001";

        public static bool IsUsbTransport(string path, string serialNumber, bool isNintendoController) {
            if (isNintendoController && IsBluetoothPath(path))
                return false;

            if (String.Equals(serialNumber, LegacyUsbSerial, StringComparison.Ordinal))
                return true;

            if (!isNintendoController)
                return false;

            if (String.IsNullOrWhiteSpace(serialNumber))
                return true;

            return !LooksLikeBluetoothMac(serialNumber);
        }

        public static bool IsSuccessfulWrite(int written) {
            return written > 0;
        }

        public static bool IsEstablishedStream(int validInputReports) {
            return validInputReports >= 3;
        }

        public static bool IsExpectedResponse(byte[] buffer, int received, byte command) {
            return buffer != null && received >= 2 && received <= buffer.Length &&
                   buffer[0] == 0x81 && buffer[1] == command;
        }

        private static bool IsBluetoothPath(string path) {
            if (String.IsNullOrWhiteSpace(path))
                return false;

            return path.IndexOf("BTHENUM", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   path.IndexOf("BTHLEDEVICE", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool LooksLikeBluetoothMac(string serialNumber) {
            if (serialNumber == null || serialNumber.Length != 12)
                return false;

            for (int i = 0; i < serialNumber.Length; i++)
                if (!Uri.IsHexDigit(serialNumber[i]))
                    return false;

            return true;
        }
    }
}
