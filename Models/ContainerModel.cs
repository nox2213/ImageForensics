namespace ImageForensics.Models
{
    public class ContainerModel
    {
        public string Name { get; set; } = "Unknown";
        public string Description { get; set; } = "No description available.";
        public string ImagePath { get; set; } = "Assets/Images/container_fallback.png";
        public string ActionText { get; set; } = "Build Container";
    }
}
