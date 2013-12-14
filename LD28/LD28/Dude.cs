using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
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
    public enum AIState
    {
        Panic,
        Attacking,
        GoingForParachute,
        GoingForDoor
    }

    public class Dude
    {
        static Random rand = new Random();

        public bool IsPlayer = false;

        public Vector2 Position;
        public Vector2 Speed;
        public Vector2 JumpSpeed;

        public float Scale = 1f;

        public bool Active = true;
        public bool Dead = false;

        Vector2 gravity = new Vector2(0f, 0.333f);

        Rectangle collisionRect = new Rectangle(0, 0, 3, 150);

        SkeletonRenderer skeletonRenderer;
        Skeleton skeleton;
        Dictionary<string, Animation> Animations = new Dictionary<string, Animation>();

        float animTime;

        public AIState State;

        public int faceDir = 1;

        bool walking = false;
        bool jumping = false;
        bool falling = false;

        bool punchHeld = false;


        Vector2 spawnPosition;

        Color tint = Color.White;


        double knockbackTime = 0;
        double deadTime = 0;
        double justChangedDirTime = 0;
        float alpha = 1f;

        bool pickingUp = false;
        bool hasPickedUp = false;
        double pickupTime = 0;

        // AI stuff
        Vector2 targetPosition = Vector2.Zero;
        bool backingOff = false;
        double notMovedTime = 0;
        bool AIchargingAttack = false;

        public SoundEffectInstance fistSound;

        public Dude(Vector2 spawnpos, bool isPlayer)
        {
            IsPlayer = isPlayer;
            spawnPosition = spawnpos;

            Position = spawnPosition;
       
        }

        public void Reset(Vector2 spawn)
        {
            spawnPosition = spawn;

            faceDir = 1;

            walking = false;
            jumping = false;
            falling = false;

            Position = spawnPosition;
            

            Dead = false;
            deadTime = 0;
            Active = true;

            alpha = 1f;

            knockbackTime = 0;


        }

        public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            //blankTex = content.Load<Texture2D>("blank");

            skeletonRenderer = new SkeletonRenderer(graphicsDevice);
            Atlas atlas = new Atlas(graphicsDevice, "dude/dude.atlas", content);
            SkeletonJson json = new SkeletonJson(atlas);
            skeleton = new Skeleton(json.readSkeletonData("dude", File.ReadAllText(Path.Combine(content.RootDirectory, "dude/dude.json"))));
            skeleton.SetSkin("default");
            skeleton.SetSlotsToBindPose();

          

            Animations.Add("walk", skeleton.Data.FindAnimation("walk"));
            Animations.Add("punch-hold", skeleton.Data.FindAnimation("punch-hold"));
            Animations.Add("punch-release", skeleton.Data.FindAnimation("punch-release"));
            Animations.Add("knockback", skeleton.Data.FindAnimation("knockback"));
            Animations.Add("pickup", skeleton.Data.FindAnimation("pickup"));
            Animations.Add("knockout", skeleton.Data.FindAnimation("knockout"));

            skeleton.RootBone.X = Position.X;
            skeleton.RootBone.Y = Position.Y;
            skeleton.RootBone.ScaleX = Scale;
            skeleton.RootBone.ScaleY = Scale;

            skeleton.UpdateWorldTransform();


            //ItemManager.Instance.Spawn(this);

            skeleton.SetAttachment("melee-item", null);

            skeleton.FindSlot("fist-item").A = 0f;
        }

        public void LoadContent(SkeletonRenderer sr, Atlas atlas, string json)
        {
            //blankTex = bt;
            skeletonRenderer =sr;

            SkeletonJson skjson = new SkeletonJson(atlas);
            skeleton = new Skeleton(skjson.readSkeletonData("robo", json));

            tint = new Color(0.5f + ((float)rand.NextDouble() * 0.5f), 0.5f + ((float)rand.NextDouble() * 0.5f), 0.5f + ((float)rand.NextDouble() * 0.5f));
            skeleton.R = tint.ToVector3().X;
            skeleton.G = tint.ToVector3().Y;
            skeleton.B = tint.ToVector3().Z;

            foreach (Slot s in skeleton.Slots)
            {
                if (s.Data.Name != "melee-item" && s.Data.Name != "projectile-item" && s.Data.Name != "fist-item")
                {
                    s.Data.R = skeleton.R;
                    s.Data.G = skeleton.G;
                    s.Data.B = skeleton.B;
                }
            }

            skeleton.SetSkin("default");
            skeleton.SetSlotsToBindPose();
            Animations.Add("walk", skeleton.Data.FindAnimation("walk"));
            Animations.Add("punch-hold", skeleton.Data.FindAnimation("punch-hold"));
            Animations.Add("punch-release", skeleton.Data.FindAnimation("punch-release"));
            Animations.Add("knockback", skeleton.Data.FindAnimation("knockback"));
            Animations.Add("pickup", skeleton.Data.FindAnimation("pickup"));
            Animations.Add("knockout", skeleton.Data.FindAnimation("knockout"));
            Animations.Add("panic", skeleton.Data.FindAnimation("panic"));

            skeleton.RootBone.X = Position.X;
            skeleton.RootBone.Y = Position.Y;
            skeleton.RootBone.ScaleX = Scale;
            skeleton.RootBone.ScaleY = Scale;

            skeleton.UpdateWorldTransform();

            skeleton.SetAttachment("melee-item", null);
            skeleton.FindSlot("fist-item").A = 0f;

            State = AIState.Panic;
        }

        public void Update(GameTime gameTime, Map gameMap, Dude gameHero, float planeRot)
        {
            if (Active)
            {
                float attackRange = 100f;
                //if (Item != null) attackRange = Item.Range;

                if (!IsPlayer)
                {
                    justChangedDirTime -= gameTime.ElapsedGameTime.TotalMilliseconds;
                    switch (State)
                    {
                        case AIState.Panic:
                            

                            if (Helper.Random.Next(100) == 0 && justChangedDirTime<0)
                            {
                                justChangedDirTime = 1000;
                                targetPosition = new Vector2(200f + Helper.Random.Next((gameMap.Width * gameMap.TileWidth) - 200), Position.Y);
                            }
                            break;
                    }

                    //if (notMovedTime > 500)
                    //{
                    //    if (CheckJump(gameMap, levelSectors, walkableLayers))
                    //    {
                    //        notMovedTime = 0;
                    //        Jump();
                    //    }
                    //}

                    //if (notMovedTime > 1000)
                    //{
                    //    backingOff = true;
                    //    targetPosition = MoveToRandomPosition(gameMap, levelSectors, walkableLayers);
                    //}

                    //if (!backingOff)
                    //{
                    //    targetPosition.X = gameHero.Position.X;
                    //    targetPosition.Y = gameHero.landingHeight;
                    //}
                    //else
                    //    if (rand.Next(250) == 1) backingOff = false;

                    //if (Item == null)
                    //{
                    //    Item tryItem = ItemManager.Instance.ClosestItem(this);
                    //    if (tryItem != null)
                    //    {
                    //        if ((Position - tryItem.Position).Length() < 400f)
                    //        {
                    //            if (rand.Next(100) == 1)
                    //                targetPosition = tryItem.Position;

                    //            if (tryItem.Position.X > Position.X - 75f && tryItem.Position.X < Position.X + 75f)
                    //            {
                    //                if (tryItem.Position.Y > landingHeight - 30f && tryItem.Position.Y < landingHeight + 30f)
                    //                {
                    //                    if (rand.Next(10) == 1)
                    //                        Pickup();
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    //if ((new Vector2(Position.X, landingHeight) - targetPosition).Length() < 100f)
                    //{
                    //    if (rand.Next(50) == 1)
                    //    {
                    //        backingOff = true;
                    //        targetPosition.X = (gameHero.Position.X - 300f) + (600f * (float)rand.NextDouble());
                    //        targetPosition.Y = (gameHero.landingHeight - 100f) + (200f * (float)rand.NextDouble());
                    //    }
                    //}

                    if (targetPosition.X-50 > Position.X)
                        MoveLeftRight(1);

                    if (targetPosition.X+50 < Position.X)
                        MoveLeftRight(-1);

                    if (Vector2.Distance(targetPosition, Position) < 10f) targetPosition = Position;

                    //if (targetPosition.Y - landingHeight < -5 || targetPosition.Y - landingHeight > 5)
                    //{
                    //    if (targetPosition.Y > landingHeight)
                    //        MoveUpDown(1);

                    //    if (targetPosition.Y < landingHeight)
                    //        MoveUpDown(-1);
                    //}

                    //if (gameHero.Position.X > Position.X) faceDir = 1; else faceDir = -1;

                    // Attacking
                    //if (!AIchargingAttack && rand.Next(100) == 1)
                    //{
                    //    AIchargingAttack = true;
                    //}

                    //if (AIchargingAttack) Attack(true);
                    //else Attack(false);



                    //if ((gameHero.Position - Position).Length() < attackRange && gameHero.Active)
                    //{
                    //    if ((faceDir == 1 && gameHero.Position.X > Position.X) || (faceDir == -1 && gameHero.Position.X < Position.X))
                    //    {
                    //        if (gameHero.Position.Y > Position.Y - 30f && gameHero.Position.Y < Position.Y + 30f)
                    //        {
                    //            if (rand.Next(20) == 1)
                    //            {
                    //                if (!AIchargingAttack) AIchargingAttack = true;
                    //                else Attack(false);
                    //            }
                    //        }
                    //    }
                    //}

                    ///////////////
                }

                //if ((int)Position.X > furthestX)
                //{
                //    furthestX = (int)Position.X;
                //    Score++;
                //}

                if (!walking && !jumping && knockbackTime <= 0)
                {
                    Animations["walk"].Apply(skeleton, 0f, false);
                    if (!IsPlayer && State == AIState.Panic)
                    {
                        Animations["panic"].Mix(skeleton, animTime, true, 1f);
                        animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f) * 2;
                    }
                }

                if (walking && !jumping && knockbackTime <= 0)
                {
                    animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f) * 2;

                    Animations["walk"].Mix(skeleton, animTime, true, 0.3f);
                    if (!IsPlayer && State == AIState.Panic) Animations["panic"].Mix(skeleton, animTime, true, 0.8f);
                }

                if (pickingUp)
                {
                    pickupTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                    animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f) * 3;

                    Animations["pickup"].Apply(skeleton, animTime, false);

                    if (pickupTime > 150 && !hasPickedUp)
                    {
                        //ItemManager.Instance.AttemptPickup(this);
                        hasPickedUp = true;
                    }
                    if (pickupTime >= 300)
                    {
                        pickingUp = false;
                        hasPickedUp = false;
                    }
                }

                if (knockbackTime > 0)
                {
                    knockbackTime -= gameTime.ElapsedGameTime.TotalMilliseconds;

                    animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f) * 3;
                    Animations["knockback"].Mix(skeleton, animTime, true, 0.3f);

                   // CheckCollision(gameTime, gameMap, levelSectors, walkableLayers, gameHero.Sector);
                    Position += (Speed);

                    if (Speed.X > 0) Speed.X -= 0.1f;
                    else if (Speed.X < 0) Speed.X += 0.1f;

                    if (fistSound.Volume > 0f) fistSound.Volume = MathHelper.Clamp(fistSound.Volume -= 0.1f,0f,1f);
                    if (fistSound.Pitch > -1f) fistSound.Pitch = MathHelper.Clamp(fistSound.Pitch - 0.1f,-0.9f,0.9f);

                    //if (Speed.X > -0.1f && Speed.X < 0.1f) knockbackTime = 0;
                }
                else
                {
                    

                    if (jumping)
                    {
                        Position += JumpSpeed;
                        JumpSpeed += gravity;
                        if (JumpSpeed.Y > 0f) { jumping = false; falling = true; }

                        animTime += gameTime.ElapsedGameTime.Milliseconds / 1000f;
                        //Animations["jump"].Mix(skeleton, animTime, false, 0.5f);
                    }

                    //if (!jumping && !falling) landingHeight = Position.Y;

                    //if (punchHeld && !punchReleased)
                    //{
                        
                    //    if (Item == null)
                    //    {
                    //        attackCharge += 0.25f * (IsPlayer?2f:1f);
                    //        Animations["punch-hold"].Apply(skeleton, 1f, false);

                    //        fistSound.Volume = MathHelper.Clamp((0.2f / 50f) * (attackCharge), 0f,1f);
                    //        fistSound.Pitch = MathHelper.Clamp(-1f + ((2f / 50f) * (attackCharge)), -0.9f,0.9f);
                    //    }
                    //    else if (Item.Type == ItemType.Melee)
                    //    {
                    //        attackCharge += 0.25f;
                    //        Animations["punch-hold"].Apply(skeleton, 1f, false);


                    //    }
                    //    else if (Item.Type == ItemType.Projectile)
                    //    {
                    //        attackCharge += 0.25f;
                    //        Animations["punch-release"].Apply(skeleton, 1f, false);
                    //        if(IsPlayer || rand.Next(50)==0)
                    //            Item.Use(faceDir, attackCharge, gameHero);
                    //    }

                    //    if(rand.Next(51 - (int)attackCharge)==0 && Item==null)
                    //        ParticleManager.Instance.Add(ParticleType.Standard, (Position - new Vector2(faceDir * 50, 75)) + (new Vector2(-15f + ((float)rand.NextDouble() * 30f), -10f + ((float)rand.NextDouble() * 20f))), (landingHeight - 10f) + ((float)rand.NextDouble() * 20f), new Vector2(-0.5f + (float)rand.NextDouble() * 1f, -0.5f + (float)rand.NextDouble() * 1f), 0f, true, new Rectangle(0, 0, 2, 2), 0f, Color.DeepSkyBlue);

                        
                    //}
                    //else if (punchReleased)
                    //{
                    //    AIchargingAttack = false;

                    //    if (Item == null)
                    //    {
                    //        if (punchReleaseTime == 0)
                    //        {
                    //            AudioController.PlaySFX("swipe",0.5f, -0.25f + ((float)rand.NextDouble() * 0.5f),0f);
                    //            if (IsPlayer)
                    //                EnemyManager.Instance.CheckAttack(Position, faceDir, attackCharge, attackRange, 1, gameHero);
                    //            else
                    //                if ((Position - gameHero.Position).Length() < attackRange) gameHero.DoHit(Position, attackCharge, faceDir, gameHero);
                    //        }
                    //    }
                    //    else if (Item.Type == ItemType.Melee)
                    //    {
                    //        if (punchReleaseTime == 0)
                    //        {
                    //            AudioController.PlaySFX("swipe",0.5f,-0.25f + ((float)rand.NextDouble() * 0.5f), 0f);
                    //            Item.Use(faceDir, attackCharge, gameHero);
                                
                    //        }
                    //    }
                    //    else if (Item.Type == ItemType.Projectile)
                    //    {
                    //        punchReleaseTime = Item.Cooldown;
                    //    }

                    //    punchReleaseTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                    //    if (punchReleaseTime >= (Item!=null?Item.Cooldown:200))
                    //    {
                    //        punchReleaseTime = 0;
                    //        punchReleased = false;
                    //        AIchargingAttack = false;
                    //        Animations["punch-release"].Apply(skeleton, 0f, false);
                    //    }

                    //    Animations["punch-release"].Apply(skeleton, 1f, false);


                    //    if (fistSound.Volume > 0f) fistSound.Volume = MathHelper.Clamp(fistSound.Volume -= 0.1f, 0f, 1f);
                    //    if (fistSound.Pitch > -1f) fistSound.Pitch = MathHelper.Clamp(fistSound.Pitch - 0.1f, -0.9f, 0.9f);

                    //    attackCharge = 0f;
                    //}

                    //attackCharge = MathHelper.Clamp(attackCharge, 0f, 50f);

                    Speed.Normalize();

                    if (Speed.Length() > 0f)
                    {
                        //CheckCollision(gameTime, gameMap, levelSectors, walkableLayers, gameHero.Sector);
                        if (Speed.Length() > 0f)
                            Position += (Speed * 4f);
                    }

                    Speed = Vector2.Zero;
                }

                //if (Item != null)
                //{
                //    if (fistSound.Volume > 0f) fistSound.Volume = MathHelper.Clamp(fistSound.Volume -= 0.1f, 0f, 1f);
                //    if (fistSound.Pitch > -1f) fistSound.Pitch = MathHelper.Clamp(fistSound.Pitch - 0.1f, -0.9f, 0.9f);

                //    if (Item.Type == ItemType.Melee)
                //    {
                //        skeleton.SetAttachment("melee-item", Item.Name);
                //        skeleton.SetAttachment("projectile-item", null);
                //    }
                //    else
                //    {
                //        skeleton.SetAttachment("projectile-item", Item.Name);
                //        skeleton.SetAttachment("melee-item", null);
                //    }

                //    //skeleton.FindSlot("fist-item").A = 1f;
                //    if (skeleton.FindSlot("fist-item").A > 0f) skeleton.FindSlot("fist-item").A -= 0.1f;
                //}
                //else
                //{
                //    if (attackCharge > 0f)
                //        skeleton.FindSlot("fist-item").A = (1f / 50f) * attackCharge;
                //    else
                //        if (skeleton.FindSlot("fist-item").A > 0f) skeleton.FindSlot("fist-item").A -= 0.1f;
                //    skeleton.SetAttachment("fist-item", "fistweap" + (rand.Next(2) + 1).ToString());

                //    skeleton.SetAttachment("melee-item", null);
                //    skeleton.SetAttachment("projectile-item", null);
                //}

                //pushingUp = false;
            }

            if (!Active)
            {
                Active = false;

                animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f);
                Animations["knockout"].Mix(skeleton, animTime, true, 0.2f);

                deadTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                if (deadTime > 0 && deadTime<1000)
                {
                    //CheckCollision(gameTime, gameMap, levelSectors, walkableLayers, gameHero.Sector);
                    Position.X += ((float)-faceDir) * (0.001f * (1000f - (float)deadTime));
                }
                if (deadTime > 2000 && alpha > 0f)
                {
                    alpha -= 0.025f;
                    alpha = MathHelper.Clamp(alpha, 0f, 1f);
                }

                if (skeleton.FindSlot("fist-item").A > 0f) skeleton.FindSlot("fist-item").A -= 0.1f;

                if (deadTime >= 3000)
                {
                    Dead = true;

                    fistSound.Stop();
                    fistSound.Dispose();
                }

                if (fistSound.Volume > 0f) fistSound.Volume = MathHelper.Clamp(fistSound.Volume -= 0.1f, 0f, 1f);
                if (fistSound.Pitch > -1f) fistSound.Pitch = MathHelper.Clamp(fistSound.Pitch -= 0.1f, -0.9f, 0.9f);
            }

            if (falling)
            {
                Position += JumpSpeed;
                JumpSpeed += gravity;

                
            }

            skeleton.A = alpha;
            foreach (Slot s in skeleton.Slots)
            {
                if (s.Data.Name != "melee-item" && s.Data.Name != "projectile-item" && s.Data.Name != "fist-item")
                {
                    s.A = skeleton.A;
                }
            }

            skeleton.RootBone.ScaleX = Scale;
            skeleton.RootBone.ScaleY = Scale;

            collisionRect.Location = new Point((int)Position.X - (collisionRect.Width / 2), (int)Position.Y - (collisionRect.Height));

            Position.X -= (planeRot * 5f);

            Position.X = MathHelper.Clamp(Position.X, 0, gameMap.Width * gameMap.TileWidth);
            Position.Y = MathHelper.Clamp(Position.Y, 0, gameMap.Height * gameMap.TileHeight);

            skeleton.RootBone.X = Position.X;
            skeleton.RootBone.Y = Position.Y;

            if (faceDir == -1) skeleton.FlipX = true; else skeleton.FlipX = false;

            skeleton.UpdateWorldTransform();

            walking = false;


          //  Health = MathHelper.Clamp(Health, 0f, 121f);

            //if (fistSound.Volume < 0.01f) fistSound.Volume = 0f;
        }

        public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera gameCamera)
        {
            skeletonRenderer.Begin(graphicsDevice, gameCamera.CameraMatrix);
            skeletonRenderer.Draw(skeleton);
            skeletonRenderer.End();

            // Draw collision box
            
            //spriteBatch.Draw(blankTex, collisionRect, Color.White * 0.3f);
            //spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, gameCamera.CameraMatrix);

            if (!IsPlayer)
            {
                Vector2 pos = Position + new Vector2(-25, -150);
                int i = 0;

                //for (i = 0; i < (int)(Health/10f); i++)
                //{
                //    spriteBatch.Draw(EnemyManager.Instance.hudTex, pos, new Rectangle(11, 0, 15, 8), Color.Red, 0f, new Vector2(7, 4), 0.5f, i % 2 == 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 1);
                //    pos.X += 5;
                //}

                //spriteBatch.Draw(EnemyManager.Instance.hudTex, pos, new Rectangle(11, 0, 15, 8), Color.Red * ((Health/10f) - (int)(Health/10f)), 0f, new Vector2(7, 4), 0.5f, i % 2 == 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 1);

            }

            spriteBatch.End();
        }

        

        public void MoveLeftRight(float dir)
        {
            if (knockbackTime > 0 || pickingUp) return;

            if (dir > 0) faceDir = 1; else faceDir = -1;

            Speed.X = dir * 2f;
            walking = true;

        }

        

        public void Jump()
        {
            if (knockbackTime > 0 || pickingUp) return;

            if (!jumping && !falling)
            {
               // AudioController.PlaySFX("jump", 0.5f, 0f, 0f);
               
                jumping = true;
                animTime = 0;
              
                JumpSpeed.Y = -12f;
            }
        }

        public void Pickup()
        {
            if (knockbackTime > 0 || pickingUp || jumping || falling) return;

            animTime = 0;
            pickingUp = true;
            pickupTime = 0;
        }

        public void Attack(bool p)
        {
            if (knockbackTime > 0 || pickingUp) return;

            //if (punchHeld && !p) punchReleased = true;
            punchHeld = p;
        }


        //void CheckCollision(GameTime gameTime, Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers, int ghs)
        //{
        //    if ((jumping || falling))
        //    {
        //        float originalLandingHeight = landingHeight;
        //        bool found = false;

        //        for (landingHeight = originalLandingHeight; landingHeight >= Position.Y; landingHeight--)
        //        {
        //            if (Speed.X < 0 && !CheckCollisionLeft(gameMap, levelSectors, walkableLayers, ghs)) { found = true; break; }
        //            if (Speed.X > 0 && !CheckCollisionRight(gameMap, levelSectors, walkableLayers, ghs)) { found = true; break; }
        //            if (pushingUp && !CheckCollisionUp(gameMap, levelSectors, walkableLayers, ghs)) { found = true; break; }
        //        }
        //        if (!found) landingHeight = originalLandingHeight;
        //    }

        //    if (Speed.X > 0f)
        //    {
        //        if (CheckCollisionRight(gameMap, levelSectors, walkableLayers, ghs))
        //        {
        //            if (!FallTest(gameMap, levelSectors, walkableLayers))
        //            {
        //                Speed.X = 0f;
        //                notMovedTime += gameTime.ElapsedGameTime.TotalMilliseconds;
        //            }
        //            else notMovedTime = 0;
        //        }
        //        else notMovedTime = 0;
        //    }
        //    if (Speed.X < 0f)
        //    {
        //        if (CheckCollisionLeft(gameMap, levelSectors, walkableLayers, ghs))
        //        {
        //            if (!FallTest(gameMap, levelSectors, walkableLayers))
        //            {
        //                Speed.X = 0f;
        //                notMovedTime += gameTime.ElapsedGameTime.TotalMilliseconds;
        //            }
        //            else notMovedTime = 0;
        //        }
        //        else notMovedTime = 0;
        //    }

        //    if (Speed.Y > 0f)
        //    {
        //        if (CheckCollisionDown(gameMap, levelSectors, walkableLayers, ghs))
        //        {
        //            if (!FallTest(gameMap, levelSectors, walkableLayers))
        //            {
        //                Speed.Y = 0f;
        //                notMovedTime += gameTime.ElapsedGameTime.TotalMilliseconds;
        //            }
        //            else notMovedTime = 0;
        //        }
        //        else notMovedTime = 0;
        //    }
        //    if (Speed.Y < 0f || ((jumping || falling) && pushingUp))
        //    {
        //        if (CheckCollisionUp(gameMap, levelSectors, walkableLayers, ghs))
        //        {
        //            //if (!FallTest(gameMap, levelSectors, walkableLayers))
        //            //{
        //                Speed.Y = 0f;
        //                notMovedTime += gameTime.ElapsedGameTime.TotalMilliseconds;
        //            //}
        //            //else notMovedTime = 0;
        //        }
        //        else notMovedTime = 0;
        //    }

           

        //}

        bool FallTest(Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers)
        {
            //if (knockbackTime > 0) return false;

            //for (int i = 0; i < levelSectors.Count; i++)
            //{
            //    MapObjectLayer walkableLayer = walkableLayers[levelSectors[i]];

                
            //        for (float y = landingHeight + 20; y < (gameMap.TileHeight * (gameMap.Height-5)); y+=5)
            //        {
            //            for (int o = 0; o < walkableLayer.Objects.Count; o++)
            //            {

            //            if (Helper.IsPointInShape(new Vector2((Position.X + (Speed.X * 10)) - ((gameMap.Width * gameMap.TileWidth) * i), y), walkableLayer.Objects[o].LinePoints))
            //            {
            //                if (Helper.IsPointInShape(new Vector2((Position.X + (Speed.X * -10)) - ((gameMap.Width * gameMap.TileWidth) * i), Position.Y+10), walkableLayer.Objects[o].LinePoints)) return false;

            //                if ((y - landingHeight) > gameMap.TileHeight)
            //                {
            //                    landingHeight = y;
            //                    falling = true;
            //                    return true;
            //                }
            //                else return false;
            //            }
            //        }
            //    }
            //}

            return false;
        }

        //bool CheckCollisionRight(Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers, int ghs)
        //{
        //    for (int i = 0; i < levelSectors.Count; i++)
        //    {
        //        if (i < ghs - 1 || i > ghs + 1) continue;
        //        MapObjectLayer walkableLayer = walkableLayers[levelSectors[i]];

        //        for (int o = 0; o < walkableLayer.Objects.Count; o++)
        //        {
        //            for(int x=1;x<10;x+=3)
        //                if (Helper.IsPointInShape(new Vector2(Position.X - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight) + new Vector2(x, 0), walkableLayer.Objects[o].LinePoints)) return false;
        //        }
        //    }

        //    return true;
        //}
        //bool CheckCollisionLeft(Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers, int ghs)
        //{
        //    for (int i = 0; i < levelSectors.Count; i++)
        //    {
        //        if (i < ghs - 1 || i > ghs + 1) continue;
        //        MapObjectLayer walkableLayer = walkableLayers[levelSectors[i]];

        //        for (int o = 0; o < walkableLayer.Objects.Count; o++)
        //        {
        //            for (int x = 1; x < 10; x+=3)
        //                if (Helper.IsPointInShape(new Vector2(Position.X - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight) + new Vector2(-x, 0), walkableLayer.Objects[o].LinePoints)) return false;
        //        }
        //    }

        //    return true;
        //}
        //bool CheckCollisionUp(Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers, int ghs)
        //{
        //    for (int i = 0; i < levelSectors.Count; i++)
        //    {
        //        if (i < ghs - 1 || i > ghs + 1) continue;
        //        MapObjectLayer walkableLayer = walkableLayers[levelSectors[i]];

        //        for (int o = 0; o < walkableLayer.Objects.Count;o++)
        //        {
        //            if (Helper.IsPointInShape(new Vector2(Position.X - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight) + new Vector2(0, -10), walkableLayer.Objects[o].LinePoints)) return false;
        //        }
        //    }

        //    return true;
        //}
        //bool CheckCollisionDown(Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers, int ghs)
        //{
        //    for (int i = 0; i < levelSectors.Count; i++)
        //    {
        //        if (i < ghs - 1 || i > ghs + 1) continue;
        //        MapObjectLayer walkableLayer = walkableLayers[levelSectors[i]];

        //        for (int o = 0; o < walkableLayer.Objects.Count; o++)
        //        {
        //            if (Helper.IsPointInShape(new Vector2(Position.X - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight) + new Vector2(0, 10), walkableLayer.Objects[o].LinePoints)) return false;
        //        }
        //    }

        //    return true;
        //}

        //Vector2 MoveToRandomPosition(Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers)
        //{
        //    for (int i = 0; i < levelSectors.Count; i++)
        //    {
        //        MapObjectLayer walkableLayer = walkableLayers[levelSectors[i]];

        //        foreach (MapObject o in walkableLayer.Objects)
        //        {
        //            if (Helper.IsPointInShape(new Vector2((Position.X + 5f) - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight), o.LinePoints) ||
        //                Helper.IsPointInShape(new Vector2((Position.X - 5f) - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight), o.LinePoints) ||
        //                Helper.IsPointInShape(new Vector2((Position.X) - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight), o.LinePoints))
        //            {
        //                int lx = 100000;
        //                int ly = 100000;
        //                int mx = -100000;
        //                int my = -100000;
        //                foreach (Point l in o.LinePoints)
        //                {
        //                    if (l.X < lx) lx = l.X;
        //                    if (l.X > mx) mx = l.X;
        //                    if (l.Y < ly) ly = l.Y;
        //                    if (l.Y > my) my = l.Y;
        //                }

        //                Vector2 testPoint = new Vector2(lx + (rand.Next(mx - lx)), ly + (rand.Next(my - ly)));
        //                if (Helper.IsPointInShape(testPoint, o.LinePoints)) return testPoint + new Vector2(((gameMap.Width * gameMap.TileWidth) * i),0);
        //            }
        //        }
        //    }

        //    return Position;
        //}

        //bool CheckJump(Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers)
        //{
        //    for (int i = 0; i < levelSectors.Count; i++)
        //    {
        //        MapObjectLayer walkableLayer = walkableLayers[levelSectors[i]];


        //        for (float y = landingHeight - 20; y > landingHeight-300; y--)
        //        {

        //            foreach (MapObject o in walkableLayer.Objects)
        //            {
        //                if (Helper.IsPointInShape(new Vector2((Position.X) - ((gameMap.Width * gameMap.TileWidth) * i), y), o.LinePoints))
        //                {
                            
        //                        return true;
        //                }
        //            }
        //        }
        //    }

        //    return false;
        //}

        //internal void DoHit(Vector2 pos, float power, int face, Robot gameHero)
        //{
        //    if (knockbackTime > 0) return;

        //    if (power > 5f && knockbackTime <= 0)
        //    {
        //        knockbackTime = (double)((power * 100f) / 2f);
        //        if (IsPlayer) knockbackTime *= 0.5;
        //        Speed.X = 10f * (float)face;

        //        if (jumping)
        //        {
        //            jumping = false;
        //            falling = true;
        //        }
        //    }

        //    if (IsPlayer)
        //        Health -= (power / 2f);
        //    else
        //    {
        //        gameHero.Score += (int)power;
        //        Health -= power;
        //    }

        //    AudioController.PlaySFX("hit" + (1 + rand.Next(3)).ToString(), 1f, -0.25f + ((float)rand.NextDouble() * 0.5f), 0f);
        //    AudioController.PlaySFX("thud", 0.8f,0f,0f);

        //    ParticleManager.Instance.AddHurt(Position + new Vector2(0,-75f), new Vector2((power/5f) * face,0f), landingHeight, tint);

        //    AIchargingAttack = false;
        //    attackCharge = 0f;

        //    if (Health <= 0)
        //    {
        //        AudioController.PlaySFX("powerdown", 0.5f, 0f, 0f);

        //        if (Item != null)
        //        {
        //            Item.InWorld = true;
        //            Item.Position = Position + new Vector2(0, -75);
        //            Item.DroppedPosition = Position;
        //            Item.Speed.Y = 2f;
        //            Item = null;
        //        }

        //        if (!IsPlayer)
        //        {
        //            gameHero.Score += 500;

        //            if ((int)gameHero.Health < 120)
        //            {
        //                for (int i = 0; i < ((120 - (int)gameHero.Health)/2) + (rand.Next((120 - (int)gameHero.Health)) / 2); i++)
        //                {
        //                    ParticleManager.Instance.Add(ParticleType.Health, Position + new Vector2(0, -75f), (landingHeight - 10f) + ((float)rand.NextDouble() * 20f), new Vector2(-10f + ((float)rand.NextDouble() * 20f), 0), 2000, true, new Rectangle(11, 0, 15, 8), (float)rand.NextDouble() * MathHelper.TwoPi, Color.Red);
        //                }
        //            }
        //        }
        //    }
        //}
    }
}
