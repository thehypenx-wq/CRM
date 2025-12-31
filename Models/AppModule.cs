namespace OfficeSuite.Models
{
    public class AppModule
    {
        public int Id { get; set; }
        public string ModuleName { get; set; }
        public string DisplayName { get; set; }
        public bool IsGranted { get; set; } // Helper for UI
    }
}
