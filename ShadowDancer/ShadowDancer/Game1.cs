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
using Microsoft.Research.Kinect.Nui;

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

        //Kinect
        Runtime nui;
        //kinect data
        Texture2D kinectDepth;

        //float i = 0f;

        //private uint[] data;
        //private Vertices verts;
        //private Vector2 scale;

        Texture2D polyonTexture;
        Texture2D circleTexture;

        Vector2 gOrigin;

        public DebugViewXNA debugViewXNA;

        private const float MeterInPixels = 64f;

        Boolean _player = false;

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
            // Create the world for Farseer
            world = new World(new Vector2(0,0.01f));

            //set the window size [800x600]
            graphics.PreferredBackBufferHeight = 600;
            graphics.PreferredBackBufferWidth = 800;

            //initialize the kinect
            nui = Runtime.Kinects[0];
            nui.Initialize(RuntimeOptions.UseDepthAndPlayerIndex);

            nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex);
            nui.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_DepthFrameReady);

            base.Initialize();
        }

        private void nui_DepthFrameReady(object sender, ImageFrameReadyEventArgs e) {
            PlanarImage p = e.ImageFrame.Image;
            _player = false;
            Color[] DepthColor = new Color[p.Height * p.Width];

            float maxDist = 4000;
            float minDist = 850;
            float distOffset = maxDist - minDist;

            kinectDepth = new Texture2D(GraphicsDevice, p.Width, p.Height);
            
            int index = 0;

            for (int y = 0; y < p.Height; y++)
            {
                for (int x = 0; x < p.Width; x++, index += 2)
                {
                    int n = (y * p.Width + x) * 2;
                    int distance = (p.Bits[n + 0] | p.Bits[n + 1] << 8);
                    int player = distance % 8;
                    distance = distance / 8;
                    if (player != 0)
                    {
                        if (!_player)
                        {
                            _player = true;
                        }
                        byte intensity = (byte)(255 - (255 * Math.Max(distance - minDist, 0) / (distOffset)));
                        //DepthColor[y * p.Width + x] = new Color(intensity, intensity, intensity);
                        DepthColor[y * p.Width + x] = new Color(255, 255, 255, 255);
                    }
                    else
                    {
                        DepthColor[y * p.Width + x] = new Color(0, 0, 0, 0);
                    }
                }
            }

            kinectDepth.SetData(DepthColor);
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
            
            /*
            data = new uint[kinectDepth.Width * kinectDepth.Height];
            kinectDepth.GetData(data);
            if (kinectDepth != null)
            {
                verts = PolygonTools.CreatePolygon(data, kinectDepth.Width, false);
                scale = new Vector2(0.015f, -0.015f);
                verts.Scale(ref scale);

                List<Vertices> _list = BayazitDecomposer.ConvexPartition(verts);
            }
            */
            // TODO: use this.Content to load your game content here
            

            Vector2 circPos = new Vector2(500, 105) / MeterInPixels;
            myBody = BodyFactory.CreateCircle(world, (32f / MeterInPixels)/2, 1, circPos);
            myBody.BodyType = BodyType.Dynamic;
            myBody.Restitution = .4f;
            myBody.Friction = 0.5f;        
            

            //debugViewXNA.LoadContent(GraphicsDevice, Content);
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
            
            //define the origin, I probably don't need the defenition here as it exists.
            gOrigin = new Vector2(
                polyonTexture.Width / 2f,
                polyonTexture.Height / 2f);

            //uses the toVerticies code (slightly modified) found at: http://farseerphysics.codeplex.com/discussions/246636
            if (_player)
            {
                List<Vertices> vertices = ToVertices(kinectDepth, out gOrigin);

                //creates a body based on the verticies in the List verticies
                compund = BodyFactory.CreateCompoundPolygon(world, vertices, 1f);


                //sets the position of the ground sprite and converts pixels to meters for the engine.
                compund.Position = new Vector2(0, 0) / MeterInPixels;
            }
            // Steps the game forward.
            world.Step((float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.001f);
            //i -= .1f;
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);


            /* This takes the positions for the circle and ground positions 
             * from the engine in meters and converts them to pixels.
             */
            Vector2 circlePos = myBody.Position * MeterInPixels;
            //Vector2 groundPos = compund.Position * MeterInPixels;

            Vector2 circleOrigin = new Vector2(
                circleTexture.Width/2f,
                circleTexture.Height/2f);
            Vector2 groundOrigin = new Vector2(
                polyonTexture.Width/2f,
                polyonTexture.Height/2f);

            spriteBatch.Begin();
            //draw the kinect data
            if (_player)
            {
                spriteBatch.Draw(kinectDepth, new Rectangle(0, 0, 800, 600), Color.White);
            }
            //draw game data
            spriteBatch.Draw(circleTexture, circlePos, null, Color.White, 0f, circleOrigin, 1f, SpriteEffects.None, 0f);  
            //spriteBatch.Draw(polyonTexture, groundPos, null, Color.White, 0f, gOrigin, 1f, SpriteEffects.None, 0f);
            spriteBatch.End();

            base.Draw(gameTime);
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
            try
            {
                List<Vertices> list = BayazitDecomposer.ConvexPartition(textureVertices);
            

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
            catch
            {
                return new List<Vertices>();
            }
        }
    }
}
