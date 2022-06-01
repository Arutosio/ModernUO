using Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Ultima;

namespace Server.ArutosioElements
{
    class LandConverter : Item
    {
        [Constructible]
        public LandConverter() : base(0x2345) { }

        public LandConverter(Serial serial) : base(serial) { }
        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
        }
        public override void OnDoubleClick(Mobile from)
        {
            base.OnDoubleClick(from);
            Server.TileMatrix tiles = from.Map.Tiles;

            int pieces = (int)Math.Ceiling((double)(from.Map.Tiles.BlockWidth * from.Map.Tiles.BlockHeight) / (312501.0d));
            Console.WriteLine("Pieces:" + pieces);

            int lastx = 0;
            int lasty = 0;
            for (int i = 0; i < pieces; ++i)
            {
                Console.WriteLine("Writing " + from.Map.Name + "_land_" + (i + 1) + ".bin" + "...");
                FileStream stream = File.Open(from.Map.Name + "_land_" + (i + 1) + ".bin", FileMode.Create);
                BinaryWriter writer = new BinaryWriter(stream);
                Server.LandTile tile;
                int processed = 0;
                for (int x = lastx; x < from.Map.Tiles.BlockWidth; ++x)
                {
                    for (int y = lasty; y < from.Map.Tiles.BlockHeight; ++y)
                    {
                        tile = from.Map.Tiles.GetLandTile(x, y);
                        writer.Write(x);
                        writer.Write(y);
                        writer.Write(tile.Z);
                        writer.Write(tile.ID);
                        ++processed;
                        if (processed >= 312501)
                        {
                            lasty = y + 1;
                            break;
                        }
                        lasty = 0;
                    }
                    if (processed >= 312501)
                    {
                        lastx = x;
                        processed = 0;
                        break;
                    }
                }
                writer.Close();
            }
        }
    }
}
