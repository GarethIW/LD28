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

            sourceDict.Add("crowbar", new Rectangle(2, 151, 50, 19));
            sourceDict.Add("laserpistol", new Rectangle(54, 206, 46, 28));
            sourceDict.Add("axe", new Rectangle(2, 2, 77, 77));
            sourceDict.Add("chute", new Rectangle(99,96,23,48));
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
            foreach (Item i in Items)
            {
                  i.Draw(sb,gameCamera);
            }
            sb.End();
        }

        public void Spawn(Dude owner, ItemType type, ItemName name)
        {
            int item = rand.Next(3);

            Item newItem = null;

            switch (item)
            {
                //case 0:
                //    // crowbar
                //    newItem = new Crowbar(itemTex, sourceDict["crowbar"]);
                //    break;
                //case 1:
                //    // laserpistol
                //    newItem = new LaserPistol(itemTex, sourceDict["laserpistol"]);
                //    break;
                //case 2:
                //    // axe
                //    newItem = new Axe(itemTex, sourceDict["axe"]);
                //    break;
            }

            newItem = new Item(type, name, itemTex, sourceDict[type.ToString().ToLower()]);
            newItem.Owner = owner;
            owner.Item = newItem;
            Items.Add(newItem);
        }

        internal void SpawnWorld(ItemType itemType, ItemName itemName, Vector2 pos)
        {
            Item newItem = new Item(itemType, itemName, itemTex, sourceDict[itemType.ToString().ToLower()]);
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
                    if (i.Health > 0f && !i.Dead && i.InWorld)
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
                if (i.Health > 0f && !i.Dead && i.InWorld)
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
