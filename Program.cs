using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Delivery
{
    public class Order
    {
        public string OrderId { get; set; }
        public double Weight { get; set; }
        public string District { get; set; }
        public DateTime DeliveryDateTime { get; set; }
    }

    public class Logger
    {
        private readonly string _logFilePath;

        public Logger(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        public void Log(string message)
        {
            File.AppendAllText(_logFilePath, $"{DateTime.Now}: {message}\n");
        }
    }

    public class DeliveryService
    {
        private readonly List<Order> _orders = new List<Order>();
        public List<Order> orders 
        {
            get
            {
                return this._orders;
            }
        }
        private readonly Logger _logger;

        
        public DeliveryService(string logFilePath)
        {
            _logger = new Logger(logFilePath);
        }

        public void LoadOrders(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length != 4)
                    {

                        _logger.Log($"Неверный формат строки: {line}");
                        continue;
                    }
                    if (!double.TryParse(parts[1].Trim(), out double weight))
                    {

                        _logger.Log($"Вес в неверном формате: '{parts[1]}' в строке: {line}");
                        continue;
                    }
                    if (!DateTime.TryParseExact(parts[3].Trim(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                    {

                        _logger.Log($"Дата и время в неверном формате.: '{parts[3]}' в строке: {line}");
                        continue;
                    }

                    var order = new Order
                    {
                        OrderId = parts[0].Trim(),
                        Weight = weight,
                        District = parts[2].Trim(),
                        DeliveryDateTime = dateTime
                    };
                    _orders.Add(order);
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Ошибка в загрузке: {ex.Message}");
            }
        }

        public List<Order> FilterOrders(string district, DateTime firstDeliveryDateTime)
        {
            var endTime = firstDeliveryDateTime.AddMinutes(30);
            var filteredOrders = _orders
                .Where(o => o.District.Equals(district, StringComparison.OrdinalIgnoreCase) &&
                             o.DeliveryDateTime >= firstDeliveryDateTime &&
                             o.DeliveryDateTime <= endTime)
                .ToList();

            return filteredOrders;
        }

        public void SaveResults(string outputFilePath, List<Order> results)
        {
            try
            {
                using (var writer = new StreamWriter(outputFilePath))
                {
                    foreach (var order in results)
                    {
                        writer.WriteLine($"{order.OrderId},{order.Weight},{order.District},{order.DeliveryDateTime:yyyy-MM-dd HH:mm:ss}");
                        _logger.Log($"Запись в output: {order.OrderId},{order.Weight},{order.District},{order.DeliveryDateTime:yyyy-MM-dd HH:mm:ss}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Ошибка в записи: {ex.Message}");
            }
            _logger.Log($"Фильтрация окончена");
        }
    }

    

    class Program
    {
        public static DateTime ChangeDateTime()
        {
            DateTime DT;
            try
            {
                Console.WriteLine("Введите дату и время (yyyy-MM-dd HH:mm:ss): ");
                DT = DateTime.ParseExact(Console.ReadLine(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            catch 
            {
                Console.WriteLine("Дата и время в неверном формате.");
                return ChangeDateTime();
            }
            return DT;
        }
        public static string ChangeDistrict()
        {
            Console.WriteLine("Введите район: ");
            string district = Console.ReadLine();
            string pattern = @"^district\d+$";
            if (Regex.IsMatch(district, pattern))
            {
                return district;
            }
            else
            {
                Console.WriteLine("Район в неверном формате.");
                return ChangeDistrict();
            }
            
        }
        static void Main(string[] args)
        {
            CultureInfo culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            //default
            var inputFilePath = "inputFile.txt";
            var district = "district1";
            var DeliveryDateTime = DateTime.Now;
            var logFilePath = "loggs.txt";
            var outputFilePath = "outputFile.txt";

            //
            Console.WriteLine("Хотите изменить район? default: district1 (y/n)");
            if (Console.ReadLine() == "y") 
            {
                district = ChangeDistrict();
            }
            Console.WriteLine("Хотите изменить дату и время? default: now (y/n)");
            if (Console.ReadLine() == "y")
            {
                DeliveryDateTime = ChangeDateTime();
            } 


            var deliveryService = new DeliveryService(logFilePath);
            deliveryService.LoadOrders(inputFilePath);
            var results = deliveryService.FilterOrders(district, DeliveryDateTime);
            deliveryService.SaveResults(outputFilePath, results);

            Console.WriteLine("Фильтрация окончена. Проверьте файл вывода.");
        }
    }
}