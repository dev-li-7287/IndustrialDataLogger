using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using IndustrialDataLogger.Services;
using System.Diagnostics;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.Painting;

namespace IndustrialDataLogger {
    public class MainViewModel : INotifyPropertyChanged
    {

        private readonly ModbusService _modbusService;
        private readonly DatabaseService _databaseService;
        private DispatcherTimer _timer;

        //绑定属性
        
        //温度
        private double _currentTemperature;
        public double CurrentTemperature
        {
            get => _currentTemperature;
            set { _currentTemperature = value; OnPropertyChanged(); }
        }

        //压力
        private double _currentPressure;
        public double CurrentPressure
        {
            get => _currentPressure;
            set { _currentPressure = value; OnPropertyChanged(); }
        }

        //设备连接状态
        private string _deviceStatus = "未连接";
        public string DeviceStatus
        {
            get => _deviceStatus;
            set { _deviceStatus = value; OnPropertyChanged(); }
        }

        //设备id
        private string _queryDeviceId = "DEV-1";
        public string QueryDeviceId
        {
            get => _queryDeviceId;
            set { _queryDeviceId = value; OnPropertyChanged(); }
        }

        //开始时间
        private DateTime _queryStartDate = DateTime.Now.AddDays(-1);
        public DateTime QueryStartDate
        {
            get => _queryStartDate;
            set { _queryStartDate = value; OnPropertyChanged(); }
        }

        //结束时间
        private DateTime _queryEndDate = DateTime.Now;
        public DateTime QueryEndDate
        {
            get => _queryEndDate;
            set { _queryEndDate = value; OnPropertyChanged(); }
        }

        //历史数据列表
        public ObservableCollection<DeviceData> HistoryList { get; set; } = new();



        //命令
        public ICommand StartMonitoringCommand { get; }
        public ICommand StopMonitoringCommand { get; }
        public ICommand QueryHistoryCommand { get; }

        //温度数据序列
        public ISeries[] TemperatureSeries {  get; set; }

        //X轴 Y轴
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        //图标数据点集合
        private ObservableCollection<double> _temperatureValues = new ObservableCollection<double>();
        private ObservableCollection<double> _pressureValues = new ObservableCollection<double>();

        //构造函数
        public MainViewModel()
        {
            _modbusService = new ModbusService();
            _databaseService = new DatabaseService();
            _databaseService.InitializeDatabase();

            StartMonitoringCommand = new RelayCommand(StartMonitoring);
            StopMonitoringCommand = new RelayCommand(StopMonitoring);
            QueryHistoryCommand = new RelayCommand(QueryHistory);

            //定时器：每3秒采集一次
            _timer = new DispatcherTimer();
            _timer.Interval=TimeSpan.FromSeconds(3);
            _timer.Tick += Timer_Tick;

            //初始化序列：折线图
            TemperatureSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = _temperatureValues,
                    Name = "温度",
                    Stroke=new SolidColorPaint(SKColors.Red){StrokeThickness=2},
                    GeometrySize=6,
                    Fill = null,
                    ScalesYAt=0
                },
                new LineSeries<double>
                {
                    Values = _pressureValues,
                    Name = "压力",
                    Stroke=new SolidColorPaint(SKColors.Blue){StrokeThickness=2},
                    GeometrySize=6,
                    Fill = null,
                    ScalesYAt=1
                }
            };

            //X轴
            XAxes = new Axis[]
            {
                new Axis
                {
                    Name= "采样点",
                    MinStep = 1,
                    LabelsPaint=null

                }
            };

            //Y轴
            YAxes = new Axis[]
            {
                new Axis
                {
                    Name= "温度(℃)",
                    MinStep = 1,
                    MaxLimit = 100
                },
                new Axis
                {
                    Name= "压力(MPa)",
                    MinStep = 1,
                    MaxLimit = 2
                }
            };

        }

        //定时采集

        private async void Timer_Tick(object sender, EventArgs e)
        {
            //读Modbus
            var data = await Task.Run(() => _modbusService.ReadDevice(
                "127.0.0.1", 502, 1,
                temAddress: 1, temScale: 0.1,
                presAddress: 2, presScale: 0.1));

            if (data!=null)
            {
                //读取成功 -> 更新界面 + 存数据库
                CurrentTemperature = data.Temperature;
                CurrentPressure = data.Pressure;
                DeviceStatus = "在线";
                _databaseService.InsertRecord(data);

                // ====== 图表实时更新 ======
                
                _temperatureValues.Add(data.Temperature);
                _pressureValues.Add(data.Pressure);

                // 滑动窗口：超过60个点就删最旧的
                if (_temperatureValues.Count > 60)
                {
                    _temperatureValues.RemoveAt(0);
                }

                if (_pressureValues.Count > 60)
                {
                    _pressureValues.RemoveAt(0);
                }
            }
            else
            {
                //读取失败-> 显示离线
                DeviceStatus = "离线";
            }
        }


        /* 
         //RTU版
         private async void Timer_Tick(object sender, EventArgs e)
         {
             //读Modbus
             var data = await Task.Run(() => _modbusService.ReadDevice(
                 "COM3", 9600, 1,
                 temAddress: 1, temScale: 0.1,
                 presAddress: 2, presScale: 0.1));

             if (data != null)
             {
                 //读取成功 -> 更新界面 + 存数据库
                 CurrentTemperature = data.Temperature;
                 CurrentPressure = data.Pressure;
                 DeviceStatus = "在线";
                 _databaseService.InsertRecord(data);
             }
             else
             {
                 //读取失败-> 显示离线
                 DeviceStatus = "离线";
             }
         }
         */


        private void StartMonitoring()
        {

            _timer.Start();
            DeviceStatus = "采集中...";
        }

        private void StopMonitoring()
        {
            _timer.Stop();
            DeviceStatus = "已停止";
        }

        //历史查询
        private void QueryHistory()
        {
            Debug.WriteLine($"查询条件：设备={QueryDeviceId}, 起始={QueryStartDate}, 结束={QueryEndDate}");

            DateTime endTime = QueryEndDate.Date.AddDays(1).AddSeconds(-1);

            var results = _databaseService.QueryHistory(QueryDeviceId, QueryStartDate, endTime);

            Debug.WriteLine($"查到 {results.Count} 条记录");

            HistoryList.Clear();
            foreach (var item in results)
            {
                HistoryList.Add(item);
            }
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
