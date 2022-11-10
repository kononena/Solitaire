using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Solitaire
{
    public class Card
    {
        public int number { get; }
        public bool isVisible { get; set; }
        public Vector2 position { get; set; }
        public Vector2 shift { get; set; }
        public int steps { get; set; }

        public Card(int number, bool isVisible)
        {
            this.number = number;
            this.isVisible = isVisible;
            this.position = Vector2.Zero;
            this.shift = Vector2.Zero;
            this.steps = 0;
        }
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private GraphicsDevice _device;
        private SpriteFont systemFont;

        private int _screenWidth;
        private int _screenHeight;
        private Card[] cards;
        private Texture2D cardTextures;
        private Texture2D hiddenCardTexture;
        private float cardWidth;
        private List<Card>[] stacks;
        private bool isMouseDragging;
        private List<Card> draggedStack;
        private int draggedStackOrigin;
        private Vector2 dealButtonLocation;
        private Vector2 resetButtonLocation;
        private Color[] cardColors;
        private int[] recoveredCards;
        private Vector2 stackOffset;
        private Vector2 dragOffset;
        private List<Card> movingCards;
        private int[] recoveredArrived;
        private Texture2D buttonTextures;
        private Texture2D emptyTexture;
        private bool isDealing;
        private bool isRetrying;

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
            stacks = new List<Card>[7];

            for (int x = 0; x < 7; x++)
                stacks[x] = new List<Card>();

            Random rand = new Random();
            List<int> cardNumbers = new List<int>();
            for (int c = 0; c < 52; c++)
                cardNumbers.Add(c);
            cardNumbers = cardNumbers.OrderBy(n => rand.Next()).ToList();

            for (int c = 0; c < 52; c++)
            {
                int cardNumber = cardNumbers[c];
                bool isVisible = (c % 7) <= (c / 7);
                stacks[c % 7].Add(cards[cardNumber]);
                cards[cardNumber].isVisible = isVisible;
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

            cards = new Card[52];
            for (int i = 0; i < 52; i++)
                cards[i] = new Card(i, false);

            _screenWidth = _device.PresentationParameters.BackBufferWidth;
            _screenHeight = _device.PresentationParameters.BackBufferHeight;

            stackOffset = new Vector2(10, 10);
            cardWidth = (_screenWidth - 12 * stackOffset.Y) / 9;
            isMouseDragging = false;
            draggedStack = new List<Card>();
            draggedStackOrigin = -1;

            cardTextures = Content.Load<Texture2D>("cards");
            hiddenCardTexture = Content.Load<Texture2D>("hiddenCard");
            emptyTexture = Content.Load<Texture2D>("emptyCard");
            CreateStacks();

            dealButtonLocation = new Vector2(7 * (cardWidth + stackOffset.X), 0) + stackOffset * 2;
            resetButtonLocation = new Vector2(7 * (cardWidth + stackOffset.X), 50) + stackOffset * 2;
            buttonTextures = Content.Load<Texture2D>("buttons");

            cardColors = new Color[] { Color.LightPink, Color.LightYellow, Color.LightGreen, Color.LightSkyBlue };

            movingCards = new List<Card>();
            isDealing = false;
            isRetrying = false;
        }

        private void DealCards()
        {
            List<int> cardNumbers = new List<int>();
            for (int x = 0; x < 7; x++)
            {
                List<Card> stack = stacks[x];
                int count = stack.Count;
                int hidden = stack.Sum(e => e.isVisible ? 0 : 1);

                for (int i = count - 1; i >= hidden; i--)
                    cardNumbers.Add(stack[i].number);
                for (int i = 0; i < hidden; i++)
                    cardNumbers.Add(stack[i].number);
            }

            for (int i = 0; i < 7; i++)
                stacks[i].Clear();

            for (int c = 0; c < cardNumbers.Count; c++)
            {
                int cardNumber = cardNumbers[c];
                bool isVisible = (c % 7) <= (c / 7);
                stacks[c % 7].Add(cards[cardNumber]);
                cards[cardNumber].isVisible = isVisible;
            }

            for (int i = 0; i < 7; i++)
            {
                int count = stacks[i].Count;
                if (count > 0)
                    stacks[i][count - 1].isVisible = true;
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

                    if (xi >= 0 && xi < 7 && yi >= 0 && yi < stacks[xi].Count && stacks[xi][yi].isVisible)
                    {
                        Card selectedCard = stacks[xi][yi];

                        if (yi == stacks[xi].Count - 1 && canBeRecovered(selectedCard.number))
                        {
                            Vector2 position = new Vector2(xi * (cardWidth + stackOffset.X) + stackOffset.X, yi * (4 * 6) + stackOffset.Y);
                            Vector2 target = new Vector2(8 * (cardWidth + stackOffset.X) + 2 * stackOffset.X, (selectedCard.number / 13) * (cardWidth * 2 + stackOffset.Y)) + stackOffset;
                            float distance = (target - position).Length();
                            selectedCard.position = position;
                            selectedCard.shift = (target - position) / distance * 20;
                            selectedCard.steps = (int)distance / 20;
                            movingCards.Add(selectedCard);

                            recoveredCards[stacks[xi][yi].number / 13]++;
                            stacks[xi].RemoveAt(yi);
                            int count = stacks[xi].Count;
                            if (count > 0)
                                stacks[xi][count - 1].isVisible = true;
                        }
                        else
                        {
                            while (yi > 0 && stacks[xi][yi - 1].number % 13 > 0 && stacks[xi][yi - 1].number == stacks[xi][yi].number + 1)
                                yi--;

                            draggedStack = stacks[xi].TakeLast(stacks[xi].Count - yi).ToList();
                            draggedStackOrigin = xi;
                            dragOffset = new Vector2(x - xi * (cardWidth + stackOffset.X) - stackOffset.X, y - yi * (4 * 6) - stackOffset.Y);
                            stacks[xi].RemoveRange(yi, stacks[xi].Count - yi);
                        }
                    }
                    else if (draggedStack.Count == 0)
                    {
                        if (x >= dealButtonLocation.X && x < dealButtonLocation.X + cardWidth 
                         && y >= dealButtonLocation.Y && y < dealButtonLocation.Y + cardWidth * 11 / 32)
                        {
                            isDealing = true;
                            DealCards();
                        }
                        else if (x >= resetButtonLocation.X && x < resetButtonLocation.X + cardWidth 
                              && y >= resetButtonLocation.Y && y < resetButtonLocation.Y + cardWidth * 11 / 32)
                        {
                            isRetrying = true;
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
                    return draggedStack[0].number % 13 == 12;
                }
                else
                {
                    int target = stacks[index].Last().number;
                    int dragged = draggedStack[0].number;
                    return stacks[index].Last().isVisible && target / 13 == dragged / 13 && target == dragged + 1;
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
                            stacks[draggedStackOrigin][count - 1].isVisible = true;
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

            if (true || (!isDealing && !isRetrying))
                ProcessMouse();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();
            DrawButtons();
            DrawRecovered();
            if (true || (!isDealing && !isRetrying))
            {
                DrawCards();
                DrawMovingCards();
            }
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawCards()
        {
            float scaler = cardWidth / hiddenCardTexture.Width;

            for (int x = 0; x < stacks.Length; x++)
            {
                List<Card> stack = stacks[x];
                for (int y = 0; y < stack.Count; y++)
                {
                    Vector2 position = new Vector2(x * (cardWidth + stackOffset.X), y * 4 * 6) + stackOffset;

                    if (stack[y].isVisible)
                    {
                        Rectangle source = new Rectangle((stack[y].number % 13) * 30, (stack[y].number / 13) * 60, 30, 60);
                        _spriteBatch.Draw(cardTextures, position, source, cardColors[stack[y].number / 13], 0, Vector2.Zero, scaler, SpriteEffects.None, 0);
                    }
                    else
                        _spriteBatch.Draw(hiddenCardTexture, position, null, Color.White, 0, Vector2.Zero, scaler, SpriteEffects.None, 0);
                }
            }

            MouseState mouseState = Mouse.GetState();
            for (int y = 0; y < draggedStack.Count; y++)
            {
                Vector2 position = new Vector2(mouseState.X, mouseState.Y + y * 4 * 6) - dragOffset;
                Rectangle source = new Rectangle((draggedStack[y].number % 13) * 30, (draggedStack[y].number / 13) * 60, 30, 60);
                _spriteBatch.Draw(cardTextures, position, source, cardColors[draggedStack[y].number / 13], 0, Vector2.Zero, scaler, SpriteEffects.None, 0);
            }
        }

        private void DrawButtons()
        {
            float scaler = cardWidth / buttonTextures.Width;

            Rectangle sourceDeal = new Rectangle(0, 0, 32, 11);
            Rectangle sourceReset = new Rectangle(0, 11, 32, 11);

            _spriteBatch.Draw(buttonTextures, dealButtonLocation, sourceDeal, Color.White, 0, Vector2.Zero, scaler, SpriteEffects.None, 0);
            _spriteBatch.Draw(buttonTextures, resetButtonLocation, sourceReset, Color.White, 0, Vector2.Zero, scaler, SpriteEffects.None, 0);

            scaler = cardWidth / emptyTexture.Width;
            Vector2 position = new Vector2(0, resetButtonLocation.Y - dealButtonLocation.Y) + resetButtonLocation;
            _spriteBatch.Draw(emptyTexture, position, null, Color.White, 0, Vector2.Zero, scaler, SpriteEffects.None, 0);
        }

        private void DrawRecovered()
        {
            for (int i = 0; i < 4; i++)
            {
                int cardNumber = recoveredArrived[i];
                Vector2 position = new Vector2(8 * (cardWidth + stackOffset.X) + 2 * stackOffset.X, i * (cardWidth * 2 + stackOffset.Y)) + stackOffset;

                if (cardNumber >= 0)
                {
                    float scaler = cardWidth / hiddenCardTexture.Width;
                    Rectangle source = new Rectangle(cardNumber * 30, i * 60, 30, 60);
                    _spriteBatch.Draw(cardTextures, position, source, cardColors[i], 0, Vector2.Zero, scaler, SpriteEffects.None, 0);
                }
                else
                {
                    float scaler = cardWidth / emptyTexture.Width;
                    _spriteBatch.Draw(emptyTexture, position, null, Color.White, 0, Vector2.Zero, scaler, SpriteEffects.None, 0);
                }
            }
        }

        private void DrawMovingCards()
        {
            for (int i = movingCards.Count - 1; i >= 0; i--)
            {
                Card card = movingCards[i];

                float scaler = cardWidth / hiddenCardTexture.Width;
                Rectangle source = new Rectangle((card.number % 13) * 30, (card.number / 13) * 60, 30, 60);
                _spriteBatch.Draw(cardTextures, card.position, source, cardColors[card.number / 13], 0, Vector2.Zero, scaler, SpriteEffects.None, 0);

                if (card.steps > 0)
                {
                    card.steps--;
                    card.position += card.shift;
                }
                    
                else
                {
                    movingCards.RemoveAt(i);
                    int country = card.number / 13;
                    int max = card.number % 13;
                    if (max < recoveredArrived[country])
                        max = recoveredArrived[country];
                    recoveredArrived[country] = max;
                }
            }
        }
    }
}