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
        Texture2D texBlank;

        KeyboardState lks;

        double introTimer = 0;

        float fadeIn =1f;

        float planeRot = 0f;
        float planeRotTarget;

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

            texBlank = Content.Load<Texture2D>("blank");

            speechBubble = new Speechbubble(Content);

            gameMap = Content.Load<Map>("planemap");
            gameCamera = new Camera(GraphicsDevice.Viewport, gameMap);

            particleManager = new ParticleManager();
            particleManager.LoadContent(Content);

            enemyManager = new EnemyManager();
            enemyManager.LoadContent(Content, GraphicsDevice);
            enemyManager.Spawn(gameMap);

            itemManager = new ItemManager();
            itemManager.LoadContent(Content, GraphicsDevice);

            itemManager.SpawnWorld(ItemType.Chute, ItemName.Chute, new Vector2((gameMap.Width * gameMap.TileWidth) - 650f, 500f));

            //pilot = new Dude(new Vector2(100,100), true);
            pilot = new Dude(new Vector2((gameMap.Width * gameMap.TileWidth) - 400f, 610f), true);
            pilot.Scale = 2f;
            pilot.LoadContent(Content, GraphicsDevice);

            gameCamera.Position = pilot.Position;

            sfxEngine.IsLooped = true;
            sfxPanic.IsLooped = true;
            sfxPanic.Volume = 0.4f;
            sfxEngine.Play();

            gameState = GameState.Intro;
            introState = IntroState.FadeIn;
            introTimer = 0;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
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
                        if (cks.IsKeyDown(Keys.Left)) pilot.MoveLeftRight(-1);
                        if (cks.IsKeyDown(Keys.Right)) pilot.MoveLeftRight(1);
                        pilot.Attack(cks.IsKeyDown(Keys.Z));

                        if (Helper.Random.Next(200) == 0) gameCamera.Shake(Helper.Random.NextDouble() * 1000, Helper.RandomFloat(10f));
                        if (Helper.Random.Next(100) == 0)
                            if (planeRotTarget >= 0f) planeRotTarget = Helper.RandomFloat(-0.3f, 0f);
                        if (Helper.Random.Next(500) == 0)
                            if (planeRotTarget < 0f) planeRotTarget = 0f;

                        planeRot = (float)MathHelper.Lerp(planeRot, planeRotTarget, 0.01f);
                        pilot.Update(gameTime, gameMap, pilot, planeRot);

                        enemyManager.Update(gameTime, gameCamera, gameMap, pilot, planeRot);

                        
                        break;
#region Intro
                    case GameState.Intro:
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
                                if (planeRot > -0.1f) planeRot -= 0.0001f;
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
                                }
                                break;
                        }
                        break;
#endregion
                }


                if (Helper.Random.Next(10) == 0)
                {
                    float zindex = Helper.RandomFloat(0f, 1f);
                    particleManager.Add(ParticleType.Cloud,
                                        new Vector2((gameMap.Width * gameMap.TileWidth) + (GraphicsDevice.Viewport.Width / 2), Helper.RandomFloat(-50f, GraphicsDevice.Viewport.Height + 200f)),
                                        new Vector2(-zindex * (20f), -0.1f + planeRot),
                                        30000f * (1f - zindex), false, new Rectangle(0, 0, 400, 200), 0f, Color.White, zindex);
                }

                gameCamera.Rotation = planeRot;
                gameCamera.Target = pilot.Position;// -new Vector2(GraphicsDevice.Viewport.Width / 2, 550f);
                gameCamera.Update(gameTime);

                particleManager.Update(gameTime, gameMap, planeRot);
                itemManager.Update(gameTime, gameCamera, gameMap, pilot, planeRot);

                sfxEngine.Pitch = MathHelper.Clamp(-0.5f + ((-planeRot) * 8f), -1f, 1f);

            }

            

            lks = cks;



            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            particleManager.Draw(GraphicsDevice, spriteBatch, gameCamera,0f,0.9f);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, gameCamera.CameraMatrix);
            gameMap.DrawLayer(spriteBatch, "internal", gameCamera);
            spriteBatch.End();

            enemyManager.Draw(GraphicsDevice, spriteBatch, gameCamera, 0f, 0f);
            pilot.Draw(GraphicsDevice, spriteBatch, gameCamera);
            itemManager.Draw(GraphicsDevice, spriteBatch, gameCamera);

            //particleManager.Draw(GraphicsDevice, spriteBatch, gameCamera, 0.901f, 1f);
            if (gameState == GameState.Intro)
            {
                speechBubble.Draw(spriteBatch, gameCamera);
                spriteBatch.Begin();
                spriteBatch.Draw(texBlank, GraphicsDevice.Viewport.Bounds, Color.Black * fadeIn);
                spriteBatch.End();
            }

            base.Draw(gameTime);
        }
    }
}
