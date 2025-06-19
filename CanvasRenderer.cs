using Godot;
using System;
using System.Collections.Generic;

public partial class CanvasRenderer : TextureRect
{
    private Image image;
    private ImageTexture texture;
    private bool showGrid = true;
    private Color gridColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
    private int size;

    public void Initialize(int canvasSize)
    {
        size = canvasSize;
        image = Godot.Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        image.Fill(Colors.White);
        texture = ImageTexture.CreateFromImage(image);
        Texture = texture;
    }

    public int GetSize() => size;

    public void ClearCanvas()
    {
        image.Fill(Colors.White);
        UpdateTexture();
    }

   public Vector2I DrawLine(Vector2I start, Vector2I end, int width, Color color)
{
    BresenhamLine(start, end, width, color);
    UpdateTexture();
    return end; // Devuelve el punto final
}

    public void DrawCircle(Vector2I center, int radius, int width, Color color)
    {
        MidpointCircle(center, radius, width, color);
        UpdateTexture();
    }

    public void DrawRectangle(Vector2I center, int width, int height, int strokeWidth, Color color)
    {
        int halfWidth = width / 2;
        int halfHeight = height / 2;
        
        for (int x = center.X - halfWidth; x <= center.X + halfWidth; x++)
        {
            for (int w = 0; w < strokeWidth; w++)
            {
                SetPixelSafe(x, center.Y - halfHeight - w, color);
                SetPixelSafe(x, center.Y + halfHeight + w, color);
            }
        }
        
        for (int y = center.Y - halfHeight; y <= center.Y + halfHeight; y++)
        {
            for (int w = 0; w < strokeWidth; w++)
            {
                SetPixelSafe(center.X - halfWidth - w, y, color);
                SetPixelSafe(center.X + halfWidth + w, y, color);
            }
        }
        UpdateTexture();
    }

    public void Fill(Vector2I startPosition, Color fillColor)
    {
        if (!IsPositionValid(startPosition.X, startPosition.Y)) return;
        FloodFill(startPosition, fillColor);
        UpdateTexture();
    }

    public bool IsPositionValid(int x, int y) => x >= 0 && y >= 0 && x < size && y < size;

    public int GetColorCount(Color color, int x1, int y1, int x2, int y2)
    {
        int count = 0;
        Vector2I min = new Vector2I(Math.Min(x1, x2), Math.Min(y1, y2));
        Vector2I max = new Vector2I(Math.Max(x1, x2), Math.Max(y1, y2));
        
        for (int x = min.X; x <= max.X; x++)
        {
            for (int y = min.Y; y <= max.Y; y++)
            {
                if (IsPositionValid(x, y) && image.GetPixel(x, y) == color)
                    count++;
            }
        }
        return count;
    }

    public bool CheckColor(Vector2I pos, Color color) => 
        IsPositionValid(pos.X, pos.Y) && image.GetPixel(pos.X, pos.Y) == color;

    public void ToggleGrid(bool visible)
    {
        showGrid = visible;
        QueueRedraw();
    }

    public override void _Draw()
    {
        base._Draw();
        
        if (showGrid && size > 0)
        {
            Vector2 cellSize = Size / size;
            
            for (int i = 0; i <= size; i++)
            {
                // Líneas verticales
                DrawLine(
                    new Vector2(i * cellSize.X, 0),
                    new Vector2(i * cellSize.X, Size.Y),
                    gridColor
                );
                
                // Líneas horizontales
                DrawLine(
                    new Vector2(0, i * cellSize.Y),
                    new Vector2(Size.X, i * cellSize.Y),
                    gridColor
                );
            }
        }
    }
    private void BresenhamLine(Vector2I start, Vector2I end, int width, Color color)
    {
        int dx = Math.Abs(end.X - start.X);
        int dy = Math.Abs(end.Y - start.Y);
        int sx = start.X < end.X ? 1 : -1;
        int sy = start.Y < end.Y ? 1 : -1;
        int err = dx - dy;
        int halfWidth = width / 2;

        while (true)
        {
            DrawThickPoint(start, halfWidth, color);
            
            if (start.X == end.X && start.Y == end.Y) break;
            
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                start.X += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                start.Y += sy;
            }
        }
    }

    private void MidpointCircle(Vector2I center, int radius, int width, Color color)
    {
        int x = radius;
        int y = 0;
        int decisionOver2 = 1 - x;
        int halfWidth = (int)Math.Ceiling(width / 2.0);

        while (x >= y)
        {
            PlotCirclePoints(center, x, y, halfWidth, color);
            y++;
            if (decisionOver2 <= 0)
            {
                decisionOver2 += 2 * y + 1;
            }
            else
            {
                x--;
                decisionOver2 += 2 * (y - x) + 1;
            }
        }
    }

    private void PlotCirclePoints(Vector2I center, int x, int y, int halfWidth, Color color)
    {
        DrawThickPoint(new Vector2I(center.X + x, center.Y + y), halfWidth, color);
        DrawThickPoint(new Vector2I(center.X - x, center.Y + y), halfWidth, color);
        DrawThickPoint(new Vector2I(center.X + x, center.Y - y), halfWidth, color);
        DrawThickPoint(new Vector2I(center.X - x, center.Y - y), halfWidth, color);
        DrawThickPoint(new Vector2I(center.X + y, center.Y + x), halfWidth, color);
        DrawThickPoint(new Vector2I(center.X - y, center.Y + x), halfWidth, color);
        DrawThickPoint(new Vector2I(center.X + y, center.Y - x), halfWidth, color);
        DrawThickPoint(new Vector2I(center.X - y, center.Y - x), halfWidth, color);
    }

    private void FloodFill(Vector2I start, Color newColor)
    {
        if (!IsPositionValid(start.X, start.Y)) return;
        
        Color targetColor = image.GetPixel(start.X, start.Y);
        if (targetColor == newColor) return;

        Queue<Vector2I> queue = new();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2I point = queue.Dequeue();
            if (!IsPositionValid(point.X, point.Y) || image.GetPixel(point.X, point.Y) != targetColor)
                continue;

            image.SetPixel(point.X, point.Y, newColor);

            queue.Enqueue(new Vector2I(point.X + 1, point.Y));
            queue.Enqueue(new Vector2I(point.X - 1, point.Y));
            queue.Enqueue(new Vector2I(point.X, point.Y + 1));
            queue.Enqueue(new Vector2I(point.X, point.Y - 1));
        }
    }

    private void DrawThickPoint(Vector2I point, int radius, Color color)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2I current = new Vector2I(point.X + x, point.Y + y);
                if (IsPositionValid(current.X, current.Y))
                {
                    image.SetPixel(current.X, current.Y, color);
                }
            }
        }
    }

    private void SetPixelSafe(int x, int y, Color color)
    {
        if (IsPositionValid(x, y))
        {
            image.SetPixel(x, y, color);
        }
    }

    private void UpdateTexture()
    {
        texture.Update(image);
        QueueRedraw();
    }
}