using STOTool.Enum;

namespace STOTool.Class
{
    public class MaintenanceInfo
    {
        public MaintenanceTimeType ShardStatus { get; set; }

        public int Days { get; set; }

        public int Hours { get; set; }

        public int Minutes { get; set; }

        public int Seconds { get; set; }

        public override string ToString()
        {
            string shardStatusString = ShardStatus.ToString();

            string timeString = $"{Days} days, {Hours} hours, {Minutes} minutes, {Seconds} seconds";

            return $"Shard Status: {shardStatusString}, Maintenance Time: {timeString}";
        }
    }
}
