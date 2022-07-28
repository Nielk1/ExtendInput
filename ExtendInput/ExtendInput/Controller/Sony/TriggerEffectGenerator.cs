/*
 * MIT License
 * 
 * Copyright (c) 2021 John "Nielk1" Klein
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Linq;

namespace ExtendInput.DataTools.DualSense
{
    public enum TriggerEffectType : byte
    {
        Off       = 0x05, // 00 00 0 101 // Safe to Use, part of libpad
        Feedback  = 0x21, // 00 10 0 001 // Safe to Use, part of libpad
        Bow       = 0x22, // 00 10 0 010 // Suspect, part of safe block
        Galloping = 0x23, // 00 10 0 011 // Suspect, part of safe block
        Weapon    = 0x25, // 00 10 0 101 // Safe to Use, part of libpad
        Vibration = 0x26, // 00 10 0 110 // Safe to Use, part of libpad
        Machine   = 0x27, // 00 10 0 111 // Suspect, part of safe block
    }

    /// <summary>
    /// DualSense controller trigger effect generators.
    /// Revision: CUSTOM
    /// </summary>
    unsafe public static class TriggerEffectGenerator
    {
        /// <summary>
        /// Reset effect data generator.
        /// This is used by libpad and is expected to be present in future DualSense firmware.
        /// </summary>
        /// <param name="destinationArray">The byte[] that receives the data.</param>
        /// <param name="destinationIndex">A 32-bit integer that represents the index in the destinationArray at which storing begins.</param>
        /// <returns>The success of the effect write.</returns>
        static public bool Off(byte* destinationArray, int destinationIndex)
        {
            destinationArray[destinationIndex + 0] = (byte)TriggerEffectType.Off;
            destinationArray[destinationIndex + 1] = 0x00;
            destinationArray[destinationIndex + 2] = 0x00;
            destinationArray[destinationIndex + 3] = 0x00;
            destinationArray[destinationIndex + 4] = 0x00;
            destinationArray[destinationIndex + 5] = 0x00;
            destinationArray[destinationIndex + 6] = 0x00;
            destinationArray[destinationIndex + 7] = 0x00;
            destinationArray[destinationIndex + 8] = 0x00;
            destinationArray[destinationIndex + 9] = 0x00;
            destinationArray[destinationIndex + 10] = 0x00;
            return true;
        }

        /// <summary>
        /// Resistance effect data generator.
        /// This is used by libpad and is expected to be present in future DualSense firmware.
        /// </summary>
        /// <seealso cref="SimpleResistance(byte[], int, byte, byte)"/>
        /// <seealso cref="LimitedResistance(byte[], int, byte, byte)"/>
        /// <param name="destinationArray">The byte[] that receives the data.</param>
        /// <param name="destinationIndex">A 32-bit integer that represents the index in the destinationArray at which storing begins.</param>
        /// <param name="start">The starting zone of the trigger effect. Must be between 0 and 9 inclusive.</param>
        /// <param name="resistance">The force of the resistance. Must be between 0 and 8 inclusive.</param>
        /// <returns>The success of the effect write.</returns>
        static public bool Feedback(byte* destinationArray, int destinationIndex, byte start, byte resistance)
        {
            if (start > 9)
                return false;
            if (resistance > 8)
                return false;
            if (resistance > 0)
            {
                byte forceValue = (byte)((resistance - 1) & 0x07);
                UInt32 forceZones = 0;
                UInt16 activeZones = 0;
                for (int i = start; i < 10; i++)
                {
                    forceZones |= (UInt32)(forceValue << (3 * i));
                    activeZones |= (UInt16)(1 << i);
                }

                destinationArray[destinationIndex + 0] = (byte)TriggerEffectType.Feedback;
                destinationArray[destinationIndex + 1] = (byte)((activeZones >> 0) & 0xff);
                destinationArray[destinationIndex + 2] = (byte)((activeZones >> 8) & 0xff);
                destinationArray[destinationIndex + 3] = (byte)((forceZones >> 0) & 0xff);
                destinationArray[destinationIndex + 4] = (byte)((forceZones >> 8) & 0xff);
                destinationArray[destinationIndex + 5] = (byte)((forceZones >> 16) & 0xff);
                destinationArray[destinationIndex + 6] = (byte)((forceZones >> 24) & 0xff);
                destinationArray[destinationIndex + 7] = 0x00; // (byte)((forceZones >> 32) & 0xff); // need 64bit for this, but we already have enough space
                destinationArray[destinationIndex + 8] = 0x00; // (byte)((forceZones >> 40) & 0xff); // need 64bit for this, but we already have enough space
                destinationArray[destinationIndex + 9] = 0x00;
                destinationArray[destinationIndex + 10] = 0x00;
                return true;
            }
            return Off(destinationArray, destinationIndex);
        }

        /// <summary>
        /// Bow effect data generator.
        /// This is not used by libpad but is in the used effect block, it may be removed in a future DualSense firmware.
        /// </summary>
        /// <param name="destinationArray">The byte[] that receives the data.</param>
        /// <param name="destinationIndex">A 32-bit integer that represents the index in the destinationArray at which storing begins.</param>
        /// <param name="start">The starting zone of the trigger effect. Must be between 0 and 8 inclusive.</param>
        /// <param name="end">The ending zone of the trigger effect. Must be between <paramref name="start"/> + 1 and 8 inclusive.</param>
        /// <param name="resistance">The force of the resistance. Must be between 0 and 8 inclusive.</param>
        /// <param name="snapForce">The force of the snap-back. Must be between 0 and 8 inclusive.</param>
        /// <returns>The success of the effect write.</returns>
        static public bool Bow(byte* destinationArray, int destinationIndex, byte start, byte end, byte resistance, byte snapForce)
        {
            if (start > 8)
                return false;
            if (end > 8)
                return false;
            if (start >= end)
                return false;
            if (resistance > 8)
                return false;
            if (snapForce > 8)
                return false;
            if (end > 0 && resistance > 0 && snapForce > 0)
            {
                UInt16 startAndStopZones = (UInt16)((1 << start) | (1 << end));
                UInt32 forcePair = (UInt32)((((resistance - 1) & 0x07) << (3 * 0))
                                          | (((snapForce - 1) & 0x07) << (3 * 1)));

                destinationArray[destinationIndex + 0] = (byte)TriggerEffectType.Bow;
                destinationArray[destinationIndex + 1] = (byte)((startAndStopZones >> 0) & 0xff);
                destinationArray[destinationIndex + 2] = (byte)((startAndStopZones >> 8) & 0xff);
                destinationArray[destinationIndex + 3] = (byte)((forcePair >> 0) & 0xff);
                destinationArray[destinationIndex + 4] = (byte)((forcePair >> 8) & 0xff);
                destinationArray[destinationIndex + 5] = 0x00;
                destinationArray[destinationIndex + 6] = 0x00;
                destinationArray[destinationIndex + 7] = 0x00;
                destinationArray[destinationIndex + 8] = 0x00;
                destinationArray[destinationIndex + 9] = 0x00;
                destinationArray[destinationIndex + 10] = 0x00;
                return true;
            }
            return Off(destinationArray, destinationIndex);
        }

        /// <summary>
        /// Galloping effect data generator.
        /// This is not used by libpad but is in the used effect block, it may be removed in a future DualSense firmware.
        /// </summary>
        /// <param name="destinationArray">The byte[] that receives the data.</param>
        /// <param name="destinationIndex">A 32-bit integer that represents the index in the destinationArray at which storing begins.</param>
        /// <param name="start">The starting zone of the trigger effect. Must be between 0 and 8 inclusive.</param>
        /// <param name="end">The ending zone of the trigger effect. Must be between <paramref name="start"/> + 1 and 9 inclusive.</param>
        /// <param name="firstFoot">Position of second foot in cycle. Must be between 0 and 6 inclusive.</param>
        /// <param name="secondFoot">Position of second foot in cycle. Must be between <paramref name="firstFoot"/> + 1 and 7 inclusive.</param>
        /// <param name="frequency">Frequency of the automatic cycling action in hertz.</param>
        /// <returns>The success of the effect write.</returns>
        static public bool Galloping(byte* destinationArray, int destinationIndex, byte start, byte end, byte firstFoot, byte secondFoot, byte frequency)
        {
            if (start > 8)
                return false;
            if (end > 9)
                return false;
            if (start >= end)
                return false;
            if (secondFoot > 7)
                return false;
            if (firstFoot > 6)
                return false;
            if (firstFoot >= secondFoot)
                return false;
            if (frequency > 0)
            {
                UInt16 startAndStopZones = (UInt16)((1 << start) | (1 << end));
                UInt32 timeAndRatio = (UInt32)(((secondFoot & 0x07) << (3 * 0))
                                             | ((firstFoot  & 0x07) << (3 * 1)));

                destinationArray[destinationIndex + 0] = (byte)TriggerEffectType.Galloping;
                destinationArray[destinationIndex + 1] = (byte)((startAndStopZones >> 0) & 0xff);
                destinationArray[destinationIndex + 2] = (byte)((startAndStopZones >> 8) & 0xff);
                destinationArray[destinationIndex + 3] = (byte)((timeAndRatio >> 0) & 0xff);
                destinationArray[destinationIndex + 4] = frequency; // this is actually packed into 3 bits, but since it's only one why bother with the fancy code?
                destinationArray[destinationIndex + 5] = 0x00;
                destinationArray[destinationIndex + 6] = 0x00;
                destinationArray[destinationIndex + 7] = 0x00;
                destinationArray[destinationIndex + 8] = 0x00;
                destinationArray[destinationIndex + 9] = 0x00;
                destinationArray[destinationIndex + 10] = 0x00;
                return true;
            }
            return Off(destinationArray, destinationIndex);
        }

        /// <summary>
        /// Semi-automatic gun effect data generator.
        /// This is used by libpad and is expected to be present in future DualSense firmware.
        /// </summary>
        /// <seealso cref="SimpleSemiAutomaticGun(byte[], int, byte, byte, byte)"/>
        /// <seealso cref="LimitedSemiAutomaticGun(byte[], int, byte, byte, byte)"/>
        /// <param name="destinationArray">The byte[] that receives the data.</param>
        /// <param name="destinationIndex">A 32-bit integer that represents the index in the destinationArray at which storing begins.</param>
        /// <param name="start">The starting zone of the trigger effect. Must be between 2 and 7 inclusive.</param>
        /// <param name="end">The ending zone of the trigger effect. Must be between <paramref name="start"/>+1 and 8 inclusive.</param>
        /// <param name="resistance">The force of the resistance. Must be between 0 and 8 inclusive.</param>
        /// <returns>The success of the effect write.</returns>
        static public bool Weapon(byte* destinationArray, int destinationIndex, byte start, byte end, byte resistance)
        {
            if (start > 7 || start < 2)
                return false;
            if (end > 8)
                return false;
            if (end <= start)
                return false;
            if (resistance > 8)
                return false;
            if (resistance > 0)
            {
                UInt16 startAndStopZones = (UInt16)((1 << start) | (1 << end));

                destinationArray[destinationIndex + 0] = (byte)TriggerEffectType.Weapon;
                destinationArray[destinationIndex + 1] = (byte)((startAndStopZones >> 0) & 0xff);
                destinationArray[destinationIndex + 2] = (byte)((startAndStopZones >> 8) & 0xff);
                destinationArray[destinationIndex + 3] = (byte)(resistance - 1); // this is actually packed into 3 bits, but since it's only one why bother with the fancy code?
                destinationArray[destinationIndex + 4] = 0x00;
                destinationArray[destinationIndex + 5] = 0x00;
                destinationArray[destinationIndex + 6] = 0x00;
                destinationArray[destinationIndex + 7] = 0x00;
                destinationArray[destinationIndex + 8] = 0x00;
                destinationArray[destinationIndex + 9] = 0x00;
                destinationArray[destinationIndex + 10] = 0x00;
                return true;
            }
            return Off(destinationArray, destinationIndex);
        }

        /// <summary>
        /// Automatic gun effect data generator.
        /// This is used by libpad and is expected to be present in future DualSense firmware.
        /// </summary>
        /// <seealso cref="SimpleAutomaticGun(byte[], int, byte, byte, byte)"/>
        /// <param name="destinationArray">The byte[] that receives the data.</param>
        /// <param name="destinationIndex">A 32-bit integer that represents the index in the destinationArray at which storing begins.</param>
        /// <param name="start">The starting zone of the trigger effect. Must be between 0 and 9 inclusive.</param>
        /// <param name="amplitude">Strength of the automatic cycling action. Must be between 0 and 8 inclusive.</param>
        /// <param name="frequency">Frequency of the automatic cycling action in hertz.</param>
        /// <returns>The success of the effect write.</returns>
        static public bool Vibration(byte* destinationArray, int destinationIndex, byte start, byte amplitude, byte frequency)
        {
            if (start > 9)
                return false;
            if (amplitude > 8)
                return false;
            if (amplitude > 0 && frequency > 0)
            {
                byte strengthValue = (byte)((amplitude - 1) & 0x07);
                UInt32 strengthZones = 0;
                UInt16 activeZones = 0;
                for (int i = start; i < 10; i++)
                {
                    strengthZones |= (UInt32)(strengthValue << (3 * i));
                    activeZones |= (UInt16)(1 << i);
                }

                destinationArray[destinationIndex + 0] = (byte)TriggerEffectType.Vibration;
                destinationArray[destinationIndex + 1] = (byte)((activeZones >> 0) & 0xff);
                destinationArray[destinationIndex + 2] = (byte)((activeZones >> 8) & 0xff);
                destinationArray[destinationIndex + 3] = (byte)((strengthZones >> 0) & 0xff);
                destinationArray[destinationIndex + 4] = (byte)((strengthZones >> 8) & 0xff);
                destinationArray[destinationIndex + 5] = (byte)((strengthZones >> 16) & 0xff);
                destinationArray[destinationIndex + 6] = (byte)((strengthZones >> 24) & 0xff);
                destinationArray[destinationIndex + 7] = 0x00; // (byte)((strengthZones >> 32) & 0xff); // need 64bit for this, but we already have enough space
                destinationArray[destinationIndex + 8] = 0x00; // (byte)((strengthZones >> 40) & 0xff); // need 64bit for this, but we already have enough space
                destinationArray[destinationIndex + 9] = frequency;
                destinationArray[destinationIndex + 10] = 0x00;
                return true;
            }
            return Off(destinationArray, destinationIndex);
        }

        /// <summary>
        /// Machine effect data generator.
        /// This is not used by libpad but is in the used effect block, it may be removed in a future DualSense firmware.
        /// </summary>
        /// <param name="destinationArray">The byte[] that receives the data.</param>
        /// <param name="destinationIndex">A 32-bit integer that represents the index in the destinationArray at which storing begins.</param>
        /// <param name="start">The starting zone of the trigger effect. Must be between 0 and 8 inclusive.</param>
        /// <param name="end">The ending zone of the trigger effect. Must be between <paramref name="start"/> and 9 inclusive.</param>
        /// <param name="strengthA">Primary strength of cycling action. Must be between 0 and 7 inclusive.</param>
        /// <param name="strengthB">Secondary strength of cycling action. Must be between 0 and 7 inclusive.</param>
        /// <param name="frequency">Frequency of the automatic cycling action in hertz.</param>
        /// <param name="period">Period of the oscillation between <paramref name="strengthA"/> and <paramref name="strengthB"/> in tenths of a second.</param>
        /// <returns>The success of the effect write.</returns>
        static public bool Machine(byte* destinationArray, int destinationIndex, byte start, byte end, byte strengthA, byte strengthB, byte frequency, byte period)
        {
            if (start > 8)
                return false;
            if (end > 9)
                return false;
            if (end <= start)
                return false;
            if (strengthA > 7)
                return false;
            if (strengthB > 7)
                return false;
            if (frequency > 0)
            {
                UInt16 startAndStopZones = (UInt16)((1 << start) | (1 << end));
                UInt32 strengthPair = (UInt32)(((strengthA & 0x07) << (3 * 0))
                                             | ((strengthB & 0x07) << (3 * 1)));

                destinationArray[destinationIndex + 0] = (byte)TriggerEffectType.Machine;
                destinationArray[destinationIndex + 1] = (byte)((startAndStopZones >> 0) & 0xff);
                destinationArray[destinationIndex + 2] = (byte)((startAndStopZones >> 8) & 0xff);
                destinationArray[destinationIndex + 3] = (byte)((strengthPair >> 0) & 0xff);
                destinationArray[destinationIndex + 4] = frequency;
                destinationArray[destinationIndex + 5] = period;
                destinationArray[destinationIndex + 6] = 0x00;
                destinationArray[destinationIndex + 7] = 0x00;
                destinationArray[destinationIndex + 8] = 0x00;
                destinationArray[destinationIndex + 9] = 0x00;
                destinationArray[destinationIndex + 10] = 0x00;
                return true;
            }
            return Off(destinationArray, destinationIndex);
        }
    }
}