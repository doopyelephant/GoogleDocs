using System;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;

namespace GoogleDocs;

public enum Move
{
    Up,Down,Left,Right,
}

public struct KeyState
{
    public bool left;
    public bool right;
    public bool up;
    public bool down;

    public KeyState()
    {
        left = false;
        right = false;
        up = false;
        down = false;
    }
    public override string ToString()
    {
        return $"left: {left} right: {right} up: {up} down: {down}";
    }
}
public static class CursorManager
{
    private static KeyState keystate = new();
    private static Vector2 Acceleration = new(0f,0f);
    private static DateTime lastkey = DateTime.Now;
    private static DateTime lastupdate = DateTime.Now;
    public static Vector2 Position = new(0f,0f);
    private static MainWindow window;
    private static TextBlock? mainText = null;
    private static TextLayout textlayout;
    private static Vector2 LastCursorPosition = new(0f,0f);
    private static Vector2 LastCursorOffset = new(0f,0f);
    private static SaveKeys SaveKeys;
    private static bool verticalmove;

    public static void Init(MainWindow _window)
    {
        window = _window;
        SaveKeys = JsonParsing.GetSaveKeys();
    }

    public static void SetTextLayout(TextLayout _textlayout)
    {
        textlayout = _textlayout;
    }

    public static void UpdateCursor()
    {

        var dt = (float)(DateTime.Now - lastkey).TotalSeconds;
        var x = (keystate.left ? -1f : 0f) + (keystate.right ? 1f : 0f);
        var y = (keystate.up ? -1f : 0f) + (keystate.down ? 1f : 0f);
        PrintLineDebugMenu($"dt: {dt} x: {x} y: {y} ");
       // window.PrintLineDebugMenu($"x: {x} y: {y}");
     /*   x = (float)Math.Pow(x, 2f) * 0.2f * (x < 0 ? -1f : 0f) + (x > 0 ? 5f : 0f) - (x < 0 ? 5f : 0f);
        y = (float)Math.Pow(y, 2f) * 0.2f * (y < 0 ? -1f : 0f) + (y > 0 ? 5f : 0f) - (y < 0 ? 5f : 0f);*/
     x = ((float)Math.Pow(dt, 2f) * 0.3f) * x;
     y = ((float)Math.Pow(dt, 2f) * 0.3f) * y;
        Acceleration = new Vector2(x,y);
        PrintLineDebugMenu($"Acceleration: {Acceleration} ");
    }
    public static int GetCursorPosition()
    {
       UpdateCursor();
         var tmp = Position;
        if (mainText == null)
        {
            mainText = window.GetMainText();
        }

       // var textlayout = mainText.TextLayout;
        
        var length = 0;
        if(tmp.Y < textlayout.TextLines.Count)
        {
            length = textlayout.TextLines[(int)tmp.Y].Length;
        }

        PrintLineDebugMenu($"Length: {length} ");
        while (length < tmp.X)
        {
        tmp.X = 0;
        tmp.Y++;
        Position.X = 0;
        Position.Y++;
        length = 0;
        if(tmp.Y < textlayout.TextLines.Count)
        {
            length = textlayout.TextLines[(int)tmp.Y].Length;
        }
        PrintLineDebugMenu($"Moved to next line: {tmp} ");
        }
        int index = 0;
        int charcnt = 0;
        foreach (var line in textlayout.TextLines)
        {
            if (index == (int)tmp.Y)
            {
                charcnt += (int)tmp.X;
                break;
            }
            charcnt += line.Length + 1;

            index++;
        }
        return charcnt;
    }
    public static Vector2 GetOffsetFromCharacter(Vector2? position = null)
    {
        var tmp = Position;
        if (position != null)
        {
            tmp = position.Value;
        }
        if (mainText == null)
        {
            mainText = window.GetMainText();
        }

       // var textlayout = mainText.TextLayout;

        var length = 0;
        if(tmp.Y < textlayout.TextLines.Count)
        {
            length = textlayout.TextLines[(int)tmp.Y].Length;
        }

        PrintLineDebugMenu($"Length: {length} ");

        if (verticalmove && position == null)
        {

            int iter = (int)Math.Ceiling(Math.Log2(length));
            int beforechars = 0;
            for (int i = 0; i < tmp.Y; i++)
            {
                beforechars += textlayout.TextLines[i].Length + textlayout.TextLines[i].NewLineLength;
            }
            Vector2 httpvec = new Vector2();
            int startcut = 0;
            int endcut = length;
            int mid = 0;
            //Binary search for the correct position
            Console.WriteLine("Target: " + LastCursorOffset.X);
            Console.WriteLine("Current: " + textlayout.HitTestTextPosition(beforechars + (int)tmp.X).X);
            for (int i = iter; i > 0; i--)
            {
                Console.WriteLine("Iter: " + i);
                Console.WriteLine("Start: " + startcut + " End: " + endcut);

                mid = startcut + (endcut - startcut) / 2;
                Console.WriteLine("Mid: " + mid);
                var http = textlayout.HitTestTextPosition(beforechars + mid);
                httpvec = new Vector2((float)http.X, (float)http.Y);
                Console.WriteLine("Http Vec: " + httpvec.X);

                if (LastCursorOffset.X < httpvec.X)
                {
                    endcut = (int)mid;
                }

                else if (LastCursorOffset.X > httpvec.X)
                {
                    startcut = (int)mid;
                }
                else

                {
                    break;
                }
            }
            tmp.X = mid;
            Position.X = mid;
        }
        if (verticalmove && length < tmp.X && position == null)
        {
            tmp.X = length;
            Position.X = length;
        }
        while (length < tmp.X)
        {
        tmp.X = 0;
        tmp.Y++;
        Position.X = 0;
        Position.Y++;
        length = 0;
        if(tmp.Y < textlayout.TextLines.Count)
        {
            length = textlayout.TextLines[(int)tmp.Y].Length;
        }
        PrintLineDebugMenu($"Moved to next line: {tmp} ");
        }
        while (tmp.X < 0)
        {
            if (tmp.Y > 0)
            {
                tmp.Y--;
                Position.Y--;
                if (tmp.Y < textlayout.TextLines.Count)
                {
                    Console.WriteLine("Y: " + tmp.Y);
                    length = textlayout.TextLines[(int)tmp.Y].Length;
                }

                tmp.X = length;
                Position.X = length;
                PrintLineDebugMenu($"Moved to previous line: {tmp} ");
            }
            else
            {
                tmp.X = 0;
                tmp.Y = 0;
                Position.X = 0;
                Position.Y = 0;
                if (tmp.Y < textlayout.TextLines.Count)
                {
                    length = textlayout.TextLines[(int)tmp.Y].Length;
                }
                PrintLineDebugMenu($"Moved to start: {tmp} ");
            }
        }

        if (tmp.Y >= textlayout.TextLines.Count - 1 && tmp.X > length)
        {
            tmp.Y = textlayout.TextLines.Count - 1;
            Position.Y = textlayout.TextLines.Count - 1;
            if (tmp.Y < textlayout.TextLines.Count)
            {
                Console.WriteLine("Y: " + tmp.Y);
                length = textlayout.TextLines[(int)tmp.Y].Length;
            }
            tmp.X = length;
            Position.X = length;

        }
        int index = 0;
        int charcnt = 0;
        foreach (var line in textlayout.TextLines)
        {
            if (index == (int)tmp.Y)
            {
                charcnt += (int)tmp.X;
                break;
            }
            charcnt += line.Length + line.NewLineLength;

            index++;
        }

         var box = new Avalonia.Rect();
        try
        {
        box = textlayout.HitTestTextPosition(charcnt);
        }
        catch(Exception ex)
        {
            PrintLineDebugMenu($"[WARNING] Textlayout is too small: {ex.Message}");
            return LastCursorPosition;
        }
        Console.WriteLine($"Hit test position: {box.X}, {box.Y}");

        /*    for (int i = 0; i < textlayout.TextLines.Count; i++)
        {
            var line = textlayout.TextLines[i];
            PrintLineDebugMenu($"Line {i}: {line.Length} chars");
        }*/
        PrintLineDebugMenu("\n");
        LastCursorOffset = new Vector2((float)box.X, (float)box.Y);
        if (position == null)
        {
            verticalmove = false;
        }

        return new Vector2((float)box.X, (float)box.Y);
    }

    public static void MoveCursor(Vector2 offset)
    {
        Position += offset;
    }

    public static void MoveCursor()
    {
        MoveCursor(new Vector2(1,0));
    }

    public static void UpdateCursorPosition()
    {
        var dt = (float)(DateTime.Now - lastupdate).TotalSeconds;
        var delta = Acceleration * dt;
       
        Position += delta;
      //  Position.X = Math.Clamp(Position.X, 0, int.MaxValue);
        Position.Y = Math.Clamp(Position.Y, 0, int.MaxValue);
     /*   Position.X = (int)Position.X;
        Position.Y = (int)Position.Y;*/
        PrintLineDebugMenu($"Delta: {delta} Position: {Position}  ");
        verticalmove = (int)LastCursorPosition.Y != (int)Position.Y || verticalmove;
        var cursorPosition = GetOffsetFromCharacter();
        //Console.WriteLine("Verticalmove: " + verticalmove);
        Dispatcher.UIThread.InvokeAsync(() =>
        {
          //  window.PrintLineDebugMenu($"Position: {Position.X} {Position.Y}");
            window.SetCursorOffsets(cursorPosition.X, cursorPosition.Y);
        });
        LastCursorPosition = Position;
        lastupdate = DateTime.Now;
    }
    public static void KeyDown(Move move)
    {
        PrintLineDebugMenu(move.ToString());
        lastkey = DateTime.Now;
        switch (move)
        {
            case Move.Up:
                keystate.up = true;
                verticalmove = true;
                MoveCursor(new Vector2(0,-1));
                break;
            case Move.Down:
                keystate.down = true;
                verticalmove = true;
                MoveCursor(new Vector2(0,1));
                break;
            case Move.Left:
                keystate.left = true;
                MoveCursor(new Vector2(-1,0));
                break;
            case Move.Right:
                keystate.right = true;
                MoveCursor(new Vector2(1,0));
                break;
        }
        PrintLineDebugMenu(keystate.ToString());
    }

    public static void KeyUp(Move move)
    {
        lastkey = DateTime.Now;
        switch (move)
        {
            case Move.Up:
                keystate.up = false;
                break;
            case Move.Down:
                keystate.down = false;
                break;
            case Move.Left:
                keystate.left = false;
                break;
            case Move.Right:
                keystate.right = false;
                break;
        }
        PrintLineDebugMenu(keystate.ToString());
    }

    public static void PrintLineDebugMenu(string text)
    {
        if (SaveKeys.cursorupdatelog)
        {
            Dispatcher.UIThread.InvokeAsync(() => { window.PrintLineDebugMenu(text); });
        }
    }
    public static void PrintDebugMenu(string text)
    {
        if (SaveKeys.cursorupdatelog)
        {
            Dispatcher.UIThread.InvokeAsync(() => { window.PrintDebugMenu(text); });
        }
    }
}