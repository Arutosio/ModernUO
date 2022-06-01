using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server;
using Server.Items;

namespace Server.ArutosioElements
{
    class ItemsConverter : Item
    {
        [Constructible]
        public ItemsConverter()
            : base(0x14F0)
        {
            Weight = 1.0;
            Hue = 0x4;
            LootType = LootType.Blessed;
        }

        public ItemsConverter(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
        Boolean generating = false;
        public override void OnDoubleClick(Mobile from)
        {
            if (!generating)
            {
                generating = true;
                int max_x = this.Map.Width;
                int max_y = this.Map.Height;
                int file_count = 1;
                Console.WriteLine("Starting to generate land map({0}, {1})...", max_x, max_y);
                FileStream stream = new FileStream(this.Map.Name + "_multis_" + file_count + ".bin", FileMode.Append);
                byte[] buf;

                for (int x = 0; x < max_x; ++x)
                {
                    for (int y = 0; y < max_y; ++y)
                    {
                        foreach (StaticTile tile in this.Map.Tiles.GetStaticTiles(x, y))
                        {
                            Console.WriteLine("TileID: " + tile.ID + " at " + x + " " + y + " " + tile.Z);
                            if (tile.ID > 1)
                            {
                                if (stream.Length > 5000000)
                                {
                                    stream.Flush();
                                    stream.Dispose();
                                    stream.Close();
                                    ++file_count;
                                    stream = new FileStream(this.Map.Name + "_multis_" + file_count + ".bin", FileMode.Append);
                                }
                                buf = BitConverter.GetBytes(x);
                                Array.Reverse(buf);
                                stream.Write(buf, 0, buf.Length);

                                buf = BitConverter.GetBytes(y);
                                Array.Reverse(buf);
                                stream.Write(buf, 0, buf.Length);

                                buf = BitConverter.GetBytes(tile.Z);
                                Array.Reverse(buf);
                                stream.Write(buf, 0, buf.Length);

                                buf = BitConverter.GetBytes(tile.ID);
                                Array.Reverse(buf);
                                stream.Write(buf, 0, buf.Length);

                                buf = BitConverter.GetBytes(tile.Height);
                                Array.Reverse(buf);
                                stream.Write(buf, 0, buf.Length);
                            }
                        }
                    }
                }
                Console.WriteLine("Done!");

                stream.Flush();
                stream.Dispose();
                stream.Close();
                generating = false;
            }
        }
    }
}
