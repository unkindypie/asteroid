using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Box2DX.Common;
using Box2DX.Dynamics;
using Box2DX.Collision;

using Color = Microsoft.Xna.Framework.Color;

using Asteroid.src.utils;
using Asteroid.src.physics;
using Asteroid.src.entities;
using Asteroid.src.worlds;
using Asteroid.src.render;

namespace Asteroid
{
    public class Asteroid : Game
    {
        readonly int WINDOW_WIDTH = 1280;
        readonly int WINDOW_HEIGHT = 720;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        BaseWorld world;

        public Asteroid()
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
            graphics.PreferredBackBufferWidth = WINDOW_WIDTH;
            graphics.PreferredBackBufferHeight = WINDOW_HEIGHT;
            graphics.ApplyChanges();

            // создаю матрицы проекции и вида
            Camera.ViewMatrix = Matrix.CreateLookAt(new Vector3(0, 0, 6), Vector3.Zero, Vector3.Up);
            Camera.ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
                (float)WINDOW_WIDTH / (float)WINDOW_HEIGHT,
                1, 100);
            // задаю шейдер и его настройки
            Camera.CurrentEffect = new BasicEffect(GraphicsDevice);
            Camera.CurrentEffect.VertexColorEnabled = true;
            RasterizerState rs = new RasterizerState();
            rs.CullMode = CullMode.None; // видно задние части полигонов
            GraphicsDevice.RasterizerState = rs;
            Camera.CurrentEffect.View = Camera.ViewMatrix;
            Camera.CurrentEffect.Projection = Camera.ProjectionMatrix;

            // старт физической симуляции
            SyncSimulation.Initialize();
            // создание игрового мира
            world = new SpaceWorld();
            world.AddEntity(
                new Box(
                    new Vec2(0, 0),
                    0.5f,
                    0.5f
                    ));

            world.AddEntity(
            new Box(
                new Vec2(0, 0.6f),
                0.5f,
                0.5f
                ));

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

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            world.Update(gameTime.ElapsedGameTime);
            SyncSimulation.Step();
            
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            world.Render(gameTime.ElapsedGameTime, spriteBatch);
           
            base.Draw(gameTime);
        }
    }
}
