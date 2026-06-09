using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GoogleDocsInstaller;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Disable(PathSelectWindow);
        Disable(ModeSelectWindow);
        Disable(ConfirmWindow);
        Disable(ProgressWindow);
        Disable(FinishWindow);
    }

    private async void StartSetup(object? sender, RoutedEventArgs e)
    {
        AnimateOut(StartingWindow,PathSelectWindow);
        foreach (var radio in PathSelectWindow.Children)
        {
            if (radio is RadioButton rb)
            {
                rb.IsEnabled = Directory.Exists(rb.Content.ToString());
            }
        }
    }

    private async void Disable(InputElement item)
    {
        item.IsVisible = false;
        item.IsEnabled = false;
        item.Opacity = 0;
    }

    private async void AnimateOut(InputElement itemout, InputElement itemin)
    {
        itemin.IsEnabled = true;
        itemin.IsVisible = true;
        itemin.Opacity = 0;
        Anim(itemout);
        await Task.Delay(900);
        itemin.Opacity = 1;
    }


    private void NextPath(object? sender, RoutedEventArgs e)
    {
    AnimateOut(PathSelectWindow,ModeSelectWindow);
    }
    private void NextMode(object? sender, RoutedEventArgs e)
    {
        string drive = "";
        string mode = "";
        foreach (var radio in PathSelectWindow.Children)
        {
            if (radio is RadioButton rb)
            {
                if (rb.IsChecked == true)
                {
                    drive = rb.Content.ToString();
                }
            }
        }

        foreach (var radio in ModeSelectWindow.Children)
        {
            if (radio is RadioButton rb)
            {
                if (rb.IsChecked == true)
                {
                    mode = rb.Content.ToString();
                }
            }
        }
    AnimateOut(ModeSelectWindow,ConfirmWindow);
    DriveConfirm.Text = $"Drive: {drive}";
    ModeConfirm.Text = $"Mode: {mode}";

    }

    private async void Confirm(object? sender, RoutedEventArgs e)
    {
        AnimateOut(ConfirmWindow, ProgressWindow);
        var info = new ProcessStartInfo("cmd.exe", "/c start pwsh -ExecutionPolicy Bypass -File ./Install.ps1");
        var process = Process.Start(info);
        ProgressBar.Value = 5;
        while (!process.HasExited)
        {
            if (process.StandardOutput.Peek() != -1)
            {
                string s = "";
                s += process.StandardOutput.Read();
                while (process.StandardOutput.Peek() == -1)
                {
                    await Task.Delay(10);
                }

                s += process.StandardOutput.Read();
                int progress = int.Parse(s);
                ProgressBar.Value = progress;
            }

            await Task.Delay(50);
        }
    }

    private async void Quit(object? sender, RoutedEventArgs e)
    {

            if (Application.Current?.ApplicationLifetime is IControlledApplicationLifetime lifetime)
            {
                lifetime.Shutdown();
            }


    }

    private void AddOffset(Visual item, Vector3 offset)
    {
        var vis = ElementComposition.GetElementVisual(item);
        var baseoffset = new Vector3((float)vis.Offset.X, (float)vis.Offset.Y, (float)vis.Offset.Z);
        vis.Offset = baseoffset + offset;
    }

    private void Anim(Visual item)
    {
        var vis = ElementComposition.GetElementVisual(item);
        var comp = vis.Compositor;
        var anim = comp.CreateVector3KeyFrameAnimation();
        anim.Duration = TimeSpan.FromMilliseconds(900);
        var prevoffset3d = vis.Offset;
        var prevoffset = new Vector3((float)prevoffset3d.X, (float)prevoffset3d.Y, (float)prevoffset3d.Z);
        anim.InsertKeyFrame(0f,prevoffset,new CubicEaseInOut());
        anim.InsertKeyFrame(1f,prevoffset + new Vector3(-1000,0,0),new CubicEaseInOut());
        vis.StartAnimation("Offset",anim);
    }
}