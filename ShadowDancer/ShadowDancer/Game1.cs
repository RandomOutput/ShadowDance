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
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using FarseerPhysics.Common.PolygonManipulation;
using FarseerPhysics.Common.Decomposition;
using FarseerPhysics.Common;
using FarseerPhysics.Controllers;
using FarseerPhysics.Collision;
using System.Diagnostics;
using FarseerPhysics.DebugViews;

namespace ShadowDancer
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        //setting up farseer world
        World world;

        //setting up collision body
        Body myBody;
        Body compund;

        private uint[] data;
        private Vertices verts;
        private Vector2 scale;

        Texture2D polyonTexture;
        Texture2D circleTexture;

        Vector2 gOrigin;

        public DebugViewXNA debugViewXNA;

        private const float MeterInPixels = 64f;

        public Game1()
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
            // TODO: Add your initialization logic here
            world = new World(new Vector2(0,5));
            graphics.PreferredBackBufferHeight = 600;
            graphics.PreferredBackBufferWidth = 800;
            debugViewXNA = new DebugViewXNA(world);

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

            polyonTexture = Content.Load<Texture2D>("colMask");
            circleTexture = Content.Load<Texture2D>("circ");
            
            data = new uint[polyonTexture.Width * polyonTexture.Height];
            polyonTexture.GetData(data);
            verts = PolygonTools.CreatePolygon(data, polyonTexture.Width, false);
            scale = new Vector2(0.015f, -0.015f);
            verts.Scale(ref scale);
            
            List<Vertices> _list = BayazitDecomposer.ConvexPartition(verts);

            // TODO: use this.Content to load your game content here
            

            Vector2 circPos = new Vector2(500, 100) / MeterInPixels;
            myBody = BodyFactory.CreateCircle(world, (32f / MeterInPixels)/2, 1, circPos);
            myBody.BodyType = BodyType.Dynamic;
            myBody.Restitution = .2f;
            myBody.Friction = 0.5f;

            /*
            Vector2 groundPos = new Vector2(510, 200) / MeterInPixels;
            //compund = BodyFactory.CreateCircle(world, (32f / MeterInPixels) / 2, 1, groundPos);         
            compund = BodyFactory.CreateCompoundPolygon(world, _list, 1f, groundPos);
            compund.BodyType = BodyType.Static;
            */
            
            
            

            debugViewXNA.LoadContent(GraphicsDevice, Content);
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
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            if (compund != null)
            {
                world.RemoveBody(compund);
            }
            
            gOrigin = new Vector2(
                polyonTexture.Width / 2f,
                polyonTexture.Height / 2f);
            List<Vertices> vertices = ToVertices(polyonTexture, out gOrigin);
            compund = BodyFactory.CreateCompoundPolygon(world, vertices, 1f);
            compund.Position = new Vector2(510, 300) / MeterInPixels;
            // TODO: Add your update logic here
            world.Step((float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.001f);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            Vector2 circlePos = myBody.Position * MeterInPixels;
            Vector2 groundPos = compund.Position * MeterInPixels;

            Vector2 circleOrigin = new Vector2(
                circleTexture.Width/2f,
                circleTexture.Height/2f);
            Vector2 groundOrigin = new Vector2(
                polyonTexture.Width/2f,
                polyonTexture.Height/2f);

            spriteBatch.Begin();
            spriteBatch.Draw(circleTexture, circlePos, null, Color.White, 0f, circleOrigin, 1f, SpriteEffects.None, 0f);  
            spriteBatch.Draw(polyonTexture, groundPos, null, Color.White, 0f, gOrigin, 1f, SpriteEffects.None, 0f);
            DebugDraw();
            spriteBatch.End();

            base.Draw(gameTime);
        }

        protected void DebugDraw()
        {
            Matrix proj = Matrix.CreateOrthographic(
                graphics.PreferredBackBufferWidth / 1f / 100.0f,
                -graphics.PreferredBackBufferHeight / 1f / 100.0f, 0, 1000000);
            Vector3 campos = new Vector3();
            campos.X = (graphics.PreferredBackBufferWidth / 2) / 100.0f;
            campos.Y = (graphics.PreferredBackBufferHeight / 2) / -100.0f;
            campos.Z = 0;
            Matrix tran = Matrix.Identity;
            tran.Translation = campos;
            Matrix view = tran;

            debugViewXNA.RenderDebugData(ref proj, ref view);
            return;
        }

        static List<Vertices> ToVertices(Texture2D texture, out Vector2 origin)
        {
            //Create an array to hold the data from the texture
            uint[] data = new uint[texture.Width * texture.Height];

            //Transfer the texture data to the array
            texture.GetData(data);

            Vertices textureVertices = PolygonTools.CreatePolygon(data, texture.Width, false);

            //The tool return vertices as they were found in the texture.
            //We need to find the real center (centroid) of the vertices for 2 reasons:

            //1. To translate the vertices so the polygon is centered around the centroid.
            var centroid = -textureVertices.GetCentroid();
            textureVertices.Translate(ref centroid);

            //2. To draw the texture the correct place.
            origin = -centroid;

            //We simplify the vertices found in the texture.
            textureVertices = SimplifyTools.ReduceByDistance(textureVertices, 5f);

            //Since it is a concave polygon, we need to partition it into several smaller convex polygons
            //List<Vertices> list = BayazitDecomposer.ConvexPartition(textureVertices);
            List<Vertices> list = EarclipDecomposer.ConvexPartition(textureVertices);

            //Now we need to scale the vertices (result is in pixels, we use meters)
            //At the same time we flip the y-axis.
            var scale = new Vector2(0.015f, 0.015f);

            foreach (Vertices vertices in list)
            {
                vertices.Scale(ref scale);

                //When we flip the y-axis, the orientation can change.
                //We need to remember that FPE works with CCW polygons only.
                vertices.ForceCounterClockWise();
            }
            return list;
        }
    }
}
