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
        private List<(int, Vector2, Vector2, int)> movingCards;
        private int[] recoveredArrived;

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
            recoveredArrived = new int[] { -1, -1, -1, -1 };

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

            movingCards = new List<(int, Vector2, Vector2, int)>();
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
                            Vector2 position = new Vector2(xi * (cardWidth + stackOffset.X) + stackOffset.X, yi * (4 * 6) + stackOffset.Y);
                            Vector2 target = new Vector2(9 * (cardWidth + stackOffset.X), (stacks[xi][yi].Item1 / 13) * 150) + stackOffset;
                            float distance = (target - position).Length();
                            movingCards.Add((stacks[xi][yi].Item1, position, (target - position) / distance * 20, (int)distance / 20));

                            recoveredCards[stacks[xi][yi].Item1 / 13]++;
                            stacks[xi].RemoveAt(yi);
                            int count = stacks[xi].Count;
                            if (count > 0)
                                stacks[xi][count - 1] = (stacks[xi][count - 1].Item1, true);
                        }
                        else
                        {
                            while (yi > 0 && stacks[xi][yi - 1].Item1 % 13 > 0 && stacks[xi][yi - 1].Item1 == stacks[xi][yi].Item1 + 1)
                                yi--;

                            draggedStack = stacks[xi].TakeLast(stacks[xi].Count - yi).ToList();
                            draggedStackOrigin = xi;
                            dragOffset = new Vector2(x - xi * (cardWidth + stackOffset.X) - stackOffset.X, y - yi * (4 * 6) - stackOffset.Y);
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
            DrawMovingCards();
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
                int cardNumber = recoveredArrived[i];
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

        private void DrawMovingCards()
        {
            for (int i = movingCards.Count - 1; i >= 0; i--)
            {
                (int, Vector2, Vector2, int) element = movingCards[i];
                int cardNumber = element.Item1;
                Vector2 position = element.Item2;
                Vector2 step = element.Item3;
                int stepsLeft = element.Item4;

                float scaler = cardWidth / hiddenCardTexture.Width;
                Rectangle source = new Rectangle((cardNumber % 13)* 30, (cardNumber / 13) * 60, 30, 60);
                _spriteBatch.Draw(cardTextures, position, source, cardColors[cardNumber / 13], 0, Vector2.Zero, scaler, SpriteEffects.None, 0);

                movingCards.RemoveAt(i);
                if (stepsLeft > 0)
                    movingCards.Add((cardNumber, position + step, step, stepsLeft - 1));
                else
                {
                    int country = cardNumber / 13;
                    int max = cardNumber % 13;
                    if (max < recoveredArrived[country])
                        max = recoveredArrived[country];
                    recoveredArrived[country] = max;
                }
            }
        }
    }
}