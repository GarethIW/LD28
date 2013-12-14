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
    class EnemyManager
    {
        public List<Dude> Enemies = new List<Dude>();

        public static EnemyManager Instance;

        Texture2D blankTex;
        public Texture2D hudTex;

        SkeletonRenderer skeletonRenderer;

        public Dictionary<string, Atlas> AtlasDict = new Dictionary<string, Atlas>();
        Dictionary<string, string> JsonDict = new Dictionary<string, string>();

        static Random rand = new Random();

        public int largestNumberSpawned = 0;

        public int spawnsWithoutWeapon = 0;

        public EnemyManager()
        {
            Instance = this;
        }

        public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            //blankTex = content.Load<Texture2D>("blank");
            //hudTex = content.Load<Texture2D>("particles");

            skeletonRenderer = new SkeletonRenderer(graphicsDevice);

            AtlasDict.Add("dude", new Atlas(graphicsDevice, "dude/dude.atlas", content));
            JsonDict.Add("dude", File.ReadAllText(Path.Combine(content.RootDirectory, "dude/dude.json")));
        }

        public void Update(GameTime gameTime, Camera gameCamera, Map gameMap, Dude gameHero, float planeRot)
        {
            foreach (Dude r in Enemies)
            {
                r.Update(gameTime, gameMap, gameHero, planeRot);
                //r.Position = Vector2.Clamp(r.Position, gameCamera.Position - (new Vector2(gameCamera.Width + 300f, gameCamera.Height*2) / 2), gameCamera.Position + (new Vector2(gameCamera.Width + 300f, gameCamera.Height*2) / 2));
            }

            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                if (Enemies[i].Dead) Enemies.RemoveAt(i);
            }
        }

        public void Draw(GraphicsDevice gd, SpriteBatch sb, Camera gameCamera, float minY, float maxY)
        {
            foreach (Dude r in Enemies)
            {
                //if(r.Position.Y>=minY && r.Position.Y<maxY)
                    r.Draw(gd,sb,gameCamera);
            }
        }

        public void Spawn(Map gameMap)
        {
            //int numSpawned = 0;
            // Left or right side?

            foreach (MapObject o in ((MapObjectLayer)gameMap.GetLayer("spawns")).Objects)
            {
                Dude d = new Dude(new Vector2(o.Location.Center.X, o.Location.Bottom), false);
                d.LoadContent(skeletonRenderer, AtlasDict["dude"], JsonDict["dude"]);
                d.faceDir = 1;
                d.Scale = 2f;
                Enemies.Add(d);
            }

        }

        //public bool CheckAttack(Vector2 pos, int faceDir, float power, float maxDist, int maxHits, Robot gameHero)
        //{
        //    float mindist = 10000f;
        //    Robot target = null;
        //    int numHits = 0;
            

        //    foreach (Robot r in Enemies)
        //    {
        //        if ((r.Position - pos).Length() < mindist && (r.Position - pos).Length()<maxDist && r.Active)
        //        {
        //            if ((faceDir == 1 && r.Position.X > pos.X) || (faceDir == -1 && r.Position.X < pos.X))
        //            {
        //                if (r.Position.Y > pos.Y - 30f && r.Position.Y < pos.Y + 30f)
        //                {
        //                    numHits++;
        //                    if(numHits<=maxHits)
        //                        r.DoHit(pos, power, faceDir, gameHero);
        //                    mindist = (r.Position - pos).Length();
        //                }
        //            }
        //        }
        //    }

        //    return (numHits > 0);
        //}

        
    }
}
