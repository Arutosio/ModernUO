using Server;
using Server.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UOContent.ArutosioElements
{
    internal class DungeonKey : Key
    {
        private DungeonsZone enDungeonsZone = DungeonsZone.Unknown;

        [Constructible]
        public DungeonKey ()
        {
            Name = "Dungeon Key";
            Hue = 0x7AC;
        }

        public DungeonKey(Serial serial) : base(serial) { }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
            writer.Write((int)enDungeonsZone);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    {
                        EnDungeonsZone = (DungeonsZone)reader.ReadInt();
                        break;
                    }
            }
        }

        private string GetNameKey() => $"Dungeon Key { enDungeonsZone.ToString() }";

        [CommandProperty(AccessLevel.GameMaster)]
        public DungeonsZone EnDungeonsZone
        {
            get { return enDungeonsZone; }
            set { enDungeonsZone = value; KeyValue = 5000 + Convert.ToUInt32((int)enDungeonsZone); Name = GetNameKey(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string StringDungeonsZone
        {
            get { return enDungeonsZone.ToString(); }
            set
            {
                try
                {
                    EnDungeonsZone = (DungeonsZone)Enum.Parse(typeof(DungeonsZone), value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
