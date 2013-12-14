using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using TiledLib;

namespace LD28
{
    public enum ParticleType
    {
        Standard,
        Cloud,
    }

    public class Particle
    {
        public ParticleType Type;
        public Texture2D Tex;
        public Vector2 Position;
        public Vector2 Speed;
        public float ZIndex;
        public bool Active;
        public bool AffectedByGravity;
        public float Alpha;
        public double Life;
        public float Scale;
        public float RotationSpeed;
        public float Rotation;
        public Color Color;
        public Rectangle SourceRect;
    }

    public class ParticleManager
    {
        public static ParticleManager Instance;
        const int MAX_PARTICLES = 3000;

        public Particle[] Particles;
        public Random Rand = new Random();

        public Texture2D _texParticles;
        public Texture2D _texCloud;

        public ParticleManager()
        {
            Particles = new Particle[MAX_PARTICLES];
            Instance = this;
        }

        public void LoadContent(ContentManager content)
        {
            //_texParticles = content.Load<Texture2D>("particles");
            _texCloud = content.Load<Texture2D>("cloud");

            for (int i = 0; i < MAX_PARTICLES; i++)
                Particles[i] = new Particle();
        }

        public void Update(GameTime gameTime, Map gameMap)
        {
            foreach (Particle p in Particles.Where(part => part.Active))
            {

                p.Life -= gameTime.ElapsedGameTime.TotalMilliseconds;

                p.Position += p.Speed;

                if (p.Life <= 0)
                {
                    p.Alpha -= 0.01f;
                    if (p.Alpha < 0.05f) p.Active = false;
                }

            }
        }

        public void Draw(GraphicsDevice gd, SpriteBatch sb, Camera gameCamera, float minZ,float maxZ)
        {
            sb.Begin(SpriteSortMode.Deferred, null, null, null, null, null, gameCamera.CameraMatrix);
            foreach (Particle p in Particles.Where(part=>part.Active && part.ZIndex>minZ && part.ZIndex<=maxZ).OrderByDescending(pro => pro.ZIndex))
            {
               
                sb.Draw(p.Tex,
                        p.Position,
                        p.SourceRect, p.Color * p.Alpha, p.Rotation, new Vector2(p.SourceRect.Width / 2, p.SourceRect.Height / 2), p.Scale, SpriteEffects.None, 1);
            }
            sb.End();
          
        }

        public void Add(ParticleType type, Vector2 spawnPos, Vector2 speed, float life, bool affectedbygravity, Rectangle sourcerect, float rot, Color col, float zindex)
        {
            foreach (Particle p in Particles)
                if (!p.Active)
                {
                    p.Type = type;
                    p.Tex = (type == ParticleType.Cloud) ? _texCloud : _texParticles;
                    p.Position = spawnPos;
                    p.Speed = speed;
                    p.Life = life;
                    p.ZIndex = zindex;
                    p.Scale = 0.3333f + (zindex / 3f);
                    p.AffectedByGravity = affectedbygravity;
                    p.SourceRect = sourcerect;
                    p.Alpha = p.Scale;
                    p.Active = true;
                    //p.RotationSpeed = rot;
                    p.Color = col;
                    p.Rotation = rot;
                    break;
                }
        }


        internal void Reset()
        {
            foreach (Particle p in Particles)
            {
                p.Active = false;
            }
        }

       
    }
}
