using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class LD28Game : Microsoft.Xna.Framework.Game
    {
        enum GameState
        {
            Intro,
            InGame,
            Outro
        }

        enum IntroState
        {
            FadeIn,
            FirstDelay,
            PostExplosion,
            Speech2,
            Speech3,
            Speech4,
            IntoGame,
        }

        GameState gameState;
        IntroState introState;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Dude pilot;

        Map gameMap;

        Camera gameCamera;

        ParticleManager particleManager;
        EnemyManager enemyManager;
        ItemManager itemManager;

        Speechbubble speechBubble;

        SoundEffectInstance sfxEngine;
        SoundEffectInstance sfxPanic;
        SoundEffectInstance sfxWind;
        SoundEffectInstance sfxRattle;
        Texture2D texBlank;
        Texture2D texDoor;
        Texture2D texGradient;
        SpriteFont fontAltitude;

        KeyboardState lks;

        double doorAnimTime = 0;
        int doorFrame = 0;
        bool doorOpen = false;

        double introTimer = 0;

        float fadeIn = 1f;
        Color currentFade = Color.White * 0f;
        bool fadingWhite = false;

        float planeRot = 0f;
        float planeRotTarget;

        int planeAltitude = 35000;

        float gradHeight = 0f;
        float outroCameraOffset = 0f;
        float outroCameraRot = 0f;

        float planeFloorHight = 1510f;

        double rattleTime = 0;

        double tutorialTime = 15000;

        string[] endingQuips = new string[] { "Yep.", "Well that just happened.", "Owned." };

        public LD28Game()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            
            base.OnExiting(sender, args);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            AudioController.LoadContent(Content);
            sfxEngine = Content.Load<SoundEffect>("sfx/engine").CreateInstance();
            sfxPanic = Content.Load<SoundEffect>("sfx/panic").CreateInstance();
            sfxWind = Content.Load<SoundEffect>("sfx/wind").CreateInstance();
            sfxRattle = Content.Load<SoundEffect>("sfx/rattle").CreateInstance();
            sfxEngine.IsLooped = true;
            sfxPanic.IsLooped = true;
            sfxWind.IsLooped = true;
            sfxRattle.IsLooped = true;

            texBlank = Content.Load<Texture2D>("blank");
            texDoor = Content.Load<Texture2D>("door");
            texGradient = Content.Load<Texture2D>("skygradient");
            fontAltitude = Content.Load<SpriteFont>("altfont");

            speechBubble = new Speechbubble(Content);

            gameMap = Content.Load<Map>("planemap");
            gameCamera = new Camera(GraphicsDevice.Viewport, gameMap);

            Reset();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            KeyboardState cks = Keyboard.GetState();

            // Allows the game to exit
            if (cks.IsKeyDown(Keys.Escape))
                this.Exit();

            if (this.IsActive)
            {
                switch (gameState)
                {
                    case GameState.InGame:
                        tutorialTime -= gameTime.ElapsedGameTime.TotalMilliseconds;
                        speechBubble.Visible = false;

                        if (cks.IsKeyDown(Keys.Left)) pilot.MoveLeftRight(-1);
                        if (cks.IsKeyDown(Keys.Right)) pilot.MoveLeftRight(1);
                        pilot.Attack(cks.IsKeyDown(Keys.Z));
                        if(cks.IsKeyDown(Keys.X) && !lks.IsKeyDown(Keys.X)) pilot.Pickup();

                        if (Helper.Random.Next(200) == 0 && pilot.IsInPlane)
                        {
                            rattleTime = Helper.Random.NextDouble() * 1000;
                            gameCamera.Shake(rattleTime, Helper.RandomFloat(10f));
                            rattleTime += 1000;
                        }
                        if (rattleTime > 0)
                        {
                            if(sfxRattle.State== SoundState.Paused) sfxRattle.Resume();
                            rattleTime -= gameTime.ElapsedGameTime.TotalMilliseconds;
                            if (rattleTime <= 0) sfxRattle.Pause();
                        }
                        if (Helper.Random.Next(200) == 0)
                            if (planeRotTarget >= -0.1f) planeRotTarget = Helper.RandomFloat(-0.3f, -0.1f);
                        if (Helper.Random.Next(200) == 0)
                            if (planeRotTarget < -0.1f) planeRotTarget = -0.1f;

                        planeAltitude -= 1 + (int)(-50f * planeRot);
                        gradHeight = ((texGradient.Height - GraphicsDevice.Viewport.Height) / 35000f) * (35000f - (float)planeAltitude);

                        planeRot = (float)MathHelper.Lerp(planeRot, planeRotTarget, 0.01f);
                        pilot.Update(gameTime, gameMap, pilot, planeRot, (doorOpen && doorFrame == 3));

                        enemyManager.Update(gameTime, gameCamera, gameMap, pilot, planeRot, (doorOpen && doorFrame==3));

                        CheckDoor();

                        if (doorOpen)
                        {
                            if (doorFrame < 3)
                            {
                                doorAnimTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                                if (doorAnimTime >= 200)
                                {
                                    doorAnimTime = 0;
                                    doorFrame++;
                                }
                            }
                        }

                        if (planeAltitude <= 0 && pilot.IsInPlane)
                        {
                            planeAltitude = 0;
                            gameState = GameState.Outro;
                            fadingWhite = true;
                            currentFade = Color.White * 0f;
                            AudioController.PlaySFX("explode");
                            sfxPanic.Stop();
                            sfxEngine.Stop();
                            sfxRattle.Pause();
                        }

                        if (!pilot.IsInPlane)
                        {
                            if (pilot.Position.Y < -400) outroCameraRot = MathHelper.Lerp(outroCameraRot, 0f, 0.1f);
                            else outroCameraRot = planeRot;

                            sfxWind.Play();
                            if (sfxPanic.Volume > 0f) sfxPanic.Volume = MathHelper.Clamp(sfxPanic.Volume - 0.01f, 0f, 1f);
                            if (sfxEngine.Volume > 0f) sfxEngine.Volume = MathHelper.Clamp(sfxEngine.Volume - 0.01f, 0f, 1f);
                            if (sfxWind.Volume < 1f) sfxWind.Volume = MathHelper.Clamp(sfxWind.Volume + 0.01f, 0f, 1f);

                            //if (sfxWind.Volume == 1f) gameState = GameState.Outro;
                            planeRotTarget = -0.5f;
                            //planeRot = (float)MathHelper.Lerp(planeRot, planeRotTarget, 0.01f);
                            //planeAltitude -= 1 + (int)(-50f * planeRot);
                            //gradHeight = ((texGradient.Height - GraphicsDevice.Viewport.Height) / 35000f) * (35000f - (float)planeAltitude);
                            if (planeAltitude <= 0)
                            {
                                if (outroCameraOffset > -100f)
                                {
                                    outroCameraOffset -= (pilot.HasParachute?5f:20f);
                                }
                                else
                                {
                                    if (!pilot.HasParachute)
                                    {
                                        fadingWhite = true;
                                        AudioController.PlaySFX("splat");
                                    }
                                    else currentFade = Color.Black * 0f;
                                    introTimer = 0;
                                    gameState = GameState.Outro;
                                    if(pilot.HasParachute) speechBubble.Show(pilot.Position + new Vector2(0f, -280f), endingQuips[Helper.Random.Next(endingQuips.Length)]);
                                    sfxWind.Stop();
                                }
                            }
                        }

                        break;
#region Intro
                    case GameState.Intro:
                        //planeAltitude -= (int)(-300f * planeRot);
                        if (fadeIn > 0f) fadeIn -= 0.01f;
                        if ((cks.IsKeyDown(Keys.Space) && !lks.IsKeyDown(Keys.Space)) || (cks.IsKeyDown(Keys.Z)) && !lks.IsKeyDown(Keys.Z)) introTimer += 5000f; 
                        introTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
                        switch (introState)
                        {
                            case IntroState.FadeIn:
                                if (introTimer > 5000)
                                {
                                    introTimer = 0;
                                    gameCamera.Shake(1000, 20f);
                                    AudioController.PlaySFX("explode");
                                    sfxPanic.Play();
                                    introState = IntroState.PostExplosion;
                                }
                                break;
                            case IntroState.PostExplosion:
                                if (planeRot > -0.05f) planeRot -= 0.0001f;
                                if (introTimer > 2000)
                                {
                                    introTimer = 0;
                                    speechBubble.Show(pilot.Position + new Vector2(50f, -250f), "Mayday! Mayday! We're going down!");
                                    introState = IntroState.Speech2;
                                }
                                break;
                            case IntroState.Speech2:
                                if (introTimer > 3000)
                                {
                                    introTimer = 0;
                                    speechBubble.Show(pilot.Position + new Vector2(90f, -260f), "...And we've only got one parachute!");
                                    introState = IntroState.Speech3;
                                }
                                break;
                            case IntroState.Speech3:
                                if (introTimer > 3000)
                                {
                                    introTimer = 0;
                                    speechBubble.Show(pilot.Position + new Vector2(50f, -250f), "That parachute is mine, ensign.");
                                    introState = IntroState.Speech4;
                                }
                                break;
                            case IntroState.Speech4:
                                if (introTimer > 3000)
                                {
                                    introTimer = 0;
                                    speechBubble.Show(pilot.Position + new Vector2(90f, -260f), "Haha, nope. See ya!");
                                    introState = IntroState.IntoGame;
                                }
                                break;
                            case IntroState.IntoGame:
                                if (introTimer > 3000)
                                {
                                    introTimer = 0;
                                    speechBubble.Visible = false;
                                    gameState = GameState.InGame;
                                    planeRotTarget = -0.1f;
                                }
                                break;
                        }
                        break;
#endregion
                    case GameState.Outro:
                        if (planeAltitude <= 0)
                        {
                            if (pilot.HasParachute && !pilot.IsInPlane)
                            {
                                introTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
                                if (introTimer > 3000)
                                {
                                    speechBubble.Visible = false;
                                    currentFade = Color.Lerp(currentFade, Color.Black * 1f, 0.05f);
                                    if (currentFade.R < 30 && currentFade.A > 230)
                                    {
                                        currentFade = Color.Black * 1f;
                                        introTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
                                        if (introTimer >= 3000) Reset();
                                    }
                                }
                            }
                            else
                            {
                                if (fadingWhite)
                                {
                                    currentFade = Color.Lerp(currentFade, Color.White * 1f, 0.1f);
                                    if (currentFade.A > 230) fadingWhite = false;
                                }
                                else
                                {
                                    currentFade = Color.Lerp(currentFade, Color.Black * 1f, 0.05f);
                                    if (currentFade.R < 30 && currentFade.A > 230)
                                    {
                                        currentFade = Color.Black * 1f;
                                        introTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
                                        if (introTimer >= 5000) Reset();
                                    }

                                }
                            }
                        }

                        break;
                }

                
                if (Helper.Random.Next(10) == 0)
                {
                    float zindex = Helper.RandomFloat(0f, 1f);
                    if (planeAltitude > 1000)
                    {
                        if (pilot.IsInPlane)
                        {
                            particleManager.Add(ParticleType.Cloud,
                                                new Vector2((gameMap.Width * gameMap.TileWidth) + (GraphicsDevice.Viewport.Width / 2), Helper.RandomFloat(700f, (gameMap.Height * gameMap.TileHeight))),
                                                new Vector2(-zindex * (20f), -0.1f + planeRot),
                                                30000f * (1f - zindex), false, new Rectangle(0, 0, 400, 200), 0f, Color.White, zindex);
                        }
                        else
                        {
                            if(planeAltitude>3000)
                                particleManager.Add(ParticleType.Cloud,
                                                new Vector2(Helper.RandomFloat(pilot.Position.X - (GraphicsDevice.Viewport.Width / 2), pilot.Position.X + (GraphicsDevice.Viewport.Width / 2)), pilot.Position.Y + 400f),
                                                new Vector2(0f, (pilot.HasParachute?-40f:-40f) - zindex * (20f)),
                                                30000f * (1f - zindex), false, new Rectangle(0, 0, 400, 200), 0f, Color.White, zindex);
                        }
                    }
                }

                gradHeight = MathHelper.Clamp(gradHeight, 0f, (texGradient.Height - GraphicsDevice.Viewport.Height));

                gameCamera.Rotation = pilot.IsInPlane ? planeRot : outroCameraRot;
                gameCamera.Target = pilot.Position + new Vector2(0f,outroCameraOffset);// -new Vector2(GraphicsDevice.Viewport.Width / 2, 550f);
                gameCamera.Update(gameTime);

                particleManager.Update(gameTime, gameMap, pilot.IsInPlane?planeRot:0f);
                itemManager.Update(gameTime, gameCamera, gameMap, pilot, planeRot);

                sfxEngine.Pitch = MathHelper.Clamp(-0.5f + ((-planeRot) * 8f), -1f, 1f);

            }

            

            lks = cks;



            base.Update(gameTime);
        }

        void CheckDoor()
        {
            if (doorOpen) return;
            MapObject door = ((MapObjectLayer)gameMap.GetLayer("door")).Objects[0];
            Point checkpoint;
            if (pilot.HasParachute) 
            {
                checkpoint = new Point((int)pilot.Position.X, (int)pilot.Position.Y);
                if (door.Location.Contains(checkpoint))
                {
                    doorOpen = true;
                    AudioController.PlaySFX("door");
                }
            }
            foreach (Dude d in enemyManager.Enemies)
            {
                if (d.HasParachute)
                {
                    checkpoint = new Point((int)d.Position.X, (int)d.Position.Y);
                    if (door.Location.Contains(checkpoint))
                    {
                        doorOpen = true;
                        AudioController.PlaySFX("door");

                    }
                }
            }
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            spriteBatch.Draw(texGradient, new Rectangle(0,-(int)gradHeight,GraphicsDevice.Viewport.Width,texGradient.Height), Color.White);
            spriteBatch.End();

            particleManager.Draw(GraphicsDevice, spriteBatch, gameCamera,0f,0.9f);

            enemyManager.Draw(GraphicsDevice, spriteBatch, gameCamera, 0f, 0f, false);
            if (!pilot.IsInPlane) pilot.Draw(GraphicsDevice, spriteBatch, gameCamera);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, gameCamera.CameraMatrix);
            spriteBatch.Draw(texDoor, new Vector2(1200, 1000), new Rectangle(300 * doorFrame, 0, 300, 600), Color.White);
            gameMap.DrawLayer(spriteBatch, "internal", gameCamera);
            spriteBatch.End();

            enemyManager.Draw(GraphicsDevice, spriteBatch, gameCamera, 0f, 0f, true);
            if(pilot.IsInPlane) pilot.Draw(GraphicsDevice, spriteBatch, gameCamera);
            itemManager.Draw(GraphicsDevice, spriteBatch, gameCamera);

            //particleManager.Draw(GraphicsDevice, spriteBatch, gameCamera, 0.901f, 1f);

            if (pilot.IsInPlane)
            {
                spriteBatch.Begin();
                spriteBatch.DrawString(fontAltitude, planeAltitude.ToString() + "ft", new Vector2(GraphicsDevice.Viewport.Width/2, 20)+Vector2.One, Color.Black, 0f, fontAltitude.MeasureString(planeAltitude.ToString() + "ft") * new Vector2(0.5f, 0), 1f, SpriteEffects.None, 1);
                spriteBatch.DrawString(fontAltitude, planeAltitude.ToString() + "ft", new Vector2(GraphicsDevice.Viewport.Width/2, 20), Color.White, 0f, fontAltitude.MeasureString(planeAltitude.ToString() + "ft") * new Vector2(0.5f, 0), 1f, SpriteEffects.None, 1);
                spriteBatch.End();
            }

            
            speechBubble.Draw(spriteBatch, gameCamera);
                
            

            if (tutorialTime > 0 && gameState == GameState.InGame)
            {
                spriteBatch.Begin();
                spriteBatch.DrawString(fontAltitude, "Cursors Left/Right: Move / Z: Attack / X: Get weapon", new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height - 50) + Vector2.One, Color.Black, 0f, fontAltitude.MeasureString("Cursors Left/Right: Move / Z: Attack / X: Get weapon") * new Vector2(0.5f, 0), 0.5f, SpriteEffects.None, 1);
                spriteBatch.DrawString(fontAltitude, "Cursors Left/Right: Move / Z: Attack / X: Get weapon", new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height - 50), Color.White, 0f, fontAltitude.MeasureString("Cursors Left/Right: Move / Z: Attack / X: Get weapon") * new Vector2(0.5f, 0), 0.5f, SpriteEffects.None, 1);
                spriteBatch.End();
            }

            spriteBatch.Begin();
            spriteBatch.Draw(texBlank, GraphicsDevice.Viewport.Bounds, Color.Black * fadeIn);
            spriteBatch.Draw(texBlank, GraphicsDevice.Viewport.Bounds, currentFade);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        void Reset()
        {
            currentFade = Color.White * 0f;
            fadeIn = 1f;

            particleManager = new ParticleManager();
            particleManager.LoadContent(Content);

            enemyManager = new EnemyManager();
            enemyManager.LoadContent(Content, GraphicsDevice);
            enemyManager.Spawn(gameMap, planeFloorHight);

            itemManager = new ItemManager();
            itemManager.LoadContent(Content, GraphicsDevice);

            itemManager.SpawnWorld(ItemType.Chute, ItemName.Chute, new Vector2((gameMap.Width * gameMap.TileWidth) - 650f, planeFloorHight-100f));
            itemManager.SpawnRandom(10, planeFloorHight);

            //pilot = new Dude(new Vector2(100,100), true);
            pilot = new Dude(new Vector2((gameMap.Width * gameMap.TileWidth) - 400f, planeFloorHight), true);
            pilot.Scale = 2f;
            pilot.LoadContent(Content, GraphicsDevice);

            gameCamera.Position = pilot.Position;

            //sfxEngine.Stop();
            sfxPanic.Stop();
            sfxWind.Stop();
            sfxPanic.Volume = 0.4f;
            sfxEngine.Volume = 1f;
            sfxWind.Volume = 0f;
            sfxEngine.Play();

            sfxRattle.Play();
            sfxRattle.Pause();

            gameState = GameState.Intro;
            introState = IntroState.FadeIn;
            introTimer = 0;

            planeRot = 0f;
            planeRotTarget = 0f;
            planeAltitude = 35000;
            gradHeight = 0f;
            outroCameraOffset = 0f;

            tutorialTime = 15000;

            doorOpen = false;
            doorFrame = 0;
        }
    }
}
