using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;

namespace Solitaire
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private GraphicsDevice _device;
        private SpriteFont systemFont;

        private int _screenWidth;
        private int _screenHeight;
        private Texture2D cardTextures;
        private Texture2D hiddenCardTexture;
        private float cardWidth;
        private List<(int, bool)>[] stacks;
        private bool isMouseDragging;
        private List<(int, bool)> draggedStack;
        private int draggedStackOrigin;
        private Vector2 dealButtonLocation;
        private Vector2 resetButtonLocation;
        private Color[] cardColors;
        private int[] recoveredCards;
        private Vector2 stackOffset;
        private Vector2 dragOffset;

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

        private void CreateStacks()
        {
            stacks = new List<(int, bool)>[7];

            for (int x = 0; x < 7; x++)
                stacks[x] = new List<(int, bool)>();

            Random rand = new Random();
            List<int> cards = new List<int>();
            for (int c = 0; c < 52; c++)
                cards.Add(c);
            cards = cards.OrderBy(n => rand.Next()).ToList();

            for (int c = 0; c < 52; c++)
            {
                bool isVisible = (c % 7) <= (c / 7);
                stacks[c % 7].Add((cards[c], isVisible));
            }

            recoveredCards = new int[] { -1, -1, -1, -1 };

            Window.Title = "Solitaire";
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _device = _graphics.GraphicsDevice;
            systemFont = Content.Load<SpriteFont>("SystemFont");

            _screenWidth = _device.PresentationParameters.BackBufferWidth;
            _screenHeight = _device.PresentationParameters.BackBufferHeight;

            cardWidth = (float)_screenWidth / 12;
            isMouseDragging = false;
            draggedStack = new List<(int, bool)>();
            draggedStackOrigin = -1;
            stackOffset = new Vector2(10, 10);

            cardTextures = Content.Load<Texture2D>("cards");
            hiddenCardTexture = Content.Load<Texture2D>("hiddenCard");
            CreateStacks();

            dealButtonLocation = new Vector2(7 * (cardWidth + stackOffset.X), 0) + stackOffset;
            resetButtonLocation = new Vector2(7 * (cardWidth + stackOffset.X), 50) + stackOffset;

            cardColors = new Color[] { Color.LightPink, Color.LightYellow, Color.LightGreen, Color.LightSkyBlue };
        }

        private void DealCards()
        {
            List<int> cards = new List<int>();
            for (int x = 0; x < 7; x++)
            {
                List<(int, bool)> stack = stacks[x];
                int count = stack.Count;
                int hidden = stack.Sum(e => e.Item2 ? 0 : 1);

                for (int i = count - 1; i >= hidden; i--)
                    cards.Add(stack[i].Item1);
                for (int i = 0; i < hidden; i++)
                    cards.Add(stack[i].Item1);
            }

            for (int i = 0; i < 7; i++)
                stacks[i].Clear();

            for (int c = 0; c < cards.Count; c++)
            {
                bool isVisible = (c % 7) <= (c / 7);
                stacks[c % 7].Add((cards[cards.Count-1-c], isVisible));
            }

            for (int i = 0; i < 7; i++)
            {
                int count = stacks[i].Count;
                if (count > 0)
                    stacks[i][count - 1] = (stacks[i][count - 1].Item1, true);
            }

            Window.Title = Window.Title + " I";
        }

        private void ProcessMouse()
        {
            MouseState mouseState = Mouse.GetState();

            int x = mouseState.X;
            int y = mouseState.Y;

            bool isValidLocation = x >= 0 && x < _screenWidth && y >= 0 && y < _screenHeight;

            if (!isValidLocation)
                return;

            bool canBeRecovered(int n)
            {
                return recoveredCards[n / 13] == (n % 13) - 1;
            }

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                if (!isMouseDragging)
                {
                    int xi = (int)((x - stackOffset.X) / (cardWidth + stackOffset.X));
                    int yi = (y - (int)stackOffset.Y) / (4 * 6);
                    if (xi >= 0 && xi < 7 && stacks[xi].Count > 0 && yi >= stacks[xi].Count && yi < stacks[xi].Count + 4)
                        yi = stacks[xi].Count - 1;
                    
                    if (xi >= 0 && xi < 7 && yi >= 0 && yi < stacks[xi].Count && stacks[xi][yi].Item2)
                    {
                        if (yi == stacks[xi].Count - 1 && canBeRecovered(stacks[xi][yi].Item1))
                        {
                            recoveredCards[stacks[xi][yi].Item1 / 13]++;
                            stacks[xi].RemoveAt(yi);
                            int count = stacks[xi].Count;
                            if (count > 0)
                                stacks[xi][count - 1] = (stacks[xi][count - 1].Item1, true);
                        }
                        else
                        {
                            int shift = 0;
                            while (yi > 0 && stacks[xi][yi].Item1 % 13 > 0 && stacks[xi][yi - 1].Item1 == stacks[xi][yi].Item1 + 1)
                            {
                                yi--;
                                shift++;
                            }

                            draggedStack = stacks[xi].TakeLast(stacks[xi].Count - yi).ToList();
                            draggedStackOrigin = xi;
                            dragOffset = new Vector2(x - xi * (cardWidth + stackOffset.X) - stackOffset.X, y - (yi + shift) * (4 * 6) - stackOffset.Y);
                            stacks[xi].RemoveRange(yi, stacks[xi].Count - yi);
                        }
                    }
                    else if (draggedStack.Count == 0)
                    {
                        if (x >= dealButtonLocation.X && x < dealButtonLocation.X + 50 && y >= dealButtonLocation.Y && y < dealButtonLocation.Y + 20)
                        {
                            DealCards();
                        }
                        else if (x >= resetButtonLocation.X && x < resetButtonLocation.X + 50 && y >= resetButtonLocation.Y && y < resetButtonLocation.Y + 20)
                        {
                            CreateStacks();
                        }
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
                    int xi = (int)((x - stackOffset.X) / (cardWidth + stackOffset.X));
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
                    dragOffset = Vector2.Zero;
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
            DrawButtons();
            DrawRecovered();
            DrawCards();
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawCards()
        {
            float scaler = cardWidth / hiddenCardTexture.Width;

            for (int x = 0; x < stacks.Length; x++)
            {
                List<(int, bool)> stack = stacks[x];
                for (int y = 0; y < stack.Count; y++)
                {
                    Vector2 position = new Vector2(x * (cardWidth + stackOffset.X), y * 4 * 6) + stackOffset;

                    if (stack[y].Item2)
                    {
                        Rectangle source = new Rectangle((stack[y].Item1 % 13) * 30, (stack[y].Item1 / 13) * 60, 30, 60);
                        _spriteBatch.Draw(cardTextures, position, source, cardColors[stack[y].Item1 / 13], 0, Vector2.Zero, scaler, SpriteEffects.None, 0);
                    }
                    else
                        _spriteBatch.Draw(hiddenCardTexture, position, null, Color.White, 0, Vector2.Zero, scaler, SpriteEffects.None, 0);
                }
            }

            MouseState mouseState = Mouse.GetState();
            for (int y = 0; y < draggedStack.Count; y++)
            {
                Vector2 position = new Vector2(mouseState.X, mouseState.Y + y * 4 * 6) - dragOffset;
                Rectangle source = new Rectangle((draggedStack[y].Item1 % 13) * 30, (draggedStack[y].Item1 / 13) * 60, 30, 60);
                _spriteBatch.Draw(cardTextures, position, source, cardColors[draggedStack[y].Item1 / 13], 0, Vector2.Zero, scaler, SpriteEffects.None, 0);
            }
        }

        private void DrawButtons()
        {
            _spriteBatch.DrawString(systemFont, "Deal again", dealButtonLocation, Color.White);
            _spriteBatch.DrawString(systemFont, "Retry", resetButtonLocation, Color.White);
        }

        private void DrawRecovered()
        {
            for (int i = 0; i < 4; i++)
            {
                int cardNumber = recoveredCards[i];
                Vector2 position = new Vector2(9 * (cardWidth + stackOffset.X), i * 150) + stackOffset;

                if (cardNumber >= 0)
                {
                    float scaler = cardWidth / hiddenCardTexture.Width;
                    Rectangle source = new Rectangle(cardNumber * 30, i * 60, 30, 60);
                    _spriteBatch.Draw(cardTextures, position, source, cardColors[i], 0, Vector2.Zero, scaler, SpriteEffects.None, 0);
                }
                else
                {
                    float scaler = cardWidth / hiddenCardTexture.Width;
                    _spriteBatch.Draw(hiddenCardTexture, position, null, Color.White, 0, Vector2.Zero, scaler, SpriteEffects.None, 0);
                }
            }
        }
    }
}