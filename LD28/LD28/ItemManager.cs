using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Spine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TiledLib;

namespace LD28
{
    class ItemManager
    {
        const int NUM_WEAPONS = 2;
        public List<Item> Items = new List<Item>();

        public static ItemManager Instance;

        SkeletonRenderer skeletonRenderer;

        Texture2D itemTex;
        Dictionary<string, Rectangle> sourceDict = new Dictionary<string,Rectangle>(); 

        static Random rand = new Random();

        public ItemManager()
        {
            Instance = this;
        }

        public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            itemTex = content.Load<Texture2D>("dude/dude");

            skeletonRenderer = new SkeletonRenderer(graphicsDevice);

            sourceDict.Add("weapon0", new Rectangle(70, 91, 33, 30));
            sourceDict.Add("weapon1", new Rectangle(2, 204, 19, 49));
            sourceDict.Add("chute", new Rectangle(99, 154, 23, 48));
        }

        public void Update(GameTime gameTime, Camera gameCamera, Map gameMap, Dude gameHero, float planeRot)
        {
            foreach (Item i in Items)
            {
                i.Update(gameTime, gameMap, planeRot);
            }

            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (Items[i].Dead) Items.RemoveAt(i);
            }
        }

        public void Draw(GraphicsDevice gd, SpriteBatch sb, Camera gameCamera)
        {
            sb.Begin(SpriteSortMode.Deferred, null,null,null,null,null,gameCamera.CameraMatrix);
            foreach (Item i in Items.OrderBy(it=>it.Type== ItemType.Chute))
            {
                  i.Draw(sb,gameCamera);
            }
            sb.End();
        }

        public void Spawn(Dude owner, ItemType type, ItemName name)
        {
            int item = rand.Next(3);

            Item newItem = null;

            newItem = new Item(type, name, itemTex, sourceDict[type.ToString().ToLower()]);
            newItem.Owner = owner;
            owner.Item = newItem;
            Items.Add(newItem);
        }

        public void SpawnRandom(int number, float floorHeight)
        {
            for (int i = 0; i < number; i++)
            {
                int type = Helper.Random.Next(NUM_WEAPONS);
                SpawnWorld(ItemType.Melee, (ItemName)type, new Vector2(Helper.RandomFloat(1500f, 5000f),floorHeight));
            }
        }

        internal void SpawnWorld(ItemType itemType, ItemName itemName, Vector2 pos)
        {
            Item newItem = new Item(itemType, itemName, itemTex, sourceDict[itemName.ToString().ToLower()]);
            newItem.Owner = null;
            newItem.InWorld = true;
            newItem.Position = pos;
            newItem.DroppedPosition = pos;
            Items.Add(newItem);
        }

        public void CheckChutePickup(Dude dude)
        {
            foreach (Item i in Items.Where(it=>it.Type== ItemType.Chute))
            {
                if (i.InWorld && i.Speed.Y==0f && i.Position.Y==i.DroppedPosition.Y)
                {
                    if (i.Position.X > dude.Position.X - 100f && i.Position.X < dude.Position.X + 100f)
                    {

                        i.InWorld = false;
                        dude.ChuteItem = i;
                        i.Owner = dude;
                        dude.HasParachute = true;
                        //AudioController.PlaySFX("pickup", 1f, 0f, 0f);

                        break;

                    }
                }
            }
        }

        internal void AttemptPickup(Dude dude)
        {
            if (dude.Item != null)
            {
                Item dropItem = dude.Item;
                dude.Item = null;
                dropItem.InWorld = true;
                dropItem.Position = dude.Position + new Vector2(0, 0);
                dropItem.DroppedPosition = dude.Position;
                //AudioController.PlaySFX("pickup", 1f, 0f, 0f);

            }
            else
            {
                foreach (Item i in Items.OrderBy(it => (it.Position - dude.Position).Length()))
                {
                    if (i.Health > 0f && !i.Dead && i.InWorld && i.Type != ItemType.Chute)
                    {
                        if (i.Position.X > dude.Position.X - 75f && i.Position.X < dude.Position.X + 75f)
                        {
                            
                                i.InWorld = false;
                                dude.Item = i;
                                i.Owner = dude;
                                //AudioController.PlaySFX("pickup", 1f, 0f, 0f);

                                break;
                           
                        }
                    }
                }
            }
        }

        public Item ClosestItem(Dude robot)
        {
            float dist = 10000f;
            Item returnItem = null;

            foreach (Item i in Items)
            {
                if (i.Health > 0f && !i.Dead && i.InWorld && i.Type!= ItemType.Chute)
                {
                    if ((robot.Position - i.Position).Length() < dist)
                    {
                        dist = (robot.Position - i.Position).Length();
                        returnItem = i;
                    }
                }
            }

            return returnItem;
        }

        
    }
}
