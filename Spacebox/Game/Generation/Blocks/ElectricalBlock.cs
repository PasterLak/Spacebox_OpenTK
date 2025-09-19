using Spacebox.Game.Resource;

using System.Runtime.CompilerServices;

namespace Spacebox.Game.Generation.Blocks
{
    [Flags]
    public enum ElectricalFlags : byte
    {
        None = 0,
        CanGenerate = 1 << 0,
        CanTransfer = 1 << 1,
        CanConsume = 1 << 2
    }

    public class ElectricalBlock : Block
    {
        private int eData1;
        private int eData2;


        public ElectricalBlock(BlockJSON blockData) : base(blockData)
        {
            CurrentPower = 0;
            EnableEmission = false;
            // MaxPower = 100;
        }

        private bool _isActive;

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                SetEnableEmission(value);
            }
        }
        public short CurrentPower
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (short)(eData1 >> 0 & 0x3FFF);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                int v = value & 0x3FFF;
                eData1 = eData1 & ~(0x3FFF << 0) | v << 0;
            }
        }
        public short MaxPower
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (short)(eData1 >> 14 & 0x3FFF);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                int v = value & 0x3FFF;
                eData1 = eData1 & ~(0x3FFF << 14) | v << 14;
            }
        }
        public ElectricalFlags EFlags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ElectricalFlags)(eData1 >> 28 & 0xF);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                int v = ((int)value & 0xF) << 28;
                eData1 = eData1 & ~(0xF << 28) | v;
            }
        }

        public byte GenerationRate
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)(eData2 >> 0 & 0xFF);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                int v = value & 0xFF;
                eData2 = eData2 & ~(0xFF << 0) | v << 0;
            }
        }
        public byte ConsumptionRate
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)(eData2 >> 8 & 0xFF);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                int v = value & 0xFF;
                eData2 = eData2 & ~(0xFF << 8) | v << 8;
            }
        }

        public virtual void TickElectric()
        {
            if ((EFlags & ElectricalFlags.CanGenerate) != 0)
            {
                short tmp = (short)(CurrentPower + GenerationRate);
                CurrentPower = tmp > MaxPower ? MaxPower : tmp;
            }
            if ((EFlags & ElectricalFlags.CanConsume) != 0)
            {
                short remain = (short)(CurrentPower - ConsumptionRate);
                CurrentPower = remain < 0 ? (short)0 : remain;
            }

            //Debug.Log("electric block energy "  + CurrentPower);

        }

        public short ProvidePower(short amount)
        {
            short actual = Math.Min(CurrentPower, amount);
            CurrentPower -= actual;
            return actual;
        }

        public void ReceivePower(short amount)
        {
            short sum = (short)(CurrentPower + amount);
            CurrentPower = sum > MaxPower ? MaxPower : sum;
        }
    }

}
