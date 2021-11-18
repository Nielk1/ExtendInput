using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput
{
    static class XInputNative
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct XInputGamepad
        {
            [MarshalAs(UnmanagedType.I2)]
            [FieldOffset(0)]
            public short wButtons;

            [MarshalAs(UnmanagedType.I1)]
            [FieldOffset(2)]
            public byte bLeftTrigger;

            [MarshalAs(UnmanagedType.I1)]
            [FieldOffset(3)]
            public byte bRightTrigger;

            [MarshalAs(UnmanagedType.I2)]
            [FieldOffset(4)]
            public short sThumbLX;

            [MarshalAs(UnmanagedType.I2)]
            [FieldOffset(6)]
            public short sThumbLY;

            [MarshalAs(UnmanagedType.I2)]
            [FieldOffset(8)]
            public short sThumbRX;

            [MarshalAs(UnmanagedType.I2)]
            [FieldOffset(10)]
            public short sThumbRY;

            public bool IsButtonPressed(int buttonFlags)
            {
                return (wButtons & buttonFlags) == buttonFlags;
            }

            public bool IsButtonPresent(int buttonFlags)
            {
                return (wButtons & buttonFlags) == buttonFlags;
            }

            public void Copy(XInputGamepad source)
            {
                sThumbLX = source.sThumbLX;
                sThumbLY = source.sThumbLY;
                sThumbRX = source.sThumbRX;
                sThumbRY = source.sThumbRY;
                bLeftTrigger = source.bLeftTrigger;
                bRightTrigger = source.bRightTrigger;
                wButtons = source.wButtons;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is XInputGamepad))
                    return false;
                XInputGamepad source = (XInputGamepad)obj;
                return ((sThumbLX == source.sThumbLX)
                     && (sThumbLY == source.sThumbLY)
                     && (sThumbRX == source.sThumbRX)
                     && (sThumbRY == source.sThumbRY)
                     && (bLeftTrigger == source.bLeftTrigger)
                     && (bRightTrigger == source.bRightTrigger)
                     && (wButtons == source.wButtons));
            }

            public override int GetHashCode()
            {
                return sThumbLX.GetHashCode()
                     ^ sThumbLY.GetHashCode()
                     ^ sThumbRX.GetHashCode()
                     ^ sThumbRY.GetHashCode()
                     ^ bLeftTrigger.GetHashCode()
                     ^ bRightTrigger.GetHashCode()
                     ^ wButtons.GetHashCode();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XInputVibration
        {
            [MarshalAs(UnmanagedType.I2)]
            public ushort LeftMotorSpeed;

            [MarshalAs(UnmanagedType.I2)]
            public ushort RightMotorSpeed;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct XInputCapabilities
        {
            [MarshalAs(UnmanagedType.I1)]
            [FieldOffset(0)]
            byte Type;

            [MarshalAs(UnmanagedType.I1)]
            [FieldOffset(1)]
            public byte SubType;

            [MarshalAs(UnmanagedType.I2)]
            [FieldOffset(2)]
            public short Flags;


            [FieldOffset(4)]
            public XInputGamepad Gamepad;

            [FieldOffset(16)]
            public XInputVibration Vibration;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct XInputCapabilitiesEx
        {
            public XInputCapabilities Capabilities;
            [MarshalAs(UnmanagedType.U2)]
            public UInt16 VID;
            [MarshalAs(UnmanagedType.U2)]
            public UInt16 PID;
            [MarshalAs(UnmanagedType.U2)]
            public UInt16 REV;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 XID;
        };


        [DllImport("xinput1_4.dll")]
        public static extern int XInputGetCapabilities
        (
            int dwUserIndex,   // [in] Index of the gamer associated with the device
            int dwFlags,       // [in] Input flags that identify the device type
            ref XInputCapabilities pCapabilities  // [out] Receives the capabilities
        );

        [DllImport("xinput1_4.dll", EntryPoint = "#108")]
        public static extern int XInputGetCapabilitiesEx
        (
            int a1,            // [in] unknown, should probably be 1
            int dwUserIndex,   // [in] Index of the gamer associated with the device
            int dwFlags,       // [in] Input flags that identify the device type
            ref XInputCapabilitiesEx pCapabilities  // [out] Receives the capabilities
        );


        [StructLayout(LayoutKind.Sequential)]
        public struct XInputBaseBusInformation
        {
            [MarshalAs(UnmanagedType.U2)]
            UInt16 VID;
            [MarshalAs(UnmanagedType.U2)]
            UInt16 PID;
            [MarshalAs(UnmanagedType.U4)]
            UInt32 a3;
            [MarshalAs(UnmanagedType.U4)]
            UInt32 Flags; // probably
            [MarshalAs(UnmanagedType.U1)]
            byte a4;
            [MarshalAs(UnmanagedType.U1)]
            byte a5;
            [MarshalAs(UnmanagedType.U1)]
            byte a6;
            [MarshalAs(UnmanagedType.U1)]
            byte reserved;
        }
        [DllImport("xinput1_4.dll", EntryPoint = "#104")]
        public static extern int XInputGetBaseBusInformation(int dwUserIndex, ref XInputBaseBusInformation pInfo);





        [StructLayout(LayoutKind.Sequential)]
        public struct XInputState
        {
            public UInt32 dwPacketNumber;
            public XInputGamepad Gamepad;
        }
        [DllImport("xinput1_4.dll")]
        public static extern int XInputGetState
        (
            int dwUserIndex,   // [in] Index of the gamer associated with the device
            ref XInputState pState  // [out] Receives the state
        );

        [DllImport("xinput1_4.dll", EntryPoint = "#100")]
        public static extern int XInputGetStateEx
        (
            int dwUserIndex,   // [in] Index of the gamer associated with the device
            ref XInputState pState  // [out] Receives the state
        );
    }
}
