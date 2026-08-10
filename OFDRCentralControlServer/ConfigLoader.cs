using System.Text.Json;

namespace OFDRCentralControlServer
{
    public class ServerConfig
    {
        // real | mock
        public string RunMode { get; set; } = "real";

        public int ListenPort { get; set; } = 5600;

        public string FlaHost { get; set; } = "192.168.1.1";

        public int FlaPort { get; set; } = 4300;

        public string SwitchCom { get; set; } = "COM3";

        public int SwitchBaud { get; set; } = 115200;

        public int SwitchIndex { get; set; } = 1;

        public int SwitchInput { get; set; } = 1;

        /// <summary>
        /// 归零扫描最大长度（米），默认 100m
        /// </summary>
        public double ZeroEnd { get; set; } = 100.0;

        /// <summary>
        /// 测长扫描最大长度（米），默认 300m
        /// </summary>
        public double ScanEnd { get; set; } = 300.0;

        /// <summary>
        /// 归零选峰策略：last-peak | first-peak | nth-peak | strongest-db
        /// </summary>
        public string ZeroPeakSelectionStrategy { get; set; } = "last-peak";

        /// <summary>
        /// 归零在 nth-peak 策略下使用的峰序（1-based）
        /// </summary>
        public int ZeroPeakSelectionOrdinal { get; set; } = 3;

        /// <summary>
        /// 扫描选峰策略：last-peak | first-peak | nth-peak | strongest-db
        /// </summary>
        public string ScanPeakSelectionStrategy { get; set; } = "last-peak";

        /// <summary>
        /// 扫描在 nth-peak 策略下使用的峰序（1-based）
        /// </summary>
        public int ScanPeakSelectionOrdinal { get; set; } = 1;

        /// <summary>
        /// 预测式扫描补测次数，仅在 scan 选峰策略为 scored-candidate 时生效。
        /// </summary>
        public int ScanPredictiveRetestCount { get; set; } = 1;

        /// <summary>
        /// 与最近一次成功 scan 的长度差超过该值时，触发低置信度判断（米）。
        /// </summary>
        public double ScanPredictiveHistoryJumpThresholdM { get; set; } = 25.0;

        /// <summary>
        /// 两次预测扫描结果若落在该误差内，则认为补测已收敛（米）。
        /// </summary>
        public double ScanPredictiveConvergenceToleranceM { get; set; } = 2.0;
    }

    public static class ConfigLoader
    {
        private const string FileName = "ServerConfig.json";

        public static ServerConfig Load()
        {
            try
            {
                if (!File.Exists(FileName))
                {
                    var def = new ServerConfig();
                    Save(def);
                    return def;
                }
                var json = File.ReadAllText(FileName);
                var cfg = JsonSerializer.Deserialize<ServerConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return cfg ?? new ServerConfig();
            }
            catch
            {
                return new ServerConfig();
            }
        }

        public static void Save(ServerConfig cfg)
        {
            var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FileName, json);
        }
    }
}