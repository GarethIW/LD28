using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TiledLib;

namespace LD28
{
    public class Speechbubble
    {
        public Vector2 Position;
        public bool Visible;
        public string Text;

        Texture2D texBG;
        SpriteFont font;

        public Speechbubble(ContentManager content)
        {
            texBG = content.Load<Texture2D>("speechbubble");
            font = content.Load<SpriteFont>("speechfont");
            Visible = false;
        }

        public void Show(Vector2 pos, string text)
        {
            Visible = true;
            Text = text;
            Position = pos;
        }

        public void Draw(SpriteBatch sb, Camera gameCamera)
        {
            if (Visible)
            {
                sb.Begin(SpriteSortMode.Deferred, null, null, null, null, null, gameCamera.CameraMatrix);
                sb.Draw(texBG, Position, null, Color.White, 0f, new Vector2(62, 280), 1f, SpriteEffects.None, 1);
                sb.DrawString(font, WrapText(Text, 315), Position + new Vector2(-10, -175), Color.Black);
                sb.End();
            }
        }

        private string WrapText(string text, float maxLineWidth)
        {
            string[] words = text.Split(' ');

            StringBuilder sb = new StringBuilder();

            float lineWidth = 0f;

            float spaceWidth = font.MeasureString(" ").X;

            foreach (string word in words)
            {
                Vector2 size = font.MeasureString(word);

                if (lineWidth + size.X < maxLineWidth)
                {
                    sb.Append(word + " ");
                    lineWidth += size.X + spaceWidth;
                }
                else
                {
                    sb.Append("\n" + word + " ");
                    lineWidth = size.X + spaceWidth;
                }
            }
            return sb.ToString();
        }
    }
}
