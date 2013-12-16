using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TiledLib;

namespace LD28
{
    public enum ItemType
    {
        Melee,
        Chute

    }

    public enum ItemName
    {
        Weapon0,
        Weapon1,
        Weapon2,
        Weapon3,
        Weapon4,
        Chute
    }

    public class Item
    {
        public static Random rand = new Random();

        public ItemType Type;
        public ItemName Name;
        public Vector2 Position;
        public Vector2 DroppedPosition;
        public Vector2 Speed;
        public bool InWorld = false;
        public bool Dead = false;
        public float Health;
        public Dude Owner;
        public float Range;
        public double Cooldown;

        Texture2D itemTexture;
        Rectangle itemSource;

        internal double coolDownTime = 0;

        float alpha = 1f;

        public Item(ItemType type, ItemName name, Texture2D tex, Rectangle src)
        {
            itemTexture = tex;
            itemSource = src;
            Type = type;
            Name = name;
            Health = 121f;
        }

        public virtual void Update(GameTime gameTime, Map gameMap, float planeRot)
        {
            coolDownTime -= gameTime.ElapsedGameTime.TotalMilliseconds;

            if (InWorld)
            {
                Speed.Y += 0.25f;

                

                if (Position.Y >= DroppedPosition.Y)
                {
                    Position.Y = DroppedPosition.Y;
                    if (Speed.Y > 2f)
                    {
                        Speed.Y = -(Speed.Y * 0.5f);
                    }
                    else
                    {
                        Speed.Y = 0f;
                        
                    }
                }

                Position.X -= (planeRot * 2f);
                Position.Y += Speed.Y;

                Position.X = MathHelper.Clamp(Position.X, 1150, gameMap.Width * gameMap.TileWidth - 400f);
                Position.Y = MathHelper.Clamp(Position.Y, 0, gameMap.Height * gameMap.TileHeight);
            }

            if (Health <= 0f)
            {
                if(!InWorld)
                {
                    InWorld = true;
                    Owner.Item = null;
                    DroppedPosition = Owner.Position;
                    Position = Owner.Position + new Vector2(Owner.faceDir * 75, -75);
                }
                alpha -= 0.01f;
                alpha = MathHelper.Clamp(alpha, 0f, 1f);
                if(alpha<=0f)
                    Dead = true;
            }
        }

        public virtual void Draw(SpriteBatch sb, Camera gameCamera)
        {
            if(InWorld)
                sb.Draw(itemTexture, Position, itemSource, Color.White * alpha, 0f, new Vector2(itemSource.Width/2, itemSource.Height-5f),2f, SpriteEffects.None, 1);
        }

        public virtual void Use(int faceDir, float attackCharge, Dude gameHero)
        {
            coolDownTime = Cooldown;
        }


    }
}
