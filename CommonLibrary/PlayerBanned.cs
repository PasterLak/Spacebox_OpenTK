namespace ServerCommon
{
    public class PlayerBanned
    {
        public string Name { get; set; }
        public string IDWhenWasBanned { get; set; }
        public string Reason { get; set; }
        public string IPAddress { get; set; }
        public string DeviceId { get; set; }
        public DateTime BannedAt { get; set; }
    }
}
