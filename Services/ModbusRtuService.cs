using NModbus;
using NModbus.Serial;
using System.IO.Ports;
namespace IndustrialDataLogger.Services
{
    public class ModbusRtuService
    {
        public DeviceData? ReadDevice(string portName, int baudRate, byte slaveId,
                                     ushort temAddress, double temScale,
                                     ushort presAddress, double presScale)
        {
            try
            {
                
                using var port = new SerialPort(portName,baudRate );
                port.Open();

                // 2. ModbusFactory.CreateMaster
                var factory = new ModbusFactory();
                var master = factory.CreateRtuMaster(port);

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
