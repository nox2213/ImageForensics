using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Input;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using ImageForensics.Models;

namespace ImageForensics
{
    public partial class MainWindow : Window
    {
        private Grid containerGrid;

        public MainWindow()
        {
            InitializeComponent();
            containerGrid = this.FindControl<Grid>("ContainerGrid")
                          ?? throw new NullReferenceException("ContainerGrid control not found in the layout.");

            InitializeContainers();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeContainers()
        {
            string dockerfilesPath = Path.Combine(Directory.GetCurrentDirectory(), "Dockerfiles");
            string containersPath = Path.Combine(Directory.GetCurrentDirectory(), "src", "Container");

            if (!Directory.Exists(dockerfilesPath))
            {
                Log.Warning("Dockerfiles directory not found.");
                return;
            }

            var dockerfileDirs = Directory.GetDirectories(dockerfilesPath);
            if (dockerfileDirs.Length == 0)
            {
                Log.Warning("No Dockerfiles found in the directory.");
                return;
            }

            foreach (var dockerfileDir in dockerfileDirs)
            {
                string containerName = Path.GetFileName(dockerfileDir);
                string containerPath = Path.Combine(containersPath, containerName);
                string iconPath = Path.Combine(dockerfileDir, "icon.png");
                string descriptionPath = Path.Combine(dockerfileDir, "description.json");

                string imagePath = File.Exists(iconPath) ? iconPath : "Assets/Images/container_fallback.png";
                string description = LoadDescription(descriptionPath);

                AddContainerElement(containerName, description, imagePath, Directory.Exists(containerPath) ? "Start Container" : "Build Container");
            }
        }

        private string LoadDescription(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    var descriptionData = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(path));
                    return descriptionData?.description ?? "No description available.";
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, $"Failed to parse description.json at {path}");
                }
            }
            return "No description available.";
        }

        private void AddContainerElement(string name, string description, string imagePath, string actionText)
        {
            var containerBorder = new Border
            {
                BorderBrush = Avalonia.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.DarkSlateGray),
                Margin = new Thickness(10),
                Padding = new Thickness(10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(30, GridUnitType.Pixel)); // Settings button size

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(10)
            };

            var image = new Image
            {
                Source = new Bitmap(imagePath),
                Width = 100,
                Height = 100,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var nameTextBlock = new TextBlock
            {
                Text = name,
                FontSize = 16,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 5)
            };

            var descriptionBorder = new Border
            {
                BorderBrush = Avalonia.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Black),
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 5, 0, 10)
            };

            var descriptionTextBlock = new TextBlock
            {
                Text = description,
                FontSize = 12,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Foreground = Avalonia.Media.Brushes.LightGray
            };

            descriptionBorder.Child = descriptionTextBlock;

            var actionButton = new Button
            {
                Content = actionText,
                Width = 150,
                Height = 40,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            actionButton.Click += (sender, e) => OpenContainerBuildWindow(name);

            var settingsButton = new Button
            {
                Width = 30,
                Height = 30,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Background = new Avalonia.Media.ImageBrush
                {
                    Source = new Bitmap("Assets/Icons/settings_icon.png")
                },
                BorderBrush = Avalonia.Media.Brushes.Black,
                BorderThickness = new Thickness(2)
            };

            stackPanel.Children.Add(image);
            stackPanel.Children.Add(nameTextBlock);
            stackPanel.Children.Add(descriptionBorder);
            stackPanel.Children.Add(actionButton);

            grid.Children.Add(stackPanel);
            grid.Children.Add(settingsButton);
            Grid.SetColumn(settingsButton, 1);

            containerBorder.Child = grid;
            containerGrid.Children.Add(containerBorder);
        }

    private void OpenContainerBuildWindow(string containerName)
    {
        string dockerfilePath = Path.Combine("Dockerfiles", containerName, "Dockerfile");
        string contextPath = Path.Combine("Dockerfiles", containerName);
        string outputPath = Path.Combine("src", "Container", containerName);

        // Validate paths
        if (!File.Exists(dockerfilePath))
        {
            Log.Error($"Dockerfile not found at {dockerfilePath}");
            return;
        }

        if (!Directory.Exists(contextPath))
        {
            Log.Error($"Build context directory not found at {contextPath}");
            return;
        }

        var buildWindow = new ContainerBuildWindow(containerName, dockerfilePath, contextPath, outputPath);
        buildWindow.Show();
        Close();
    }
    }
}
