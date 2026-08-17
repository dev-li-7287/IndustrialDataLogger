using Microsoft.Data.Sqlite;

namespace IndustrialDataLogger.Services
{
    public class DatabaseService
    {

        string connectionString = "Data Source=industrial.db";

        //初始化创建数据库
        public void InitializeDatabase()
        {
            using var connection = new SqliteConnection(connectionString);

            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS DeviceData(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DeviceId TEXT NOT NULL,
                    Temperature REAL NOT NULL,
                    Pressure REAL NOT NULL,
                    RecordTime TEXT NOT NULL)";

            command.ExecuteNonQuery();

        }

        //插入一条记录
        public void InsertRecord(DeviceData data) {
            try
            {
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO DeviceData (DeviceId,Temperature,Pressure,RecordTime)
                    VALUES (@deviceId, @temp, @pressure, @time)
";
                command.Parameters.AddWithValue("@deviceId", data.DeviceId);
                command.Parameters.AddWithValue("@temp", data.Temperature);
                command.Parameters.AddWithValue("@pressure", data.Pressure);
                command.Parameters.AddWithValue("@time", data.RecordTime.ToString("yyyy-MM-dd HH:mm:ss"));

                command.ExecuteNonQuery();

            }
            catch (Exception ex)
            {

                Console.WriteLine($"插入失败：{ex.Message}");
            }
        }

        //查询历史数据(按设备和时间范围)
        public List<DeviceData> QueryHistory(string deviceId, DateTime startTime, DateTime endTime)
        {
            var result = new List<DeviceData>();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, DeviceId, Temperature, Pressure, RecordTime
                FROM DeviceData
                WHERE DeviceId = @deviceId
                    AND RecordTime >= @startTime
                    AND RecordTime <= @endTime
                ORDER BY RecordTime";

            command.Parameters.AddWithValue("@deviceId", deviceId);
            command.Parameters.AddWithValue("@startTime", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@endTime", endTime.ToString("yyyy-MM-dd HH:mm:ss"));

            using var reader = command.ExecuteReader();
            while (reader.Read()) 
            {
                result.Add(new DeviceData
                {
                    Id = reader.GetInt32(0),
                    DeviceId = reader.GetString(1),
                    Temperature = reader.GetDouble(2),
                    Pressure = reader.GetDouble(3),
                    RecordTime = DateTime.Parse(reader.GetString(4))
                });
            }
            
            return result;

        }
    }
}
