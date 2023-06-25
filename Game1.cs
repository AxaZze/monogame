using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

[Serializable]
public class GameStateData
{
    public float PaddlePositionX { get; set; }
    public float PaddlePositionY { get; set; }

    public float BallPositionX { get; set; }
    public float BallPositionY { get; set; }

    public float BallVelocityX { get; set; }
    public float BallVelocityY { get; set; }

    public bool[,] BlocksHit { get; set; }
    public int Score { get; set; }
}



namespace game
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        // Game variables
        Texture2D paddleTexture;
        Texture2D ballTexture;
        Texture2D blockTexture;

        Vector2 paddlePosition;
        Vector2 ballPosition;

        Vector2 ballVelocity;
        Rectangle[,] blocks;

        const int BlockRows = 5;
        const int BlockColumns = 10;
        bool[,] blocksHit;

        const int scoreThreshold = 1000;
        int score = 0;
        SpriteFont font;

        List<int> highScores = new List<int>();
        string highScoreFile = "highscores.txt";

        enum GameState { SelectionScreen, StartScreen, PlayScreen, GameOverScreen, HighScoreScreen };
        GameState currentGameState = GameState.StartScreen;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();

            paddlePosition = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height - 50);
            ballPosition = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);

            ballVelocity = new Vector2(2f, 2f);

            blocks = new Rectangle[BlockRows, BlockColumns];
            blocksHit = new bool[BlockRows, BlockColumns];
            int blockWidth = GraphicsDevice.Viewport.Width / BlockColumns;
            int blockHeight = 30;

            for (int i = 0; i < BlockRows; i++)
            {
                for (int j = 0; j < BlockColumns; j++)
                {
                    blocks[i, j] = new Rectangle(j * blockWidth, (i * blockHeight) + 50, blockWidth, blockHeight);  // Décaler vers le bas
                    blocksHit[i, j] = false;
                }
            }
            if (File.Exists(highScoreFile))
            {
                string[] scores = File.ReadAllLines(highScoreFile);
                highScores = scores.Select(int.Parse).ToList();
            }
            else
            {
                File.Create(highScoreFile).Close();
            }
            score = 0;
            currentGameState = GameState.SelectionScreen; // Set the current game state to SelectionScreen
        }


        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load textures
            paddleTexture = Content.Load<Texture2D>("Paddle");
            ballTexture = Content.Load<Texture2D>("Ball");
            blockTexture = Content.Load<Texture2D>("Block");
            font = Content.Load<SpriteFont>("font");
        }

        private void SaveGameState(string filename)
        {
            var data = new GameStateData
            {
                PaddlePositionX = paddlePosition.X,
                PaddlePositionY = paddlePosition.Y,

                BallPositionX = ballPosition.X,
                BallPositionY = ballPosition.Y,

                BallVelocityX = ballVelocity.X,
                BallVelocityY = ballVelocity.Y,

                BlocksHit = blocksHit,
                Score = score
            };
            using (Stream stream = File.Open(filename, FileMode.Create))
            {
                BinaryFormatter bin = new BinaryFormatter();
                bin.Serialize(stream, data);
            }
        }

        private void LoadGameState(string filename)
        {
            using (Stream stream = File.Open(filename, FileMode.Open))
            {
                BinaryFormatter bin = new BinaryFormatter();
                var data = (GameStateData)bin.Deserialize(stream);

                paddlePosition = new Vector2(data.PaddlePositionX, data.PaddlePositionY);
                ballPosition = new Vector2(data.BallPositionX, data.BallPositionY);
                ballVelocity = new Vector2(data.BallVelocityX, data.BallVelocityY);

                blocksHit = data.BlocksHit;
                score = data.Score;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            switch (currentGameState)
            {
                case GameState.SelectionScreen:
                    if (Keyboard.GetState().IsKeyDown(Keys.D1)) // Press "1" to play a new game
                    {
                        Initialize();
                        currentGameState = GameState.PlayScreen;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.D2)) // Press "2" to load the saved game
                    {
                        LoadGameState("savedgame.dat");
                        currentGameState = GameState.PlayScreen;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.D3)) // Press "3" to see the high scores
                    {
                        currentGameState = GameState.HighScoreScreen;
                    }
                    break;

                case GameState.PlayScreen:
                    // Input handling
                    if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                        Exit();

                    var keyboardState = Keyboard.GetState();

                    if (keyboardState.IsKeyDown(Keys.Left))
                    {
                        paddlePosition.X -= 5f;
                        if (paddlePosition.X < 0)
                            paddlePosition.X = 0;
                    }

                    if (keyboardState.IsKeyDown(Keys.Right))
                    {
                        paddlePosition.X += 5f;
                        if (paddlePosition.X + paddleTexture.Width > GraphicsDevice.Viewport.Width)
                            paddlePosition.X = GraphicsDevice.Viewport.Width - paddleTexture.Width;
                    }

                    ballPosition += ballVelocity;

                    if (ballPosition.X < 0 || ballPosition.X + ballTexture.Width > GraphicsDevice.Viewport.Width)
                    {
                        ballVelocity.X *= -1;
                    }

                    if (ballPosition.Y < 0)
                    {
                        ballVelocity.Y *= -1;
                    }

                    if (ballPosition.Y + ballTexture.Height > paddlePosition.Y &&
                        ballPosition.X > paddlePosition.X &&
                        ballPosition.X < paddlePosition.X + paddleTexture.Width)
                    {
                        ballVelocity.Y *= -1;

                        // Check if the ball has hit the side of the paddle
                        if (ballPosition.X < paddlePosition.X || ballPosition.X > paddlePosition.X + paddleTexture.Width)
                        {
                            ballVelocity.X *= -1;
                        }
                    }

                    // Block collision handling
                    for (int i = 0; i < BlockRows; i++)
                    {
                        for (int j = 0; j < BlockColumns; j++)
                        {
                            if (!blocksHit[i, j] && blocks[i, j].Contains((int)ballPosition.X, (int)ballPosition.Y))
                            {
                                blocksHit[i, j] = true;
                                score += 100; // Score increases when a block is hit
                                if (score % scoreThreshold == 0) // Ball speed increases every 1000 points
                                {
                                    ballVelocity *= 1.1f;
                                }
                                ballVelocity.Y *= -1;
                                break;
                            }
                        }
                    }

                    if (ballPosition.Y > GraphicsDevice.Viewport.Height)
                    {
                        currentGameState = GameState.GameOverScreen;
                        highScores.Add(score);
                        highScores = highScores.OrderByDescending(x => x).Take(10).ToList();
                        File.WriteAllLines(highScoreFile, highScores.Select(x => x.ToString()));
                    }

                    if (Keyboard.GetState().IsKeyDown(Keys.S))
                    {
                        SaveGameState("savedgame.dat");
                    }

                    break;

                case GameState.HighScoreScreen:
                    if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        currentGameState = GameState.SelectionScreen;
                    }
                    break;

                case GameState.GameOverScreen:
                    if (Keyboard.GetState().IsKeyDown(Keys.Space))
                    {
                        Initialize();
                        currentGameState = GameState.PlayScreen;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        currentGameState = GameState.SelectionScreen;
                    }
                    break;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            string menuText;
            Vector2 textSize;
            float x, y;
            Vector2 textPosition;

            switch (currentGameState)
            {
                case GameState.SelectionScreen:
                    menuText = "Appuyer sur 1 pour Commencer une nouvelle Partie\nAppuyer sur 2 pour Charger une partie\nAppuyer sur 3 pour voir les Highscores";
                    textSize = font.MeasureString(menuText);
                    x = (GraphicsDevice.Viewport.Width - textSize.X) / 2;
                    y = (GraphicsDevice.Viewport.Height - textSize.Y) / 2;
                    textPosition = new Vector2(x, y);

                    spriteBatch.DrawString(font, menuText, textPosition, Color.Black);
                    break;

                case GameState.HighScoreScreen:
                    spriteBatch.DrawString(font, "High Scores", new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2 - 60), Color.Black);
                    for (int i = 0; i < highScores.Count; i++)
                    {
                        spriteBatch.DrawString(font, $"{i + 1}. {highScores[i]}", new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2 + i * 20), Color.Black);
                    }
                    spriteBatch.DrawString(font, "Press Enter to Return", new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2 + highScores.Count * 20), Color.Black);
                    break;

                case GameState.StartScreen:
                    spriteBatch.DrawString(font, "Press SPACE to Start", new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2), Color.Black);
                    break;

                case GameState.PlayScreen:
                    spriteBatch.Draw(paddleTexture, paddlePosition, Color.White);
                    spriteBatch.Draw(ballTexture, ballPosition, Color.White);
                    for (int i = 0; i < BlockRows; i++)
                    {
                        for (int j = 0; j < BlockColumns; j++)
                        {
                            if (!blocksHit[i, j])
                            {
                                spriteBatch.Draw(blockTexture, new Vector2(blocks[i, j].X, blocks[i, j].Y), Color.White);
                            }
                        }
                    }

                    // Calculer la taille du texte de score pour le centrer
                    Vector2 scoreSize = font.MeasureString($"Score: {score}");
                    float scoreX = (GraphicsDevice.Viewport.Width - scoreSize.X) / 2;

                    spriteBatch.DrawString(font, $"Score: {score}", new Vector2(scoreX, 10), Color.Black);
                    break;

                case GameState.GameOverScreen:
                    menuText = "Game Over\nAppuyer sur SPACE pour recommencer\nAppuyer sur ENTREE pour retourner au Menu";
                    textSize = font.MeasureString(menuText);
                    x = (GraphicsDevice.Viewport.Width - textSize.X) / 2;
                    y = (GraphicsDevice.Viewport.Height - textSize.Y) / 2;
                    textPosition = new Vector2(x, y);

                    spriteBatch.DrawString(font, menuText, textPosition, Color.Black);
                    break;
        }

        spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
