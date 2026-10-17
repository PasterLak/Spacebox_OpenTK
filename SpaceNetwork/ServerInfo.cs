namespace SpaceNetwork
{
    public class ServerInfo
    {
        public string Name { get; set; }
        public string Description { get; set; } = "No description provided.";
        public string ModFolderName { get; set; }
        public string ModFolderHash { get; set; }
        public int MaxPlayers { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }


        public override string ToString()
        {
            return $"Name: {Name} MaxPlayers: {MaxPlayers} Description:{Description} ModFolderName: {ModFolderName} Hash: {ModFolderHash}";
        }
    }
}
