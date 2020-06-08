using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Box2DX.Common;
using Box2DX.Dynamics;
using Box2DX.Collision;
using Color = Microsoft.Xna.Framework.Color;
using System.Diagnostics;
using System;
using System.Threading;

using Asteroid.Core.utils;
using Asteroid.Core.physics;
using Asteroid.Core.entities;
using Asteroid.Core.worlds;
using Asteroid.Core.render;
using Asteroid.Core.network;

namespace Asteroid
{
    public class Asteroid : Game
    {
        readonly int WINDOW_WIDTH = 1280;
        readonly int WINDOW_HEIGHT = 720;

        readonly int VIRTUAL_WIDTH = 1280;
        readonly int VIRTUAL_HEIGHT = 720;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        BaseWorld world;
        Synchronizer synchronizer;

        public Asteroid()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = WINDOW_WIDTH;
            graphics.PreferredBackBufferHeight = WINDOW_HEIGHT;
            // сглаживание
            //graphics.PreferMultiSampling = true;
            RasterizerState rs = new RasterizerState();
            rs.CullMode = CullMode.None; // видно задние части полигонов
            //rs.MultiSampleAntiAlias = true;
            GraphicsDevice.RasterizerState = rs;
            //graphics.GraphicsProfile = GraphicsProfile.HiDef;
            //GraphicsDevice.PresentationParameters.MultiSampleCount = 8;
            graphics.ApplyChanges();

            Translator.ScaleX = (float)WINDOW_WIDTH / (float)VIRTUAL_WIDTH;
            Translator.ScaleY = (float)WINDOW_HEIGHT / (float)VIRTUAL_HEIGHT;

            // создаю матрицы проекции и вида
            Camera.ViewMatrix = Matrix.CreateLookAt(new Vector3(0, 0, 10), Vector3.Zero, Vector3.Up);
            Camera.ProjectionMatrix = Matrix.CreateOrthographic(WINDOW_WIDTH, WINDOW_HEIGHT, 1, 100);
            //Camera.ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
            //    (float)WINDOW_WIDTH / (float)WINDOW_HEIGHT,
            //    1, 100);

            //Camera.DefaultWorldMatrix = Matrix.CreateWorld(new Vector3(0, 0, 0f), new Vector3(0, 0, -1), Vector3.Up);

            Camera.DefaultWorldMatrix =
                Matrix.CreateScale(
                    1f / Translator.PhysScalar * Translator.ScaleX,
                    1f / Translator.PhysScalar * Translator.ScaleY,
                    0)
                * Matrix.CreateTranslation(
                    new Vector3(-(WINDOW_WIDTH / 2), (WINDOW_HEIGHT / 2), 0));

            // задаю шейдер и его настройки
            Camera.CurrentEffect = new BasicEffect(GraphicsDevice);
            Camera.CurrentEffect.VertexColorEnabled = true;
            Camera.CurrentEffect.View = Camera.ViewMatrix;
            Camera.CurrentEffect.Projection = Camera.ProjectionMatrix;

            // старт физической симуляции
            SyncSimulation.Initialize();
            // создание игрового мира
            world = new SpaceWorld(new Vector2(VIRTUAL_WIDTH, VIRTUAL_HEIGHT), new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT));
            synchronizer = new Synchronizer(world);

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

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }
        TimeSpan lastUpd = new TimeSpan(0);
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            synchronizer.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            world.Render(gameTime, GraphicsDevice);
            base.Draw(gameTime);
        }
    }
}
