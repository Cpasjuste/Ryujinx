﻿using Ryujinx.Core.OsHle.Handles;
using System;

namespace Ryujinx.Core.Input
{
    public class Hid
    {
        /*
         * Reference:
         * https://github.com/reswitched/libtransistor/blob/development/lib/hid.c
         * https://github.com/reswitched/libtransistor/blob/development/include/libtransistor/hid.h
         * https://github.com/switchbrew/libnx/blob/master/nx/source/services/hid.c
         * https://github.com/switchbrew/libnx/blob/master/nx/include/switch/services/hid.h
         */

        private const int HidHeaderSize            = 0x400;
        private const int HidTouchScreenSize       = 0x3000;
        private const int HidMouseSize             = 0x400;
        private const int HidKeyboardSize          = 0x400;
        private const int HidUnkSection1Size       = 0x400;
        private const int HidUnkSection2Size       = 0x400;
        private const int HidUnkSection3Size       = 0x400;
        private const int HidUnkSection4Size       = 0x400;
        private const int HidUnkSection5Size       = 0x200;
        private const int HidUnkSection6Size       = 0x200;
        private const int HidUnkSection7Size       = 0x200;
        private const int HidUnkSection8Size       = 0x800;
        private const int HidControllerSerialsSize = 0x4000;
        private const int HidControllersSize       = 0x32000;
        private const int HidUnkSection9Size       = 0x800;

        private const int HidTouchHeaderSize = 0x28;
        private const int HidTouchEntrySize  = 0x298;

        private const int HidTouchEntryHeaderSize = 0x10;
        private const int HidTouchEntryTouchSize  = 0x28;

        private const int HidControllerSize        = 0x5000;
        private const int HidControllerHeaderSize  = 0x28;
        private const int HidControllerLayoutsSize = 0x350;

        private const int HidControllersLayoutHeaderSize = 0x20;
        private const int HidControllersInputEntrySize   = 0x30;

        private const int HidHeaderOffset            = 0;
        private const int HidTouchScreenOffset       = HidHeaderOffset            + HidHeaderSize;
        private const int HidMouseOffset             = HidTouchScreenOffset       + HidTouchScreenSize;
        private const int HidKeyboardOffset          = HidMouseOffset             + HidMouseSize;
        private const int HidUnkSection1Offset       = HidKeyboardOffset          + HidKeyboardSize;
        private const int HidUnkSection2Offset       = HidUnkSection1Offset       + HidUnkSection1Size;
        private const int HidUnkSection3Offset       = HidUnkSection2Offset       + HidUnkSection2Size;
        private const int HidUnkSection4Offset       = HidUnkSection3Offset       + HidUnkSection3Size;
        private const int HidUnkSection5Offset       = HidUnkSection4Offset       + HidUnkSection4Size;
        private const int HidUnkSection6Offset       = HidUnkSection5Offset       + HidUnkSection5Size;
        private const int HidUnkSection7Offset       = HidUnkSection6Offset       + HidUnkSection6Size;
        private const int HidUnkSection8Offset       = HidUnkSection7Offset       + HidUnkSection7Size;
        private const int HidControllerSerialsOffset = HidUnkSection8Offset       + HidUnkSection8Size;
        private const int HidControllersOffset       = HidControllerSerialsOffset + HidControllerSerialsSize;
        private const int HidUnkSection9Offset       = HidControllersOffset       + HidControllersSize;

        private const int HidEntryCount = 17;

        private object ShMemLock;

        private long[] ShMemPositions;

        private long CurrControllerEntry;
        private long CurrTouchEntry;
        private long CurrTouchSampleCounter;

        private Switch Ns;

        public Hid(Switch Ns)
        {
            this.Ns = Ns;

            ShMemLock = new object();

            ShMemPositions = new long[0];
        }

        internal void ShMemMap(object sender, EventArgs e)
        {
            HSharedMem SharedMem = (HSharedMem)sender;

            lock (ShMemLock)
            {
                ShMemPositions = SharedMem.GetVirtualPositions();

                long BasePosition = ShMemPositions[ShMemPositions.Length - 1];

                Logging.Info($"HID shared memory successfully mapped to 0x{BasePosition:x16}!");

                Init(BasePosition);
            }
        }

        internal void ShMemUnmap(object sender, EventArgs e)
        {
            HSharedMem SharedMem = (HSharedMem)sender;

            lock (ShMemLock)
            {
                ShMemPositions = SharedMem.GetVirtualPositions();
            }
        }

        private void Init(long BasePosition)
        {
            InitializeJoyconPair(
                BasePosition,
                JoyConColor.Body_Neon_Red,
                JoyConColor.Buttons_Neon_Red,
                JoyConColor.Body_Neon_Blue,
                JoyConColor.Buttons_Neon_Blue);
        }

        private void InitializeJoyconPair(
            long        BasePosition,
            JoyConColor LeftColorBody,
            JoyConColor LeftColorButtons,
            JoyConColor RightColorBody,
            JoyConColor RightColorButtons)
        {
            long BaseControllerOffset = BasePosition + HidControllersOffset + 8 * HidControllerSize;

            HidControllerType Type =
                HidControllerType.ControllerType_Handheld |
                HidControllerType.ControllerType_JoyconPair;

            bool IsHalf = false;

            HidControllerColorDesc SingleColorDesc =
                HidControllerColorDesc.ColorDesc_ColorsNonexistent;

            JoyConColor SingleColorBody    = JoyConColor.Black;
            JoyConColor SingleColorButtons = JoyConColor.Black;

            HidControllerColorDesc SplitColorDesc = 0;

            Ns.Memory.WriteInt32(BaseControllerOffset + 0x0,  (int)Type);

            Ns.Memory.WriteInt32(BaseControllerOffset + 0x4,  IsHalf ? 1 : 0);

            Ns.Memory.WriteInt32(BaseControllerOffset + 0x8,  (int)SingleColorDesc);
            Ns.Memory.WriteInt32(BaseControllerOffset + 0xc,  (int)SingleColorBody);
            Ns.Memory.WriteInt32(BaseControllerOffset + 0x10, (int)SingleColorButtons);
            Ns.Memory.WriteInt32(BaseControllerOffset + 0x14, (int)SplitColorDesc);

            Ns.Memory.WriteInt32(BaseControllerOffset + 0x18, (int)LeftColorBody);
            Ns.Memory.WriteInt32(BaseControllerOffset + 0x1c, (int)LeftColorButtons);

            Ns.Memory.WriteInt32(BaseControllerOffset + 0x20, (int)RightColorBody);
            Ns.Memory.WriteInt32(BaseControllerOffset + 0x24, (int)RightColorButtons);
        }

        public void SetJoyconButton(
            HidControllerId      ControllerId,
            HidControllerLayouts ControllerLayout,
            HidControllerButtons Buttons,
            HidJoystickPosition  LeftStick,
            HidJoystickPosition  RightStick)
        {
            lock (ShMemLock)
            {
                foreach (long Position in ShMemPositions)
                {
                    WriteJoyconButtons(
                        Position,
                        ControllerId,
                        ControllerLayout,
                        Buttons,
                        LeftStick,
                        RightStick);
                }
            }
        }

        private void WriteJoyconButtons(
            long                 BasePosition,
            HidControllerId      ControllerId,
            HidControllerLayouts ControllerLayout,
            HidControllerButtons Buttons,
            HidJoystickPosition  LeftStick,
            HidJoystickPosition  RightStick)
        {
            long ControllerOffset = BasePosition + HidControllersOffset;

            ControllerOffset += (int)ControllerId * HidControllerSize;

            ControllerOffset += HidControllerHeaderSize;

            ControllerOffset += (int)ControllerLayout * HidControllerLayoutsSize;

            CurrControllerEntry = (CurrControllerEntry + 1) % HidEntryCount;

            long Timestamp = GetTimestamp();

            Ns.Memory.WriteInt64(ControllerOffset + 0x0,  Timestamp);
            Ns.Memory.WriteInt64(ControllerOffset + 0x8,  HidEntryCount);
            Ns.Memory.WriteInt64(ControllerOffset + 0x10, CurrControllerEntry);
            Ns.Memory.WriteInt64(ControllerOffset + 0x18, HidEntryCount - 1);

            ControllerOffset += HidControllersLayoutHeaderSize;

            ControllerOffset += CurrControllerEntry * HidControllersInputEntrySize;

            Ns.Memory.WriteInt64(ControllerOffset + 0x0,  Timestamp);
            Ns.Memory.WriteInt64(ControllerOffset + 0x8,  Timestamp);

            Ns.Memory.WriteInt64(ControllerOffset + 0x10, (uint)Buttons);

            Ns.Memory.WriteInt32(ControllerOffset + 0x18, LeftStick.DX);
            Ns.Memory.WriteInt32(ControllerOffset + 0x1c, LeftStick.DY);

            Ns.Memory.WriteInt32(ControllerOffset + 0x20, RightStick.DX);
            Ns.Memory.WriteInt32(ControllerOffset + 0x24, RightStick.DY);

            Ns.Memory.WriteInt64(ControllerOffset + 0x28,
                (uint)HidControllerConnState.Controller_State_Connected |
                (uint)HidControllerConnState.Controller_State_Wired);
        }

        public void SetTouchPoints(params HidTouchPoint[] Points)
        {
            lock (ShMemLock)
            {
                foreach (long Position in ShMemPositions)
                {
                    WriteTouchPoints(Position, Points);
                }
            }
        }

        private void WriteTouchPoints(long BasePosition, params HidTouchPoint[] Points)
        {
            long TouchScreenOffset = BasePosition + HidTouchScreenOffset;

            long Timestamp = GetTimestamp();

            CurrTouchEntry = (CurrTouchEntry + 1) % HidEntryCount;

            Ns.Memory.WriteInt64(TouchScreenOffset + 0x0,  Timestamp);
            Ns.Memory.WriteInt64(TouchScreenOffset + 0x8,  HidEntryCount);
            Ns.Memory.WriteInt64(TouchScreenOffset + 0x10, CurrTouchEntry);
            Ns.Memory.WriteInt64(TouchScreenOffset + 0x18, HidEntryCount - 1);
            Ns.Memory.WriteInt64(TouchScreenOffset + 0x20, Timestamp);            

            long TouchEntryOffset = TouchScreenOffset + HidTouchHeaderSize;

            TouchEntryOffset += CurrTouchEntry * HidTouchEntrySize;            

            Ns.Memory.WriteInt64(TouchEntryOffset + 0x0, CurrTouchSampleCounter++);
            Ns.Memory.WriteInt64(TouchEntryOffset + 0x8, Points.Length);

            TouchEntryOffset += HidTouchEntryHeaderSize;

            const int Padding = 0;

            int Index = 0;

            foreach (HidTouchPoint Point in Points)
            {
                Ns.Memory.WriteInt64(TouchEntryOffset + 0x0,  Timestamp);
                Ns.Memory.WriteInt32(TouchEntryOffset + 0x8,  Padding);
                Ns.Memory.WriteInt32(TouchEntryOffset + 0xc,  Index++);
                Ns.Memory.WriteInt32(TouchEntryOffset + 0x10, Point.X);
                Ns.Memory.WriteInt32(TouchEntryOffset + 0x14, Point.Y);
                Ns.Memory.WriteInt32(TouchEntryOffset + 0x18, Point.DiameterX);
                Ns.Memory.WriteInt32(TouchEntryOffset + 0x1c, Point.DiameterY);
                Ns.Memory.WriteInt32(TouchEntryOffset + 0x20, Point.Angle);
                Ns.Memory.WriteInt32(TouchEntryOffset + 0x24, Padding);

                TouchEntryOffset += HidTouchEntryTouchSize;
            }
        }

        private long GetTimestamp()
        {
            return Environment.TickCount * 19_200;
        }
    }
}