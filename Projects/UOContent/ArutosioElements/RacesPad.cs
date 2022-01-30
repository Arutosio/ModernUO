using Server;
using Server.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UOContent.ArutosioElements
{
    //private static enum EnumRaces enumRace = Race.race.
    class RacesPad : Item
    {
        private EnumRaces enRaces = EnumRaces.Human;
        private TimeSpan delay = TimeSpan.FromSeconds(2.5);
        private Mobile mobileUsing = null;

        [Constructible]
        public RacesPad() : base(0x9DD7)
        {
            Name = "Races Pad";
            PadRace = Race.DefaultRace;
            Movable = true;
            Visible = false;
            IsSoloForHuman = true;
            Hue = 0x690;
        }

        public RacesPad(Serial serial) : base(serial) { }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
            writer.Write(IsSoloForHuman);
            writer.Write((int)EnRace);
            writer.Write(PadRace);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        IsSoloForHuman = reader.ReadBool();
                        EnRace = (EnumRaces)reader.ReadInt();
                        PadRace = reader.ReadRace();
                        break;
                    }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public EnumRaces EnRace
        {
            get { return enRaces; }
            set { enRaces = value; PadRace = Race.Races[(int)value]; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Server.Race PadRace { get; private set; } = Race.DefaultRace;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsSoloForHuman { get; set; } = true;

        private Mobile MobileUsing
        {
            get { return mobileUsing; }
            set { if (value == null || !value.Alive) { mobileUsing = value; } }
        }

        private bool IsMobileUsingOnPad(Mobile m)
        {
            if (MobileUsing != null && m == MobileUsing && m.Race != PadRace)
            {
                return Location == m.Location;
            }
            return false;
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (!IsMobileUsingOnPad(m))
            {
                MobileUsing = null;

                if (!m.Alive)
                {
                    if (m.Race == PadRace)
                    {
                        m.SendMessage(0x33, $"You already belong to the race of {PadRace.PluralName}.");
                        return false;
                    }
                    else if (m.Race != PadRace)
                    {
                        if (IsSoloForHuman)
                        {
                            do
                            {
                                if (m.Female)
                                {
                                    m.PlaySound(0x14C);
                                }
                                else
                                {
                                    m.PlaySound(0x156);
                                }
                                m.Say("*I feel something rejecting me..*");
                                Timer.Pause(TimeSpan.FromSeconds(1));
                            } while (IsMobileUsingOnPad(m) && !m.Alive);
                        }
                        else
                        {
                            MobileUsing = m;
                            m.Say("Something strange is going on, it would be better to wait..");
                            RaceSteps(m);
                            return true;
                        }
                    }
                }
                else
                {
                    m.SendMessage(0x33, "You must be a ghost.");
                    return true;
                }
            }
            else
            {
                m.SendMessage(0x33, "There is already someone..");
                return false;
            }
            return false;
        }

        //OnMoveOff non è quando si esce dalla posizione del oggetto
        public override bool OnMoveOff(Mobile m)
        {
            //if (m == MobileUsing)
            //{
            //    m.SendMessage(0x66, "OnMoveOff MobileUsing = null");
            //    MobileUsing = null;
            //}
            return true;
        }

        private bool IsValidToRace(Mobile m)
        {
            if (IsMobileUsingOnPad(m))
            {
                if (!IsSoloForHuman || (m.Race.Name.Equals("Human") && (m.Race != PadRace && !m.Alive)))
                {
                    if (m == MobileUsing)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private async void RaceSteps(Mobile m)
        {
            await Timer.Pause(delay);
            if (IsValidToRace(m))
            {
                m.Say("*I feel an aura approaching..*");
                m.PlaySound(0x17F);
                await Timer.Pause(delay);
                if (IsValidToRace(m))
                {
                    m.Say($"*you begin to feel the presence of {PadRace.PluralName} souls..*");
                    await Timer.Pause(delay);
                    if (IsValidToRace(m))
                    {
                        m.Say("*You start to feel cold..*");
                        if (m.Female)
                        {
                            m.PlaySound(0x14C);
                        }
                        else
                        {
                            m.PlaySound(0x156);
                        }
                        await Timer.Pause(TimeSpan.FromSeconds(1));
                        if (IsValidToRace(m))
                        {
                            m.PlaySound(0x182);
                            m.Drop(new Blood(0x122B), m.Location);
                            await Timer.Pause(TimeSpan.FromSeconds(1));
                            if (IsValidToRace(m))
                                m.Say("*I have pains everywhere..*");
                            await Timer.Pause(TimeSpan.FromMilliseconds(500));
                            if (IsValidToRace(m))
                            {
                                if (m.Female)
                                {
                                    m.PlaySound(0x150);
                                }
                                else
                                {
                                    m.PlaySound(0x15A);
                                }
                                SetRace(m);
                            }
                            await Timer.Pause(TimeSpan.FromSeconds(1));
                            m.Say("*My soul has changed..*");
                        }
                        else
                        {
                            await Timer.Pause(TimeSpan.FromSeconds(1));
                            m.Say("*The aura fades..*");
                        }
                    }
                }
            }
            else if (IsSoloForHuman || m.Race != PadRace)
            {
                if (IsMobileUsingOnPad(m) && !m.Alive)
                {
                    do
                    {
                        if (m.Female)
                        {
                            m.PlaySound(0x14C);
                        }
                        else
                        {
                            m.PlaySound(0x156);
                        }
                        m.Say("*I feel something rejecting me..*");
                        await Timer.Pause(TimeSpan.FromSeconds(1));
                    } while (IsMobileUsingOnPad(m) && !m.Alive);
                }
            }
        }

        private void SetRace(Mobile mToSet)
        {
            mToSet.Resurrect();
            mToSet.Race = PadRace;
            mToSet.StatCap = 260;
            switch (EnRace)
            {
                case EnumRaces.Human:
                    {
                        mToSet.Hue = 0x83F1;
                        mToSet.RawStr = 100;
                        mToSet.RawDex = 100;
                        mToSet.RawInt = 60;
                        break;
                    }
                case EnumRaces.Elf:
                    {
                        mToSet.Hue = 0x23B;
                        mToSet.RawStr = 100;
                        mToSet.RawDex = 150;
                        mToSet.RawInt = 10;
                        break;
                    }
                case EnumRaces.Gargoyle:
                    {
                        mToSet.Hue = 0x86DB;
                        mToSet.RawStr = 130;
                        mToSet.RawDex = 120;
                        mToSet.RawInt = 10;
                        break;
                    }
                case EnumRaces.DarkElf:
                    {
                        mToSet.Hue = 0xCE;
                        mToSet.RawStr = 100;
                        mToSet.RawDex = 10;
                        mToSet.RawInt = 150;
                        break;
                    }
                case EnumRaces.Orc:
                    {
                        mToSet.Hue = 0x78D;
                        mToSet.RawStr = 150;
                        mToSet.RawDex = 100;
                        mToSet.RawInt = 10;
                        break;
                    }
                case EnumRaces.Vampire:
                    {
                        mToSet.Hue = 0x7E8;
                        mToSet.RawStr = 115;
                        mToSet.RawDex = 130;
                        mToSet.RawInt = 15;
                        break;
                    }
                case EnumRaces.Dwarf:
                    {
                        mToSet.Hue = 0x7E1;
                        mToSet.RawStr = 140;
                        mToSet.RawDex = 100;
                        mToSet.RawInt = 20;
                        break;
                    }
                case EnumRaces.Demon:
                    {
                        mToSet.Hue = 0x7AC;
                        mToSet.RawStr = 120;
                        mToSet.RawDex = 20;
                        mToSet.RawInt = 120;
                        break;
                    }
                default:
                    {

                    }
                    break;
            }

            if (mToSet.Female)
            {
                // BodyValue
                mToSet.Body = PadRace.FemaleBody;
            }
            else
            {
                // BodyValue
                mToSet.Body = PadRace.MaleBody;
            }
            mToSet.SendMessage(0x33, $"You have become a {mToSet.Race.Name}");
        }
    }
}
