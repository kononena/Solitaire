using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;
using System.Linq;

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
        private List<(int, bool)>[] stacks;
        private bool isMouseDragging;
        private List<(int, bool)> draggedStack;
        private int draggedStackOrigin;

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
            stacks = new List<(int, bool)>[7];

            for (int x = 0; x < 7; x++)
                stacks[x] = new List<(int, bool)>();

            for (int c = 0; c < 52; c++)
            {
                bool isVisible = (c % 7) <= (c / 7);
                stacks[c % 7].Add((c, isVisible));
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _device = _graphics.GraphicsDevice;

            _screenWidth = _device.PresentationParameters.BackBufferWidth;
            _screenHeight = _device.PresentationParameters.BackBufferHeight;

            isMouseDragging = false;
            draggedStack = new List<(int, bool)>();
            draggedStackOrigin = -1;

            CreateCards();
            CreateStacks();
        }

        private void ProcessMouse()
        {
            MouseState mouseState = Mouse.GetState();

            int x = mouseState.X;
            int y = mouseState.Y;

            bool isValidLocation = x >= 0 && x < _screenWidth && y >= 0 && y < _screenHeight;

            if (!isValidLocation)
                return;

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                if (!isMouseDragging)
                {
                    int xi = x / (13 * 6);
                    int yi = y / (4 * 6);
                    if (xi >= 0 && xi < 7 && yi >= 0 && yi < stacks[xi].Count && stacks[xi][yi].Item2)
                    {
                        draggedStack = stacks[xi].TakeLast(stacks[xi].Count - yi).ToList();
                        draggedStackOrigin = xi;
                        stacks[xi].RemoveRange(yi, stacks[xi].Count - yi);
                    }
                }

                isMouseDragging = true;
            }

            bool isValidTarget(int index)
            {
                if (stacks[index].Count == 0)
                {
                    return draggedStack[0].Item1 % 13 == 12;
                }
                else
                {
                    int target = stacks[index].Last().Item1;
                    int dragged = draggedStack[0].Item1;
                    return stacks[index].Last().Item2 && target / 13 == dragged / 13 && target == dragged + 1;
                }
            }

            if (mouseState.LeftButton == ButtonState.Released)
            {
                if (isMouseDragging && draggedStack.Count > 0)
                {
                    int xi = x / (13 * 6);
                    if (xi >= 0 && xi < 7 && isValidTarget(xi))
                    {
                        stacks[xi].AddRange(draggedStack);
                        int count = stacks[draggedStackOrigin].Count;
                        if (count > 0)
                            stacks[draggedStackOrigin][count-1] = (stacks[draggedStackOrigin][count-1].Item1, true);
                    }
                    else
                        stacks[draggedStackOrigin].AddRange(draggedStack);

                    draggedStack.Clear();
                    draggedStackOrigin = -1;
                }

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
                List<(int, bool)> stack = stacks[x];
                for (int y = 0; y < stack.Count; y++)
                {
                    Vector2 position = new Vector2(x * 13 * 6, y * 4 * 6);
                    if (stack[y].Item2)
                        _spriteBatch.Draw(cardTextures[stack[y].Item1], position, null, Color.White, 0, Vector2.Zero, 5f, SpriteEffects.None, 0);
                    else
                        _spriteBatch.Draw(cardTextures[stack[y].Item1], position, null, Color.Blue, 0, Vector2.Zero, 5f, SpriteEffects.None, 0);
                }
            }
        }
    }
}