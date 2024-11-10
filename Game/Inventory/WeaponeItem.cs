﻿namespace Spacebox.Game
{
    public class WeaponeItem : Item
    {
        public byte Damage = 15;

        public WeaponeItem(byte stackSize, string name, byte x, byte y, float modelDepth) : base(stackSize, name, x, y, modelDepth)
        {
        }
    }
}
