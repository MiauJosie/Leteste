using System;
using System.IO;
using Leteste.Graphics;
using Leteste.Levels;
using Leteste.Physics;
using Microsoft.Xna.Framework;

namespace Fishbowcat.Levels;

public static class TileMap
{
    private const int TILE_SIZE = 8;

    public static void LoadFromCSV(
        Level level,
        string csvPath,
        string tilesetName,
        bool createColliders = true
    )
    {
        var tileset = new Sprite(tilesetName);

        if (!File.Exists(csvPath))
        {
            Console.WriteLine($"Warning: Map file not found at {csvPath}");
            return;
        }

        string[] lines = File.ReadAllLines(csvPath);

        for (int y = 0; y < lines.Length; y++)
        {
            string[] tiles = lines[y].Split(',');
            for (int x = 0; x < tiles.Length; x++)
            {
                if (int.TryParse(tiles[x], out int tileId) && tileId != -1)
                {
                    var position = new Vector2(x * TILE_SIZE, y * TILE_SIZE);

                    // Calculate source rectangle from tileId
                    int tilesPerRow = tileset.Width / TILE_SIZE;
                    int tileX = (tileId % tilesPerRow) * TILE_SIZE;
                    int tileY = (tileId / tilesPerRow) * TILE_SIZE;

                    if (createColliders)
                    {
                        var solid = new Solid(
                            level,
                            position,
                            TILE_SIZE,
                            TILE_SIZE
                        );
                        solid.sprite = tileset;
                        solid.sourceRect = new Rectangle(tileX, tileY, TILE_SIZE, TILE_SIZE);
                        level.AddSolid(solid);
                    }
                }
            }
        }
    }
}