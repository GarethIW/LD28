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
        KnockBack,
        AttackingHero,
        AttackingOther,
        GoingForParachute,
        GoingForDoor
    }

    public class Dude
    {
        static Random rand = new Random();

        public bool IsPlayer = false;
        public bool IsCoPilot = false;

        public Vector2 Position;
        public Vector2 Speed;
        public Vector2 JumpSpeed;

        public float Scale = 1f;

        public bool Active = true;
        public bool Dead = false;

        public bool HasParachute = false;

        public int Health = 3;

        public bool IsInPlane = true;

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
        double punchHeldTime = 0;
        bool punchReleased = false;
        double punchReleaseTime = 0;

        public Item Item;
        public Item ChuteItem;

        Vector2 spawnPosition;

        public Color Tint = Color.White;


        public double knockbackTime = 0;
        double deadTime = 0;
        double justChangedDirTime = 0;
        float alpha = 1f;

        bool pickingUp = false;
        bool hasPickedUp = false;
        double pickupTime = 0;

        int walkFrameCount = 0;

        float fallSpeedX;

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

            Color topColor = Color.Navy;
            Color bottomColor = Color.Navy;
            Color shoesColor = Color.DarkGray;
            Vector3 skinColor = SkinTone().ToVector3();
            Vector3 hairColor = HairColor().ToVector3();


            foreach (Slot s in skeleton.Slots)
            {
                if (s.Data.Name == "torso" ||
                    s.Data.Name == "arm-back-upper" ||
                    s.Data.Name == "arm-back-lower" ||
                    s.Data.Name == "arm-upper" ||
                    s.Data.Name == "arm-lower")
                {
                    s.Data.R = topColor.R;
                    s.Data.G = topColor.G;
                    s.Data.B = topColor.B;
                }

                if (s.Data.Name == "leg-left" ||
                    s.Data.Name == "leg-right")
                {
                    s.Data.R = bottomColor.R;
                    s.Data.G = bottomColor.G;
                    s.Data.B = bottomColor.B;
                }

                if (s.Data.Name == "foot-left" ||
                    s.Data.Name == "foot-right")
                {
                    s.Data.R = shoesColor.R;
                    s.Data.G = shoesColor.G;
                    s.Data.B = shoesColor.B;
                }

                if (s.Data.Name == "head" ||
                    s.Data.Name == "hand" ||
                    s.Data.Name == "hand-copy")
                {
                    s.Data.R = skinColor.X;
                    s.Data.G = skinColor.Y;
                    s.Data.B = skinColor.Z;
                }

                if (s.Data.Name == "hair")
                {
                    s.Data.R = hairColor.X;
                    s.Data.G = hairColor.Y;
                    s.Data.B = hairColor.Z;
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
            skeleton.SetAttachment("hat", (IsCoPilot || IsPlayer) ? "Pilot-Hat" : null);
            skeleton.SetAttachment("hair", (Helper.Random.Next(2) == 0 ? "Hair-Male" : "Hair-Female"));
            skeleton.SetAttachment("chute", null);

            HasParachute = false;

            //skeleton.FindSlot("fist-item").A = 0f;
        }

        public void LoadContent(SkeletonRenderer sr, Atlas atlas, string json)
        {
            //blankTex = bt;
            skeletonRenderer =sr;

            SkeletonJson skjson = new SkeletonJson(atlas);
            skeleton = new Skeleton(skjson.readSkeletonData("robo", json));

            //skeleton.R = Tint.ToVector3().X;
            //skeleton.G = Tint.ToVector3().Y;
            //skeleton.B = Tint.ToVector3().Z;

            Vector3 topColor = ClothesTint().ToVector3();
            Vector3 bottomColor = ClothesTint().ToVector3();
            Vector3 shoesColor = ClothesTint().ToVector3();
            Vector3 skinColor = SkinTone().ToVector3();
            Vector3 hairColor = HairColor().ToVector3();

            if (IsCoPilot)
            {
                topColor = Color.Blue.ToVector3();
                bottomColor = Color.Blue.ToVector3();
                shoesColor = Color.DarkGray.ToVector3();
            }
           

            foreach (Slot s in skeleton.Slots)
            {
                if (s.Data.Name == "torso" ||
                    s.Data.Name=="arm-back-upper"  ||
                    s.Data.Name == "arm-back-lower" ||
                    s.Data.Name== "arm-upper" ||
                    s.Data.Name =="arm-lower") 
                {
                    s.Data.R = topColor.X;
                    s.Data.G = topColor.Y;
                    s.Data.B = topColor.Z;
                }

                if (s.Data.Name == "leg-left" ||
                    s.Data.Name == "leg-right")
                {
                    s.Data.R = bottomColor.X;
                    s.Data.G = bottomColor.Y;
                    s.Data.B = bottomColor.Z;
                }

                if (s.Data.Name == "foot-left" ||
                    s.Data.Name == "foot-right")
                {
                    s.Data.R = shoesColor.X;
                    s.Data.G = shoesColor.Y;
                    s.Data.B = shoesColor.Z;
                }

                if (s.Data.Name == "head" ||
                    s.Data.Name == "hand" ||
                    s.Data.Name == "hand-copy")
                {
                    s.Data.R = skinColor.X;
                    s.Data.G = skinColor.Y;
                    s.Data.B = skinColor.Z;
                }

                if (s.Data.Name == "hair")
                {
                    s.Data.R = hairColor.X;
                    s.Data.G = hairColor.Y;
                    s.Data.B = hairColor.Z;
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
            skeleton.SetAttachment("hat", (IsCoPilot||IsPlayer)?"Pilot-Hat":null);
            skeleton.SetAttachment("hair", (Helper.Random.Next(2)==0?"Hair-Male":"Hair-Female"));
            skeleton.SetAttachment("chute", null);
            //skeleton.FindSlot("fist-item").A = 0f;

            HasParachute = false;

            State = AIState.Panic;
        }

        Color ClothesTint()
        {
            return new Color(0.1f + ((float)rand.NextDouble() * 0.9f), 0.1f + ((float)rand.NextDouble() * 0.9f), 0.1f + ((float)rand.NextDouble() * 0.9f));
        }

        Color SkinTone()
        {
            Color light = new Color(251,216,197);
            Color dark = new Color(81,40,17);

            return Color.Lerp(light,dark,Helper.RandomFloat(0f,1f));
        }

        Color HairColor()
        {
            List<Color> lightColors = new List<Color>();
            List<Color> darkColors = new List<Color>();

            lightColors.Add(new Color(36,51,100));
            darkColors.Add(new Color(144,1,1));
            lightColors.Add(new Color(180,110,0));
            darkColors.Add(new Color(92,52,0));
            lightColors.Add(new Color(255,238,143));
            darkColors.Add(new Color(131,117,1));
            lightColors.Add(new Color(63,63,63));
            darkColors.Add(new Color(15,15,15));
            lightColors.Add(new Color(210,210,210));
            darkColors.Add(new Color(140,120,120));

            int mainColor = Helper.Random.Next(5);
            return Color.Lerp(lightColors[mainColor], darkColors[mainColor], Helper.RandomFloat(0f, 1f));
        }

        public void Update(GameTime gameTime, Map gameMap, Dude gameHero, float planeRot, bool doorOpen)
        {
            if (HasParachute)
            {
                skeleton.SetAttachment("chute", "chute");
                ChuteItem.Position = Position;
            }
            else
                skeleton.SetAttachment("chute", null);

            if (doorOpen && IsInPlane)
            {
                Active = false;
                animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f) * 3;
                Animations["knockback"].Mix(skeleton, animTime, true, 0.3f);
                if (Position.X > 1350f) Position.X -= 40f;
                if (Position.X < 1350f) Position.X += 40f;
                if (Position.X > 1250f && Position.X < 1450f)
                {
                    IsInPlane = false;
                    fallSpeedX = Helper.RandomFloat(-20f, 0f);
                }
            }

            if (!IsInPlane)
            {
                Active = false;
                animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f) * 3;
                Animations["panic"].Mix(skeleton, animTime, true, 0.3f);
                Position.Y -= (HasParachute?30f:20f);
                if(!IsPlayer) Position.X += fallSpeedX;
            }

            if (Active)
            {
                float attackRange = 200f;
                //if (Item != null) attackRange = Item.Range;

                

                if(knockbackTime<=0) ItemManager.Instance.CheckChutePickup(this);


                if (!IsPlayer)
                {
                    Item chute = ItemManager.Instance.Items.Where(it => it.Type == ItemType.Chute).First();

                    justChangedDirTime -= gameTime.ElapsedGameTime.TotalMilliseconds;


                    switch (State)
                    {
                        case AIState.Panic:

                            
                            
                            if (Vector2.Distance(chute.Position, Position) < 300f)
                            {
                                State = AIState.GoingForParachute;
                            }

                            if (Helper.Random.Next(500) == 0) State = AIState.GoingForParachute;
                          

                            if (Helper.Random.Next(100) == 0 && justChangedDirTime < 0)
                            {
                                justChangedDirTime = 1000;
                                targetPosition = new Vector2(200f + Helper.Random.Next((gameMap.Width * gameMap.TileWidth) - 200), Position.Y);
                            }
                            if (targetPosition.X - 50 > Position.X)
                                MoveLeftRight(1);
                            if (targetPosition.X + 50 < Position.X)
                                MoveLeftRight(-1);
                            if (HasParachute) State = AIState.GoingForDoor;
                            break;

                        case AIState.AttackingOther:
                        case AIState.AttackingHero:
                            punchHeldTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                            if (punchHeldTime > 100)
                            {
                                punchReleased = true;
                                punchHeld = false;
                                punchHeldTime = 0;
                                //AudioController.PlaySFX("swipe", 0.5f, -0.1f, 0.1f, Position);

                            }
                            break;

                        case AIState.GoingForDoor:
                            targetPosition = new Vector2(300f, Position.Y);
                            if (targetPosition.X - 50 > Position.X)
                                MoveLeftRight(1);
                            if (targetPosition.X + 50 < Position.X)
                                MoveLeftRight(-1);
                            break;

                        case AIState.GoingForParachute:
                            if (Vector2.Distance(chute.Position, Position) > 50f)
                                targetPosition = chute.Position;
                            //else targetPosition = Position;
                            if (targetPosition.X - 50 > Position.X)
                                MoveLeftRight(1);
                            if (targetPosition.X + 50 < Position.X)
                                MoveLeftRight(-1);
                            if (HasParachute) State = AIState.GoingForDoor;
                            //if (Helper.Random.Next(500) == 0) State = AIState.Panic;
                            break;
      
                    }

                    if (State == AIState.GoingForDoor || State == AIState.GoingForParachute || State == AIState.Panic)
                    {

                        if (Helper.Random.Next(200) == 0)
                        {
                            if (EnemyManager.Instance.Enemies.Where(en => (Vector2.Distance(Position, en.Position) < attackRange) && en!=this && en.Active && en.knockbackTime <= 0).Count() > 0)
                            {
                                punchHeld = true;
                                punchHeldTime = 0;
                                State = AIState.AttackingOther;
                            }
                            if (Vector2.Distance(gameHero.Position, Position) < attackRange)
                            {
                                punchHeld = true;
                                punchHeldTime = 0;
                                State = AIState.AttackingHero;
                            }
                        }

                        

                    }

                    if (knockbackTime<0 && Vector2.Distance(targetPosition, Position) < 10f) targetPosition = Position;

                    if ((State == AIState.GoingForDoor || State == AIState.GoingForParachute || State == AIState.Panic) && Item == null)
                    {
                        Item closest = ItemManager.Instance.ClosestItem(this);
                        
                        if(closest!=null && Vector2.Distance(closest.Position, Position)<50f)
                        {
                            Pickup();
                        }
                    }
                }

               

                if (!walking && !jumping && knockbackTime <= 0)
                {
                    walkFrameCount = 0;
                    Animations["walk"].Apply(skeleton, 0f, false);
                    if (!IsPlayer && State == AIState.Panic)
                    {
                        Animations["panic"].Mix(skeleton, animTime, true, 0.8f);
                        animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f) * 2;
                    }
                }

                if (walking && !jumping && knockbackTime <= 0)
                {
                    walkFrameCount++;
                    if (walkFrameCount > 5)
                    {
                        animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f) * 2;
                        Animations["walk"].Mix(skeleton, animTime, true, 0.3f);
                    }
                    if (!IsPlayer && State == AIState.Panic) Animations["panic"].Mix(skeleton, animTime, true, 0.8f);
                }

                if (pickingUp)
                {
                    pickupTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                    animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f) * 3;

                    Animations["pickup"].Apply(skeleton, animTime, false);

                    if (pickupTime > 150 && !hasPickedUp)
                    {
                        ItemManager.Instance.AttemptPickup(this);
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
                    if (HasParachute && ChuteItem != null)
                    {
                        ChuteItem.InWorld = true;
                        ChuteItem.DroppedPosition = Position;
                        ChuteItem.Position = Position + new Vector2(0, -50);
                        ChuteItem.Speed = new Vector2(0f,0.1f);
                        ChuteItem = null;
                        HasParachute = false;
                    }
                    if (Item != null)
                    {
                        Item.InWorld = true;
                        Item.DroppedPosition = Position;// +new Vector2(0, -75);
                        Item.Position = Position + new Vector2(0, -50);
                        Item.Speed = new Vector2(0f,0.1f);
                        Item = null;
                    }

                    knockbackTime -= gameTime.ElapsedGameTime.TotalMilliseconds;

                    animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f) * 3;
                    Animations["knockback"].Mix(skeleton, animTime, true, 0.3f);

                   // CheckCollision(gameTime, gameMap, levelSectors, walkableLayers, gameHero.Sector);
                    Position += (Speed);

                    if (Speed.X > 0) Speed.X -= 0.1f;
                    else if (Speed.X < 0) Speed.X += 0.1f;

                    if (knockbackTime < 100) State = AIState.Panic;
                    //if (fistSound.Volume > 0f) fistSound.Volume = MathHelper.Clamp(fistSound.Volume -= 0.1f,0f,1f);
                   // if (fistSound.Pitch > -1f) fistSound.Pitch = MathHelper.Clamp(fistSound.Pitch - 0.1f,-0.9f,0.9f);

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

                    if (punchHeld)
                    {

                        if (Item == null)
                        {
                            
                            Animations["punch-hold"].Apply(skeleton, 1f, false);

                            
                        }
                        else if (Item.Type == ItemType.Melee)
                        {
                          
                            Animations["punch-hold"].Apply(skeleton, 1f, false);


                        }
                        //else if (Item.Type == ItemType.Projectile)
                        //{
                        //    attackCharge += 0.25f;
                        //    Animations["punch-release"].Apply(skeleton, 1f, false);
                        //    if (IsPlayer || rand.Next(50) == 0)
                        //        Item.Use(faceDir, attackCharge, gameHero);
                        //}

                        

                    }
                    else if (punchReleased)
                    {

                        if (Item == null)
                        {
                            if (punchReleaseTime == 0)
                            {
                                //AudioController.PlaySFX("swipe", 0.5f, -0.25f + ((float)rand.NextDouble() * 0.5f), 0f);
                                if (IsPlayer)
                                    EnemyManager.Instance.CheckAttack(Position, faceDir, 0f, attackRange, 1, gameHero);
                                else
                                {
                                    switch (State)
                                    {
                                        case AIState.AttackingHero:
                                            if ((Position - gameHero.Position).Length() < attackRange && gameHero.IsInPlane) gameHero.DoHit(Position, 0f, faceDir, gameHero);
                                            State = AIState.Panic;
                                            if (HasParachute) State = AIState.GoingForDoor;
                                            break;
                                        case AIState.AttackingOther:
                                            EnemyManager.Instance.CheckAttack(Position, faceDir, 0f, attackRange, 1, gameHero);
                                            State = AIState.Panic;
                                            if (HasParachute) State = AIState.GoingForDoor;
                                            break;
                                    }
                                }
                                
                            }
                        }
                        else if (Item.Type == ItemType.Melee)
                        {
                            if (punchReleaseTime == 0)
                            {
                                //AudioController.PlaySFX("swipe", 0.5f, -0.25f + ((float)rand.NextDouble() * 0.5f), 0f);
                                if (IsPlayer)
                                    EnemyManager.Instance.CheckAttack(Position, faceDir, (float)(250 * ((int)Item.Name + 2)), attackRange + (20 * ((int)Item.Name + 2)), (int)Item.Name + 2, gameHero);
                                else
                                {
                                    //switch (State)
                                    //{
                                      //  case AIState.AttackingHero:
                                    if ((Position - gameHero.Position).Length() < attackRange + (20 * ((int)Item.Name + 2)) && gameHero.IsInPlane)
                                    {
                                        gameHero.DoHit(Position, (float)(250 * ((int)Item.Name + 2)), faceDir, gameHero);
                                        EnemyManager.Instance.CheckAttack(Position, faceDir, (float)(250 * ((int)Item.Name + 2)), attackRange + (20 * ((int)Item.Name + 2)), (int)Item.Name + 2, gameHero);

                                    }
                                    else
                                    {

                                        //    break;
                                        //case AIState.AttackingOther:
                                        EnemyManager.Instance.CheckAttack(Position, faceDir, (float)(250 * ((int)Item.Name + 2)), attackRange + (20 * ((int)Item.Name + 2)), (int)Item.Name + 2, gameHero);
                                        //  State = AIState.Panic;
                                        //break;
                                    }
                                    State = AIState.Panic;
                                    if (HasParachute) State = AIState.GoingForDoor;

                                    //}
                                }

                            }
                        }
                        //else if (Item.Type == ItemType.Projectile)
                        //{
                        //    punchReleaseTime = Item.Cooldown;
                        //}

                        punchReleaseTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                        if (punchReleaseTime >= 200)
                        {
                            punchReleaseTime = 0;
                            punchReleased = false;
                            Animations["punch-release"].Apply(skeleton, 0f, false);
                        }

                        Animations["punch-release"].Apply(skeleton, 1f, false);

                    }

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
               // punchHeld = false;
            }

            if (!Active)
            {
                if (HasParachute && ChuteItem != null && IsInPlane)
                {
                    ChuteItem.InWorld = true;
                    ChuteItem.DroppedPosition = Position;
                    ChuteItem.Position = Position + new Vector2(0, -50);
                    ChuteItem.Speed = new Vector2(0f, 0.1f);
                    ChuteItem = null;
                    HasParachute = false;
                    
                }
                if (Item != null)
                {
                    Item.InWorld = true;
                    Item.DroppedPosition = Position;// +new Vector2(0, -75);
                    Item.Position = Position + new Vector2(0, -50);
                    Item.Speed = new Vector2(0f, 0.1f);
                    Item = null;
                }

                if (!IsInPlane)
                {
                    if (!HasParachute)
                    {
                        animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f);
                        Animations["knockout"].Mix(skeleton, animTime, true, 0.2f);
                    }
                    else Animations["walk"].Apply(skeleton, 0f, true);
                }
                else
                {
                    animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f);
                    Animations["knockout"].Mix(skeleton, animTime, true, 0.2f);
                }

                deadTime -= gameTime.ElapsedGameTime.TotalMilliseconds;
                //if (deadTime > 0 && deadTime<1000)
                //{
                //    //CheckCollision(gameTime, gameMap, levelSectors, walkableLayers, gameHero.Sector);
                //    Position.X += ((float)-faceDir) * (0.001f * (1000f - (float)deadTime));
                //}
                //if (deadTime > 2000 && alpha > 0f)
                //{
                //    alpha -= 0.025f;
                //    alpha = MathHelper.Clamp(alpha, 0f, 1f);
                //}

                //if (skeleton.FindSlot("fist-item").A > 0f) skeleton.FindSlot("fist-item").A -= 0.1f;

                if (deadTime <= 0)
                {
                    Health = 3;
                    Active = true;
                    State = AIState.Panic;
                    deadTime = 0;
                }

            }

            if (Item != null)
            {
                if (Item.Type == ItemType.Melee)
                {
                    skeleton.SetAttachment("melee-item", Item.Name.ToString().ToLower());
                }
            }
            else skeleton.SetAttachment("melee-item", null);

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

            if (IsInPlane && !doorOpen)
            {
                Position.X -= (planeRot * 10f);
                targetPosition.X -= (planeRot * 10f);

                Position.X = MathHelper.Clamp(Position.X, 1150, gameMap.Width * gameMap.TileWidth-400f);
                Position.Y = MathHelper.Clamp(Position.Y, 0, gameMap.Height * gameMap.TileHeight);
            }

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
            if (!Active || knockbackTime > 0 || pickingUp) return;

            animTime = 0;
            pickingUp = true;
            pickupTime = 0;
        }

        public void Attack(bool p)
        {
            if (knockbackTime > 0 || pickingUp) return;

            if (punchHeld && !p)
            {
                punchReleased = true;
                punchReleaseTime = 0;
                AudioController.PlaySFX("swipe", 0.5f, -0.1f, 0.1f, Position);
            }
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

        internal void DoHit(Vector2 pos, float power, int face, Dude attacker)
        {
            if (attacker.Item == null)
                AudioController.PlaySFX("hit_punch", 0.5f, -0.2f, 0.2f, Position);
            else
                AudioController.PlaySFX("hit_" + (int)attacker.Item.Name, 0.5f, -0.2f, 0.2f, Position);

            if (knockbackTime > 0 || !Active) return;

            if (Health > 1)
            {

                if (knockbackTime <= 0)
                {
                    knockbackTime = 1000f + power;
                    faceDir = -face;
                    if (IsPlayer) knockbackTime *= 0.5;
                    Speed.X = 15f * (float)face;

                    if (jumping)
                    {
                        jumping = false;
                        falling = true;
                    }
                }
                Health--;
            }
            else
            {
                //knockbackTime = 3000f;
                //knockbackTime = 1000f;
                deadTime = 1000 + Helper.Random.NextDouble()*4000;
                faceDir = -face;
                if (IsPlayer) knockbackTime *= 0.5;
                Speed.X = 15f * (float)face;
                Active = false;
            }

            //if (IsPlayer)
            //    Health -= (power / 2f);
            //else
            //{
            //    gameHero.Score += (int)power;
            //    Health -= power;
            //}

            //AudioController.PlaySFX("hit" + (1 + rand.Next(3)).ToString(), 1f, -0.25f + ((float)rand.NextDouble() * 0.5f), 0f);
            //AudioController.PlaySFX("thud", 0.8f, 0f, 0f);

            //ParticleManager.Instance.AddHurt(Position + new Vector2(0, -75f), new Vector2((power / 5f) * face, 0f), landingHeight, tint);

            //AIchargingAttack = false;
            //attackCharge = 0f;

            //if (Health <= 0)
            //{
            //    AudioController.PlaySFX("powerdown", 0.5f, 0f, 0f);

            //    if (Item != null)
            //    {
            //        Item.InWorld = true;
            //        Item.Position = Position + new Vector2(0, -75);
            //        Item.DroppedPosition = Position;
            //        Item.Speed.Y = 2f;
            //        Item = null;
            //    }

            //    if (!IsPlayer)
            //    {
            //        gameHero.Score += 500;

            //        if ((int)gameHero.Health < 120)
            //        {
            //            for (int i = 0; i < ((120 - (int)gameHero.Health) / 2) + (rand.Next((120 - (int)gameHero.Health)) / 2); i++)
            //            {
            //                ParticleManager.Instance.Add(ParticleType.Health, Position + new Vector2(0, -75f), (landingHeight - 10f) + ((float)rand.NextDouble() * 20f), new Vector2(-10f + ((float)rand.NextDouble() * 20f), 0), 2000, true, new Rectangle(11, 0, 15, 8), (float)rand.NextDouble() * MathHelper.TwoPi, Color.Red);
            //            }
            //        }
            //    }
            //}
        }
    }
}
