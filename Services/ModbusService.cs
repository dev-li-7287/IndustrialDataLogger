using NModbus;
using System.Net.Sockets;
namespace IndustrialDataLogger.Services
{
    public class ModbusService
    {
        public DeviceData? ReadDevice(string ip, int port, byte slaveId,
                                     ushort temAddress, double temScale,
                                     ushort presAddress, double presScale) 
        {
            try
            {
                // 1. TcpClient 连接
                using var client = new TcpClient(ip, port);

                // 2. ModbusFactory.CreateMaster
                var factory = new ModbusFactory();
                var master = factory.CreateMaster(client);

                // 3. 地址映射：手册地址 - 1 = 代码地址
                ushort temCodeAddress = (ushort)(temAddress - 1);
                ushort presCodeAddress = (ushort)(presAddress - 1);

                // 4. master.ReadHoldingRegisters(slaveId, codeAddress, 1)
                ushort[] temRegisters = master.ReadHoldingRegisters(slaveId, temCodeAddress, 1);
                ushort[] presRegisters = master.ReadHoldingRegisters(slaveId, presCodeAddress, 1);


                // 5. 原始值 × scale = 实际值
                double temperature = Math.Round(temRegisters[0] * temScale, 1);
                double pressure = Math.Round(presRegisters[0] * presScale, 1);

                //组装DeviceData
                return new DeviceData
                {
                    DeviceId = $"DEV-{slaveId}",
                    Temperature = temperature,
                    Pressure = pressure,
                    RecordTime = DateTime.Now
                };
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Modbus读取失败：{ex.Message}");
                return null;
            }
           
        }
    }
}
