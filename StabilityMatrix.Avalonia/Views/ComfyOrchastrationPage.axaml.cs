using System;
using System.IO;
using System.Linq;
using AsyncAwaitBestPractices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using StabilityMatrix.Avalonia.Controls;
using StabilityMatrix.Avalonia.Extensions;
using StabilityMatrix.Avalonia.Models;
using StabilityMatrix.Avalonia.ViewModels;
using StabilityMatrix.Avalonia.ViewModels.CheckpointManager;
using StabilityMatrix.Core.Attributes;
using StabilityMatrix.Core.Models.FileInterfaces;
using StabilityMatrix.Core.Services;

namespace StabilityMatrix.Avalonia.Views;

[Singleton]
public partial class ComfyOrchastrationPage : UserControlBase
{
    public ComfyOrchastrationPage()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var sourceDataContext = (e.Source as Control)?.DataContext as ComfyOrchastrationViewModel;
        var x = e.Data.GetFiles().First();
        sourceDataContext.FilePath = x.Path.ToString().Substring(8);

        if (File.Exists(sourceDataContext.FilePath))
        {
            sourceDataContext.PreviewImage = new Bitmap(sourceDataContext.FilePath);
        }
    }
}
