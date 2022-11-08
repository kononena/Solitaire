using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;

namespace Solitaire
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private GraphicsDevice _device;
        private int _screenWidth;
        private int _screenHeight;
        private Texture2D[] cardTextures;
        private List<int>[] stacks;
        private bool isMouseDragging;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 800;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();

            base.Initialize();
        }

        private void CreateCards()
        {
            cardTextures = new Texture2D[52];
            for (int c = 0; c < 52; c++)
            {
                Color[] cardColors = new Color[52];

                for (int x = 0; x < 52; x++)
                {
                    cardColors[x] = Color.White;
                }

                for (int y = 0; y <= c % 13; y++)
                {
                    cardColors[(int)(c / 13) * 13 + y] = Color.Black;
                }

                cardTextures[c] = new Texture2D(_device, 13, 4, false, SurfaceFormat.Color);
                cardTextures[c].SetData(cardColors);
            }
        }

        private void CreateStacks()
        {
            stacks = new List<int>[4];

            for (int x = 0; x < 4; x++)
            {
                stacks[x] = new List<int>();
                for (int y = 0; y < 13; y++)
                {
                    stacks[x].Add(13 * x + y);
                }
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _device = _graphics.GraphicsDevice;

            _screenWidth = _device.PresentationParameters.BackBufferWidth;
            _screenHeight = _device.PresentationParameters.BackBufferHeight;

            isMouseDragging = false;

            CreateCards();
            CreateStacks();
        }

        private void ProcessMouse()
        {
            MouseState mouseState = Mouse.GetState();

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                int x = mouseState.X;
                int y = mouseState.Y;

                if (!isMouseDragging && x >= 0 && x < _screenWidth && y >= 0 && y < _screenHeight)
                {
                    int xi = (int)(x / (13 * 9)) % 4;
                    int yi = (int)(y / (4 * 9)) % stacks[xi].Count;

                    int element = stacks[xi][yi];

                    stacks[xi].Remove(element);
                    stacks[(xi + 1) % 4].Add(element);
                }

                isMouseDragging = true;
            }

            if (mouseState.LeftButton == ButtonState.Released)
            {
                isMouseDragging = false;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            ProcessMouse();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();
            DrawCards();
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawCards()
        {
            for (int x = 0; x < stacks.Length; x++)
            {
                List<int> stack = stacks[x];
                for (int y = 0; y < stack.Count; y++)
                {
                    Vector2 position = new Vector2(x * 13 * 9, y * 4 * 9);
                    _spriteBatch.Draw(cardTextures[stack[y]], position, null, Color.White, 0, Vector2.Zero, 8f, SpriteEffects.None, 0);
                }
            }
        }
    }
}