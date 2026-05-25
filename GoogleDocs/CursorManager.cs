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
    private static Vector2 Position = new(0f,0f);
    private static MainWindow window;
    private static TextBlock? mainText = null;
    private static TextLayout textlayout;
    private static Vector2 LastCursorPosition = new(0f,0f);
    private static SaveKeys SaveKeys;

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
        PrintDebugMenu($"dt: {dt} x: {x} y: {y} ");
       // window.PrintLineDebugMenu($"x: {x} y: {y}");
     /*   x = (float)Math.Pow(x, 2f) * 0.2f * (x < 0 ? -1f : 0f) + (x > 0 ? 5f : 0f) - (x < 0 ? 5f : 0f);
        y = (float)Math.Pow(y, 2f) * 0.2f * (y < 0 ? -1f : 0f) + (y > 0 ? 5f : 0f) - (y < 0 ? 5f : 0f);*/
     x = ((float)Math.Pow(dt, 2f) * 0.3f) * x;
     y = ((float)Math.Pow(dt, 2f) * 0.3f) * y;
        Acceleration = new Vector2(x,y);
        PrintDebugMenu($"Acceleration: {Acceleration} ");
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

        PrintDebugMenu($"Length: {length} ");
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
        PrintDebugMenu($"Moved to next line: {tmp} ");
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
    public static Vector2 GetOffsetFromCharacter()
    {
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

        PrintDebugMenu($"Length: {length} ");
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
        PrintDebugMenu($"Moved to next line: {tmp} ");
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
         var box = new Avalonia.Rect();
        try
        {
        box = textlayout.HitTestTextPosition(charcnt);
        }
        catch(Exception ex)
        {
            PrintDebugMenu($"[WARNING] Textlayout is too small: {ex.Message}");
            return LastCursorPosition;
        }
        PrintLineDebugMenu($"Hit test position: {box.X}, {box.Y}");
        for (int i = 0; i < textlayout.TextLines.Count; i++)
        {
            var line = textlayout.TextLines[i];
            PrintDebugMenu($"Line {i}: {line.Length} chars");
        }
        PrintDebugMenu("\n");
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
        Position.X = Math.Clamp(Position.X, 0, int.MaxValue);
        Position.Y = Math.Clamp(Position.Y, 0, int.MaxValue);
     /*   Position.X = (int)Position.X;
        Position.Y = (int)Position.Y;*/
 PrintLineDebugMenu($"Delta: {delta} Position: {Position}  ");
        var cursorPosition = GetOffsetFromCharacter();
        Dispatcher.UIThread.InvokeAsync(() =>
        {
          //  window.PrintLineDebugMenu($"Position: {Position.X} {Position.Y}");
            window.SetCursorOffsets(cursorPosition.X, cursorPosition.Y);
        });
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
                MoveCursor(new Vector2(0,-1));
                break;
            case Move.Down:
                keystate.down = true;
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
        if (SaveKeys.log)
        {
            Dispatcher.UIThread.InvokeAsync(() => { window.PrintLineDebugMenu(text); });
        }
    }
    public static void PrintDebugMenu(string text)
    {
        if (SaveKeys.log)
        {
            Dispatcher.UIThread.InvokeAsync(() => { window.PrintDebugMenu(text); });
        }
    }
}